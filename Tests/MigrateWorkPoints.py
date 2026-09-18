"""One-time, fail-closed YAML migration. Preview by default; --apply writes assets.

Keeps existing object IDs, unrelated overrides, and the WorkPoint script GUID.
Run from the Unity project root. Snapshots/report are written under Logs.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path

BASE = '0b31801d45fcc514aa711763b19e2747'
WP = 8365889305115178693
OLD_WP = 3740296066875838150
GO = 2944756444144404595
LEGACY = {'wpType', '_ingredientMaker', '_conveyorBelt', '_boxStorage', '_boxPackaging', 'truck'}
FIELDS = {0: '_ingredientMaker', 1: '_conveyorBelt', 2: '_boxPackaging',
          3: '_boxStorage', 4: '_boxStorage', 5: 'truck', 6: '_boxPackaging'}
ENTRY = re.compile(r'    - target: (\{[^}]+\})\n      propertyPath: ([^\n]+)\n'
                   r'      value:([^\n]*)\n      objectReference: (\{[^}]+\})\n')
report = []
mappings = {}


def guid(path):
    return re.search(r'^guid: (\w+)', Path(str(path) + '.meta').read_text(), re.M)[1]


def ref_id(ref):
    return int(re.search(r'fileID: (-?\d+)', ref)[1])


def ref_guid(ref):
    match = re.search(r'guid: (\w+)', ref)
    return match[1] if match else None


def split(text):
    return re.split(r'(?=^--- !u!)', text, flags=re.M)


def fresh_id(path, instance, label, used):
    value = int(hashlib.sha256(f'{path}:{instance}:{label}'.encode()).hexdigest()[:15], 16)
    assert value not in used, ('ID collision', path, value)
    used.add(value)
    return value


def modification(target, prop, reference):
    return (f'    - target: {{fileID: {target}, guid: {BASE}, type: 3}}\n'
            f'      propertyPath: {prop}\n      value:\n      objectReference: {reference}\n')


def convert_direct(path, text):
    blocks = split(text)
    used = set(map(int, re.findall(r'^--- !u!\d+ &(-?\d+)', text, re.M)))
    extra = []
    for index, block in enumerate(blocks):
        if not block.startswith('--- !u!1001 '):
            continue
        source = re.search(r'  m_SourcePrefab: (\{[^}]+\})', block)
        if not source or ref_guid(source[1]) != BASE:
            continue
        instance = int(re.search(r'&(-?\d+)', block)[1])
        entries = [m for m in ENTRY.finditer(block)
                   if m[2] in LEGACY and ref_guid(m[1]) == BASE and ref_id(m[1]) in (WP, OLD_WP)]
        assert entries, ('Unconfigured direct WorkPoint', path, instance)
        # Prefer current component overrides. IngredientSpawn still has retired-ID overrides.
        settings = {}
        for target in (OLD_WP, WP):
            for entry in entries:
                if ref_id(entry[1]) == target:
                    settings[entry[2]] = (entry[3].strip(), entry[4])
        kind = int(settings.get('wpType', ('0', ''))[0])
        assert 0 <= kind <= 8, kind
        endpoint = settings[FIELDS[kind]][1] if kind in FIELDS else None
        assert endpoint is None or ref_id(endpoint) != 0, ('Missing endpoint', path, instance)
        action_class = ('PackagingInteraction' if kind == 6 else 'UpgradeInteraction' if kind == 7
                        else 'StoreInteraction' if kind == 8 else 'ItemTransfer')
        action_id = fresh_id(path, instance, 'action', used)
        game_id = None
        for candidate in blocks:
            if not candidate.startswith('--- !u!1 ') or ' stripped\n' not in candidate:
                continue
            if (f'm_PrefabInstance: {{fileID: {instance}}}' in candidate
                    and f'm_CorrespondingSourceObject: {{fileID: {GO}, guid: {BASE}' in candidate):
                game_id = int(re.search(r'&(-?\d+)', candidate)[1])
                break
        if game_id is None:
            game_id = fresh_id(path, instance, 'gameObject', used)
            extra.append(f'--- !u!1 &{game_id} stripped\nGameObject:\n'
                         f'  m_CorrespondingSourceObject: {{fileID: {GO}, guid: {BASE}, type: 3}}\n'
                         f'  m_PrefabInstance: {{fileID: {instance}}}\n  m_PrefabAsset: {{fileID: 0}}\n')
        block = ENTRY.sub(lambda m: '' if m[2] in LEGACY and ref_guid(m[1]) == BASE
                          and ref_id(m[1]) in (WP, OLD_WP) else m[0], block)
        marker = '    m_RemovedComponents:'
        assert marker in block and 'm_AddedComponents:' not in block
        block = block.replace(marker, modification(WP, 'action', f'{{fileID: {action_id}}}') + marker, 1)
        block = block.replace('  m_SourcePrefab:',
                              '    m_AddedComponents:\n'
                              f'    - targetCorrespondingSourceObject: {{fileID: {GO}, guid: {BASE}, type: 3}}\n'
                              f'      insertIndex: -1\n      addedObject: {{fileID: {action_id}}}\n'
                              '  m_SourcePrefab:', 1)
        fields = ''
        if action_class == 'ItemTransfer':
            fields = f'  endpoint: {endpoint}\n  playerOnly: {int(kind in (4, 5))}\n'
        elif action_class == 'PackagingInteraction':
            fields = f'  packaging: {endpoint}\n'
        script = guid(Path('Assets/1. Scripts/Work') / f'{action_class}.cs')
        extra.append(f'--- !u!114 &{action_id}\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n'
                     '  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n'
                     f'  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {game_id}}}\n'
                     '  m_Enabled: 1\n  m_EditorHideFlags: 0\n'
                     f'  m_Script: {{fileID: 11500000, guid: {script}, type: 3}}\n'
                     '  m_Name:\n  m_EditorClassIdentifier:\n' + fields)
        blocks[index] = block
        info = dict(path=str(path), instance=instance, kind=kind, action=action_class,
                    action_id=action_id, endpoint=endpoint, game_id=game_id)
        report.append(info)
        if path.suffix == '.prefab':
            for old in (WP, OLD_WP):
                mappings[(guid(path), old ^ instance)] = info
    return ''.join(blocks) + ''.join(extra)


def convert_inherited(text):
    def change(match):
        if match[2] not in LEGACY:
            return match[0]
        key = (ref_guid(match[1]), ref_id(match[1]))
        assert key in mappings, ('Unknown inherited WorkPoint override', match[0])
        info = mappings[key]
        if match[2] == 'wpType':
            assert int(match[3]) == info['kind'], ('Scene changes action kind', match[0])
            return ''
        if match[2] != FIELDS[info['kind']]:
            return ''
        prop = 'packaging' if info['kind'] == 6 else 'endpoint'
        target = re.sub(r'fileID: -?\d+', f'fileID: {info["action_id"]}', match[1], count=1)
        return (f'    - target: {target}\n      propertyPath: {prop}\n'
                f'      value: \n      objectReference: {match[4]}\n')
    return ENTRY.sub(change, text)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    base = Path('Assets/3. Prefab/Work Point.prefab')
    prefabs = [Path('Assets/3. Prefab/Factory Machinery') / name for name in
               ('IngredientSpawn.prefab', 'ChuruConveyerBelt Obj.prefab', 'Box Packaging.prefab')]
    scene = Path('Assets/2. Scene/Game.unity')
    paths = [base] + prefabs + [scene]
    original = {p: p.read_bytes() for p in paths}
    source = {p: b.decode('utf-8-sig').replace('\r\n', '\n') for p, b in original.items()}
    assert '  wpType:' in source[base], 'Migration already applied or source differs.'
    output = {}
    for path in prefabs:
        output[path] = convert_direct(path, source[path])
    output[scene] = convert_inherited(convert_direct(scene, source[scene]))
    output[base] = re.sub(r'^  (?:_ingredientMaker|_conveyorBelt|_boxStorage|_boxPackaging|truck|wpType):[^\n]*\n',
                          '', source[base], flags=re.M).replace(
                              '  m_Script: {fileID: 11500000, guid: 717316efe29766b48a0c80ffd07ba194, type: 3}\n',
                              '  m_Script: {fileID: 11500000, guid: 717316efe29766b48a0c80ffd07ba194, type: 3}\n  action: {fileID: 0}\n')
    for path, text in output.items():
        assert not any(m[2] in LEGACY for m in ENTRY.finditer(text)), ('Legacy override left', path)
        ids = re.findall(r'^--- !u!\d+ &(-?\d+)', text, re.M)
        assert len(ids) == len(set(ids)), ('Duplicate object ID', path)
        # Retain every non-workpoint property override byte-for-byte.
        before = [m[0] for m in ENTRY.finditer(source[path]) if m[2] not in LEGACY]
        after = [m[0] for m in ENTRY.finditer(text) if m[2] not in ('action', 'endpoint', 'packaging')]
        assert before == after, ('Unrelated overrides changed', path)
    print(json.dumps(report, indent=2))
    print(f'Validated {len(report)} action components across {len(paths)} assets.')
    if args.apply:
        artifacts = Path('Logs/WorkPointMigration')
        artifacts.mkdir(parents=True, exist_ok=True)
        for path in paths:
            assert path.read_bytes() == original[path], ('Concurrent edit', path)
        for path in paths:
            snapshot = artifacts / path
            snapshot.parent.mkdir(parents=True, exist_ok=True)
            snapshot.write_bytes(original[path])
            newline = '\r\n' if b'\r\n' in original[path] else '\n'
            path.write_bytes(output[path].replace('\n', newline).encode('utf-8'))
        (artifacts / 'report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()

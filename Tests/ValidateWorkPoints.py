"""Validate migrated action/endpoint links without starting Unity.

Run from the project root. Checks authored components and effective Game scene
overrides, including player-only permissions. Unity import/play tests remain required.
"""
from collections import Counter
from pathlib import Path
import re

from MigrateWorkPoints import BASE, WP, GO, LEGACY, ENTRY, guid, ref_id, ref_guid, split

ROOT = Path('Assets/1. Scripts/Work')
ACTION_NAMES = ('ItemTransfer', 'PackagingInteraction', 'StoreInteraction', 'UpgradeInteraction')
SCRIPTS = {guid(ROOT / f'{name}.cs'): name for name in ACTION_NAMES + (
    'IngredientMaker', 'ConveyorBelt', 'BoxStorage', 'BoxPackaging', 'Truck')}
ASSETS = [Path('Assets/3. Prefab/Factory Machinery') / name for name in
          ('IngredientSpawn.prefab', 'ChuruConveyerBelt Obj.prefab', 'Box Packaging.prefab')]
SCENE = Path('Assets/2. Scene/Game.unity')
DOCUMENTS = {}


def reference(block, field):
    match = re.search(r'^  ' + field + r': (\{[^}]+\})', block, re.M)
    assert match, ('Missing field', field, block[:80])
    return match[1]


def load(path):
    text = path.read_text(encoding='utf-8-sig')
    records = [b for b in split(text) if b.startswith('--- !u!')]
    blocks = {int(re.search(r'&(-?\d+)', b)[1]): b for b in records}
    assert len(records) == len(blocks), ('Duplicate ID', path)
    assert not any(m[2] in LEGACY for m in ENTRY.finditer(text)), ('Legacy override', path)
    return blocks


def script_name(block):
    return SCRIPTS.get(ref_guid(reference(block, 'm_Script')))


def check_endpoint(path, blocks, action, endpoint):
    if action['name'] not in ('ItemTransfer', 'PackagingInteraction'):
        return action['name']
    assert ref_id(endpoint) in blocks, ('Unresolved endpoint', path, endpoint)
    station = script_name(blocks[ref_id(endpoint)])
    if action['name'] == 'PackagingInteraction':
        assert station == 'BoxPackaging', ('Wrong packaging target', path, station)
        return 'Packaging'
    assert station in ('IngredientMaker', 'ConveyorBelt', 'BoxStorage', 'BoxPackaging', 'Truck'), station
    if station == 'BoxStorage':
        storage = blocks[ref_id(endpoint)]
        if not re.search(r'^  bsType:', storage, re.M):
            original = reference(storage, 'm_CorrespondingSourceObject')
            storage = DOCUMENTS[ref_guid(original)][ref_id(original)]
        storage_type = int(re.search(r'^  bsType: (\d+)', storage, re.M)[1])
        assert storage_type in (0, 1), ('Unexpected storage source', path, storage_type)
        assert action['player_only'] == (storage_type == 1), ('Storage permission changed', path)
        return 'BoxPickup' if action['player_only'] else 'ChuruPickup'
    assert action['player_only'] == (station == 'Truck'), ('Changed actor permissions', path, station)
    return station


def authored(path, blocks):
    actions = {}
    for instance_id, block in blocks.items():
        if not block.startswith('--- !u!1001 ') or ref_guid(reference(block, 'm_SourcePrefab')) != BASE:
            continue
        links = [m for m in ENTRY.finditer(block) if m[2] == 'action' and ref_id(m[1]) == WP]
        assert len(links) == 1, ('Expected one WorkPoint action', path, instance_id)
        action_id = ref_id(links[0][4])
        action_block = blocks[action_id]
        action_name = script_name(action_block)
        assert action_name in ACTION_NAMES, ('Unknown action', path, action_id)
        attached = re.search(r'm_AddedComponents:\n    - targetCorrespondingSourceObject: (\{[^}]+\})\n'
                             r'      insertIndex: -1\n      addedObject: (\{[^}]+\})', block)
        assert attached and ref_id(attached[1]) == GO and ref_id(attached[2]) == action_id
        game_block = blocks[ref_id(reference(action_block, 'm_GameObject'))]
        assert ref_id(reference(game_block, 'm_PrefabInstance')) == instance_id
        assert ref_id(reference(game_block, 'm_CorrespondingSourceObject')) == GO
        info = dict(name=action_name, player_only=False, endpoint=None)
        if action_name == 'ItemTransfer':
            info['player_only'] = bool(int(re.search(r'^  playerOnly: (\d+)', action_block, re.M)[1]))
            info['endpoint'] = reference(action_block, 'endpoint')
        elif action_name == 'PackagingInteraction':
            info['endpoint'] = reference(action_block, 'packaging')
        info['kind'] = check_endpoint(path, blocks, info, info['endpoint'])
        actions[action_id] = info
    return actions


def main():
    templates = {}
    for path in ASSETS:
        blocks = load(path)
        DOCUMENTS[guid(path)] = blocks
        templates[guid(path)] = (path, blocks, authored(path, blocks))
    scene_blocks = load(SCENE)
    direct = authored(SCENE, scene_blocks)
    effective = Counter(info['kind'] for info in direct.values())
    for block in scene_blocks.values():
        if not block.startswith('--- !u!1001 '):
            continue
        source_guid = ref_guid(reference(block, 'm_SourcePrefab'))
        if source_guid not in templates:
            continue
        path, template_blocks, actions = templates[source_guid]
        overrides = {(ref_id(m[1]), m[2]): m[4] for m in ENTRY.finditer(block)
                     if m[2] in ('endpoint', 'packaging')}
        assert all(key[0] in actions for key in overrides), ('Dangling action override', overrides)
        for action_id, info in actions.items():
            prop = 'packaging' if info['name'] == 'PackagingInteraction' else 'endpoint'
            if (action_id, prop) in overrides:
                kind = check_endpoint(SCENE, scene_blocks, info, overrides[(action_id, prop)])
                assert kind == info['kind'], ('Scene endpoint changes station role', kind, info)
            else:
                kind = info['kind']
            effective[kind] += 1
    expected = Counter(IngredientMaker=3, ConveyorBelt=3, ChuruPickup=3, BoxPickup=1,
                       BoxPackaging=1, Packaging=1, Truck=1, UpgradeInteraction=1, StoreInteraction=1)
    assert effective == expected, ('Game scene action coverage changed', effective, expected)
    # No other scene/prefab may retain old WorkPoint properties.
    for path in Path('Assets').rglob('*'):
        if path.suffix in ('.unity', '.prefab'):
            text = path.read_text(encoding='utf-8-sig')
            assert not re.search(r'propertyPath: wpType\b|^  wpType:', text, re.M), path
    print('PASS: 9 authored action components; 15 effective Game scene WorkPoints.')
    print('PASS: action ownership, local endpoint links, inherited overrides, player-only permissions.')
    print(dict(effective))


if __name__ == '__main__':
    main()

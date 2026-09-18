# Unity 테스트 자동 실행

프로젝트 루트에서 실행합니다. 프로젝트를 열고 있는 Unity Editor는 먼저 닫아 주세요.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\RunUnityTests.ps1
```

기존 Edit Mode 테스트를 Unity Test Runner로 실행합니다. 기본 빌드 대상은 Google Play Games의 Android 조건부 코드를 포함하도록 `Android`입니다. Unity CLI나 AI 패키지를 추가할 필요가 없습니다. `ProjectSettings/ProjectVersion.txt`와 동일한 Editor 및 사용 가능한 Unity 라이선스가 필요합니다. 최초 실행에는 패키지 다운로드와 임포트가 필요할 수 있습니다.

기본 설치 경로는 `%ProgramFiles%\Unity\Hub\Editor\<버전>\Editor\Unity.exe`입니다. 다른 경로는 `-UnityEditor 'D:\Unity\<버전>\Editor\Unity.exe'` 또는 `UNITY_EDITOR_PATH` 환경 변수로 지정합니다. CLI의 `unity.exe`가 아니라 Editor 실행 파일을 지정해야 합니다.

```powershell
# 특정 테스트 클래스만 실행
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\RunUnityTests.ps1 -Filter WorkSchedulerTests

# Windows 전용 코드를 점검할 때 빌드 대상 변경
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\RunUnityTests.ps1 -BuildTarget StandaloneWindows64

# Play Mode 테스트를 추가한 뒤 실행
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\RunUnityTests.ps1 -Mode PlayMode
```

각 실행 결과는 Git에서 제외되는 `Logs/Tests/<실행 ID>/<모드>/`에 저장됩니다.

- `results.xml`: NUnit 테스트 결과와 실패 상세
- `editor.log`: 컴파일, 라이선스, 패키지 문제를 포함한 Unity 로그
- `summary.json`: 보고서가 생성된 실행의 집계 결과

성공 시 종료 코드 0, 테스트 실패·0개 발견·보고서 누락·Editor 오류·시간 초과 시 1을 반환합니다. 기본 제한 시간은 20분이며 `-TimeoutMinutes`로 변경합니다. 이전 결과는 덮어쓰지 않습니다. 실행 중인 프로젝트의 잠금 파일은 삭제하지 않습니다.

CI에서도 같은 명령의 종료 코드로 통과 여부를 판정하고, 실패 시에도 위 결과 폴더를 아티팩트로 수집하면 됩니다. CI 머신에는 동일한 Editor와 라이선스, 의존 패키지 접근 환경이 필요합니다. 원격 CI 연결은 이 단계에 포함하지 않았습니다.

실행 계정이 바뀌면 라이선스 접근 여부도 달라질 수 있습니다. 이번 검증에서는 Codex 샌드박스 계정이 라이선스를 찾지 못해 Editor가 198로 종료됐고, 사용자 계정 실행에서 정상 통과했습니다. 같은 증상은 `editor.log`의 `No valid Unity Editor license found`로 확인합니다.

2026-09-14 검증: Unity 6000.3.23f1에서 Edit Mode 21개 통과, 실패 0개, Editor와 실행 스크립트의 종료 코드 0을 확인했습니다. 한글 프로젝트명이 포함된 UTF-8 NUnit XML도 정상 처리했습니다. 테스트 실패·0개 발견·시간 초과 분기는 구현했으며, 실제 실행으로 확인한 오류 경로는 라이선스 문제로 인한 보고서 누락입니다.

현재 범위는 작업 예약·게임 데이터·업그레이드의 회귀 검사입니다. 실제 플레이, 모바일 기기, 광고·결제·서버 연동 검증은 별도 시나리오가 필요합니다. `ValidateBackend.ps1`은 기존 별도 검증 도구로 유지합니다.

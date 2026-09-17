# GitHub Actions Unity 테스트

`.github/workflows/unity-tests.yml`은 push, 같은 저장소에서 만든 PR, 수동 실행에 반응합니다. 이 PC에 등록된 Windows self-hosted runner가 Unity Hub에 로그인된 현재 사용자 라이선스로 Android 빌드 대상의 Edit Mode 테스트를 실행하므로 `.ulf`, Unity 이메일, 비밀번호 Secret이 필요하지 않습니다.

## 실행 조건

- 이 PC가 켜져 있고 `C:\Workspace\actions-runner\run.cmd`가 실행 중이어야 합니다.
- Unity Hub에서 Personal 라이선스가 활성화되어 있어야 합니다.
- 프로젝트와 같은 Unity 6000.3.23f1이 설치되어 있어야 합니다.
- 테스트 실행 중 같은 작업 폴더를 Unity Editor로 열면 안 됩니다.
- Google Play Games 코드가 `UNITY_ANDROID` 조건으로 컴파일되므로 테스트도 `-BuildTarget Android`로 실행합니다.

워크플로는 `self-hosted`, `Windows`, `X64` 레이블을 사용합니다. GitHub가 매 실행마다 별도의 작업 폴더에 저장소를 체크아웃하므로 평소 개발 중인 `C:\Workspace\2.5D-Mobile`의 미커밋 파일을 읽거나 변경하지 않습니다.

## 결과

- `Unity EditMode` 상태에서 성공·실패 확인
- `unity-editmode-results` 아티팩트에서 NUnit XML, 요약 JSON, Editor 로그 확인
- 테스트 실패, 테스트 0개, 보고서 누락, Unity 오류, 제한 시간 초과를 실패 처리
- 실행 결과는 7일 보관

저장소가 공개되어 있으므로 외부 fork PR은 self-hosted runner에서 실행하지 않습니다. 같은 저장소 안의 브랜치에서 만든 PR과 push만 실행합니다. 저장소 쓰기 권한이 있는 사람의 코드는 이 PC에서 실행될 수 있으므로 협업자를 추가할 때 권한을 신중하게 관리해야 합니다.

Runner를 종료하면 새 작업은 GitHub에서 대기합니다. 다시 시작하려면 이 PC에서 `C:\Workspace\actions-runner\run.cmd`를 실행합니다.

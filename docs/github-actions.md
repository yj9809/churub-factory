# GitHub Actions Unity 테스트

`.github/workflows/unity-tests.yml`은 모든 브랜치 push, PR 생성·코드 갱신·재오픈, 수동 실행에 반응합니다. GitHub 제공 Ubuntu 서버에서 GameCI로 프로젝트 버전에 맞는 Unity를 실행합니다. PC를 켜둘 필요가 없습니다. 로컬 Windows용 `Tests/RunUnityTests.ps1`과 별도로 Unity Test Runner의 같은 Edit Mode 테스트를 실행합니다.

## 최초 설정

저장소 [Actions Secrets 설정](https://github.com/yj9809/churub-factory/settings/secrets/actions)에 다음 값을 직접 등록합니다. 값은 코드나 채팅에 넣지 않습니다.

- `UNITY_EMAIL`: Unity 계정 이메일
- `UNITY_PASSWORD`: Unity 계정 비밀번호
- Personal: `UNITY_LICENSE`에 유효한 `.ulf` 라이선스 내용
- Pro 등 시리얼 방식: `UNITY_SERIAL`에 시리얼. 해당 방식에서는 `UNITY_LICENSE`를 등록하지 않습니다.

[GameCI 활성화 안내](https://game.ci/docs/github/activation/)에 따라 라이선스를 준비합니다. 로컬 Hub 로그인만으로 GitHub 서버가 인증되는 것은 아닙니다. Personal `.ulf` 발급 여부와 CI 활성화 성공은 계정 및 라이선스 환경에서 확인해야 합니다. 라이선스 파일이나 계정 값을 자동 수집·업로드하지 않습니다.

워크플로가 원격 브랜치에 올라가고 Secret 설정이 완료되면 push 또는 PR 업데이트로 실행됩니다. 수동 Run workflow 버튼은 기본 브랜치에 워크플로가 존재해야 나타납니다. 실패한 실행은 Secrets 등록 후 Re-run all jobs로 다시 실행할 수 있습니다.

## 결과와 범위

- PR의 `Unity EditMode` 상태에서 성공·실패 확인
- Actions 실행 Summary에서 NUnit 집계 확인
- `unity-editmode-results` 아티팩트에서 결과 XML·로그 다운로드(7일 보관)
- 테스트 실패, 테스트 0개, 결과 누락, 인증 오류는 실패 처리
- 같은 이벤트/브랜치의 이전 실행은 새 변경이 오면 취소
- PR이 열린 브랜치를 push하면 push 검사와 PR 병합 결과 검사가 각각 실행될 수 있음

GitHub-hosted Linux에서 첫 Unity 임포트와 패키지 복원이 필요합니다. 현재는 캐시 없이 시작하며 실제 실행 시간을 확인한 뒤 추가합니다. 로컬 Windows에서 21개 통과한 결과가 Linux CI 통과를 보장하지는 않습니다. 원격에 커밋된 테스트와 코드만 검사하므로 로컬 미커밋 변경은 포함되지 않습니다.

공개 저장소의 fork PR 및 Dependabot PR에는 Actions Secrets가 제공되지 않아 Unity EditMode job을 경고와 함께 건너뜁니다. 이때 Unity가 통과한 것으로 표시하지 않습니다. `pull_request_target`으로 외부 코드를 인증 정보와 함께 실행하거나 개인 PC self-hosted runner로 우회하지 않습니다.

브랜치 보호는 자동 변경하지 않습니다. 첫 CI 성공 후 GitHub Settings의 Rulesets에서 `Unity EditMode`를 필수 상태 검사로 지정하면 실패한 PR의 병합을 차단할 수 있습니다.

## 현재 도입 상태

워크플로 작성 단계입니다. GitHub 서버의 Unity 활성화와 테스트 통과는 최초 실행으로 확인해야 합니다. 기존 게임플레이·외부 서비스 테스트 범위는 이번 설정으로 늘어나지 않습니다.

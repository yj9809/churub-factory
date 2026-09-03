# Unity 6 엔진 마이그레이션 보고서

## 진행 상태

- 기준일: 2026-09-03 (Asia/Seoul)
- 현재 단계: 2단계 패키지 호환성 조사 및 Unity 2022.3 설치 준비
- 판정: **0단계 완료, 1단계 자동화 기준선 완료, 실제 기기 검증 대기**
- 다음 단계 진입: 공식 호환성이 확인된 변경만 순차 적용한다. 실제 기기가 필요한 플레이·성능·외부 서비스 검증은 미실행 상태로 유지한다.

이 문서는 `v1.1.0`의 현재 상태를 기록한다. 엔진, 패키지, 에셋, 씬, 프리팹, 직렬화 데이터 및 Android 설정은 아직 변경하지 않았다.

## Git 기준점

| 항목 | 값 |
| --- | --- |
| 원격 저장소 | `https://github.com/yj9809/churub-factory.git` |
| 시작 브랜치 | `main` |
| 작업 브랜치 | `upgrade/unity-engine` |
| HEAD | `e1ecee13ba4985a94721f9ba696e2562d4b4c919` |
| 기준 태그 | `v1.1.0` (HEAD와 일치) |
| 원격 기준 | `origin/main`이 HEAD와 일치 |
| 시작 시 작업 트리 | clean |

`v1.1.0` 태그는 이동하거나 다시 생성하지 않았다. `main`에는 직접 변경하지 않았다.

`.gitignore`는 `Library`, `Temp`, `Obj`, `Build`, `Builds`, `Logs`, `UserSettings`, APK/AAB, `Key`, `*.keystore`, `*.jks`를 제외한다. 점검 시 이 경로에 추적 중인 파일은 없었다.

## Unity 설치 및 프로젝트 버전

| 항목 | 값 |
| --- | --- |
| 프로젝트 Editor | `2021.3.32f1` |
| Editor revision | `3b9dae9532f5` |
| 설치 확인 | `C:\Program Files\Unity\Hub\Editor\2021.3.32f1` |
| Android Build Support | 설치됨 |
| Android SDK platforms | API 29, 30, 34 |
| Android Build Tools | 30.0.2 |
| Android NDK | 21.3.6528147 |
| OpenJDK | Java 8 |
| 추가 설치 Editor | `6000.0.24f1`, `6000.5.4f1` |
| 필요한 중간 Editor | `2022.3.76f1` (`d938583f0741`) 설치 완료 |
| 목표 Editor | Unity 6.3 (`6000.3.x`) 미설치 |

프로젝트의 정확한 버전 근거는 `ProjectSettings/ProjectVersion.txt`다. 설치된 `6000.5.4f1`은 요청한 Unity 6.3 계열이 아니므로 목표 Editor로 사용하지 않는다.

Unity Hub 공식 headless 설치 흐름으로 2022.3.76f1 및 Android 모듈을 설치했다. 설치 후 실제 도구 버전은 OpenJDK 11.0.14.1, Android NDK 23.1.7779620, Build Tools 34.0.0이며 SDK Platform 34·35·36이 포함되어 있다.

## Unity 패키지 기준선

직접 의존성은 `Packages/manifest.json`, 실제 해석 결과는 `Packages/packages-lock.json`을 기준으로 한다.

| 패키지 | 현재 버전 |
| --- | --- |
| 2D Tilemap | 1.0.0 (built-in) |
| 2D Tilemap Extras | 2.2.7 |
| Unity Ads | 4.12.0 |
| Version Control | 2.2.0 |
| Mobile feature set | 1.0.0 |
| Rider Editor | 3.0.25 |
| Visual Studio Editor | 2.0.22 |
| VS Code Editor | 1.2.5 |
| Test Framework | 1.1.33 |
| TextMeshPro | 3.0.9 |
| Timeline | 1.6.5 |
| uGUI | 1.0.0 (built-in) |
| Visual Scripting | 1.9.4 |

주요 잠금 의존성은 Adaptive Performance 4.0.1, Android Logcat 1.3.2, Mobile Notifications 2.3.0, Profiling Core 1.0.2 및 NUnit extension 1.0.6이다. 전체 내역은 lock 파일을 변경 없이 보존한다.

## 외부 SDK 및 플러그인 기준선

| SDK/플러그인 | 현재 버전 | 확인 근거 |
| --- | --- | --- |
| External Dependency Manager for Unity | 1.2.181 | 설치 manifest 및 DLL meta |
| Google Play Games plugin | 0.11.01 | package.json, `PluginVersion.cs` |
| Google Play In-App Update | 1.8.2 | package.json |
| Google Play Core | 1.8.4 | package.json |
| Google Play Common | 1.9.1 | package.json |
| Google Android App Bundle | 1.9.0 | package.json |
| BackEnd SDK | 5.14.1 | `Backend.dll` file/product version |
| DOTween | 1.2.765 | `DG.Tweening.DOTween.Version` 필드 |
| Odin Inspector | 3.3.1.7 | Editor assembly 내 버전 문자열 및 scripting define |

Odin과 DOTween의 일반 assembly file version은 각각 1.0.0.0으로 고정되어 있어 제품 버전 근거로 사용하지 않았다. Unity 2022.3, Unity 6.3 및 Android API 36 호환성은 2단계에서 공식 출처로 별도 조사한다. 현재는 추측해 업데이트 버전을 선택하지 않았다.

## 패키지 및 SDK 호환성 조사

조사 기준일은 2026-09-03이다. `지원`은 공급자가 해당 Unity 계열을 명시했거나 그 계열용 패키지를 제공한다는 뜻이며, Android API 36 열은 공급자의 명시 또는 실제 API 36 AAB 검증 여부를 별도로 표시한다. 명시 자료를 찾지 못한 항목은 추측하지 않고 `미확인`으로 둔다.

| SDK/패키지 | 현재 | Unity 2022.3 후보 | Unity 6.3 후보 | API 36 | API/데이터 영향 | 적용 시점 / 롤백 |
| --- | --- | --- | --- | --- | --- | --- |
| External Dependency Manager | 1.2.181 | 1.2.187 | 1.2.187 | 명시 없음; Resolver 검증 필요 | Android Resolver 산출 Gradle/Maven 항목 재생성. 게임 데이터·프리팹 마이그레이션 없음 | 다른 Google SDK보다 먼저. 플러그인 디렉터리와 Gradle 변경 커밋 되돌림 |
| Google Play Games | 0.11.01 | 2.1.0 | 2.1.0 | 명시 없음; API 36 빌드/기기 검증 필요 | v1 API 폐기 경로, Google Sign-In/Drive 의존성 제거, server-side access scope API 변경. 로그인 호출 코드 수정 가능 | EDM4U 다음, 단독 커밋. 기존 0.11.01 플러그인과 코드 커밋 복원 |
| Google Play In-App Update | 1.8.2 | 1.8.5 | 1.8.5 | 명시 없음; 새 저장소는 targetSdk 34+ 대응 계열 | 최소 Android API 21. 직접 API 변경 자료 없음. Android 의존성 재 resolve | Google Play 묶음에서 개별 적용. 1.8.2 디렉터리/manifest 복원 |
| Google Play Core | 1.8.4 | 1.8.6 | 1.8.6 | 명시 없음 | 공통 Android 의존성 변경, 데이터 영향 없음 | In-App Update 의존성으로 함께 검증. 1.8.4 복원 |
| Google Play Common | 1.9.1 | 1.9.2 | 1.9.2 | 명시 없음 | 공통 타입/Android 의존성, 데이터 영향 없음 | Google Play 플러그인과 함께. 1.9.1 복원 |
| Google Android App Bundle | 1.9.0 | 1.10.0 | 1.10.0 | bundletool 1.18.1 포함; API 36 AAB 실제 검증 필요 | 빌드 도구만 변경, 게임 데이터·프리팹 영향 없음 | Play 플러그인 뒤. 1.9.0 복원 |
| Unity Ads | 4.12.0 | 4.17.0 | 4.17.0 | Android SDK 요구 compileSdk 33+, API 36 명시 없음 | Legacy Advertisement API 유지. 2026-04-01 이후 직접 연동은 성능상 LevelPlay 권고지만 이번 엔진 전환에서는 제품 변경을 분리 | Google Play 뒤 단독 적용. manifest/package lock 복원 |
| BackEnd Base SDK | 5.14.1 | 5.18.10 후보 | **미확인** | **미확인** | 공식 커뮤니티에 Unity 6000.3.15f1에서 5.18.10 초기화/로그인 사례가 있으나 정식 Unity 6.3/API 36 지원표는 찾지 못함. 직렬화/서버 스키마는 변경하지 않음 | 공급사 확인 전 버전 확정 금지. 현재 DLL/메타 복원 |
| DOTween | 1.2.765 | 1.3.030 | 1.3.030 | Android API 비의존 | 현재 버전이 1.2.815 미만이므로 공식 업그레이드 절차 필요. 설정 파일 재생성 및 컴포넌트/프리팹 참조 검증 | BackEnd 뒤 단독 적용. 기존 DOTween 디렉터리/설정 복원 |
| Odin Inspector | 3.3.1.7 | 현 버전 유지 가능성 검증 | 최소 3.3.1.14 | Android API 비의존 | 3.3.1.14가 Unity 6000.3 호환 수정 포함. 4.x는 serializer 변경 위험 때문에 이번 범위에서 선택하지 않음 | 라이선스 보유 패키지 확보 후 적용. 3.3.1.7 DLL/메타 복원 |
| 2D Tilemap Extras | 2.2.7 | 3.1.1 | 5.x | Android API 비의존 | 3.x/4.x에서 Editor namespace 및 일부 직렬화 동작 변경. Tile/RuleTile 에셋 GUID·참조와 씬/프리팹 diff 필수 확인 | Unity 공식 패키지 단계. manifest/lock 복원 |
| Visual Scripting | 1.9.4 | 1.8.0이 2022.3 검증 버전이나 **다운그레이드하지 않고 현 버전 컴파일 검증** | 1.9.12 | Android API 비의존 | 업데이트 전 graph/settings 백업 권고. graph/unit 재생성 여부와 scene/prefab diff 확인 | Unity 공식 패키지 단계. manifest/lock 및 백업 graph 복원 |

### 조사 근거

- [EDM4U 1.2.187 릴리스와 지원 범위](https://github.com/googlesamples/unity-jar-resolver/releases/tag/v1.2.187)
- [Google Play Games plugin 2.1.0 변경 사항](https://github.com/playgameservices/play-games-plugin-for-unity/releases/tag/v2.1.0)
- [구 Google Play Unity 저장소 보관 및 targetSdk 34 경고](https://github.com/google/play-unity-plugins)
- [Google Play In-App Updates Unity 릴리스](https://github.com/google/play-in-app-updates-unity/releases)
- [Google Play Core Unity 릴리스](https://github.com/google/play-core-unity/releases)
- [Google Play Common Unity 릴리스](https://github.com/google/play-common-unity/releases)
- [Google Android App Bundle Unity 릴리스](https://github.com/google/play-appbundle-unity/releases)
- [Unity Advertisement Legacy 4.17 변경 로그](https://docs.unity3d.com/Packages/com.unity.ads@4.17/changelog/CHANGELOG.html)
- [Unity Ads Android 요구사항](https://docs.unity.com/en-us/grow/ads/android-sdk/requirements)
- [Unity Ads 직접 연동과 LevelPlay 권고](https://docs.unity.com/en-us/grow/ads)
- [BackEnd SDK 5.18.10 / Unity 6000.3.15f1 커뮤니티 확인 사례](https://community.thebackend.io/t/topic/11619)
- [Unity 6.3 관련 BackEnd SDK 이슈 공지 사례](https://community.thebackend.io/t/sdk/11038)
- [DOTween 1.3.030 및 업그레이드 절차](https://dotween.demigiant.com/download/documentation.php)
- [Odin Inspector 3.3.1.14 Unity 6000.3 호환 수정](https://odininspector.com/patch-notes/3-3-1-14)
- [Unity 2022.3용 Tilemap Extras 3.1.1](https://docs.unity3d.com/2022.3/Documentation/Manual/com.unity.2d.tilemap.extras.html)
- [Tilemap Extras Editor 계열 호환표](https://docs.unity3d.com/Packages/com.unity.2d.tilemap.extras@6.0/index.html)
- [Unity 2022.3용 Visual Scripting 1.8.0](https://docs.unity3d.com/2022.3/Documentation/Manual/com.unity.visualscripting.html)
- [Visual Scripting 업데이트 전 백업 지침](https://docs.unity3d.com/Packages/com.unity.visualscripting@1.8/manual/vs-update.html)
- [Unity 6000.3.21f1의 Visual Scripting 1.9.12 업데이트](https://unity.com/releases/editor/whats-new/6000.3.21f1)

### Editor 전환 버전

| 단계 | 확정 버전 | 변경셋 | 공식 릴리스 |
| --- | --- | --- | --- |
| 중간 2022 LTS | 2022.3.76f1 | `d938583f0741` | [Unity 2022.3.76f1](https://unity.com/releases/editor/whats-new/2022.3.76f1) |
| 중간 Unity 6.0 LTS | 6000.0.82f1 | `2fb0dae735e1` | [Unity 6000.0.82f1](https://unity.com/releases/editor/whats-new/6000.0.82f1) |
| 최종 Unity 6.3 LTS | 6000.3.22f1 | `1c726e1fb402` | [Unity 6000.3.22f1](https://unity.com/releases/editor/whats-new/6000.3.22f1) |

Unity 6000.3.22f1은 릴리스 페이지 기준 2026-08-13 공개된 당시 최신 6.3 LTS 패치다. 설치 시점에 더 최신 패치가 Hub에 나타나면 공식 릴리스 페이지와 변경셋을 다시 확인한 후 이 표를 갱신한다.

## Android Player Settings 기준선

| 항목 | 현재 값 |
| --- | --- |
| Application Identifier | `com.Churub.ChurubFactory` |
| Application version | `1.1.0` |
| Android version code | 73 |
| Minimum API Level | 24 |
| Target API Level | 34 |
| Scripting Backend | IL2CPP (`Android: 1`) |
| Target Architectures | ARMv7 + ARM64 (`AndroidTargetArchitectures: 3`) |
| Graphics API | Vulkan → OpenGLES3, 자동 선택 꺼짐 |
| Rendering Pipeline | Built-in (`m_CustomRenderPipeline.fileID: 0`) |
| Multithreaded Rendering | Android 활성화 |
| Managed Stripping Level | Android 명시 항목 없음; 실제 Editor 해석 값은 라이선스 복구 후 API로 확인 필요 |
| Custom main manifest | 활성화 |
| Custom main Gradle template | 활성화 |
| Custom Gradle properties | 활성화 |
| Custom Proguard file | 활성화 |
| Keystore/alias 저장 값 | 비어 있음 |

Graphics API 순서는 `m_APIs: 150000000b000000`을 Unity 2021 enum 값으로 해석한 결과다. Managed Stripping Level은 프로젝트 YAML에 Android override가 직렬화되어 있지 않으므로 임의로 `Disabled`, `Minimal` 또는 `Low`라고 단정하지 않는다.

## Android 커스텀 파일 기준선

| 파일 | 현재 역할과 특이점 |
| --- | --- |
| `Assets/Plugins/Android/AndroidManifest.xml` | 앱 package 명시, UnityPlayerActivity launcher, Google Play services metadata, AD_ID/VIBRATE/INTERNET 권한 |
| `Assets/Plugins/Android/mainTemplate.gradle` | EDM4U가 저장소와 의존성을 직접 삽입한 Unity 2021 템플릿 |
| `Assets/Plugins/Android/gradleTemplate.properties` | AndroidX/Jetifier 활성화, R8 placeholder 사용, `android.overridePathCheck=true` |
| `Assets/Plugins/Android/proguard-user.txt` | Google 전체 클래스와 BackEnd Google login/Unity 클래스 keep |

현재 main template의 해석된 Android 의존성은 다음과 같다.

- `androidx.fragment:fragment:1.3.6`
- `com.google.android.gms:play-services-auth:19.0.0`
- `com.google.android.play:app-update:2.1.0`
- `com.google.android.play:core-common:2.0.4`
- `com.google.games:gpgs-plugin-support:0.11.01`
- `com.unity3d.ads:unity-ads:[4.12.0,4.13[`

Google Maven 저장소가 URL 표기 차이로 두 번 들어가 있고 `mavenLocal()`도 활성화되어 있다. 이는 6단계에서 Unity 6.3 기본 템플릿과 비교한 뒤 필요한 항목만 재적용할 대상으로 기록하며, 지금은 수정하지 않는다.

## Android 빌드 스크립트 기준선

`Assets/Editor/PortfolioBuild.cs`는 다음 특성이 있다.

- `Build/Android/Churub-v1.1.0.apk`를 출력 경로로 하드코딩한다.
- 활성화된 EditorBuildSettings 씬을 사용한다.
- Android Development APK를 `BuildPipeline.BuildPlayer`로 생성한다.
- 현재 Editor 설치 폴더 아래 SDK, NDK, OpenJDK 경로를 강제로 설정한다.
- Title 및 Game 씬이 빌드 목록에 활성화되어 있다.

Unity 실행 파일의 절대 경로는 코드에 없지만 출력 파일의 버전 문자열이 하드코딩되어 있다. 5단계에서 `Application.version` 기반 이름과 Build Profile 책임 분리를 검토한다.

## Unity 2021 기준선 검증 결과

### 라이선스 및 EditMode 테스트

- 샌드박스 안 첫 실행에서는 Licensing Client IPC 채널 생성이 차단되어 Unity 자체 종료 코드 199가 발생했다.
- 동일한 Unity 2021.3.32f1 명령을 샌드박스 밖에서 다시 실행하자 라이선스 초기화가 정상 완료됐다. 따라서 최초 오류는 계정 라이선스 부재가 아니라 실행 격리 환경의 IPC 제한으로 판정한다.
- 테스트 그룹: WorkScheduler 4개, GameDataState 5개, UpgradeService 10개
- 실행 환경: Unity 2021.3.32f1, Android build target, EditMode, batch mode
- 결과 XML: `Build/Baseline/editmode-results.xml` (Git 제외 경로)
- 실행 결과: **총 19개 / 통과 19개 / 실패 0개 / 건너뜀 0개**
- 테스트 실행 시간: 0.0476768초

테스트를 실제로 재실행해 결과 XML을 확보했으므로 이 19개를 이후 단계의 회귀 기준선으로 사용한다.

### 컴파일, 플레이 및 Android 빌드

| 검증 | 상태 | 이유 |
| --- | --- | --- |
| Unity Console 컴파일 오류/경고 확인 | 완료 | 컴파일 오류 0개. CS0108 1개, 미사용 필드 CS0414 2개 기록 |
| Title 씬 실행 | 부분 확인 | Android 빌드 포함 및 직렬화/컴파일 성공. Editor 수동 플레이는 미실행 |
| Game 씬 및 핵심 게임 플레이 | 미실행 | 입력 가능한 Editor 세션 또는 Android 기준 기기 필요 |
| 기존 세이브 로드/재저장 | 미실행 | 기준 기기/기준 세이브 접근 필요 |
| Android Development APK | 완료 | `Build/Android/Churub-v1.1.0.apk`, 103,906,726 bytes |
| 실제 Android 기기 검증 | 미실행 | `adb devices -l` 결과 연결 기기 0대 |
| 성능 기준선 | 미측정 | 실제 기기 검증 전 단계 |
| 기준 세이브 파일 보관 | 미실행 | 정상 플레이 및 기기 접근 필요 |

개발 APK는 실제 Android 빌드 파이프라인으로 생성했고 `aapt dump badging` 및 ZIP 엔트리를 확인했다.

| APK 검증 항목 | 결과 |
| --- | --- |
| Package | `com.Churub.ChurubFactory` |
| Version | code 73 / name `1.1.0` |
| minSdkVersion | 24 |
| targetSdkVersion | 34 |
| compileSdkVersion | 34 |
| Development | `debuggable` 확인 |
| Native ABI | `arm64-v8a`, `armeabi-v7a` |
| IL2CPP 산출물 | 양쪽 ABI의 `libil2cpp.so` 확인 |

## 0단계 완료 조건 판정

| 완료 조건 | 판정 |
| --- | --- |
| 별도 작업 브랜치 생성 | 충족 |
| 현재 패키지와 Android 설정 문서화 | 충족(Managed Stripping 실제 해석 값은 재확인 필요) |
| 기준선 테스트 가능 여부 기록 | 충족 — 19/19 통과 |
| 기준선 빌드 가능 여부 기록 | 충족 — Android Development APK 생성 및 정적 검증 완료 |
| Unity 라이선스 정상 활성 상태 확인 | 충족 — 제한 밖 재실행에서 정상 초기화 |

0단계 완료 조건은 충족했다. 1단계 중 자동화 가능한 컴파일·EditMode 테스트·Android 개발 빌드는 완료했으나, 실제 플레이·세이브·기기 성능 기준선은 연결된 Android 기기가 없어 미실행이다. 이 항목들은 통과로 간주하지 않으며 7~8단계의 출시 판정을 보류시키는 필수 잔여 검증으로 관리한다.

## 다음 작업

1. 공식 호환성 조사 결과에 따라 패키지 업데이트 순서와 최소 버전을 확정한다.
2. Unity 2022.3.76f1에서 프로젝트를 변환하고, 컴파일·19개 EditMode 테스트·Android 개발 빌드를 반복한다.
3. 각 중간 단계 성공 후에만 Unity 6.0 최신 패치와 Unity 6.3.22f1로 진행한다.
4. 실제 Android 기기가 연결되면 기준 세이브, 핵심 플레이, 외부 서비스, 성능 검증을 수행한다.

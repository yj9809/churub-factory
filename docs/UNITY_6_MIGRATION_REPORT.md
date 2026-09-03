# Unity 6 엔진 마이그레이션 보고서

## 진행 상태

- 기준일: 2026-09-03 (Asia/Seoul)
- 현재 단계: 0단계 작업 전 상태 점검
- 판정: **중단됨 — Unity 라이선스 IPC 오류**
- 다음 단계 진입: 금지. 라이선스 문제를 해소하고 Unity 2021 기준선 검증을 다시 실행해야 한다.

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
| 필요한 중간 Editor | Unity 2022.3 LTS 미설치 |
| 목표 Editor | Unity 6.3 (`6000.3.x`) 미설치 |

프로젝트의 정확한 버전 근거는 `ProjectSettings/ProjectVersion.txt`다. 설치된 `6000.5.4f1`은 요청한 Unity 6.3 계열이 아니므로 목표 Editor로 사용하지 않는다.

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

### EditMode 테스트

- 정적 발견: `[Test]` 19개
- 테스트 그룹: WorkScheduler 4개, GameDataState 5개, UpgradeService 10개
- 실행 명령 대상: Unity 2021.3.32f1, EditMode, batch mode
- 실행 결과: **실행 불가**
- 결과 XML: 생성되지 않음
- 성공/실패 테스트 수: 판정 불가

실제 Unity 로그:

```text
[Licensing::IpcConnector] Connection attempt to the License Client on channel: "LicenseClient-윤" failed because channel doesn't exist; code: "0x80000002"
[Licensing::Module] Successfully launched the LicensingClient (PId: 8068)
[Licensing::Module] Timed-out after 60.00s, waiting for channel: "LicenseClient-윤"
IPC channel to LicensingClient doesn't exist; aborting
Application will terminate with return code 199
```

PowerShell 호스트가 보고한 외부 명령 종료 상태와 달리 Unity 자체 로그의 실제 종료 코드는 199다. 테스트 결과 파일이 없으므로 테스트를 통과했다고 간주하지 않는다. 실행 로그는 Git에서 제외된 `Build/Baseline/editmode.log`에 보관되어 있다.

### 컴파일, 플레이 및 Android 빌드

| 검증 | 상태 | 이유 |
| --- | --- | --- |
| Unity Console 컴파일 오류/경고 확인 | 미실행 | 라이선스 초기화 단계에서 Editor 중단 |
| Title 씬 실행 | 미실행 | 동일 차단 |
| Game 씬 및 핵심 게임 플레이 | 미실행 | 동일 차단 |
| 기존 세이브 로드/재저장 | 미실행 | 동일 차단 |
| Android Development APK | 미실행 | 동일 차단 |
| 실제 Android 기기 검증 | 미실행 | APK 및 기준 기기 없음 |
| 성능 기준선 | 미측정 | 실제 기기 검증 전 단계 |
| 기준 세이브 파일 보관 | 미실행 | 정상 플레이 및 기기 접근 필요 |

## 0단계 완료 조건 판정

| 완료 조건 | 판정 |
| --- | --- |
| 별도 작업 브랜치 생성 | 충족 |
| 현재 패키지와 Android 설정 문서화 | 충족(Managed Stripping 실제 해석 값은 재확인 필요) |
| 기준선 테스트 가능 여부 기록 | 충족 — 라이선스 IPC로 실행 불가 |
| 기준선 빌드 가능 여부 기록 | 충족 — 라이선스 IPC 선행 차단으로 미검증 |
| Unity 라이선스 정상 활성 상태 확인 | **미충족** |

따라서 0단계는 전체 완료로 승인할 수 없으며 1단계 이후 작업을 시작하지 않는다.

## 재개 조건과 다음 작업

1. Unity Hub와 Licensing Client를 정상 상태로 복구한다.
2. 동일한 Unity 2021.3.32f1 batch mode 명령으로 19개 EditMode 테스트를 재실행하고 결과 XML을 확보한다.
3. Editor에서 Console, Title/Game 씬 및 핵심 동작을 확인한다.
4. Android Development APK를 clean build하고 가능하면 동일 기준 기기에서 실행한다.
5. 실제 Managed Stripping Level과 기준 세이브·성능 데이터를 확정한 뒤에만 0/1단계 완료를 승인한다.

라이선스 복구 전에는 패키지 업데이트, ProjectVersion 변경, Unity 2022.3/6.x로 프로젝트 열기, Android 템플릿 재생성 및 URP 전환을 하지 않는다.

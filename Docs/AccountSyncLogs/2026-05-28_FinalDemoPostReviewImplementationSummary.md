# FinalDemo Post Audio Review Implementation Summary - 2026-05-28

이 문서는 사용자가 `Docs/FinalDemoAudioReview_2026-05-28.md`를 기준으로 개선 요청을 낸 이후, 이 대화에서 수정된 내용과 다음 개발 흐름을 빠르게 이어받기 위한 요약입니다.

## 기준점

- 기준 문서: `Docs/FinalDemoAudioReview_2026-05-28.md`
- 후속 로그: `Docs/AccountSyncLogs/2026-05-28_FinalDemoAudioFollowup.md`
- 핵심 목표: FinalDemo의 오디오, LED, 종 이동, HRTF, 비/앰비언트, 모델, 인스펙터 조정성을 실제 체험 가능한 수준으로 묶는 것.
- 노트북 호환성: 장치 포트/구조를 크게 바꾸지 않았고, 기존 COM40/COM30 기반 흐름을 유지했다.

## 대략적으로 수정한 것들

### 오디오 큐와 라우팅

- `FinalDemoCueLibrary`와 `FinalDemoCueLibrary.asset`에 후속 큐/대체 클립을 정리했다.
- 오프닝의 `ForestBed` 사용을 제거하고, 별도 `OpeningAmbienceBed`를 추가했다.
- `OpeningAmbienceBed`는 `05_mixkit_open_ground_texture_b_very_low.wav`를 사용하도록 연결했다.
- `BellMovementTexture`는 실제 이동 중인 종 위치에서 루프/페이드되도록 라우팅했다.
- 다만 이후 사용자 피드백에 따라, 초기 종이 사용자 주위를 도는 구간에서는 이동소리를 제거하고 종소리만 남기는 방향으로 다시 조정했다.
- 일반 이명 처치 중 유효 매칭 상태에서는 `TinnitusHealingLoop`가 시작되고, 이탈/완료 시 멈추도록 연결했다.
- 보스 약점 이동 중 `BossWeakpointMove`가 나오도록 연결했다.
- 종이 멀리 있을 때 약한 거리 리버브가 걸리도록 `FinalDemoAudioRouter`에 거리 기반 wet 처리를 넣었다.

### HRTF / 방향감

- `FinalDemoDirector`의 HRTF/software binaural preview 기본값을 ON으로 바꿨다.
- `BellRingerBinauralSpatializer`의 좌우 방향 가중을 더 강하게 조정했다.
- `TinnitusAudioController`의 절차음, 긴 글리치 루프, 짧은 글리치 소스가 같은 월드 위치 기준으로 들리도록 child source 로컬 위치를 0으로 고정했다.
- 목표는 종소리와 이명이 머리 회전에 따라 좌우 차이가 더 명확하게 들리는 것이다.

### 종 이동과 종소리

- 초기 종 회전 경로를 `FinalDemoTuningProfile`의 경로 기반으로 쓰게 정리했다.
- 경로는 사용자가 의도한 흐름에 가깝게 왼쪽, 중앙, 오른쪽, 뒤쪽, 왼쪽, 중앙, 위쪽, 뒤쪽, 아래쪽, 중앙 순서로 수정했다.
- 스플라인 보간을 open path로 바꿔 마지막 중앙 지점에서 첫 번째 추적 위치로 자연스럽게 넘어가게 했다.
- 첫 종 회전 속도와 종소리 간격은 여러 차례 체험 피드백을 반영해 조정했다. 현재 최신 기본값은 `bellOrbitSeconds = 19.1`, `bellOrbitCallIntervalSeconds = 0.39`다.
- 1번째 추적 위치에서 2번째 추적 위치로 넘어갈 때 종이 텔레포트하지 않고 실제 이동하도록 relocation 상태를 추가했다.
- 종 추적 구간의 종소리 반복은 처음 11초, 못 찾을수록 0.5초씩 줄어 최소 8초까지 짧아지게 했다.
- 종이 멀어도 소리와 LED가 둘 다 같은 위치 기준으로 약해지도록 거리 기준을 확장했다.
- 종을 지나쳐 앞으로 가면 영구적으로 찾지 못하는 문제를 막기 위해, 추적 구간에서 종 목표를 지나 너무 멀리 가지 못하게 route-plane blocker를 넣었다.

### LED / 폐안 빛

- 소리가 시야 밖으로 나간 경우 LED가 보드 가장자리에 갇히지 않도록, 매핑 실패 시 출력을 clear하도록 수정했다.
- 종 LED는 소리가 날 때만 나오게 정리했고, 상시 초록 점이 남는 문제를 줄였다.
- 종의 파형 반응은 빠르게 번쩍이는 방식보다 onset 기반 퍼짐으로 완화했다.
- 종 LED 광량은 여러 차례 낮췄다. 최신 기본값은 `bellBrightnessBoost = 0.87`, `bellWaveBrightnessBoost = 0.43`, `bellWaveMaximumBrightness = 0.21`이다.
- 하드웨어와 16x8 프리뷰가 같은 논리 프레임을 쓰도록 유지했다.

### 비 / 앰비언트

- 비는 레인존 진입 시 `RainLightBed`, `RainStrongBed`가 시작되고, 3초 동안 페이드인되도록 했다.
- `rainIntensityRampSeconds = 3`으로 기본값을 고정했다.
- 비바람 속에서 종소리를 놓치지 말라는 안내는 바로 나오지 않고 지연되도록 기존 튜닝값을 유지/정리했다.
- 오프닝 앰비언트는 별도 cue로 분리해, 숲 엔딩 bed와 섞이지 않게 했다.

### 패드 흔들기와 햅틱

- 패드를 흔들면 종소리를 다시 확인할 수 있는 로직을 FinalDemo에 연결했다.
- 초기 종 회전과 첫 위치로 이동 중에는 패드 흔들기 반응을 막는 방향으로 정리했다.
- 패드 흔들기 안내 반복 간격은 사용자가 너무 자주 들린다고 피드백해 더 길게 조정했다.
- 햅틱 라우터에는 인스펙터에서 찾기 쉽도록 `패드 흔들기 / Haptic Output` 섹션을 추가했다.

### 모델과 인스펙터

- `Assets/Art/Models/Bell`, `Assets/Art/Models/Pad` 안의 모델을 확인했다.
- Bell OBJ와 Pad FBX를 FinalDemo 전용 런타임 fallback으로 쓰기 위해 `Assets/Resources/FinalDemoModels`에 복사했다.
- `FinalDemoModelPresenter`를 추가해 FinalDemo의 `bellVisual`, `padVisual`에 모델을 런타임으로 적용한다.
- 패드 모델은 흰색 계열 material로 보정한다.
- `FinalDemoKoreanGuide`를 추가해 `FinalDemoRoot`에서 오디오, LED, 종 경로, 패드 흔들기, 비/앰비언트, 이명, 모델 관련 조정 위치를 한글로 볼 수 있게 했다.
- `Bell Ringer/Final Demo/Apply Polish` 에디터 메뉴를 추가했다. Unity가 잠겨 있지 않을 때 씬/모델 적용을 다시 강제하는 용도다.

### 문서와 동기화

- `Docs/AccountSyncLogs`를 만들고, 다른 계정이 이어받을 수 있도록 후속 로그를 남겼다.
- `AGENT.md`에 앞으로 새 작업 전 최신 handoff log를 읽으라는 내용을 추가했다.
- 노트북 호환성은 계속 유지해야 한다는 메모도 추가했다.

## 주요 파일

- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
- `Assets/Scripts/FinalDemo/FinalDemoTuningProfile.cs`
- `Assets/Scripts/FinalDemo/FinalDemoAudioRouter.cs`
- `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
- `Assets/Scripts/FinalDemo/FinalDemoAudioReactiveLight.cs`
- `Assets/Scripts/FinalDemo/FinalDemoHapticRouter.cs`
- `Assets/Scripts/FinalDemo/FinalDemoCueLibrary.cs`
- `Assets/Scripts/FinalDemo/FinalDemoStage.cs`
- `Assets/Scripts/Audio/BellRingerAudioLedMapper.cs`
- `Assets/Scripts/Audio/BellRingerBinauralSpatializer.cs`
- `Assets/Scripts/Audio/TinnitusAudioController.cs`
- `Assets/Scripts/FinalDemo/FinalDemoKoreanGuide.cs`
- `Assets/Scripts/FinalDemo/FinalDemoModelPresenter.cs`
- `Assets/Scripts/Debug/Editor/FinalDemoPolishTool.cs`
- `Assets/Scenes/FinalDemo.unity`
- `Assets/ScriptableObjects/FinalDemo/FinalDemoCueLibrary.asset`
- `Assets/Resources/FinalDemoModels`

## 검증 상태

- `BellRinger.Runtime.csproj` 빌드 통과.
- `BellRinger.Editor.csproj` 빌드 통과.
- 남은 경고는 기존 `PadTrackingReceiver.PadTrackingPacket` 필드 경고와 `USG0001` 계열이다.
- Arduino 스케치 변경은 이 구간에서 핵심이 아니었고, Unity FinalDemo 쪽 수정이 중심이었다.

## 다음으로 할 가능성이 높은 개발

### 1. VR 실착 QA와 수치 고정

가장 먼저 해야 할 일은 실제 VR 기기, 헤드폰, COM40 LED, COM30 패드/IMU를 연결한 상태에서 FinalDemo를 반복 실행하며 수치를 고정하는 것이다.

확인할 것:
- 종 LED가 여전히 눈 아픈지.
- 종 파형 퍼짐이 폐안 상태에서 부드럽게 읽히는지.
- 초기 종 회전 경로가 사용자의 의도대로 느껴지는지.
- 종소리 좌우 방향감이 충분히 과장되어 들리는지.
- 비 페이드인이 확실히 들리는지.
- 이명이 전체음처럼 들리지 않고 월드 위치에서 들리는지.
- 종 추적 중 blocker가 답답하지 않고, 단순히 길을 잃는 문제만 막는지.

### 2. FinalDemo 인스펙터 튜닝 UX 개선

현재 조정값은 나뉘어 있지만, 아직 완전한 단일 튜닝 콘솔은 아니다.

다음 작업 후보:
- `FinalDemoTuningProfile` 섹션 이름을 더 명확하게 정리.
- 자주 바꿀 값만 별도 top-level preset으로 노출.
- 오디오, LED, 종 경로, 패드 흔들기, 비/앰비언트, 이명, 모델 순서로 실제 인스펙터 흐름을 정돈.
- 사용자 QA 중 바꾼 값을 따로 기록하는 preset snapshot 기능 추가.

### 3. 벽 접촉 오디오

`FinalDemoAudioReview_2026-05-28.md` 기준으로 아직 완전히 처리되지 않은 명시 항목이다.

다음 작업 후보:
- 벽 접촉 cue family 추가.
- `06_mixkit_button_click_metal_tactile`, `08_mixkit_mechanical_tick_alt`를 후보로 연결.
- 플레이어 또는 패드가 벽/장애물에 닿을 때 짧게 재생.
- 필요하면 햅틱과 약한 흰색/시안 LED 피드백도 같이 연결.

### 4. 일반 이명 / 대왕 이명 체험 QA

이명 처치 로직은 들어가 있지만, 최종 체험 난이도는 아직 실제 손 QA가 필요하다.

확인할 것:
- 일반 이명 4초 유지가 너무 쉽거나 어렵지 않은지.
- 대왕 이명 3단계가 학습 가능하게 느껴지는지.
- 패드 위치, pitch, roll, yaw가 모두 처치 판정에 자연스럽게 반영되는지.
- 이명 완료 사운드, 에코, 정적, LED 소거가 헷갈리지 않는지.
- 햅틱이 IMU 드리프트를 악화시키는 수준인지.

### 5. 오프닝에서 종 획득까지의 연출 완성도

현재 MVP 흐름은 연결되어 있으나, 전시용으로는 초반 1분의 완성도가 중요하다.

다음 작업 후보:
- 정면 안내 후 완전 정적 시간과 앰비언트 시작 타이밍 재확인.
- 첫 왼쪽 종소리의 방향감 강화.
- 종 회전 경로의 고도, 거리, 속도 커브를 사용자 체감 기준으로 조정.
- 1번째/2번째 종 추적에서 종소리 범위와 LED 범위를 최종 조정.
- 패드 흔들기 안내가 너무 자주 나오지 않도록 최종 간격 고정.

### 6. 노트북 호환성 재확인

앞으로 구조 변경이 들어가면 노트북에서도 깨질 수 있다.

확인할 것:
- COM 포트 기본값이 노트북과 데스크톱에서 각각 어떻게 잡히는지.
- `Resources/FinalDemoModels` 기반 모델 fallback이 노트북에서도 import되는지.
- 오디오 파일 GUID가 노트북 프로젝트에서도 유지되는지.
- FinalDemo 씬 실행 시 LED, HRTF, PadTracking, IMU가 각각 degrade 가능하게 동작하는지.

## 다음 작업 제안 순서

1. FinalDemo를 VR 실착으로 한 번 더 QA하고, 종 LED/종 경로/종소리 방향감 수치를 먼저 고정한다.
2. 그 다음 일반 이명과 대왕 이명 처치 난이도를 조정한다.
3. 이후 벽 접촉 오디오와 작은 피드백을 붙인다.
4. 마지막으로 모델 스케일/위치, 인스펙터 정리, 노트북 호환성 검증을 마무리한다.

이 순서가 적절한 이유는 현재 게임의 핵심 체험이 “폐안 상태에서 종을 소리와 빛으로 따라갈 수 있는가”에 가장 크게 걸려 있기 때문이다. 이 체감이 고정되지 않으면 이명/보스/벽 피드백을 더 붙여도 전체 품질 판단이 흔들린다.

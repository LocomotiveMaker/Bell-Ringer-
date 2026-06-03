# FinalDemo Emergency Reference - 2026-06-04

전시 중 급하게 수정할 때 보는 최종 요약 문서입니다. 긴급 상황에서는 먼저 `FinalDemo` 씬과 `FinalDemoTuningProfile.asset` 값을 확인하고, 스크립트 구조 변경은 마지막 수단으로만 사용합니다.

## 실행 기준

- 메인 씬: `Assets/Scenes/FinalDemo.unity`
- 메인 루트: `FinalDemoDirector`
- 핵심 튜닝 에셋: `Assets/ScriptableObjects/FinalDemo/FinalDemoTuningProfile.asset`
- 오디오 큐 에셋: `Assets/ScriptableObjects/FinalDemo/FinalDemoCueLibrary.asset`
- LED 라우터: `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
- 오디오 라우터: `Assets/Scripts/FinalDemo/FinalDemoAudioRouter.cs`
- 게임 진행: `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
- 포즈 매칭: `Assets/Scripts/Gameplay/PadPoseProvider.cs`, `Assets/Scripts/Gameplay/PadPoseMatchEvaluator.cs`

## 장치 포트

- VR/머리/WS LED 쪽 ESP32-S3: `COM40`
- 패드 IMU 쪽 ESP32-S3: `COM30`
- 패드 카메라 추적 UDP: `39051`
- 현재 튜닝 에셋의 포트 값도 위 기준입니다.
- 패드 카메라 추적 실행 예시: `powershell -ExecutionPolicy Bypass -File tools\Run-PadTracker.ps1 -CameraIndex 0 -Width 1280 -Height 720 -Fps 60 -Backend MSMF`

## 최종 게임 흐름

1. `Preflight`: 하드웨어/입력 대기
2. `OpeningAmbience`: 정면 대기 및 앰비언트
3. `OpeningSilence`: 짧은 정적
4. `OpeningCloseBell`: 가까운 종소리
5. `BellOrbit`: 종이 사용자 주변을 회전
6. `BellFollowOne`: 첫 번째 종 위치 추적
7. `BellFollowRain`: 두 번째 종 위치 추적, 비/바람 시작
8. `BellGaze`: 머리 회전으로 종 바라보기
9. `BellAcquisition`: 종 획득
10. `GeneralTinnitusOne`: 일반 이명 1개 정화
11. `BossApproach`: 보스 이명 접근
12. `BossPatternOne`: 보스 지점 1 정화
13. `BossPatternTwo`: 보스 지점 2 정화
14. `BossPatternThree`: 보스 지점 3 정화
15. `BossDefeat`: 보스 처치
16. `ForestEnding`: 엔딩 숲/종
17. `Complete`: 종료

`GeneralTinnitusTwo`는 현재 FinalDemo에서 건너뜁니다. 1번째 일반 이명 완료 후 바로 보스로 넘어갑니다.

## 보스 이명

- 현재 구조: 이동 약점이 아니라 고정 지점 3개를 순서대로 정화합니다.
- 회전 사용 여부: 보스는 위치만 사용하고 회전은 무시합니다.
- 현재 정화 시간:
  - 보스 1: `4초`
  - 보스 2: `5초`
  - 보스 3: `6초`
- 조정 위치: `FinalDemoTuningProfile.asset`
  - `bossPatternOneSeconds`
  - `bossPatternTwoSeconds`
  - `bossPatternThreeSeconds`
  - `bossHoldPositionToleranceMeters`
  - `bossBaseVolume`
  - `bossMassLedIntensity`
- 런타임 캡처 키:
  - `F7`: Boss 1 지점 캡처
  - `F8`: Boss 2 지점 캡처
  - `F9`: Boss 3 지점 캡처
- 캡처 UI: 화면의 `Authoring Capture` 창
- 보스 소리 위치: 현재 정화해야 하는 보스 지점에서 납니다.

## 일반 이명

- 현재 구조: 일반 이명은 1개만 사용합니다.
- 정화 조건: 패드 위치 + yaw/pitch/roll 회전이 정답 범위 안에 들어가야 합니다.
- 정화 시간: `generalTinnitusTreatmentSeconds`, 현재 `4초`
- 접근 잠금 범위: `generalTinnitusApproachRadius` 또는 `Range_TinnitusLock_01`
- 소리/LED 범위: `Range_Tinnitus_01`
- 런타임 캡처 키:
  - `F5`: 일반 이명 A 정답 위치/회전 캡처
- 조정 위치:
  - `generalTinnitusPositionToleranceMeters`
  - `generalTinnitusRotationToleranceDegrees`
  - `generalTinnitusToneVolume`
  - `generalTinnitusLedIntensity`
  - `tinnitusMatchToneVolume`

## 종 구간

- 초기 회전 경로: `FinalDemoTuningProfile.asset`의 `bellOrbitLocalPoints`
- 초기 회전 시간: `bellOrbitSeconds`
- 초기 회전 종소리 주기: `bellOrbitCallIntervalSeconds`
- 첫/두 번째 종 위치: `bellFollowTargetOnePosition`, `bellFollowTargetTwoPosition`
- 종 도착 판정: `bellArrivalRadius`
- 종 소리 주기:
  - `bellFollowInitialCallIntervalSeconds`
  - `bellFollowMinimumCallIntervalSeconds`
  - `bellFollowMissIntervalReductionSeconds`
  - `bellFollowNearIntervalReduction`
- 패드 흔들기:
  - `BellFollowOne`, `BellFollowRain`, `BellGaze`에서 종소리 보조가 납니다.
  - 현재 패드 흔들기 종 빛은 FinalDemo에서 꺼진 상태로 두는 것이 안전합니다.
  - 관련 값: `padShakeAssistMotionThreshold`, `padShakeAssistCooldownSeconds`, `padShakeAssistNarrationCooldownSeconds`

## 비와 바람

- 비 오디오 시작: `BellFollowRain`에서 `BeginRainLayer()`가 시작합니다.
- 비 증가 시간: `rainIntensityRampSeconds`, 현재 `8초`
- 비 최대 세기: `rainMaxIntensity`
- 비 소리 크기: `rainAudioGainMultiplier`
- 비 LED 라우팅: `FinalDemoLightRouter.ShowRainFloorBand()`에서 `HardwareBridge.SendLedRain(...)`로 보냅니다.
- 비 LED는 샘플씬의 비 문법을 기준으로 합니다.
- 비가 너무 밝으면 먼저 이 값을 낮춥니다:
  - `LightRouter_LED`의 `Rain Brightness`
  - `LightRouter_LED`의 `Rain Density`
  - `LightRouter_LED`의 `Rain Peak Contrast`
  - `LightRouter_LED`의 `Rain Preview Brightness Boost`는 모니터용이므로 실제 WS 밝기와 구분해서 봅니다.

## LED 우선순위

- 보스/패드/이명/종/비는 모두 `FinalDemoLightRouter`를 통합니다.
- 현재 전시 안정성을 위해 비와 종의 복잡한 합성은 피합니다.
- 종 파형 LED: `ShowBellWave`
- 종 위치 앵커: `ShowBellAnchor`
- 비 LED: `ShowRainFloorBand`
- 일반/보스 이명 LED: `ShowTinnitusPattern`
- WS가 아예 안 나오면 먼저 확인할 것:
  - `HardwareBridge`가 `COM40`에 연결되어 있는가
  - `LightRouter_LED`의 `Output To Hardware`가 켜져 있는가
  - VR 쪽 보드에 LED 포함 펌웨어가 업로드되어 있는가

## 오디오

- 모든 큐는 `FinalDemoCueLibrary.asset`에서 관리합니다.
- 실제 재생은 `FinalDemoAudioRouter`가 담당합니다.
- 종, 이명, 보스 이명은 월드 위치 기반 3D 오디오입니다.
- HRTF 프리뷰는 `FinalDemoDirector`의 `HrtfPreviewEnabled` 경로를 사용합니다.
- 이명 주요 큐:
  - `TinnitusLongGlitch`
  - `TinnitusBurst`
  - `TinnitusPoseLock`
  - `TinnitusPoseLost`
  - `TinnitusHealingLoop`
  - `TinnitusResolve`
- 보스 주요 큐:
  - `BossBasePulse`
  - `BossGlitchBurst`
  - `BossHit`
  - `BossDefeatRise`
  - `BossDefeatAir`

## 하드웨어 입력

- 머리 입력: VR 쪽 ESP32-S3/MPU9250에서 들어오는 머리 회전입니다.
- 패드 위치: 카메라 ArUco/AprilTag 추적 UDP입니다.
- 패드 회전: 패드 쪽 ESP32-S3/MPU9250입니다.
- 패드 최종 포즈는 `PadPoseProvider`가 합칩니다.
- FinalDemo 전용 패드 보정값은 `FinalDemoDirector`의 `Final Demo Pad Pose Calibration` 섹션에 있습니다.
- 포즈 매칭이 이상하면 먼저 확인할 것:
  - `PadPoseProvider`의 current camera-space position
  - `FinalDemoPoseAuthoringMarker`의 stored target camera-space position
  - `PadPoseMatchEvaluator`의 position error / yaw pitch roll error

## 벽과 진행 게이트

- 상시 벽:
  - `BackWall_White_Authoring`
  - `frontWall_White_Authoring`
  - `LeftSoftWall_Authoring`
  - `RightSoftWall_Authoring`
- 진행 벽:
  - `firMove`: 첫 번째 종까지 막음
  - `secondMov`: 두 번째 종까지 막음
  - `firTin`: 일반 이명 이후 보스까지의 진행 제어
  - `secondTin`: 현재 2번째 일반 이명을 제거했으므로 FinalDemo에서 쓰지 않음

## 응급 증상별 확인

### LED가 전혀 안 나옴

1. `FinalDemo` 좌상단 HUD에서 hardware connected 확인
2. `HardwareBridge` 포트가 `COM40`인지 확인
3. 샘플씬에서 LED가 나오면 FinalDemo의 `LightRouter_LED` 참조/출력 옵션 확인
4. 샘플씬에서도 안 나오면 펌웨어/배선/포트 문제로 봅니다.

### 비 LED가 이상하게 밝음

1. `LightRouter_LED > Rain Brightness`를 낮춤
2. `Rain Density`를 낮춤
3. `Rain Peak Contrast`를 낮춤
4. 비와 종 LED 합성 로직을 다시 켜지 말고, 단일 `SendLedRain` 경로를 유지합니다.

### 보스 정답이 안 맞음

1. 보스 단계에서 패드를 원하는 위치에 둠
2. `F7/F8/F9`로 각 보스 지점을 다시 캡처
3. `Authoring Capture` 창의 Boss target 값이 바뀌는지 확인
4. 너무 어렵다면 `bossHoldPositionToleranceMeters`를 키움
5. 시간이 길면 `bossPatternOne/Two/ThreeSeconds`를 낮춤

### 일반 이명 정답이 안 맞음

1. 이명 단계에서 패드를 정답 위치/회전으로 둠
2. `F5`로 다시 캡처
3. `Position Error`와 `Yaw/Pitch/Roll Error`가 움직이는지 확인
4. 너무 어렵다면 `generalTinnitusPositionToleranceMeters`와 `generalTinnitusRotationToleranceDegrees`를 키움

### 소리 방향감이 약함

1. `FinalDemoAudioRouter`의 spatial 설정 확인
2. AudioSource의 `spatialBlend`가 3D인지 확인
3. HRTF 프리뷰가 꺼져 있으면 켬
4. CueLibrary의 해당 cue가 올바른 clip인지 확인

## 빌드 검증

Unity C# 빌드:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Runtime.csproj
```

Pad tracker 실행:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-PadTracker.ps1 -CameraIndex 0 -Width 1280 -Height 720 -Fps 60 -Backend MSMF
```

## 전시 직전 최소 QA

1. `FinalDemo` 실행 시 COM40 LED 연결이 되는지 확인
2. 머리 회전으로 시점 좌우/상하가 자연스럽게 움직이는지 확인
3. 패드 위치가 카메라 추적되고, 패드 회전이 들어오는지 확인
4. 종 1, 종 2, 종 바라보기, 종 획득까지 진행되는지 확인
5. 일반 이명 F5 캡처 후 4초 정화가 되는지 확인
6. 보스 F7/F8/F9 캡처 후 4/5/6초 정화가 되는지 확인
7. 비 구간에서 눈이 아프면 LED 밝기보다 `Rain Density`부터 낮춤

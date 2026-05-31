# Bell Ringer EXPO 슬라이드 3 수정 가이드

이 문서는 `실피컴_2026 MD_EXPO_B1.pptx`의 3번째 슬라이드 내부 섹션 `01~04`를
수정할 때 기준으로 삼는 문서입니다.

## 현재 확정된 빛 색

런타임 기준 상수는 [BellRingerLightStyle.cs](C:\Bell Ringer\Assets\Scripts\Audio\BellRingerLightStyle.cs:5)에 있습니다.

- 종: `#05FF1F` 초록
- 벽: `#05D9FF` 시안
- 비: `#0514FF` 딥 블루
- 이명: `#750DFF` 바이올렛
- 패드 런타임 상수: `#FF4705` 주황

다만 발표 자료와 관람자 화면에서는 패드를 `노랑·골드 계열`로 설명하는 편이 더 읽기 좋습니다.
실제 관람자 화면 구현도 그쪽에 가깝습니다.

## 섹션별 문구 기준

### 01 개요

- 플레이어는 눈을 감거나 시야를 차단한 상태에서, 소리·빛·진동만으로 공간을 인지합니다.
- 종소리를 따라 이동하고, 비와 바람 속에서도 목표 방향을 분리해 추적합니다.
- 패드의 위치와 회전을 맞춰 이명을 정화하며 감각 규칙을 익히는 체험형 게임입니다.

### 02 배경 및 목적

- 이 프로젝트는 시각장애 보조 목적에서 출발한 것이 아니라, 시각을 거의 쓰지 않을 때 어떤 감각 규칙의 게임이 성립하는지에 대한 호기심에서 시작했습니다.
- 소리, 눈앞 LED 빛, 패드 진동만으로도 선명하고 인상적인 플레이 경험을 만들 수 있는지 확인하고자 했습니다.
- 이를 위해 ArUco 기반 패드 위치 추적, IMU 기반 머리·패드 회전 인식, 관람자 화면을 하나의 루프로 구성했습니다.
- 폐안 상태에서 구분이 쉬운 빛 색을 직접 테스트해 종=초록, 벽=시안, 비=딥 블루, 이명=바이올렛, 패드=노랑·골드 계열로 반영했습니다.

### 03 내용

- 최종 데모의 진행 흐름과 감각 규칙을 한 장의 인포그래픽으로 정리했습니다.
- 종 오리엔테이션 → 비바람 속 종 추적 → 종 바라보기 ×3 → 일반 이명 ×2 → 보스 이명 ×3 → 숲 엔딩

### 04 결과

- ArUco 기반 패드 위치 추적과 IMU 회전 추적을 결합했습니다.
- 공간 음향, 눈앞 LED, 진동, 관람자 화면을 하나의 실시간 루프로 연결했습니다.
- 아래 이미지는 실제 관람자 화면 예시, 패드 추적 구조, 현재 하드웨어 구성을 보여줍니다.

## 02번 이미지용 GPT 초안 프롬프트

대상 슬롯: 3번째 슬라이드 `02 배경 및 목적`의 우측 정사각형 이미지.

### Prompt A

```text
Square hero image for a Korean university expo poster about a sensory game called Bell Ringer. A player with closed eyes or blocked vision experiences direction through sound, small near-eye LED light, and controller vibration. Show a calm dark teal environment, a glowing green bell ahead, deep blue rain near the floor, thin cyan wall noise, and a violet tinnitus presence in the distance. The image should feel mysterious, elegant, and technical rather than fantasy. Emphasize that sound and light are linked cues. Clean composition, high detail, poster-friendly, no text.
```

### Prompt B

```text
Square concept art for a physical computing game prototype. Closed-eye player silhouette centered, green bell cue, cyan wall plane, deep blue floor rain, violet tinnitus signal, yellow-gold controller cue. The mood is quiet, experimental, and sensory, like a design research game rather than an accessibility ad. Strong sense of spatial audio turned into light. Minimal background clutter, sharp readable forms, no text, high resolution.
```

### Prompt C

```text
Square visual for an expo poster section titled background and purpose. Show the idea of replacing normal vision with sound, light, and vibration in a game: closed-eye face, floating bell, subtle LED streaks, controller motion, spatial cue lines, calm dark green-blue atmosphere. Distinct cue colors: bell green, walls cyan, rain deep blue, tinnitus violet, controller yellow-gold. Sophisticated, modern, slightly surreal, no UI text, high resolution.
```

생성 이미지 중 하나를 확정하면 아래 경로로 저장해서 기본 영웅 이미지를 덮어쓰면 됩니다.

`C:\Bell Ringer\Docs\ExpoSlide3Assets\slide3_section02_hero_custom.png`

현재는 아래 3개 초안을 프로젝트 안에 복사해 두었고, 기본 선택값은 `DraftB`입니다.

- `C:\Bell Ringer\Docs\ExpoImageDrafts_Refined\Slide3_Section02_DraftA.png`
- `C:\Bell Ringer\Docs\ExpoImageDrafts_Refined\Slide3_Section02_DraftB.png`
- `C:\Bell Ringer\Docs\ExpoImageDrafts_Refined\Slide3_Section02_DraftC.png`

## PPT 조작 방식

이번 작업에는 전용 PPT 조작 스킬이나 플러그인이 없어서, 아래 로컬 파이프라인으로 정리했습니다.

1. [updateBellRingerExpoSlide3.mjs](C:\Bell Ringer\tools\ppt-draft\updateBellRingerExpoSlide3.mjs:1)
   - 슬라이드 3 텍스트 교체
   - 섹션 02/03/04용 고해상도 PNG 생성
   - PPT 내부 media 및 relationship 자동 교체
2. [Update-BellRingerExpoPpt.ps1](C:\Bell Ringer\tools\ppt-draft\Update-BellRingerExpoPpt.ps1:1)
   - 원본 PPTX 압축 해제
   - Node 패처 실행
   - 수정된 PPTX 재압축

즉, 지금부터는 이미지 교체 후 스크립트만 다시 돌리면 됩니다.

## 재빌드 명령

PowerShell에서:

```powershell
Set-Location 'C:\Bell Ringer\tools\ppt-draft'
powershell -ExecutionPolicy Bypass -File .\Update-BellRingerExpoPpt.ps1
```

산출물:

- 수정본 PPT: `C:\Bell Ringer\Docs\BellRingerExpoSlide3Refined.pptx`
- 생성 자산: `C:\Bell Ringer\Docs\ExpoSlide3Assets`

# Expo Assets 2026-05-31

이 폴더의 자산은 `실피컴_2026 MD_EXPO_B1.pptx` 3번째 슬라이드 보강용으로 만든 분리 SVG 자산입니다.

## 코드 기준 색상 확인

- 종: `#05FF1F` (`BellGreen`)
- 벽: `#05D9FF` (`WallCyan`)
- 비: `#0514FF` (`RainDeepBlue`)
- 이명: `#750DFF` (`TinnitusViolet`)
- 패드: `#FF4705` (`PadOrange`)

현재 프로젝트 코드 기준으로 패드는 `노랑`이 아니라 `오렌지` 쪽입니다.

## 주요 파일

- `S1_closed_eye_callout_horizontal.svg`
- `S1_closed_eye_callout_vertical.svg`
- `S2_background_purpose_panel.svg`
- `S2_tracking_pipeline.svg`
- `S3_stage_card_shell.svg`
- `S3_flow_arrow.svg`
- `S3_icon_prepare_closed_eye.svg`
- `S3_icon_bell_trace.svg`
- `S3_icon_rainstorm.svg`
- `S3_icon_bell_gaze.svg`
- `S3_icon_tinnitus_purify.svg`
- `S3_icon_boss_tinnitus.svg`
- `S3_icon_forest_ending.svg`
- `S3_legend_bell.svg`
- `S3_legend_rain.svg`
- `S3_legend_tinnitus.svg`
- `S3_legend_wall.svg`
- `S3_legend_pad.svg`
- `S3_tech_pipeline.svg`

## 섹션 2 추천 문장

### 배경
시각을 줄이면 방향과 존재를 파악하는 기준이 소리, 빛, 진동으로 이동한다.  
이 프로젝트는 접근성 해결보다, 눈을 감았을 때 발생하는 낯설고 독특한 감각 경험 자체를 게임으로 만들고자 한 시도에서 출발했다.

### 목적
종, 비, 벽, 이명을 각기 다른 소리와 빛 패턴으로 구분하고, 머리 회전과 패드 위치/회전 입력을 결합하여 시야 차단 상태에서도 진행 가능한 감각 중심 플레이를 구현한다.

### 기술 반영
- ArUco 기반 패드 위치 추적
- 카메라 기반 패드 yaw + IMU 기반 pitch/roll 결합
- ESP32-S3 2대, MPU-9250 2개, WS LED 2장 사용
- 폐안 상태에서 구분이 쉬운 색상을 직접 선정 후 적용

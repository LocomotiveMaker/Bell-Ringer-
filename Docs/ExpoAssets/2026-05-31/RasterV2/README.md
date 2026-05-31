# Expo Raster Assets V2

Bell Ringer 3번째 슬라이드 보강용 고해상도 PNG 자산입니다.

## 색상 확인

- 종: `#05FF1F` ([BellRingerLightStyle.cs](C:/Bell Ringer/Assets/Scripts/Audio/BellRingerLightStyle.cs:8))
- 벽: `#05D9FF` ([BellRingerLightStyle.cs](C:/Bell Ringer/Assets/Scripts/Audio/BellRingerLightStyle.cs:9))
- 비: `#0514FF` ([BellRingerLightStyle.cs](C:/Bell Ringer/Assets/Scripts/Audio/BellRingerLightStyle.cs:10))
- 이명: `#750DFF` ([BellRingerLightStyle.cs](C:/Bell Ringer/Assets/Scripts/Audio/BellRingerLightStyle.cs:11))
- 패드: 런타임 기준 `#FF4705` (기획 표기는 오렌지골드로 유지 가능)

## 출력 폴더

- PNG: `C:\Bell Ringer\Docs\ExpoAssets\2026-05-31\RasterV2\png`
- SVG source: `C:\Bell Ringer\Docs\ExpoAssets\2026-05-31\RasterV2\svg-source`

## 섹션 2 권장 문구

- 배경: 시각 보조를 목표로 하기보다, 눈을 감은 채 공간을 탐색할 때 생기는 낯설고 독특한 감각 규칙 자체를 게임으로 만들고자 했다.
- 목적: 소리, 빛, 진동, 머리 회전, 패드 위치·회전을 하나의 감각 루프로 묶어 시야 차단 상태에서도 진행 가능한 플레이 구조를 구현한다.
- 구현 포인트: ArUco 기반 패드 위치, 카메라 yaw + IMU pitch/roll, ESP32-S3 x2, MPU-9250 x2, WS LED x2, 폐안 상태에서 구별 쉬운 빛 색 직접 선정.
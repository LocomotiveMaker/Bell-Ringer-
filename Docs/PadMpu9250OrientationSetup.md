# Pad MPU9250 Orientation Setup

이 단계의 목표는 `pad Uno + MPU9250`가 `yaw/pitch/roll`을 계속 내보내고, Unity가 그 값을 실시간으로 읽는 것입니다.

## 1. 업로드할 스케치

이번 단계에서는 이 스케치를 사용합니다.

- [PadMpu9250Orientation.ino](</C:/Bell Ringer/Arduino/PadMpu9250Orientation/PadMpu9250Orientation.ino>)

이전 raw bring-up 스케치와는 다릅니다.

## 2. 배선

배선은 bring-up 때와 같습니다.

| Uno | MPU9250 |
|---|---|
| `3.3V` | `VCC` 또는 `3V3` |
| `GND` | `GND` |
| `A4` 또는 `SDA` | `SDA` |
| `A5` 또는 `SCL` | `SCL` |
| `GND` | `AD0` 또는 `SDO` |
| `3.3V` | `CS` / `NCS` / `CSB` / `NSS` |

지금은 `INT`, `FSYNC`, `AUX_DA`, `AUX_CL`은 연결하지 않으셔도 됩니다.

## 3. 업로드 직후 주의

전원을 넣거나 업로드 직후 약 `1.5~2초` 동안은 패드를 최대한 가만히 두셔야 합니다.

이 시간에 자이로 바이어스를 잡습니다.

움직이면:

- yaw 드리프트가 커질 수 있고
- pitch/roll 안정성이 떨어질 수 있습니다.

## 4. 시리얼 모니터에서 성공 기준

`115200 baud`로 열면 이런 흐름이 보여야 합니다.

```text
[PadMPU] orientation start
[PadMPU] Found candidate at 0x68 WHO_AM_I=0x71
[PadMPU] Keep the pad still. Calibrating gyro bias...
[PadMPU] gyro bias dps x=...
[PadMPU] streaming wy/wp/wr at 100 Hz
wy=0.00,wp=0.00,wr=0.00,btn=0
wy=1.25,wp=-4.81,wr=2.33,btn=0
```

핵심은 마지막 줄처럼 `wy/wp/wr`가 계속 나오는 것입니다.

## 5. recenter 방법

현재 자세를 0도로 다시 잡으려면 둘 중 하나를 쓰시면 됩니다.

1. 시리얼 모니터 입력창에 `r` 또는 `recenter`를 보내기
2. Unity `PadTrackingTest` 씬에서 `Recenter IMU` 버튼 누르기

recenter는:

- 현재 yaw를 0으로 만들고
- 현재 pitch를 0으로 만들고
- 현재 roll을 0으로 만듭니다.

즉, 사용자가 원하는 `중립 자세`를 다시 기준으로 잡는 기능입니다.

## 6. Unity에서 볼 것

테스트 씬:

- [PadTrackingTest.unity](</C:/Bell Ringer/Assets/Scenes/PadTrackingTest.unity>)

플레이 후 좌상단 오버레이에서 아래를 보시면 됩니다.

- `IMU port`
- `connected`
- `fresh`
- `IMU yaw/pitch/roll`
- `IMU last line`

그리고 패드 고스트 앞쪽의 작은 구가 회전 방향 확인용입니다.

## 7. 포트가 잘못 잡히면

패드 Uno와 LED 보드가 동시에 연결되어 있으면 COM 포트가 2개 이상 보일 수 있습니다.

현재 코드는:

- 먼저 사용자가 지정한 포트를 우선 사용하고
- 지정이 없으면 LED 보드 포트를 피해서 다른 COM을 먼저 고르려 합니다.

그래도 잘못 잡히면 `PadImuReceiver`의 `preferredPortName`을 Inspector에서 직접 지정하시면 됩니다.

환경 변수로도 지정할 수 있습니다.

- `BELL_RINGER_PAD_IMU_PORT`
- `BELL_RINGER_PAD_IMU_BAUD`

## 8. 아직 정상은 아닌 경우

이 단계에서 다음 중 하나가 보이면 알려주시면 됩니다.

1. `Found candidate`가 안 뜬다
2. `wy/wp/wr`는 뜨지만 값이 너무 튄다
3. Unity에서 `connected`는 true인데 `fresh`가 false다
4. yaw/pitch/roll 중 축 방향이 직관과 반대로 보인다

그 경우 다음 단계에서:

- 축 부호 조정
- 회전 매핑 조정
- drift 보정

순서로 바로 맞추면 됩니다.

from pathlib import Path


ROOT = Path(r"C:\Bell Ringer")
OUT = ROOT / "Docs" / "ExpoAssets" / "2026-05-31"

FONT_STACK = "'Pretendard','Paperlogy','Malgun Gothic','Apple SD Gothic Neo',Arial,sans-serif"

COLORS = {
    "template_green": "#19B39E",
    "template_dark": "#0E6F66",
    "ink": "#14252B",
    "muted": "#5B6B73",
    "line": "#D6E5E4",
    "paper": "#FFFFFF",
    "paper_alt": "#F7FBFB",
    "soft_green": "#EAFBF3",
    "soft_cyan": "#E8FAFF",
    "soft_blue": "#EDF1FF",
    "soft_violet": "#F4EEFF",
    "soft_orange": "#FFF2EA",
    # Confirmed from code / docs.
    "bell": "#05FF1F",
    "wall": "#05D9FF",
    "rain": "#0514FF",
    "tinnitus": "#750DFF",
    "pad": "#FF4705",
    "danger": "#D43B4F",
    "forest": "#2D8A55",
}


def svg_doc(width: int, height: int, body: str) -> str:
    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}" fill="none">
  <defs>
    <style>
      .title {{ font-family: {FONT_STACK}; font-size: 82px; font-weight: 800; fill: {COLORS["ink"]}; }}
      .h2 {{ font-family: {FONT_STACK}; font-size: 54px; font-weight: 800; fill: {COLORS["ink"]}; }}
      .h3 {{ font-family: {FONT_STACK}; font-size: 40px; font-weight: 800; fill: {COLORS["ink"]}; }}
      .body {{ font-family: {FONT_STACK}; font-size: 34px; font-weight: 600; fill: {COLORS["ink"]}; }}
      .small {{ font-family: {FONT_STACK}; font-size: 28px; font-weight: 600; fill: {COLORS["muted"]}; }}
      .tiny {{ font-family: {FONT_STACK}; font-size: 24px; font-weight: 600; fill: {COLORS["muted"]}; }}
      .num {{ font-family: {FONT_STACK}; font-size: 34px; font-weight: 800; fill: white; }}
    </style>
  </defs>
  {body}
</svg>
"""


def write(name: str, width: int, height: int, body: str) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / name).write_text(svg_doc(width, height, body), encoding="utf-8")


def rounded_panel(x, y, w, h, radius=48, fill=None, stroke=None, stroke_width=4):
    fill = fill or COLORS["paper"]
    stroke = stroke or COLORS["line"]
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{radius}" fill="{fill}" stroke="{stroke}" stroke-width="{stroke_width}"/>'


def badge(x, y, text, fill):
    return (
        f'<circle cx="{x}" cy="{y}" r="34" fill="{fill}"/>'
        f'<text x="{x}" y="{y + 12}" text-anchor="middle" class="num">{text}</text>'
    )


def arrow(x1, y1, x2, y2, color="#7AA6A2", width=14):
    return f"""
    <path d="M {x1} {y1} L {x2} {y2}" stroke="{color}" stroke-width="{width}" stroke-linecap="round"/>
    <path d="M {x2 - 42} {y2 - 28} L {x2} {y2} L {x2 - 42} {y2 + 28}" stroke="{color}" stroke-width="{width}" stroke-linecap="round" stroke-linejoin="round"/>
    """


def icon_closed_eye(cx, cy, scale=1.0, color=None):
    color = color or COLORS["template_green"]
    s = scale
    return f"""
    <g stroke="{color}" stroke-width="{22 * s}" stroke-linecap="round" stroke-linejoin="round">
      <path d="M {cx - 190 * s} {cy} C {cx - 110 * s} {cy - 80 * s}, {cx + 110 * s} {cy - 80 * s}, {cx + 190 * s} {cy}" />
      <path d="M {cx - 160 * s} {cy + 34 * s} C {cx - 95 * s} {cy + 88 * s}, {cx + 95 * s} {cy + 88 * s}, {cx + 160 * s} {cy + 34 * s}" />
      <path d="M {cx - 92 * s} {cy - 122 * s} L {cx - 122 * s} {cy - 162 * s}" />
      <path d="M {cx - 22 * s} {cy - 140 * s} L {cx - 22 * s} {cy - 194 * s}" />
      <path d="M {cx + 92 * s} {cy - 122 * s} L {cx + 122 * s} {cy - 162 * s}" />
    </g>
    """


def icon_bell(cx, cy, scale=1.0, color=None):
    color = color or COLORS["bell"]
    s = scale
    return f"""
    <g stroke="{color}" stroke-width="{22 * s}" stroke-linecap="round" stroke-linejoin="round">
      <path d="M {cx - 140 * s} {cy + 86 * s}
               C {cx - 140 * s} {cy - 30 * s}, {cx - 86 * s} {cy - 156 * s}, {cx} {cy - 184 * s}
               C {cx + 86 * s} {cy - 156 * s}, {cx + 140 * s} {cy - 30 * s}, {cx + 140 * s} {cy + 86 * s}" />
      <path d="M {cx - 178 * s} {cy + 102 * s} H {cx + 178 * s}" />
      <path d="M {cx - 68 * s} {cy + 102 * s} C {cx - 28 * s} {cy + 162 * s}, {cx + 28 * s} {cy + 162 * s}, {cx + 68 * s} {cy + 102 * s}" />
      <circle cx="{cx}" cy="{cy + 130 * s}" r="{16 * s}" fill="{color}" stroke="none"/>
      <path d="M {cx - 242 * s} {cy - 42 * s} C {cx - 308 * s} {cy + 8 * s}, {cx - 308 * s} {cy + 114 * s}, {cx - 242 * s} {cy + 164 * s}" />
      <path d="M {cx + 242 * s} {cy - 42 * s} C {cx + 308 * s} {cy + 8 * s}, {cx + 308 * s} {cy + 114 * s}, {cx + 242 * s} {cy + 164 * s}" />
    </g>
    """


def icon_rain(cx, cy, scale=1.0, color=None):
    color = color or COLORS["rain"]
    s = scale
    return f"""
    <g stroke="{color}" stroke-width="{22 * s}" stroke-linecap="round" stroke-linejoin="round">
      <path d="M {cx - 168 * s} {cy + 24 * s}
               C {cx - 202 * s} {cy - 60 * s}, {cx - 126 * s} {cy - 144 * s}, {cx - 30 * s} {cy - 126 * s}
               C {cx + 2 * s} {cy - 204 * s}, {cx + 112 * s} {cy - 228 * s}, {cx + 188 * s} {cy - 160 * s}
               C {cx + 272 * s} {cy - 156 * s}, {cx + 316 * s} {cy - 62 * s}, {cx + 262 * s} {cy + 20 * s}
               Z" />
      <path d="M {cx - 182 * s} {cy + 156 * s} L {cx - 118 * s} {cy + 258 * s}" />
      <path d="M {cx - 32 * s} {cy + 156 * s} L {cx + 32 * s} {cy + 258 * s}" />
      <path d="M {cx + 118 * s} {cy + 156 * s} L {cx + 182 * s} {cy + 258 * s}" />
      <path d="M {cx - 228 * s} {cy + 304 * s} C {cx - 168 * s} {cy + 278 * s}, {cx - 96 * s} {cy + 278 * s}, {cx - 36 * s} {cy + 304 * s}" />
      <path d="M {cx + 38 * s} {cy + 304 * s} C {cx + 98 * s} {cy + 278 * s}, {cx + 170 * s} {cy + 278 * s}, {cx + 230 * s} {cy + 304 * s}" />
    </g>
    """


def icon_bell_gaze(cx, cy, scale=1.0):
    s = scale
    return f"""
    <g stroke-linecap="round" stroke-linejoin="round">
      <circle cx="{cx}" cy="{cy}" r="{230 * s}" stroke="{COLORS["line"]}" stroke-width="{18 * s}" fill="none"/>
      <circle cx="{cx}" cy="{cy}" r="{136 * s}" stroke="{COLORS["template_green"]}" stroke-width="{12 * s}" fill="none" opacity="0.35"/>
      <path d="M {cx - 316 * s} {cy - 210 * s} C {cx - 210 * s} {cy - 290 * s}, {cx - 96 * s} {cy - 264 * s}, {cx - 4 * s} {cy - 166 * s}" stroke="{COLORS["bell"]}" stroke-width="{18 * s}" fill="none" stroke-dasharray="{28 * s} {24 * s}"/>
      <path d="M {cx + 316 * s} {cy + 206 * s} C {cx + 202 * s} {cy + 272 * s}, {cx + 82 * s} {cy + 248 * s}, {cx + 2 * s} {cy + 164 * s}" stroke="{COLORS["bell"]}" stroke-width="{18 * s}" fill="none" stroke-dasharray="{28 * s} {24 * s}"/>
      {icon_bell(cx, cy, 0.54 * s, COLORS["bell"])}
      <path d="M {cx - 326 * s} {cy - 186 * s} L {cx - 254 * s} {cy - 186 * s}" stroke="{COLORS["bell"]}" stroke-width="{16 * s}"/>
      <path d="M {cx + 258 * s} {cy + 186 * s} L {cx + 330 * s} {cy + 186 * s}" stroke="{COLORS["bell"]}" stroke-width="{16 * s}"/>
    </g>
    """


def icon_tinnitus(cx, cy, scale=1.0, boss=False):
    s = scale
    core = COLORS["tinnitus"]
    accent = COLORS["danger"] if boss else COLORS["tinnitus"]
    burst = 312 if boss else 258
    stroke = 20 if boss else 18
    wave_scale = 1.15 if boss else 1.0
    return f"""
    <g stroke-linecap="round" stroke-linejoin="round">
      <circle cx="{cx}" cy="{cy}" r="{burst * s}" fill="{COLORS["soft_violet"] if not boss else '#FFF0F3'}"/>
      <circle cx="{cx}" cy="{cy}" r="{burst * s}" stroke="{accent}" stroke-width="{stroke * s}" fill="none"/>
      <circle cx="{cx}" cy="{cy}" r="{188 * s}" stroke="{core}" stroke-width="{12 * s}" fill="none" opacity="0.38"/>
      <path d="M {cx - 232 * s} {cy}
               C {cx - 192 * s} {cy}, {cx - 178 * s} {cy - 92 * s * wave_scale}, {cx - 126 * s} {cy - 92 * s * wave_scale}
               C {cx - 74 * s} {cy - 92 * s * wave_scale}, {cx - 82 * s} {cy + 112 * s * wave_scale}, {cx - 18 * s} {cy + 112 * s * wave_scale}
               C {cx + 42 * s} {cy + 112 * s * wave_scale}, {cx + 30 * s} {cy - 118 * s * wave_scale}, {cx + 92 * s} {cy - 118 * s * wave_scale}
               C {cx + 146 * s} {cy - 118 * s * wave_scale}, {cx + 154 * s} {cy}, {cx + 228 * s} {cy}" stroke="{core}" stroke-width="{24 * s}" fill="none"/>
      <path d="M {cx - 322 * s} {cy - 134 * s} L {cx - 254 * s} {cy - 186 * s}" stroke="{accent}" stroke-width="{16 * s}" opacity="{0.9 if boss else 0.55}"/>
      <path d="M {cx + 258 * s} {cy + 184 * s} L {cx + 326 * s} {cy + 134 * s}" stroke="{accent}" stroke-width="{16 * s}" opacity="{0.9 if boss else 0.55}"/>
      {"<path d='M 512 142 L 560 236 L 666 236 L 588 306 L 618 412 L 512 354 L 406 412 L 436 306 L 358 236 L 464 236 Z' fill='#D43B4F' opacity='0.18'/>" if boss else ""}
    </g>
    """


def icon_forest(cx, cy, scale=1.0):
    s = scale
    return f"""
    <g stroke-linecap="round" stroke-linejoin="round">
      <path d="M {cx - 210 * s} {cy + 230 * s} L {cx - 210 * s} {cy - 40 * s}" stroke="{COLORS["forest"]}" stroke-width="{18 * s}"/>
      <path d="M {cx} {cy + 250 * s} L {cx} {cy - 130 * s}" stroke="{COLORS["forest"]}" stroke-width="{18 * s}"/>
      <path d="M {cx + 216 * s} {cy + 220 * s} L {cx + 216 * s} {cy - 20 * s}" stroke="{COLORS["forest"]}" stroke-width="{18 * s}"/>
      <path d="M {cx - 326 * s} {cy + 30 * s} L {cx - 210 * s} {cy - 210 * s} L {cx - 94 * s} {cy + 30 * s} Z" fill="{COLORS["soft_green"]}" stroke="{COLORS["forest"]}" stroke-width="{16 * s}"/>
      <path d="M {cx - 132 * s} {cy - 42 * s} L {cx} {cy - 286 * s} L {cx + 132 * s} {cy - 42 * s} Z" fill="{COLORS["soft_green"]}" stroke="{COLORS["forest"]}" stroke-width="{16 * s}"/>
      <path d="M {cx + 102 * s} {cy + 38 * s} L {cx + 216 * s} {cy - 168 * s} L {cx + 330 * s} {cy + 38 * s} Z" fill="{COLORS["soft_green"]}" stroke="{COLORS["forest"]}" stroke-width="{16 * s}"/>
      <path d="M {cx - 58 * s} {cy + 334 * s}
               C {cx - 22 * s} {cy + 236 * s}, {cx + 76 * s} {cy + 156 * s}, {cx + 176 * s} {cy + 122 * s}"
            stroke="{COLORS["template_dark"]}" stroke-width="{24 * s}" fill="none"/>
    </g>
    """


def icon_wall_blocks(cx, cy, scale=1.0, color=None):
    color = color or COLORS["wall"]
    s = scale
    return f"""
    <g stroke="{color}" stroke-width="{14 * s}" stroke-linejoin="round" stroke-linecap="round" fill="none">
      <rect x="{cx - 150 * s}" y="{cy - 82 * s}" width="{92 * s}" height="{68 * s}" rx="{12 * s}" />
      <rect x="{cx - 44 * s}" y="{cy - 82 * s}" width="{92 * s}" height="{68 * s}" rx="{12 * s}" />
      <rect x="{cx + 62 * s}" y="{cy - 82 * s}" width="{92 * s}" height="{68 * s}" rx="{12 * s}" />
      <rect x="{cx - 96 * s}" y="{cy + 6 * s}" width="{92 * s}" height="{68 * s}" rx="{12 * s}" />
      <rect x="{cx + 10 * s}" y="{cy + 6 * s}" width="{92 * s}" height="{68 * s}" rx="{12 * s}" />
    </g>
    """


def section1_assets():
    write(
        "S1_closed_eye_callout_horizontal.svg",
        2400,
        700,
        f"""
        <rect x="18" y="18" width="2364" height="664" rx="64" fill="{COLORS["paper"]}" stroke="{COLORS["template_green"]}" stroke-width="10"/>
        <rect x="72" y="72" width="538" height="556" rx="42" fill="{COLORS["soft_green"]}" />
        {icon_closed_eye(341, 332, 0.88, COLORS["template_green"])}
        <text x="690" y="262" class="title" fill="{COLORS["template_dark"]}">이 게임은</text>
        <text x="690" y="382" class="title" fill="{COLORS["template_dark"]}">눈 감고 플레이합니다!</text>
        <text x="690" y="510" class="body">시야를 차단한 상태에서 소리, 빛, 진동 단서를 따라 이동합니다.</text>
        <text x="690" y="568" class="small">관람용 화면과 실제 플레이 감각은 분리되어 있습니다.</text>
        <circle cx="2200" cy="192" r="20" fill="{COLORS["bell"]}"/>
        <circle cx="2254" cy="192" r="20" fill="{COLORS["wall"]}"/>
        <circle cx="2308" cy="192" r="20" fill="{COLORS["rain"]}"/>
        """,
    )
    write(
        "S1_closed_eye_callout_vertical.svg",
        900,
        1800,
        f"""
        <rect x="18" y="18" width="864" height="1764" rx="68" fill="{COLORS["paper"]}" stroke="{COLORS["template_green"]}" stroke-width="10"/>
        <rect x="96" y="88" width="708" height="580" rx="56" fill="{COLORS["soft_green"]}" />
        {icon_closed_eye(450, 370, 1.02, COLORS["template_green"])}
        <text x="450" y="860" text-anchor="middle" class="title">이 게임은</text>
        <text x="450" y="980" text-anchor="middle" class="title">눈 감고</text>
        <text x="450" y="1100" text-anchor="middle" class="title">플레이합니다!</text>
        <text x="450" y="1276" text-anchor="middle" class="body">청각 · 빛 · 진동 단서</text>
        <text x="450" y="1338" text-anchor="middle" class="body">중심의 감각 플레이</text>
        <text x="450" y="1498" text-anchor="middle" class="small">실제 판단은 눈앞 LED와 공간 음향,</text>
        <text x="450" y="1552" text-anchor="middle" class="small">패드 위치/회전, 진동으로 이루어집니다.</text>
        """,
    )


def section2_assets():
    write(
        "S2_background_purpose_panel.svg",
        2200,
        1100,
        f"""
        {rounded_panel(24, 24, 2152, 1052, 56, COLORS["paper"], COLORS["line"], 6)}
        <text x="88" y="126" class="h2">배경 및 목적</text>
        <text x="88" y="198" class="small">시각을 줄였을 때 다른 감각이 전면으로 올라오는 과정을 게임으로 만들고자 했다.</text>
        {rounded_panel(88, 256, 956, 614, 42, COLORS["paper_alt"])}
        <text x="136" y="336" class="h3">출발점</text>
        {icon_closed_eye(306, 524, 0.48, COLORS["template_green"])}
        {arrow(452, 524, 594, 524, COLORS["template_green"], 12)}
        <g stroke="{COLORS["template_green"]}" stroke-width="14" stroke-linecap="round" stroke-linejoin="round">
          <path d="M 648 510 C 688 448, 764 448, 804 510" />
          <path d="M 648 538 C 688 600, 764 600, 804 538" />
          <path d="M 714 478 L 714 572" />
        </g>
        <text x="136" y="702" class="body">눈을 감으면 방향과 존재를 확인하는 기준이</text>
        <text x="136" y="760" class="body">소리 · 빛 · 진동으로 이동한다.</text>
        <text x="136" y="830" class="small">이 프로젝트는 접근성 해결보다</text>
        <text x="136" y="878" class="small">재미있는 감각 전환 자체에서 출발했다.</text>
        {rounded_panel(1116, 256, 972, 614, 42, COLORS["paper_alt"])}
        <text x="1164" y="336" class="h3">구현 목표</text>
        <g>
          <circle cx="1280" cy="520" r="72" fill="{COLORS["soft_green"]}" stroke="{COLORS["bell"]}" stroke-width="10"/>
          <circle cx="1462" cy="520" r="72" fill="{COLORS["soft_blue"]}" stroke="{COLORS["rain"]}" stroke-width="10"/>
          <circle cx="1644" cy="520" r="72" fill="{COLORS["soft_cyan"]}" stroke="{COLORS["wall"]}" stroke-width="10"/>
          <circle cx="1826" cy="520" r="72" fill="{COLORS["soft_violet"]}" stroke="{COLORS["tinnitus"]}" stroke-width="10"/>
        </g>
        {icon_bell(1280, 520, 0.20, COLORS["bell"])}
        {icon_rain(1462, 512, 0.17, COLORS["rain"])}
        {icon_wall_blocks(1644, 522, 0.22, COLORS["wall"])}
        {icon_tinnitus(1826, 520, 0.18, False)}
        <text x="1196" y="708" class="body">종, 비, 벽, 이명을</text>
        <text x="1196" y="766" class="body">각기 다른 색과 패턴으로 구분하고,</text>
        <text x="1196" y="824" class="body">머리 회전과 패드 자세를 통해 상호작용한다.</text>
        <text x="1196" y="892" class="small">시선 차단 상태에서도 플레이 흐름을 이해할 수 있는 감각 체계를 목표로 한다.</text>
        {rounded_panel(88, 910, 2000, 112, 34, COLORS["soft_orange"], "#FFD7C3", 4)}
        <text x="132" y="980" class="body">핵심 문장</text>
        <text x="352" y="980" class="body">“보지 못해도 진행할 수 있는가”보다 “보지 않을 때 더 낯설고 재미있는가”에 초점을 맞췄다.</text>
        """,
    )
    write(
        "S2_tracking_pipeline.svg",
        2400,
        1180,
        f"""
        {rounded_panel(24, 24, 2352, 1132, 56, COLORS["paper"], COLORS["line"], 6)}
        <text x="88" y="126" class="h2">하드웨어 / 기술 구성</text>
        <text x="88" y="198" class="small">게임 진행에 필요한 위치, 회전, LED, 진동 피드백을 하나의 루프로 묶는다.</text>

        {rounded_panel(96, 280, 456, 340, 40, COLORS["paper_alt"])}
        <text x="142" y="360" class="h3">Head Unit</text>
        <text x="142" y="430" class="body">ESP32-S3</text>
        <text x="142" y="486" class="small">MPU-9250 IMU</text>
        <text x="142" y="538" class="small">WS LED x2</text>
        <text x="142" y="598" class="tiny">머리 회전 + LED 출력</text>
        <g stroke="{COLORS["template_green"]}" stroke-width="14" stroke-linecap="round" stroke-linejoin="round">
          <rect x="380" y="390" width="96" height="96" rx="20" />
          <path d="M 392 448 H 464" />
          <path d="M 428 412 V 484" />
          <circle cx="428" cy="548" r="18" fill="{COLORS["bell"]}" stroke="none"/>
          <circle cx="470" cy="548" r="18" fill="{COLORS["wall"]}" stroke="none"/>
        </g>

        {rounded_panel(644, 280, 456, 340, 40, COLORS["paper_alt"])}
        <text x="690" y="360" class="h3">Pad Unit</text>
        <text x="690" y="430" class="body">ESP32-S3</text>
        <text x="690" y="486" class="small">MPU-9250 IMU</text>
        <text x="690" y="538" class="small">Gamepad Rumble</text>
        <text x="690" y="598" class="tiny">pitch / roll + 진동 피드백</text>
        <g stroke="{COLORS["pad"]}" stroke-width="14" stroke-linecap="round" stroke-linejoin="round">
          <rect x="944" y="396" width="96" height="72" rx="24" />
          <circle cx="970" cy="432" r="8" fill="{COLORS["pad"]}" stroke="none"/>
          <circle cx="1012" cy="432" r="8" fill="{COLORS["pad"]}" stroke="none"/>
          <path d="M 960 518 C 980 566, 1000 566, 1020 518" />
        </g>

        {rounded_panel(1192, 280, 456, 340, 40, COLORS["paper_alt"])}
        <text x="1238" y="360" class="h3">AruCo Camera</text>
        <text x="1238" y="430" class="body">패드 위치 추정</text>
        <text x="1238" y="486" class="small">카메라 기반 yaw</text>
        <text x="1238" y="538" class="small">ArUco marker tracking</text>
        <text x="1238" y="598" class="tiny">position + yaw</text>
        <g stroke="{COLORS["ink"]}" stroke-width="12" stroke-linecap="round" stroke-linejoin="round">
          <rect x="1494" y="394" width="98" height="74" rx="14" />
          <rect x="1514" y="412" width="58" height="38" rx="4" />
          <path d="M 1524 422 H 1544 V 442 H 1524 Z M 1548 422 H 1568 V 442 H 1548 Z" />
        </g>

        {rounded_panel(1740, 280, 548, 340, 40, COLORS["paper_alt"])}
        <text x="1786" y="360" class="h3">Unity Runtime</text>
        <text x="1786" y="430" class="body">오디오 · 빛 · 진동 통합 제어</text>
        <text x="1786" y="486" class="small">Bell / Rain / Wall / Tinnitus</text>
        <text x="1786" y="538" class="small">게임 상태에 따라 cue / pattern / rumble 출력</text>
        <text x="1786" y="598" class="tiny">FinalDemo tuning + cue routing</text>

        {arrow(552, 452, 644, 452, COLORS["template_green"], 12)}
        {arrow(1100, 452, 1192, 452, COLORS["template_green"], 12)}
        {arrow(1648, 452, 1740, 452, COLORS["template_green"], 12)}

        {rounded_panel(96, 710, 2192, 376, 40, COLORS["paper_alt"])}
        <text x="142" y="784" class="h3">데이터 결합 규칙</text>
        <text x="142" y="854" class="body">패드 위치 = ArUco 카메라</text>
        <text x="142" y="912" class="body">패드 yaw = 카메라 우선</text>
        <text x="142" y="970" class="body">패드 pitch / roll = MPU-9250 IMU</text>
        <text x="142" y="1028" class="body">머리 회전 = Head MPU-9250 IMU</text>
        <text x="1160" y="854" class="body">LED 색 선정 = 폐안 상태 구분성 기준 직접 조정</text>
        <text x="1160" y="912" class="body">종 = 초록 / 벽 = 시안 / 비 = 딥 블루 / 이명 = 바이올렛 / 패드 = 오렌지</text>
        <text x="1160" y="970" class="body">진동 = 탐색, 근접, 정답 잠금, 보스 추적 상태 피드백</text>
        <text x="1160" y="1028" class="small">현재 코드 기준 패드 색은 노랑이 아니라 오렌지에 가깝게 설정되어 있다.</text>
        """,
    )


def section3_assets():
    write(
        "S3_stage_card_shell.svg",
        900,
        1120,
        f"""
        <rect x="22" y="22" width="856" height="1076" rx="56" fill="{COLORS["paper"]}" stroke="{COLORS["line"]}" stroke-width="8"/>
        <rect x="70" y="74" width="760" height="140" rx="34" fill="{COLORS["paper_alt"]}"/>
        <circle cx="146" cy="144" r="42" fill="{COLORS["template_green"]}"/>
        <rect x="70" y="930" width="760" height="106" rx="28" fill="{COLORS["paper_alt"]}"/>
        """,
    )
    write(
        "S3_flow_arrow.svg",
        320,
        120,
        arrow(30, 60, 286, 60, COLORS["template_green"], 14),
    )

    icons = {
        "S3_icon_prepare_closed_eye.svg": icon_closed_eye(512, 540, 1.05, COLORS["template_green"]),
        "S3_icon_bell_trace.svg": icon_bell(512, 532, 1.02, COLORS["bell"]),
        "S3_icon_rainstorm.svg": icon_rain(512, 486, 1.0, COLORS["rain"]),
        "S3_icon_bell_gaze.svg": icon_bell_gaze(512, 512, 0.94),
        "S3_icon_tinnitus_purify.svg": icon_tinnitus(512, 512, 0.92, False),
        "S3_icon_boss_tinnitus.svg": icon_tinnitus(512, 512, 0.92, True),
        "S3_icon_forest_ending.svg": icon_forest(512, 486, 0.98),
    }
    for name, icon_body in icons.items():
        write(name, 1024, 1024, icon_body)

    legend_data = [
        ("S3_legend_bell.svg", COLORS["bell"], "종(초록)", "방향 / 목표 위치"),
        ("S3_legend_rain.svg", COLORS["rain"], "비(딥 블루)", "환경 / 방해 정보"),
        ("S3_legend_tinnitus.svg", COLORS["tinnitus"], "이명(바이올렛)", "정화 대상 / 경고"),
        ("S3_legend_wall.svg", COLORS["wall"], "벽(시안)", "경계 / 접근 위험"),
        ("S3_legend_pad.svg", COLORS["pad"], "패드(오렌지)", "조작 / 진동 피드백"),
    ]
    for name, color, title, sub in legend_data:
        write(
            name,
            760,
            180,
            f"""
            <rect x="12" y="12" width="736" height="156" rx="30" fill="{COLORS["paper_alt"]}" stroke="{COLORS["line"]}" stroke-width="4"/>
            <circle cx="92" cy="90" r="32" fill="{color}"/>
            <text x="150" y="82" class="body">{title}</text>
            <text x="150" y="122" class="small">{sub}</text>
            """,
        )

    write(
        "S3_tech_pipeline.svg",
        2400,
        560,
        f"""
        {rounded_panel(20, 20, 2360, 520, 46, COLORS["paper"], COLORS["line"], 6)}
        <text x="72" y="104" class="h3">기술 동작 흐름</text>
        {rounded_panel(72, 150, 360, 300, 34, COLORS["paper_alt"])}
        <text x="112" y="222" class="body">Head IMU</text>
        <text x="112" y="276" class="small">머리 yaw / pitch / roll</text>
        <text x="112" y="332" class="small">시선 방향 입력</text>
        {rounded_panel(500, 150, 360, 300, 34, COLORS["paper_alt"])}
        <text x="540" y="222" class="body">Pad IMU</text>
        <text x="540" y="276" class="small">pitch / roll</text>
        <text x="540" y="332" class="small">자세 입력</text>
        {rounded_panel(928, 150, 360, 300, 34, COLORS["paper_alt"])}
        <text x="968" y="222" class="body">AruCo Camera</text>
        <text x="968" y="276" class="small">패드 위치 + yaw</text>
        <text x="968" y="332" class="small">카메라 추적</text>
        {rounded_panel(1356, 150, 360, 300, 34, COLORS["paper_alt"])}
        <text x="1396" y="222" class="body">Unity Logic</text>
        <text x="1396" y="276" class="small">상태 / 거리 / 정답 판정</text>
        <text x="1396" y="332" class="small">종 · 이명 · 보스 제어</text>
        {rounded_panel(1784, 150, 524, 300, 34, COLORS["paper_alt"])}
        <text x="1824" y="222" class="body">출력 피드백</text>
        <text x="1824" y="276" class="small">공간 음향 / LED 패턴 / 진동</text>
        <text x="1824" y="332" class="small">플레이어 감각으로 환원</text>
        {arrow(432, 300, 500, 300, COLORS["template_green"], 12)}
        {arrow(860, 300, 928, 300, COLORS["template_green"], 12)}
        {arrow(1288, 300, 1356, 300, COLORS["template_green"], 12)}
        {arrow(1716, 300, 1784, 300, COLORS["template_green"], 12)}
        """,
    )


def write_readme():
    readme = f"""# Expo Assets 2026-05-31

이 폴더의 자산은 `실피컴_2026 MD_EXPO_B1.pptx` 3번째 슬라이드 보강용으로 만든 분리 SVG 자산입니다.

## 코드 기준 색상 확인

- 종: `{COLORS["bell"]}` (`BellGreen`)
- 벽: `{COLORS["wall"]}` (`WallCyan`)
- 비: `{COLORS["rain"]}` (`RainDeepBlue`)
- 이명: `{COLORS["tinnitus"]}` (`TinnitusViolet`)
- 패드: `{COLORS["pad"]}` (`PadOrange`)

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
"""
    (OUT / "README.md").write_text(readme, encoding="utf-8")


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    section1_assets()
    section2_assets()
    section3_assets()
    write_readme()
    print(f"Wrote assets to {OUT}")


if __name__ == "__main__":
    main()

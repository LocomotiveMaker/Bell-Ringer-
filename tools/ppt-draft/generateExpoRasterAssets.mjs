import fs from "node:fs";
import path from "node:path";
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, "..", "..");
const require = createRequire(import.meta.url);
const { Resvg } = require(path.join(__dirname, "node_modules", "@resvg", "resvg-js"));

const outRoot = path.join(repoRoot, "Docs", "ExpoAssets", "2026-05-31", "RasterV2");
const pngDir = path.join(outRoot, "png");
const svgDir = path.join(outRoot, "svg-source");
fs.mkdirSync(pngDir, { recursive: true });
fs.mkdirSync(svgDir, { recursive: true });

const C = {
  ink: "#17323B",
  body: "#26424B",
  muted: "#667B85",
  line: "#D7E3E8",
  soft: "#F4F8FA",
  panel: "#FFFFFF",
  panelAlt: "#F7FBFC",
  dark: "#132128",
  accent: "#1DAF88",
  bell: "#05FF1F",
  wall: "#05D9FF",
  rain: "#0514FF",
  tinnitus: "#750DFF",
  pad: "#FF4705",
  padGold: "#F2B733",
  padSoft: "#FFF3D8",
  dangerSoft: "#F4EEFF",
  bellSoft: "#EAFBF0",
  wallSoft: "#E7FAFF",
  rainSoft: "#EAF0FF",
  rainDeepSoft: "#DCE6FF",
  cyanSoft: "#DFF7FB"
};

const FONT = "'Malgun Gothic','Pretendard','Apple SD Gothic Neo',Arial,sans-serif";

const assets = [
  { name: "S1_closed_eye_callout_horizontal.png", svg: buildS1Horizontal(), width: 3000 },
  { name: "S1_closed_eye_callout_vertical.png", svg: buildS1Vertical(), width: 1600 },
  { name: "S2_background_purpose_panel_v2.png", svg: buildS2Panel(), width: 3200 },
  { name: "S2_tracking_pipeline_v2.png", svg: buildS2Pipeline(), width: 3400 },
  { name: "S3_stage_card_shell_v2.png", svg: buildStageCardShell(), width: 1200 },
  { name: "S3_flow_arrow_v2.png", svg: buildFlowArrow(), width: 600 },
  { name: "S3_stage_badge_01.png", svg: buildStageBadge(1, "#24B596"), width: 240 },
  { name: "S3_stage_badge_02.png", svg: buildStageBadge(2, "#1BE319"), width: 240 },
  { name: "S3_stage_badge_03.png", svg: buildStageBadge(3, "#2542FF"), width: 240 },
  { name: "S3_stage_badge_04.png", svg: buildStageBadge(4, "#F2B733"), width: 240 },
  { name: "S3_stage_badge_05.png", svg: buildStageBadge(5, "#28C8F1"), width: 240 },
  { name: "S3_stage_badge_06.png", svg: buildStageBadge(6, "#8B2AF5"), width: 240 },
  { name: "S3_stage_badge_07.png", svg: buildStageBadge(7, "#68B881"), width: 240 },
  { name: "S3_icon_prepare_closed_eye_v2.png", svg: buildIconPrepare(), width: 1800 },
  { name: "S3_icon_bell_trace_v2.png", svg: buildIconBellTrace(), width: 1800 },
  { name: "S3_icon_rainstorm_v2.png", svg: buildIconRainstorm(), width: 1800 },
  { name: "S3_icon_bell_gaze_v2.png", svg: buildIconBellGaze(), width: 1800 },
  { name: "S3_icon_tinnitus_purify_v2.png", svg: buildIconTinnitusPurify(), width: 1800 },
  { name: "S3_icon_boss_tinnitus_v2.png", svg: buildIconBossTinnitus(), width: 1800 },
  { name: "S3_icon_forest_ending_v2.png", svg: buildIconForestEnding(), width: 1800 },
  { name: "S3_legend_bell_v2.png", svg: buildLegend("종", "초록", C.bell), width: 900 },
  { name: "S3_legend_wall_v2.png", svg: buildLegend("벽", "시안", C.wall), width: 900 },
  { name: "S3_legend_rain_v2.png", svg: buildLegend("비", "딥 블루", C.rain), width: 900 },
  { name: "S3_legend_tinnitus_v2.png", svg: buildLegend("이명", "바이올렛", C.tinnitus), width: 900 },
  { name: "S3_legend_pad_v2.png", svg: buildLegend("패드", "오렌지골드", C.pad), width: 900 },
  { name: "S3_tech_pipeline_v2.png", svg: buildS3TechPipeline(), width: 3400 }
];

for (const asset of assets) {
  const svgPath = path.join(svgDir, asset.name.replace(/\.png$/i, ".svg"));
  const pngPath = path.join(pngDir, asset.name);
  fs.writeFileSync(svgPath, asset.svg, "utf8");
  const rendered = new Resvg(asset.svg, {
    fitTo: { mode: "width", value: asset.width }
  }).render();
  fs.writeFileSync(pngPath, rendered.asPng());
}

const manifestPath = path.join(outRoot, "README.md");
fs.writeFileSync(
  manifestPath,
  [
    "# Expo Raster Assets V2",
    "",
    "Bell Ringer 3번째 슬라이드 보강용 고해상도 PNG 자산입니다.",
    "",
    "## 색상 확인",
    "",
    `- 종: \`${C.bell}\` ([BellRingerLightStyle.cs](${path.join(repoRoot, "Assets", "Scripts", "Audio", "BellRingerLightStyle.cs").replace(/\\/g, "/")}:8))`,
    `- 벽: \`${C.wall}\` ([BellRingerLightStyle.cs](${path.join(repoRoot, "Assets", "Scripts", "Audio", "BellRingerLightStyle.cs").replace(/\\/g, "/")}:9))`,
    `- 비: \`${C.rain}\` ([BellRingerLightStyle.cs](${path.join(repoRoot, "Assets", "Scripts", "Audio", "BellRingerLightStyle.cs").replace(/\\/g, "/")}:10))`,
    `- 이명: \`${C.tinnitus}\` ([BellRingerLightStyle.cs](${path.join(repoRoot, "Assets", "Scripts", "Audio", "BellRingerLightStyle.cs").replace(/\\/g, "/")}:11))`,
    `- 패드: 런타임 기준 \`${C.pad}\` (기획 표기는 오렌지골드로 유지 가능)`,
    "",
    "## 출력 폴더",
    "",
    `- PNG: \`${pngDir}\``,
    `- SVG source: \`${svgDir}\``,
    "",
    "## 섹션 2 권장 문구",
    "",
    "- 배경: 시각 보조를 목표로 하기보다, 눈을 감은 채 공간을 탐색할 때 생기는 낯설고 독특한 감각 규칙 자체를 게임으로 만들고자 했다.",
    "- 목적: 소리, 빛, 진동, 머리 회전, 패드 위치·회전을 하나의 감각 루프로 묶어 시야 차단 상태에서도 진행 가능한 플레이 구조를 구현한다.",
    "- 구현 포인트: ArUco 기반 패드 위치, 카메라 yaw + IMU pitch/roll, ESP32-S3 x2, MPU-9250 x2, WS LED x2, 폐안 상태에서 구별 쉬운 빛 색 직접 선정."
  ].join("\n"),
  "utf8"
);

console.log(`Wrote ${assets.length} PNG assets to ${pngDir}`);
console.log(`Wrote manifest ${manifestPath}`);

function wrapSvg(width, height, inner) {
  return `<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" fill="none">
  <defs>
    <style>
      .title { font-family: ${FONT}; font-size: 90px; font-weight: 800; fill: ${C.ink}; }
      .h2 { font-family: ${FONT}; font-size: 62px; font-weight: 800; fill: ${C.ink}; }
      .h3 { font-family: ${FONT}; font-size: 44px; font-weight: 800; fill: ${C.ink}; }
      .body { font-family: ${FONT}; font-size: 34px; font-weight: 600; fill: ${C.body}; }
      .small { font-family: ${FONT}; font-size: 28px; font-weight: 600; fill: ${C.muted}; }
      .tiny { font-family: ${FONT}; font-size: 24px; font-weight: 600; fill: ${C.muted}; }
      .badge { font-family: ${FONT}; font-size: 40px; font-weight: 800; fill: white; text-anchor: middle; dominant-baseline: middle; }
      .mono { font-family: ${FONT}; font-size: 26px; font-weight: 700; fill: ${C.ink}; }
    </style>
    <filter id="softGlow" x="-40%" y="-40%" width="180%" height="180%">
      <feGaussianBlur stdDeviation="14" result="blur"/>
      <feMerge>
        <feMergeNode in="blur"/>
        <feMergeNode in="SourceGraphic"/>
      </feMerge>
    </filter>
  </defs>
  ${inner}
</svg>`;
}

function roundedPanel(x, y, width, height, radius = 36, fill = C.panelAlt, stroke = C.line, strokeWidth = 4) {
  return `<rect x="${x}" y="${y}" width="${width}" height="${height}" rx="${radius}" fill="${fill}" stroke="${stroke}" stroke-width="${strokeWidth}"/>`;
}

function bulletList(x, y, lines, accent = C.accent, lineHeight = 64, size = 32) {
  return lines.map((line, index) => {
    const cy = y + index * lineHeight;
    return `
      <circle cx="${x}" cy="${cy - 12}" r="10" fill="${accent}"/>
      <path d="M ${x - 4} ${cy - 12} L ${x - 1} ${cy - 7} L ${x + 6} ${cy - 18}" stroke="white" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>
      <text x="${x + 28}" y="${cy}" font-family=${quote(FONT)} font-size="${size}" font-weight="600" fill="${C.body}">${line}</text>`;
  }).join("");
}

function chip(x, y, width, height, label, color, sublabel = "") {
  return `
    <rect x="${x}" y="${y}" width="${width}" height="${height}" rx="28" fill="${C.panel}" stroke="${C.line}" stroke-width="3"/>
    <circle cx="${x + 42}" cy="${y + height / 2 - 8}" r="16" fill="${color}"/>
    <text x="${x + 74}" y="${y + height / 2 - 2}" font-family=${quote(FONT)} font-size="30" font-weight="800" fill="${C.ink}">${label}</text>
    ${sublabel ? `<text x="${x + 74}" y="${y + height / 2 + 34}" font-family=${quote(FONT)} font-size="22" font-weight="700" fill="${C.muted}">${sublabel}</text>` : ""}
  `;
}

function buildS1Horizontal() {
  return wrapSvg(
    2400,
    760,
    `
    <rect width="2400" height="760" rx="56" fill="#F7FBFC"/>
    <rect x="28" y="28" width="2344" height="704" rx="52" fill="${C.panel}" stroke="${C.line}" stroke-width="6"/>
    <rect x="78" y="108" width="436" height="544" rx="44" fill="${C.bellSoft}" stroke="#BFEFCA" stroke-width="4"/>
    ${iconClosedEyeAbstract(296, 382, 1.05)}
    <text x="600" y="256" class="title">이 게임은 눈을 감고 플레이합니다!</text>
    <text x="600" y="354" class="body">플레이 판단은 화면이 아니라 소리, 눈앞 LED, 진동, 머리 회전, 패드 위치·회전으로 이루어집니다.</text>
    <text x="600" y="420" class="body">시야를 막는 순간부터 종·비·벽·이명의 단서를 다른 감각으로 구별해야 합니다.</text>
    ${chip(600, 492, 300, 118, "소리", C.accent, "방향·거리 단서")}
    ${chip(928, 492, 300, 118, "빛", C.bell, "폐안 상태 시각 단서")}
    ${chip(1256, 492, 300, 118, "진동", C.padGold, "정답·정화 피드백")}
    ${chip(1584, 492, 300, 118, "회전", C.wall, "머리·패드 입력")}
    `
  );
}

function buildS1Vertical() {
  return wrapSvg(
    1100,
    1800,
    `
    <rect width="1100" height="1800" rx="56" fill="#F7FBFC"/>
    <rect x="28" y="28" width="1044" height="1744" rx="52" fill="${C.panel}" stroke="${C.line}" stroke-width="6"/>
    <rect x="92" y="96" width="916" height="560" rx="50" fill="${C.bellSoft}" stroke="#BFEFCA" stroke-width="4"/>
    ${iconClosedEyeAbstract(550, 374, 1.25)}
    <text x="110" y="802" class="h2">이 게임은</text>
    <text x="110" y="884" class="h2">눈을 감고 플레이합니다!</text>
    <text x="110" y="1000" class="body">플레이 판단은 소리, 빛, 진동,</text>
    <text x="110" y="1058" class="body">머리 회전, 패드 위치·회전으로 이루어집니다.</text>
    <text x="110" y="1168" class="small">시야 대신 감각 규칙을 배우며 진행하는 구조입니다.</text>
    ${chip(110, 1260, 404, 116, "소리", C.accent, "3D 방향 단서")}
    ${chip(566, 1260, 404, 116, "빛", C.bell, "폐안 상태 단서")}
    ${chip(110, 1412, 404, 116, "진동", C.padGold, "정답 피드백")}
    ${chip(566, 1412, 404, 116, "회전", C.wall, "머리·패드 입력")}
    `
  );
}

function buildS2Panel() {
  return wrapSvg(
    2600,
    1460,
    `
    ${roundedPanel(24, 24, 2552, 1412, 56, C.panel, C.line, 6)}
    <text x="96" y="136" class="h2">배경 및 목적</text>
    <text x="96" y="206" class="small">시각 보조를 목표로 하기보다, 시야를 막았을 때 생기는 감각 규칙 자체를 게임으로 만들고자 했다.</text>

    ${roundedPanel(96, 274, 1140, 858, 42, C.panelAlt)}
    <text x="154" y="356" class="h3">배경</text>
    ${iconClosedEyeMinimal(1040, 368, 0.95)}
    ${bulletList(154, 446, [
      "시각이 거의 사라졌을 때 방향감과 존재감을 무엇으로 대체할 수 있는지에서 시작했다.",
      "시각 보조보다, 눈을 감은 채 공간을 탐색할 때 생기는 낯선 감각 경험 자체에 집중했다.",
      "소리와 빛이 연결된 단서가 되고, 플레이어는 그 연결을 학습하며 게임을 진행한다."
    ], C.accent, 86, 28)}
    <rect x="154" y="910" width="1020" height="170" rx="28" fill="${C.cyanSoft}" stroke="#C7EAF2" stroke-width="3"/>
    <text x="196" y="980" class="body">핵심 질문</text>
    <text x="196" y="1040" class="small">보는 행위를 거의 제거해도, 소리·빛·진동만으로 감각 규칙을 만들 수 있는가?</text>

    ${roundedPanel(1368, 274, 1136, 858, 42, C.panelAlt)}
    <text x="1426" y="356" class="h3">목적과 구현 포인트</text>
    ${bulletList(1426, 446, [
      "ArUco 기반 패드 위치 추적",
      "카메라 pose yaw + IMU pitch/roll 결합",
      "ESP32-S3 x2 / MPU-9250 x2 / WS LED x2",
      "머리 회전, 패드 위치·회전, LED, 진동을 하나의 루프로 통합",
      "폐안 상태에서 구별이 쉬운 빛 색을 직접 선정해 적용"
    ], C.bell, 78, 28)}
    ${chip(1436, 914, 188, 110, "종", C.bell, "초록")}
    ${chip(1646, 914, 188, 110, "벽", C.wall, "시안")}
    ${chip(1856, 914, 188, 110, "비", C.rain, "딥 블루")}
    ${chip(2066, 914, 188, 110, "이명", C.tinnitus, "바이올렛")}
    ${chip(1538, 1040, 614, 110, "패드", C.pad, "런타임 기준 오렌지골드")}
    `
  );
}

function buildS2Pipeline() {
  const nodes = [
    { x: 78, title: "Head Module", line1: "ESP32-S3 + WS LED", line2: "Head IMU / 머리 회전", color: C.accent, icon: iconHeadModule(268, 236, 0.9) },
    { x: 612, title: "Pad Module", line1: "ESP32-S3 + MPU-9250", line2: "Pad pitch / roll", color: C.padGold, icon: iconPadModule(802, 236, 0.9) },
    { x: 1146, title: "ArUco Camera", line1: "Pad position + yaw", line2: "카메라 기반 pose", color: C.wall, icon: iconArucoCamera(1336, 236, 0.9) },
    { x: 1680, title: "Pose Fusion", line1: "Head + Pad 통합", line2: "게임 상태 판정", color: C.tinnitus, icon: iconPoseFusion(1870, 236, 0.9) },
    { x: 2214, title: "Feedback Loop", line1: "3D Sound / LED", line2: "Vibration / Observer", color: C.bell, icon: iconFeedbackLoop(2404, 236, 0.9) }
  ];
  return wrapSvg(
    2760,
    720,
    `
    ${roundedPanel(24, 24, 2712, 672, 50, C.panel, C.line, 6)}
    <text x="84" y="124" class="h3">하드웨어 / 동작 흐름</text>
    <text x="84" y="178" class="small">ArUco 위치, IMU 회전, LED, 진동, 공간 음향이 하나의 루프로 연결됩니다.</text>
    ${nodes.map((node, index) => `
      ${roundedPanel(node.x, 224, 380, 380, 36, C.panelAlt)}
      <rect x="${node.x + 32}" y="260" width="316" height="150" rx="28" fill="#EEF4F6"/>
      ${node.icon}
      <text x="${node.x + 42}" y="462" font-family=${quote(FONT)} font-size="34" font-weight="800" fill="${C.ink}">${node.title}</text>
      <text x="${node.x + 42}" y="522" font-family=${quote(FONT)} font-size="28" font-weight="700" fill="${C.body}">${node.line1}</text>
      <text x="${node.x + 42}" y="566" font-family=${quote(FONT)} font-size="24" font-weight="700" fill="${C.muted}">${node.line2}</text>
      <rect x="${node.x + 42}" y="586" width="196" height="14" rx="7" fill="${node.color}" opacity="0.82"/>
      ${index < nodes.length - 1 ? flowConnector(node.x + 380, 414, node.x + 534, 414) : ""}
    `).join("")}
    `
  );
}

function buildStageCardShell() {
  return wrapSvg(
    900,
    1120,
    `
    ${roundedPanel(22, 22, 856, 1076, 34, C.panel, "#D0DDE2", 4)}
    <rect x="58" y="144" width="784" height="360" rx="28" fill="#EDF2F5"/>
    ${roundedPanel(58, 558, 784, 316, 28, C.panelAlt)}
    <rect x="58" y="934" width="784" height="114" rx="26" fill="#ECF5F4"/>
    <line x1="126" y1="102" x2="374" y2="102" stroke="#D9E5E9" stroke-width="6" stroke-linecap="round"/>
    `
  );
}

function buildFlowArrow() {
  return wrapSvg(
    320,
    120,
    `
    <path d="M 22 60 H 242" stroke="#3E7D73" stroke-width="14" stroke-linecap="round"/>
    <path d="M 196 22 L 262 60 L 196 98" stroke="#3E7D73" stroke-width="14" stroke-linecap="round" stroke-linejoin="round"/>
    <circle cx="22" cy="60" r="10" fill="#3E7D73"/>
    `
  );
}

function buildStageBadge(number, color) {
  return wrapSvg(
    160,
    160,
    `
    <circle cx="80" cy="80" r="62" fill="${color}"/>
    <text x="80" y="88" class="badge">${number}</text>
    `
  );
}

function buildIconPrepare() {
  return wrapSvg(
    1024,
    1024,
    `
    <rect x="168" y="204" width="688" height="616" rx="82" fill="${C.bellSoft}" stroke="#CFE6DF" stroke-width="8"/>
    ${iconClosedEyeAbstract(512, 470, 1.18)}
    <path d="M 248 706 C 318 652, 430 640, 512 640 C 594 640, 706 652, 776 706" fill="none" stroke="${C.accent}" stroke-width="18" stroke-linecap="round"/>
    <path d="M 360 766 H 664" stroke="${C.accent}" stroke-width="16" stroke-linecap="round" opacity="0.8"/>
    `
  );
}

function buildIconBellTrace() {
  return wrapSvg(
    1024,
    1024,
    `
    <rect x="146" y="188" width="732" height="648" rx="84" fill="${C.bellSoft}" stroke="#CFE6DF" stroke-width="8"/>
    ${iconBell(470, 518, 1.16, C.bell)}
    <path d="M 286 720 C 396 726, 474 664, 560 616 C 644 570, 714 536, 784 470" fill="none" stroke="${C.bell}" stroke-width="18" stroke-linecap="round" stroke-dasharray="28 20"/>
    <path d="M 746 428 L 812 466 L 754 512" stroke="${C.bell}" stroke-width="16" stroke-linecap="round" stroke-linejoin="round"/>
    <circle cx="302" cy="718" r="12" fill="${C.bell}"/>
    <circle cx="566" cy="610" r="10" fill="${C.bell}" opacity="0.75"/>
    <circle cx="716" cy="530" r="10" fill="${C.bell}" opacity="0.55"/>
    `
  );
}

function buildIconRainstorm() {
  return wrapSvg(
    1024,
    1024,
    `
    <rect x="146" y="188" width="732" height="648" rx="84" fill="${C.rainSoft}" stroke="#D4DDF7" stroke-width="8"/>
    <path d="M 288 642 H 736" stroke="#D4DFF7" stroke-width="16" stroke-linecap="round"/>
    ${rainDrop(314, 378, 96, C.rain)}
    ${rainDrop(430, 340, 124, C.rain)}
    ${rainDrop(554, 372, 106, C.rain)}
    ${rainDrop(678, 346, 128, C.rain)}
    ${iconBell(776, 596, 0.66, C.bell)}
    <path d="M 280 710 C 348 678, 428 670, 504 686 C 580 702, 664 700, 744 670" fill="none" stroke="${C.rain}" stroke-width="12" opacity="0.28"/>
    `
  );
}

function buildIconBellGaze() {
  return wrapSvg(
    1024,
    1024,
    `
    <rect x="146" y="188" width="732" height="648" rx="84" fill="${C.bellSoft}" stroke="#CFE6DF" stroke-width="8"/>
    <circle cx="512" cy="518" r="142" fill="none" stroke="#B8C8D0" stroke-width="12"/>
    <circle cx="512" cy="518" r="72" fill="none" stroke="${C.accent}" stroke-width="12"/>
    <path d="M 448 518 H 576" stroke="${C.accent}" stroke-width="10" stroke-linecap="round"/>
    <path d="M 512 454 V 582" stroke="${C.accent}" stroke-width="10" stroke-linecap="round"/>
    ${iconBell(364, 376, 0.58, C.bell)}
    ${iconBell(676, 662, 0.58, C.bell)}
    <path d="M 432 420 C 492 462, 514 498, 512 518" fill="none" stroke="${C.bell}" stroke-width="14" stroke-linecap="round"/>
    <path d="M 596 590 C 552 562, 526 536, 512 518" fill="none" stroke="${C.bell}" stroke-width="14" stroke-linecap="round"/>
    <path d="M 398 456 L 438 434" stroke="${C.bell}" stroke-width="12" stroke-linecap="round"/>
    <path d="M 618 614 L 656 634" stroke="${C.bell}" stroke-width="12" stroke-linecap="round"/>
    `
  );
}

function buildIconTinnitusPurify() {
  return wrapSvg(
    1024,
    1024,
    `
    <rect x="146" y="188" width="732" height="648" rx="84" fill="${C.dangerSoft}" stroke="#E2D7FB" stroke-width="8"/>
    <circle cx="498" cy="504" r="124" fill="none" stroke="${C.tinnitus}" stroke-width="16"/>
    <path d="M 350 504 H 404 M 592 504 H 646" stroke="${C.tinnitus}" stroke-width="14" stroke-linecap="round"/>
    <path d="M 440 504 C 458 504, 458 446, 474 446 C 490 446, 492 568, 512 568 C 534 568, 532 420, 552 420 C 570 420, 576 504, 600 504" fill="none" stroke="${C.tinnitus}" stroke-width="18" stroke-linecap="round" stroke-linejoin="round"/>
    <rect x="618" y="612" width="154" height="94" rx="26" fill="${C.padSoft}" stroke="${C.padGold}" stroke-width="8"/>
    <circle cx="662" cy="659" r="18" fill="#2E3135"/>
    <circle cx="726" cy="659" r="18" fill="#2E3135"/>
    <rect x="676" y="644" width="34" height="10" rx="5" fill="${C.padGold}"/>
    <path d="M 594 578 L 636 616" stroke="${C.padGold}" stroke-width="14" stroke-linecap="round"/>
    <path d="M 578 632 C 612 646, 648 646, 680 632" fill="none" stroke="${C.padGold}" stroke-width="10" stroke-linecap="round"/>
    `
  );
}

function buildIconBossTinnitus() {
  return wrapSvg(
    1024,
    1024,
    `
    <rect x="146" y="188" width="732" height="648" rx="84" fill="${C.dangerSoft}" stroke="#E2D7FB" stroke-width="8"/>
    <circle cx="512" cy="508" r="214" fill="#EEE2FF"/>
    <circle cx="512" cy="508" r="182" fill="none" stroke="${C.tinnitus}" stroke-width="18" opacity="0.92"/>
    <circle cx="512" cy="508" r="120" fill="none" stroke="${C.tinnitus}" stroke-width="12" opacity="0.52"/>
    <path d="M 300 508 C 352 508, 376 422, 428 422 C 474 422, 478 612, 520 612 C 572 612, 572 380, 620 380 C 668 380, 684 508, 738 508" fill="none" stroke="${C.tinnitus}" stroke-width="22" stroke-linecap="round"/>
    <path d="M 312 390 L 372 344" stroke="#D33A58" stroke-width="12" stroke-linecap="round"/>
    <path d="M 682 690 L 746 642" stroke="#D33A58" stroke-width="12" stroke-linecap="round"/>
    <path d="M 438 318 L 470 394" stroke="${C.tinnitus}" stroke-width="12" stroke-linecap="round" opacity="0.6"/>
    <path d="M 604 648 L 646 742" stroke="${C.tinnitus}" stroke-width="12" stroke-linecap="round" opacity="0.6"/>
    <circle cx="512" cy="508" r="22" fill="${C.tinnitus}" filter="url(#softGlow)"/>
    `
  );
}

function buildIconForestEnding() {
  return wrapSvg(
    1024,
    1024,
    `
    <rect x="146" y="188" width="732" height="648" rx="84" fill="#EEF7F0" stroke="#D6E5DA" stroke-width="8"/>
    <path d="M 296 700 H 734" stroke="#B9D2C0" stroke-width="18" stroke-linecap="round"/>
    ${tree(372, 708, 1.05, "#7AAE88", "#385B42")}
    ${tree(520, 708, 1.22, "#5C986B", "#2E4D39")}
    ${tree(678, 708, 0.94, "#7AAE88", "#385B42")}
    <path d="M 284 344 C 382 332, 472 304, 560 256 C 624 220, 686 198, 760 188" fill="none" stroke="${C.bell}" stroke-width="14" stroke-linecap="round" stroke-dasharray="22 18"/>
    <path d="M 708 188 L 766 190 L 728 234" stroke="${C.bell}" stroke-width="12" stroke-linecap="round" stroke-linejoin="round"/>
    `
  );
}

function buildLegend(label, sublabel, color) {
  return wrapSvg(
    760,
    180,
    `
    <rect x="10" y="10" width="740" height="160" rx="32" fill="${C.panel}" stroke="${C.line}" stroke-width="4"/>
    <circle cx="66" cy="90" r="22" fill="${color}"/>
    <text x="106" y="82" font-family=${quote(FONT)} font-size="34" font-weight="800" fill="${C.ink}">${label}</text>
    <text x="106" y="122" font-family=${quote(FONT)} font-size="24" font-weight="700" fill="${C.muted}">${sublabel}</text>
    `
  );
}

function buildS3TechPipeline() {
  const items = [
    { x: 72, title: "Head IMU", body1: "머리 yaw / pitch / roll", body2: "시선 방향 입력", color: C.accent, icon: iconHeadModule(226, 220, 0.72) },
    { x: 532, title: "Pad IMU", body1: "패드 pitch / roll", body2: "회전 보정", color: C.padGold, icon: iconPadModule(686, 220, 0.72) },
    { x: 992, title: "ArUco Camera", body1: "패드 위치 + yaw", body2: "카메라 pose 입력", color: C.wall, icon: iconArucoCamera(1146, 220, 0.72) },
    { x: 1452, title: "Unity Logic", body1: "거리 / 정답 / 단계 판정", body2: "보스·이명·비바람 제어", color: C.rain, icon: iconPoseFusion(1606, 220, 0.72) },
    { x: 1912, title: "Feedback", body1: "3D Sound / LED / Vibration", body2: "관람 화면 연동", color: C.tinnitus, icon: iconFeedbackLoop(2066, 220, 0.72) }
  ];
  return wrapSvg(
    2380,
    520,
    `
    ${roundedPanel(16, 16, 2348, 488, 32, C.dark, C.dark, 2)}
    <text x="48" y="96" font-family=${quote(FONT)} font-size="44" font-weight="800" fill="#F4F8FA">기술 동작 흐름</text>
    <text x="48" y="144" font-family=${quote(FONT)} font-size="24" font-weight="700" fill="#AFC3CB">ArUco 위치, IMU 회전, LED, 진동, 공간 음향을 하나의 감각 루프로 결합한다.</text>
    ${items.map((item, index) => `
      <g>
        <rect x="${item.x}" y="190" width="308" height="258" rx="26" fill="#1B2A30" stroke="#304850" stroke-width="3"/>
        <rect x="${item.x + 24}" y="214" width="260" height="94" rx="20" fill="#24363C"/>
        ${item.icon}
        <text x="${item.x + 26}" y="350" font-family=${quote(FONT)} font-size="30" font-weight="800" fill="#F4F8FA">${item.title}</text>
        <text x="${item.x + 26}" y="394" font-family=${quote(FONT)} font-size="22" font-weight="700" fill="#CDD9DE">${item.body1}</text>
        <text x="${item.x + 26}" y="426" font-family=${quote(FONT)} font-size="20" font-weight="700" fill="#9CB0B8">${item.body2}</text>
        <rect x="${item.x + 26}" y="438" width="160" height="10" rx="5" fill="${item.color}"/>
      </g>
      ${index < items.length - 1 ? flowConnector(item.x + 308, 320, item.x + 460, 320, "#B7C8CE", 10) : ""}
    `).join("")}
    `
  );
}

function iconClosedEyeAbstract(cx, cy, scale = 1) {
  const s = scale;
  return `
    <circle cx="${cx}" cy="${cy}" r="${124 * s}" fill="${C.panel}" stroke="#CFE0E5" stroke-width="${10 * s}"/>
    <path d="M ${cx - 74 * s} ${cy - 12 * s} C ${cx - 38 * s} ${cy - 56 * s}, ${cx + 38 * s} ${cy - 56 * s}, ${cx + 74 * s} ${cy - 12 * s}" fill="none" stroke="${C.accent}" stroke-width="${16 * s}" stroke-linecap="round"/>
    <path d="M ${cx - 74 * s} ${cy + 28 * s} C ${cx - 30 * s} ${cy + 72 * s}, ${cx + 30 * s} ${cy + 72 * s}, ${cx + 74 * s} ${cy + 28 * s}" fill="none" stroke="${C.accent}" stroke-width="${16 * s}" stroke-linecap="round"/>
    <path d="M ${cx - 38 * s} ${cy + 108 * s} L ${cx - 18 * s} ${cy + 132 * s}" stroke="${C.ink}" stroke-width="${10 * s}" stroke-linecap="round"/>
    <path d="M ${cx} ${cy + 108 * s} L ${cx} ${cy + 136 * s}" stroke="${C.ink}" stroke-width="${10 * s}" stroke-linecap="round"/>
    <path d="M ${cx + 38 * s} ${cy + 108 * s} L ${cx + 18 * s} ${cy + 132 * s}" stroke="${C.ink}" stroke-width="${10 * s}" stroke-linecap="round"/>
  `;
}

function iconClosedEyeMinimal(cx, cy, scale = 1) {
  const s = scale;
  return `
    <circle cx="${cx}" cy="${cy}" r="${64 * s}" fill="#F4F8FA" stroke="#D0DDE2" stroke-width="${5 * s}"/>
    <path d="M ${cx - 44 * s} ${cy} C ${cx - 16 * s} ${cy - 26 * s}, ${cx + 16 * s} ${cy - 26 * s}, ${cx + 44 * s} ${cy}" fill="none" stroke="${C.ink}" stroke-width="${7 * s}" stroke-linecap="round"/>
    <path d="M ${cx - 44 * s} ${cy + 18 * s} C ${cx - 18 * s} ${cy + 42 * s}, ${cx + 18 * s} ${cy + 42 * s}, ${cx + 44 * s} ${cy + 18 * s}" fill="none" stroke="${C.accent}" stroke-width="${7 * s}" stroke-linecap="round"/>
  `;
}

function iconBell(cx, cy, scale = 1, color = C.bell) {
  const s = scale;
  return `
    <g stroke="${color}" stroke-width="${10 * s}" stroke-linecap="round" stroke-linejoin="round" fill="none">
      <path d="M ${cx - 54 * s} ${cy + 22 * s} C ${cx - 54 * s} ${cy - 36 * s}, ${cx - 30 * s} ${cy - 94 * s}, ${cx} ${cy - 110 * s} C ${cx + 30 * s} ${cy - 94 * s}, ${cx + 54 * s} ${cy - 36 * s}, ${cx + 54 * s} ${cy + 22 * s}" />
      <path d="M ${cx - 72 * s} ${cy + 22 * s} H ${cx + 72 * s}" />
      <path d="M ${cx - 28 * s} ${cy + 22 * s} C ${cx - 12 * s} ${cy + 48 * s}, ${cx + 12 * s} ${cy + 48 * s}, ${cx + 28 * s} ${cy + 22 * s}" />
      <circle cx="${cx}" cy="${cy + 34 * s}" r="${8 * s}" fill="${color}" stroke="none"/>
      <rect x="${cx - 12 * s}" y="${cy - 132 * s}" width="${24 * s}" height="${26 * s}" rx="${12 * s}" fill="${color}" stroke="none"/>
    </g>
  `;
}

function rainDrop(x, y, length = 100, color = C.rain) {
  return `
    <path d="M ${x} ${y} L ${x - 16} ${y + length * 0.72} C ${x - 20} ${y + length * 0.9}, ${x + 20} ${y + length * 0.9}, ${x + 16} ${y + length * 0.72} Z" fill="${color}" opacity="0.78"/>
    <circle cx="${x}" cy="${y + length}" r="${Math.max(10, length * 0.1)}" fill="${color}" opacity="0.18"/>
    <circle cx="${x}" cy="${y + length}" r="${Math.max(5, length * 0.05)}" fill="${color}" opacity="0.38"/>
  `;
}

function tree(x, y, scale = 1, leaf = "#6AA877", trunk = "#3F523D") {
  const s = scale;
  return `
    <path d="M ${x} ${y - 168 * s} L ${x - 84 * s} ${y - 20 * s} H ${x + 84 * s} Z" fill="${leaf}"/>
    <path d="M ${x} ${y - 236 * s} L ${x - 66 * s} ${y - 112 * s} H ${x + 66 * s} Z" fill="${leaf}"/>
    <rect x="${x - 18 * s}" y="${y - 20 * s}" width="${36 * s}" height="${86 * s}" rx="${10 * s}" fill="${trunk}"/>
  `;
}

function flowConnector(x1, y1, x2, y2, color = "#3E7D73", strokeWidth = 12) {
  return `
    <path d="M ${x1 + 16} ${y1} H ${x2 - 40}" stroke="${color}" stroke-width="${strokeWidth}" stroke-linecap="round"/>
    <path d="M ${x2 - 80} ${y2 - 28} L ${x2 - 32} ${y2} L ${x2 - 80} ${y2 + 28}" stroke="${color}" stroke-width="${strokeWidth}" stroke-linecap="round" stroke-linejoin="round"/>
  `;
}

function iconHeadModule(cx, cy, scale = 1) {
  const s = scale;
  return `
    <g stroke="${C.accent}" stroke-width="${8 * s}" stroke-linecap="round" stroke-linejoin="round" fill="none">
      <circle cx="${cx}" cy="${cy}" r="${56 * s}" fill="#F5FBF9" stroke="#9FDFCC"/>
      <path d="M ${cx - 34 * s} ${cy - 12 * s} C ${cx - 18 * s} ${cy - 30 * s}, ${cx + 18 * s} ${cy - 30 * s}, ${cx + 34 * s} ${cy - 12 * s}" />
      <path d="M ${cx - 34 * s} ${cy + 12 * s} C ${cx - 16 * s} ${cy + 30 * s}, ${cx + 16 * s} ${cy + 30 * s}, ${cx + 34 * s} ${cy + 12 * s}" />
      <path d="M ${cx - 72 * s} ${cy} H ${cx - 98 * s}" />
      <path d="M ${cx + 72 * s} ${cy} H ${cx + 98 * s}" />
    </g>
  `;
}

function iconPadModule(cx, cy, scale = 1) {
  const s = scale;
  return `
    <rect x="${cx - 86 * s}" y="${cy - 46 * s}" width="${172 * s}" height="${92 * s}" rx="${28 * s}" fill="${C.padSoft}" stroke="${C.padGold}" stroke-width="${8 * s}"/>
    <circle cx="${cx - 42 * s}" cy="${cy}" r="${16 * s}" fill="#2F3236"/>
    <circle cx="${cx + 42 * s}" cy="${cy}" r="${16 * s}" fill="#2F3236"/>
    <rect x="${cx - 18 * s}" y="${cy - 8 * s}" width="${36 * s}" height="${12 * s}" rx="${6 * s}" fill="${C.padGold}"/>
    <path d="M ${cx - 108 * s} ${cy - 52 * s} L ${cx - 132 * s} ${cy - 88 * s}" stroke="${C.padGold}" stroke-width="${8 * s}" stroke-linecap="round"/>
    <path d="M ${cx + 108 * s} ${cy - 52 * s} L ${cx + 132 * s} ${cy - 88 * s}" stroke="${C.padGold}" stroke-width="${8 * s}" stroke-linecap="round"/>
  `;
}

function iconArucoCamera(cx, cy, scale = 1) {
  const s = scale;
  return `
    <rect x="${cx - 64 * s}" y="${cy - 62 * s}" width="${128 * s}" height="${98 * s}" rx="${18 * s}" fill="#F1FBFE" stroke="${C.wall}" stroke-width="${8 * s}"/>
    <circle cx="${cx}" cy="${cy - 12 * s}" r="${18 * s}" fill="none" stroke="${C.wall}" stroke-width="${8 * s}"/>
    <rect x="${cx - 44 * s}" y="${cy + 62 * s}" width="${36 * s}" height="${36 * s}" fill="#E6F8FF" stroke="${C.wall}" stroke-width="${7 * s}"/>
    <rect x="${cx + 8 * s}" y="${cy + 62 * s}" width="${36 * s}" height="${36 * s}" fill="#E6F8FF" stroke="${C.wall}" stroke-width="${7 * s}"/>
    <rect x="${cx - 34 * s}" y="${cy + 72 * s}" width="${16 * s}" height="${16 * s}" fill="${C.wall}"/>
    <rect x="${cx + 18 * s}" y="${cy + 72 * s}" width="${16 * s}" height="${16 * s}" fill="${C.wall}"/>
  `;
}

function iconPoseFusion(cx, cy, scale = 1) {
  const s = scale;
  return `
    <circle cx="${cx - 34 * s}" cy="${cy}" r="${34 * s}" fill="#F1F4FF" stroke="${C.rain}" stroke-width="${8 * s}"/>
    <circle cx="${cx + 34 * s}" cy="${cy}" r="${34 * s}" fill="#F8EDFF" stroke="${C.tinnitus}" stroke-width="${8 * s}"/>
    <circle cx="${cx}" cy="${cy - 58 * s}" r="${26 * s}" fill="#F3FBF7" stroke="${C.accent}" stroke-width="${8 * s}"/>
    <path d="M ${cx - 8 * s} ${cy - 20 * s} L ${cx - 24 * s} ${cy - 8 * s} L ${cx - 6 * s} ${cy + 18 * s}" stroke="${C.ink}" stroke-width="${8 * s}" stroke-linecap="round"/>
    <path d="M ${cx + 8 * s} ${cy - 20 * s} L ${cx + 24 * s} ${cy - 8 * s} L ${cx + 6 * s} ${cy + 18 * s}" stroke="${C.ink}" stroke-width="${8 * s}" stroke-linecap="round"/>
  `;
}

function iconFeedbackLoop(cx, cy, scale = 1) {
  const s = scale;
  return `
    <circle cx="${cx}" cy="${cy}" r="${56 * s}" fill="#F4FBF6" stroke="${C.bell}" stroke-width="${8 * s}"/>
    <path d="M ${cx - 62 * s} ${cy - 8 * s} H ${cx - 18 * s}" stroke="${C.wall}" stroke-width="${8 * s}" stroke-linecap="round"/>
    <path d="M ${cx + 18 * s} ${cy - 8 * s} H ${cx + 62 * s}" stroke="${C.wall}" stroke-width="${8 * s}" stroke-linecap="round"/>
    <path d="M ${cx - 34 * s} ${cy + 22 * s} C ${cx - 16 * s} ${cy + 42 * s}, ${cx + 16 * s} ${cy + 42 * s}, ${cx + 34 * s} ${cy + 22 * s}" fill="none" stroke="${C.padGold}" stroke-width="${8 * s}" stroke-linecap="round"/>
    <path d="M ${cx} ${cy - 42 * s} V ${cy + 4 * s}" stroke="${C.bell}" stroke-width="${8 * s}" stroke-linecap="round"/>
  `;
}

function quote(value) {
  return `"${value.replace(/"/g, "&quot;")}"`;
}

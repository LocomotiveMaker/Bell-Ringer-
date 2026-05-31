import fs from "node:fs";
import path from "node:path";
import { Resvg } from "@resvg/resvg-js";

const outDir = process.argv[2] || path.resolve("Docs", "PosterDrafts");
fs.mkdirSync(outDir, { recursive: true });

const WIDTH = 1800;
const HEIGHT = 2540;
const fontFiles = [
  "C:\\Windows\\Fonts\\malgun.ttf",
  "C:\\Windows\\Fonts\\malgunbd.ttf",
].filter((file) => fs.existsSync(file));

const posters = [
  {
    name: "BellRinger_PosterDraft_A_CleanExpo",
    svg: buildPosterA(),
  },
  {
    name: "BellRinger_PosterDraft_B_DarkSensory",
    svg: buildPosterB(),
  },
  {
    name: "BellRinger_PosterDraft_C_PlayfulInfographic",
    svg: buildPosterC(),
  },
];

for (const poster of posters) {
  const svgPath = path.join(outDir, `${poster.name}.svg`);
  const pngPath = path.join(outDir, `${poster.name}.png`);
  fs.writeFileSync(svgPath, poster.svg, "utf8");

  const resvg = new Resvg(poster.svg, {
    fitTo: { mode: "width", value: WIDTH },
    font: {
      fontFiles,
      loadSystemFonts: true,
      defaultFontFamily: "Malgun Gothic",
    },
  });

  const pngData = resvg.render().asPng();
  fs.writeFileSync(pngPath, pngData);
  console.log(`Wrote ${svgPath}`);
  console.log(`Wrote ${pngPath}`);
}

function buildPosterA() {
  const c = {
    bgA: "#1CB090",
    bgB: "#35B9E7",
    panel: "#F9FBFD",
    ink: "#172033",
    muted: "#5F6F89",
    line: "#D9E2EE",
    green: "#17A874",
    bell: "#F1C44C",
    blue: "#4F90FF",
    violet: "#9D78FF",
    cyan: "#7BD6E4",
    orange: "#F6B44A",
  };

  return svgFrame(
    WIDTH,
    HEIGHT,
    `
    <defs>
      <linearGradient id="posterA-bg" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0%" stop-color="${c.bgA}"/>
        <stop offset="100%" stop-color="${c.bgB}"/>
      </linearGradient>
    </defs>
    <rect width="${WIDTH}" height="${HEIGHT}" fill="url(#posterA-bg)"/>
    ${waveBundle(28, 950, 340, "rgba(255,255,255,0.26)")}
    ${waveBundle(1480, 1820, 340, "rgba(255,255,255,0.22)")}
    ${dots(1500, 110, 7, 5, 26, "rgba(255,255,255,0.36)")}

    <rect x="96" y="74" width="96" height="50" rx="12" fill="#0C4E8A"/>
    <text x="144" y="108" text-anchor="middle" fill="#FFFFFF" font-size="28" font-weight="700">MD</text>
    <text x="216" y="108" fill="#EFFFFA" font-size="30" font-weight="700">체감기술(고급)</text>
    <text x="96" y="210" fill="#FFFFFF" font-size="84" font-weight="900">Bell Ringer (종지기)</text>
    <text x="96" y="272" fill="#E8FFF7" font-size="34" font-weight="700">눈을 감고 소리와 빛, 진동만으로 길을 찾는 청각 중심 인터랙션 게임</text>
    <text x="1320" y="110" fill="#F7FFFD" font-size="30" font-weight="800">실감피지컬컴퓨팅</text>
    <text x="1320" y="210" fill="#E8FFF7" font-size="28" font-weight="700">이름 / 교수명 추후 입력</text>

    <rect x="80" y="332" width="1640" height="1980" rx="44" fill="${c.panel}"/>
    <path d="M 1630 332 L 1720 422 L 1720 332 Z" fill="#C9F1E2"/>

    ${sectionHeader(130, 430, "01", "개요", c.green)}
    <text x="392" y="472" fill="${c.ink}" font-size="36" font-weight="900">소리와 빛이 하나의 감각처럼 연결되는 전시형 게임</text>
    <text x="392" y="532" fill="${c.muted}" font-size="28">플레이어는 시야를 차단한 채 종소리를 따라 이동하고, 패드의 위치와 회전으로 이명을 정화한다.</text>
    ${chip(392, 572, 248, 60, "#E9FFF7", c.green, "눈 감고 플레이")}
    ${chip(658, 572, 248, 60, "#EDF5FF", c.blue, "공간 음향 탐색")}
    ${chip(924, 572, 248, 60, "#F5EEFF", c.violet, "이명 정화")}
    ${chip(1190, 572, 248, 60, "#FFF5E6", c.orange, "패드 진동")}
    ${infoPanel(
      392,
      662,
      1240,
      176,
      "#F2F7FB",
      c.line,
      iconBell(448, 750, 30, c.bell),
      "핵심 경험",
      [
        "고요한 공간에서 종이 왼편에서 시작해 플레이어를 부르고, 비바람 속에서도 다시 길잡이 역할을 한다.",
        "종을 따라간 뒤에는 패드 pose와 진동을 이용해 일반 이명과 대왕 이명을 차례로 정화한다.",
      ],
      c.ink,
      c.muted
    )}

    ${divider(1120, c.line)}

    ${sectionHeader(130, 1180, "02", "배경 및 목적", c.green)}
    <text x="392" y="1222" fill="${c.ink}" font-size="36" font-weight="900">시각을 덜어내고도 공간 인지가 가능한지 실험</text>
    ${bullets(
      392,
      1290,
      [
        "눈을 감은 상태에서 소리의 방향, 빛의 위치, 진동의 반응만으로 다음 행동을 유도한다.",
        "종, 비, 벽, 이명처럼 서로 다른 요소를 감각 규칙으로 구분해 청각 몰입의 질을 높인다.",
        "관람자는 화면을 통해 전체 흐름을 이해하고, 플레이어는 오직 체감 정보만으로 플레이한다.",
      ],
      c.green,
      c.ink,
      c.muted,
      62
    )}
    ${miniDiagramCard(1230, 1218, 338, 310, c)}

    ${divider(1594, c.line)}

    ${sectionHeader(130, 1656, "03", "내용", c.green)}
    <text x="392" y="1698" fill="${c.ink}" font-size="36" font-weight="900">게임 주요 흐름</text>
    <text x="392" y="1746" fill="${c.muted}" font-size="27">멀리서도 직관적으로 보이도록 아이콘과 짧은 문장 중심으로 구성</text>
    ${flowRow(
      366,
      1808,
      [
        { title: "종 등장", sub: "왼편 근접 소리", icon: iconBell, color: c.bell },
        { title: "종 추적", sub: "소리 방향으로 이동", icon: iconWalk, color: c.green },
        { title: "비·바람", sub: "분리 집중", icon: iconRain, color: c.blue },
        { title: "종 바라보기", sub: "3회 누적", icon: iconEye, color: c.green },
        { title: "이명 정화", sub: "pose 4초", icon: iconWave, color: c.violet },
        { title: "보스 추적", sub: "약점 추적", icon: iconBoss, color: c.orange },
        { title: "숲 엔딩", sub: "평화 전환", icon: iconForest, color: c.cyan },
      ],
      c.ink,
      c.muted
    )}

    ${divider(2122, c.line)}

    ${sectionHeader(130, 2182, "04", "결과", c.green)}
    ${resultCard(392, 2178, 274, 138, c.bell, "청각 몰입", "화면 없이도 목표를 추적한다.", iconEar(448, 2248, 24, c.bell), c.ink, c.muted)}
    ${resultCard(688, 2178, 274, 138, c.violet, "상호작용", "패드 pose와 진동이 직접 연결된다.", iconPad(744, 2248, 24, c.violet), c.ink, c.muted)}
    ${resultCard(984, 2178, 274, 138, c.cyan, "전시성", "관람 화면이 흐름을 시각적으로 설명한다.", iconMonitor(1040, 2248, 24, c.cyan), c.ink, c.muted)}
    ${resultCard(1280, 2178, 274, 138, c.green, "확장성", "사운드와 빛 튜닝으로 인상을 조절한다.", iconSpark(1336, 2248, 24, c.green), c.ink, c.muted)}
    `
  );
}

function buildPosterB() {
  const c = {
    bgA: "#09111F",
    bgB: "#15264A",
    panel: "#10192D",
    panel2: "#142038",
    ink: "#F3F7FF",
    muted: "#A7B6D8",
    green: "#33E0AA",
    bell: "#FFD45D",
    blue: "#52A5FF",
    violet: "#B38DFF",
    cyan: "#89E4F2",
    orange: "#FFC46A",
    line: "#2A3959",
  };

  return svgFrame(
    WIDTH,
    HEIGHT,
    `
    <defs>
      <linearGradient id="posterB-bg" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0%" stop-color="${c.bgA}"/>
        <stop offset="100%" stop-color="${c.bgB}"/>
      </linearGradient>
      <radialGradient id="posterB-glowA" cx="50%" cy="35%" r="58%">
        <stop offset="0%" stop-color="rgba(51,224,170,0.26)"/>
        <stop offset="100%" stop-color="rgba(51,224,170,0)"/>
      </radialGradient>
      <radialGradient id="posterB-glowB" cx="50%" cy="50%" r="58%">
        <stop offset="0%" stop-color="rgba(179,141,255,0.22)"/>
        <stop offset="100%" stop-color="rgba(179,141,255,0)"/>
      </radialGradient>
    </defs>
    <rect width="${WIDTH}" height="${HEIGHT}" fill="url(#posterB-bg)"/>
    <circle cx="1310" cy="340" r="380" fill="url(#posterB-glowA)"/>
    <circle cx="300" cy="1740" r="440" fill="url(#posterB-glowB)"/>
    ${dots(1428, 96, 8, 6, 22, "rgba(255,255,255,0.18)")}

    <text x="110" y="118" fill="${c.green}" font-size="30" font-weight="800">MD / 체감기술(고급)</text>
    <text x="110" y="216" fill="${c.ink}" font-size="92" font-weight="900">Bell Ringer</text>
    <text x="110" y="278" fill="${c.muted}" font-size="35" font-weight="700">눈을 감고 종소리와 빛, 진동만으로 길을 찾는 감각 중심 전시 게임</text>

    ${darkBlock(84, 360, 1632, 336, "01  개요", c, `
      <text x="134" y="456" fill="${c.ink}" font-size="54" font-weight="900">보이지 않아도 따라갈 수 있는가?</text>
      <text x="134" y="520" fill="${c.muted}" font-size="29">종은 공간의 방향을 만들고, 비바람은 방해가 되며, 패드는 이명을 정화하는 도구가 된다.</text>
      ${chip(134, 574, 250, 60, "#142B2B", c.green, "눈 감고 플레이")}
      ${chip(402, 574, 250, 60, "#14233A", c.blue, "종소리 추적")}
      ${chip(670, 574, 250, 60, "#261B39", c.violet, "이명 정화")}
      ${chip(938, 574, 250, 60, "#312516", c.orange, "보스 패턴")}
      ${chip(1206, 574, 250, 60, "#162834", c.cyan, "숲 엔딩")}
    `)}

    ${darkBlock(84, 728, 1632, 410, "02  배경 및 목적", c, `
      <text x="134" y="826" fill="${c.ink}" font-size="50" font-weight="900">시각 대신 감각 결합을 전면에 세운다</text>
      ${bullets(
        134,
        894,
        [
          "소리의 위치, LED 빛의 점멸, 진동의 세기가 한 덩어리의 감각처럼 동작하도록 설계했다.",
          "플레이어는 눈을 감은 상태에서 체감 정보만 받아들이고, 관람자는 화면으로 그 과정을 이해한다.",
          "조작은 복잡하지 않지만 감각 해석은 깊게 남도록, 종 추적과 이명 정화의 대비를 핵심으로 잡았다.",
        ],
        c.bell,
        c.ink,
        c.muted,
        66
      )}
      ${sensoryInset(1238, 824, c)}
    `)}

    ${darkBlock(84, 1170, 1632, 610, "03  내용", c, `
      <text x="134" y="1266" fill="${c.ink}" font-size="50" font-weight="900">주요 플레이 흐름</text>
      <text x="134" y="1318" fill="${c.muted}" font-size="28">단계마다 감각 역할이 달라진다</text>
      ${timeline(
        202,
        1396,
        [
          { title: "시작", sub: "왼편 종소리로 깨어남", icon: iconBell, color: c.bell },
          { title: "종 추적", sub: "이동하며 종을 쫓음", icon: iconWalk, color: c.green },
          { title: "비·바람", sub: "소리 분리 집중", icon: iconRain, color: c.blue },
          { title: "종 바라보기", sub: "종 위치를 바라보기", icon: iconEye, color: c.green },
          { title: "일반 이명", sub: "패드 pose로 4초 정화", icon: iconWave, color: c.violet },
          { title: "보스 이명", sub: "움직이는 약점 추적", icon: iconBoss, color: c.orange },
          { title: "숲 엔딩", sub: "숲 소리와 종소리 회귀", icon: iconForest, color: c.cyan },
        ],
        c.ink,
        c.muted
      )}
    `)}

    ${darkBlock(84, 1812, 1632, 474, "04  결과", c, `
      ${darkResult(134, 1914, 354, 240, c.bell, "청각 몰입", "화면 없이도 공간 목표를 추적한다.", iconEar(202, 1994, 28, c.bell), c)}
      ${darkResult(520, 1914, 354, 240, c.green, "직관성", "멀리서도 읽히는 아이콘형 흐름을 만든다.", iconLight(588, 1994, 28, c.green), c)}
      ${darkResult(906, 1914, 354, 240, c.violet, "체감 상호작용", "패드 위치·회전·진동이 정화 조작이 된다.", iconPad(974, 1994, 28, c.violet), c)}
      ${darkResult(1292, 1914, 354, 240, c.cyan, "발전 방향", "HRTF와 빛 패턴 보강으로 더 확장 가능하다.", iconSpark(1360, 1994, 28, c.cyan), c)}
    `)}
    `
  );
}

function buildPosterC() {
  const c = {
    bg: "#F8F3E6",
    card: "#FFFDF8",
    line: "#D8D0BE",
    ink: "#1F2D34",
    muted: "#667780",
    green: "#148D6C",
    bell: "#F2C04C",
    blue: "#5E98FA",
    violet: "#A57DFF",
    cyan: "#72CBD7",
    orange: "#F6B24C",
  };

  return svgFrame(
    WIDTH,
    HEIGHT,
    `
    <rect width="${WIDTH}" height="${HEIGHT}" fill="${c.bg}"/>
    <circle cx="1450" cy="220" r="200" fill="#DBF1E7"/>
    <circle cx="260" cy="2240" r="220" fill="#E7F0FF"/>
    ${dots(1452, 116, 6, 5, 28, "#C6D7D0")}

    <rect x="96" y="92" width="1608" height="2350" rx="48" fill="${c.card}" stroke="${c.line}" stroke-width="3"/>
    <text x="136" y="162" fill="${c.green}" font-size="30" font-weight="800">MD / 체감기술(고급)</text>
    <text x="136" y="262" fill="${c.ink}" font-size="82" font-weight="900">Bell Ringer (종지기)</text>
    <text x="136" y="322" fill="${c.muted}" font-size="34" font-weight="700">소리·빛·진동으로 길을 찾는 폐안형 인터랙션 게임</text>
    ${chip(136, 362, 184, 54, "#EFF9F5", c.green, "눈 감고 플레이")}
    ${chip(336, 362, 184, 54, "#FFF7E8", c.bell, "종 추적")}
    ${chip(536, 362, 184, 54, "#EDF5FF", c.blue, "비·바람")}
    ${chip(736, 362, 184, 54, "#F4EFFF", c.violet, "이명 정화")}
    ${chip(936, 362, 184, 54, "#EEF9FB", c.cyan, "숲 엔딩")}

    ${creamSection(136, 454, 1528, 314, "01  개요", c.green, c, `
      <text x="188" y="548" fill="${c.ink}" font-size="48" font-weight="900">플레이어는 보지 않고, 관람자는 한눈에 이해한다</text>
      <text x="188" y="612" fill="${c.muted}" font-size="29">Bell Ringer는 종소리를 따라 이동하고, 패드 pose로 이명을 정화하며, 마지막에는 숲 소리로 귀환하는 체감형 전시 게임이다.</text>
      <text x="188" y="656" fill="${c.muted}" font-size="29">플레이어는 청각과 빛, 진동만 받아들이고 관람자는 큰 흐름과 오브젝트 상태를 화면으로 본다.</text>
    `)}

    ${creamSection(136, 804, 1528, 404, "02  배경 및 목적", c.bell, c, `
      ${bullets(
        188,
        892,
        [
          "눈을 감은 상태에서 방향을 인지하고 행동을 이어가는 감각 설계를 검증한다.",
          "종, 비, 벽, 이명을 각기 다른 소리와 빛의 규칙으로 배치해 직관성을 높인다.",
          "전시장에서 멀리서도 읽히도록 글보다 아이콘, 흐름선, 블록 구조를 우선한다.",
        ],
        c.bell,
        c.ink,
        c.muted,
        68
      )}
      ${goalBoard(1188, 866, c)}
    `)}

    ${creamSection(136, 1244, 1528, 720, "03  내용", c.blue, c, `
      <text x="188" y="1336" fill="${c.ink}" font-size="48" font-weight="900">핵심 플레이 흐름</text>
      <text x="188" y="1390" fill="${c.muted}" font-size="28">아이콘과 짧은 라벨로, 한 줄만 봐도 전체 구성이 읽히도록 설계</text>
      ${flowGrid(
        188,
        1452,
        [
          { title: "종 등장", sub: "왼편 근접 소리", icon: iconBell, color: c.bell },
          { title: "종 추적", sub: "소리 방향 이동", icon: iconWalk, color: c.green },
          { title: "비·바람", sub: "칵테일 파티 효과", icon: iconRain, color: c.blue },
          { title: "종 바라보기", sub: "3회 누적 성공", icon: iconEye, color: c.green },
          { title: "일반 이명", sub: "패드 pose 4초 유지", icon: iconWave, color: c.violet },
          { title: "보스 이명", sub: "움직이는 약점 추적", icon: iconBoss, color: c.orange },
          { title: "숲 엔딩", sub: "종소리 회귀", icon: iconForest, color: c.cyan },
          { title: "관람 화면", sub: "상태를 시각화", icon: iconMonitor, color: c.green },
        ],
        c
      )}
    `)}

    ${creamSection(136, 1994, 1528, 360, "04  결과", c.violet, c, `
      ${softResult(188, 2084, 290, 188, c.bell, "청각 중심 몰입", "화면 없이도 목표 추적이 가능하다.", iconEar(252, 2164, 26, c.bell), c)}
      ${softResult(514, 2084, 290, 188, c.green, "감각 결합", "소리·빛·진동이 각 단계의 의미를 전달한다.", iconLight(578, 2164, 26, c.green), c)}
      ${softResult(840, 2084, 290, 188, c.violet, "정화 인터랙션", "패드 위치·회전이 정화 조작이 된다.", iconPad(904, 2164, 26, c.violet), c)}
      ${softResult(1166, 2084, 290, 188, c.cyan, "전시 확장성", "사운드와 빛 패턴 보강으로 확장 가능하다.", iconSpark(1230, 2164, 26, c.cyan), c)}
    `)}
    `
  );
}

function svgFrame(width, height, body) {
  return `<?xml version="1.0" encoding="UTF-8" standalone="no"?>
<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">
  <style>
    text { font-family: "Malgun Gothic", Arial, sans-serif; }
  </style>
  ${body}
</svg>`;
}

function sectionHeader(x, y, no, title, color) {
  return `
    <text x="${x}" y="${y}" fill="${color}" font-size="48" font-weight="900">${no}</text>
    <text x="${x}" y="${y + 56}" fill="#24303E" font-size="34" font-weight="800">${title}</text>
    <line x1="${x + 110}" y1="${y - 8}" x2="${x + 270}" y2="${y - 8}" stroke="#D7DFE8" stroke-width="3" stroke-dasharray="3 9"/>
  `;
}

function chip(x, y, w, h, bg, color, label) {
  return `
    <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${h / 2}" fill="${bg}" stroke="${color}" stroke-width="2"/>
    <text x="${x + w / 2}" y="${y + h / 2 + 10}" text-anchor="middle" fill="${color}" font-size="25" font-weight="800">${label}</text>
  `;
}

function infoPanel(x, y, w, h, bg, stroke, iconSvg, title, lines, ink, muted) {
  return `
    <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="28" fill="${bg}" stroke="${stroke}" stroke-width="2"/>
    ${iconSvg}
    <text x="${x + 90}" y="${y + 64}" fill="${ink}" font-size="31" font-weight="800">${title}</text>
    <text x="${x + 90}" y="${y + 108}" fill="${muted}" font-size="25">${lines[0]}</text>
    <text x="${x + 90}" y="${y + 146}" fill="${muted}" font-size="25">${lines[1]}</text>
  `;
}

function divider(y, color) {
  return `<line x1="120" y1="${y}" x2="1680" y2="${y}" stroke="${color}" stroke-width="2"/>`;
}

function bullets(x, y, items, dotColor, ink, muted, gap) {
  return items
    .map((item, index) => {
      const yy = y + gap * index;
      return `
        <circle cx="${x + 12}" cy="${yy - 12}" r="10" fill="${dotColor}"/>
        <text x="${x + 42}" y="${yy}" fill="${index === 0 ? ink : muted}" font-size="29">${item}</text>
      `;
    })
    .join("");
}

function miniDiagramCard(x, y, w, h, c) {
  return `
    <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="32" fill="#F4FBFF" stroke="${c.line}" stroke-width="2"/>
    <circle cx="${x + 170}" cy="${y + 116}" r="84" fill="#132033"/>
    <path d="M ${x + 102} ${y + 98} Q ${x + 170} ${y + 36} ${x + 238} ${y + 98}" fill="none" stroke="#FFFFFF" stroke-width="10" stroke-linecap="round"/>
    <line x1="${x + 118}" y1="${y + 102}" x2="${x + 222}" y2="${y + 80}" stroke="${c.green}" stroke-width="8" stroke-linecap="round"/>
    ${iconLight(x + 78, y + 222, 20, c.green)}
    <text x="${x + 110}" y="${y + 226}" fill="${c.ink}" font-size="31" font-weight="800">플레이어</text>
    <text x="${x + 110}" y="${y + 268}" fill="${c.muted}" font-size="24">소리 위치, 빛, 진동으로</text>
    <text x="${x + 110}" y="${y + 300}" fill="${c.muted}" font-size="24">다음 행동을 결정</text>
  `;
}

function flowRow(startX, y, items, ink, muted) {
  const step = 188;
  return items
    .map((item, index) => {
      const x = startX + step * index;
      const arrow =
        index < items.length - 1
          ? `<line x1="${x + 152}" y1="${y + 56}" x2="${x + 178}" y2="${y + 56}" stroke="#8EA2BD" stroke-width="5" stroke-linecap="round"/><path d="M ${x + 170} ${y + 46} L ${x + 186} ${y + 56} L ${x + 170} ${y + 66}" fill="none" stroke="#8EA2BD" stroke-width="5" stroke-linecap="round" stroke-linejoin="round"/>`
          : "";
      return `
        <rect x="${x}" y="${y}" width="144" height="198" rx="24" fill="#FFFFFF" stroke="#DCE4EE" stroke-width="2"/>
        <circle cx="${x + 72}" cy="${y + 56}" r="32" fill="${fade(item.color, 0.14)}"/>
        ${item.icon(x + 72, y + 56, 19, item.color)}
        <text x="${x + 72}" y="${y + 118}" text-anchor="middle" fill="${ink}" font-size="24" font-weight="800">${item.title}</text>
        <text x="${x + 72}" y="${y + 154}" text-anchor="middle" fill="${muted}" font-size="20">${item.sub}</text>
        ${arrow}
      `;
    })
    .join("");
}

function resultCard(x, y, w, h, color, title, desc, iconSvg, ink, muted) {
  return `
    <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="24" fill="#FFFFFF" stroke="#DCE4EE" stroke-width="2"/>
    ${iconSvg}
    <text x="${x + 64}" y="${y + 54}" fill="${ink}" font-size="28" font-weight="800">${title}</text>
    <text x="${x + 24}" y="${y + 114}" fill="${muted}" font-size="20">${desc}</text>
    <line x1="${x + 24}" y1="${y + h - 24}" x2="${x + w - 24}" y2="${y + h - 24}" stroke="${color}" stroke-width="4" stroke-linecap="round"/>
  `;
}

function darkBlock(x, y, w, h, title, c, body) {
  return `
    <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="32" fill="${c.panel}" stroke="${c.line}" stroke-width="2"/>
    <rect x="${x}" y="${y}" width="${w}" height="72" rx="32" fill="${c.panel2}"/>
    <text x="${x + 34}" y="${y + 48}" fill="${c.ink}" font-size="30" font-weight="900">${title}</text>
    ${body}
  `;
}

function sensoryInset(x, y, c) {
  return `
    <rect x="${x}" y="${y}" width="380" height="270" rx="28" fill="#0E1628" stroke="${c.green}" stroke-width="2"/>
    <circle cx="${x + 190}" cy="${y + 102}" r="76" fill="#09111F" stroke="${c.green}" stroke-width="2"/>
    <path d="M ${x + 130} ${y + 86} Q ${x + 190} ${y + 32} ${x + 250} ${y + 86}" fill="none" stroke="#FFFFFF" stroke-width="10" stroke-linecap="round"/>
    <line x1="${x + 144}" y1="${y + 90}" x2="${x + 236}" y2="${y + 72}" stroke="${c.green}" stroke-width="7" stroke-linecap="round"/>
    ${iconLight(x + 74, y + 208, 18, c.green)}
    <text x="${x + 106}" y="${y + 212}" fill="${c.ink}" font-size="28" font-weight="800">감각 규칙</text>
    <text x="${x + 106}" y="${y + 248}" fill="${c.muted}" font-size="23">소리, 빛, 진동의 의미를</text>
    <text x="${x + 106}" y="${y + 278}" fill="${c.muted}" font-size="23">단계마다 명확히 분리</text>
  `;
}

function timeline(x, y, items, ink, muted) {
  return items
    .map((item, index) => {
      const yy = y + index * 58;
      return `
        <line x1="${x + 46}" y1="${yy + 18}" x2="${x + 46}" y2="${yy + 76}" stroke="#41567E" stroke-width="4"/>
        <circle cx="${x + 46}" cy="${yy + 18}" r="18" fill="${fade(item.color, 0.18)}" stroke="${item.color}" stroke-width="3"/>
        ${item.icon(x + 46, yy + 18, 11, item.color)}
        <text x="${x + 92}" y="${yy + 14}" fill="${ink}" font-size="28" font-weight="800">${item.title}</text>
        <text x="${x + 282}" y="${yy + 14}" fill="${muted}" font-size="24">${item.sub}</text>
      `;
    })
    .join("");
}

function darkResult(x, y, w, h, color, title, desc, iconSvg, c) {
  return `
    <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="24" fill="#0E1728" stroke="${c.line}" stroke-width="2"/>
    <rect x="${x + 20}" y="${y + 20}" width="78" height="78" rx="18" fill="${fade(color, 0.14)}"/>
    ${iconSvg}
    <text x="${x + 116}" y="${y + 62}" fill="${c.ink}" font-size="30" font-weight="800">${title}</text>
    <text x="${x + 24}" y="${y + 146}" fill="${c.muted}" font-size="22">${desc}</text>
  `;
}

function creamSection(x, y, w, h, title, accent, c, body) {
  return `
    <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="30" fill="#FFFDF8" stroke="${c.line}" stroke-width="2"/>
    <rect x="${x}" y="${y}" width="236" height="64" rx="30" fill="${fade(accent, 0.12)}"/>
    <text x="${x + 32}" y="${y + 42}" fill="${accent}" font-size="30" font-weight="900">${title}</text>
    ${body}
  `;
}

function goalBoard(x, y, c) {
  return `
    <rect x="${x}" y="${y}" width="360" height="270" rx="28" fill="#FBF6E8" stroke="${c.line}" stroke-width="2"/>
    ${iconEar(x + 78, y + 80, 22, c.bell)}
    <text x="${x + 116}" y="${y + 86}" fill="${c.ink}" font-size="31" font-weight="800">목표 1</text>
    <text x="${x + 116}" y="${y + 122}" fill="${c.muted}" font-size="24">눈을 감아도 따라갈 수 있는</text>
    <text x="${x + 116}" y="${y + 152}" fill="${c.muted}" font-size="24">방향 감각 만들기</text>
    ${iconPad(x + 78, y + 194, 22, c.violet)}
    <text x="${x + 116}" y="${y + 200}" fill="${c.ink}" font-size="31" font-weight="800">목표 2</text>
    <text x="${x + 116}" y="${y + 236}" fill="${c.muted}" font-size="24">패드 동작이 감각 해석과</text>
    <text x="${x + 116}" y="${y + 266}" fill="${c.muted}" font-size="24">직결되는 경험 설계</text>
  `;
}

function flowGrid(x, y, items, c) {
  const cardW = 324;
  const cardH = 172;
  const gapX = 28;
  const gapY = 28;
  return items
    .map((item, index) => {
      const col = index % 4;
      const row = Math.floor(index / 4);
      const xx = x + col * (cardW + gapX);
      const yy = y + row * (cardH + gapY);
      return `
        <rect x="${xx}" y="${yy}" width="${cardW}" height="${cardH}" rx="24" fill="#FFFFFF" stroke="${c.line}" stroke-width="2"/>
        <rect x="${xx + 24}" y="${yy + 24}" width="76" height="76" rx="20" fill="${fade(item.color, 0.14)}"/>
        ${item.icon(xx + 62, yy + 62, 22, item.color)}
        <text x="${xx + 122}" y="${yy + 62}" fill="${c.ink}" font-size="29" font-weight="800">${item.title}</text>
        <text x="${xx + 122}" y="${yy + 102}" fill="${c.muted}" font-size="23">${item.sub}</text>
      `;
    })
    .join("");
}

function softResult(x, y, w, h, color, title, desc, iconSvg, c) {
  return `
    <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="24" fill="#FFFFFF" stroke="${c.line}" stroke-width="2"/>
    <rect x="${x + 22}" y="${y + 24}" width="80" height="80" rx="22" fill="${fade(color, 0.14)}"/>
    ${iconSvg}
    <text x="${x + 120}" y="${y + 60}" fill="${c.ink}" font-size="29" font-weight="800">${title}</text>
    <text x="${x + 24}" y="${y + 144}" fill="${c.muted}" font-size="21">${desc}</text>
  `;
}

function waveBundle(x, y, width, color) {
  return Array.from({ length: 9 }, (_, i) => {
    const yy = y + i * 18;
    return `<path d="M ${x} ${yy} C ${x + 90} ${yy - 40}, ${x + 220} ${yy + 30}, ${x + width} ${yy - 10}" fill="none" stroke="${color}" stroke-width="3"/>`;
  }).join("");
}

function dots(x, y, cols, rows, gap, color) {
  let out = "";
  for (let row = 0; row < rows; row += 1) {
    for (let col = 0; col < cols; col += 1) {
      out += `<circle cx="${x + col * gap}" cy="${y + row * gap}" r="3.5" fill="${color}"/>`;
    }
  }
  return out;
}

function fade(hex, alpha) {
  const value = hex.replace("#", "");
  const r = Number.parseInt(value.slice(0, 2), 16);
  const g = Number.parseInt(value.slice(2, 4), 16);
  const b = Number.parseInt(value.slice(4, 6), 16);
  return `rgba(${r},${g},${b},${alpha})`;
}

function iconBell(cx, cy, s, color) {
  const r = s;
  return `
    <path d="M ${cx} ${cy - r * 1.2} C ${cx - r} ${cy - r * 1.2}, ${cx - r * 1.24} ${cy - r * 0.18}, ${cx - r * 1.24} ${cy + r * 0.56} L ${cx - r * 1.56} ${cy + r * 1.36} L ${cx + r * 1.56} ${cy + r * 1.36} L ${cx + r * 1.24} ${cy + r * 0.56} C ${cx + r * 1.24} ${cy - r * 0.18}, ${cx + r} ${cy - r * 1.2}, ${cx} ${cy - r * 1.2} Z" fill="${color}"/>
    <circle cx="${cx}" cy="${cy + r * 1.7}" r="${r * 0.36}" fill="${color}"/>
  `;
}

function iconWalk(cx, cy, s, color) {
  return `
    <circle cx="${cx}" cy="${cy - s * 0.9}" r="${s * 0.42}" fill="${color}"/>
    <line x1="${cx}" y1="${cy - s * 0.46}" x2="${cx}" y2="${cy + s * 0.62}" stroke="${color}" stroke-width="${s * 0.34}" stroke-linecap="round"/>
    <line x1="${cx}" y1="${cy - s * 0.02}" x2="${cx - s * 0.88}" y2="${cy + s * 0.28}" stroke="${color}" stroke-width="${s * 0.28}" stroke-linecap="round"/>
    <line x1="${cx}" y1="${cy - s * 0.02}" x2="${cx + s * 0.82}" y2="${cy + s * 0.16}" stroke="${color}" stroke-width="${s * 0.28}" stroke-linecap="round"/>
    <line x1="${cx}" y1="${cy + s * 0.62}" x2="${cx - s * 0.76}" y2="${cy + s * 1.6}" stroke="${color}" stroke-width="${s * 0.28}" stroke-linecap="round"/>
    <line x1="${cx}" y1="${cy + s * 0.62}" x2="${cx + s * 0.86}" y2="${cy + s * 1.48}" stroke="${color}" stroke-width="${s * 0.28}" stroke-linecap="round"/>
  `;
}

function iconRain(cx, cy, s, color) {
  return `
    <line x1="${cx - s}" y1="${cy - s * 1.2}" x2="${cx - s * 1.26}" y2="${cy - s * 0.1}" stroke="${color}" stroke-width="${s * 0.22}" stroke-linecap="round"/>
    <line x1="${cx}" y1="${cy - s * 1.3}" x2="${cx - s * 0.26}" y2="${cy - s * 0.12}" stroke="${color}" stroke-width="${s * 0.22}" stroke-linecap="round"/>
    <line x1="${cx + s}" y1="${cy - s * 1.2}" x2="${cx + s * 0.74}" y2="${cy - s * 0.1}" stroke="${color}" stroke-width="${s * 0.22}" stroke-linecap="round"/>
    <path d="M ${cx - s * 1.4} ${cy + s * 0.38} Q ${cx - s * 0.8} ${cy - s * 0.2} ${cx} ${cy + s * 0.38}" fill="none" stroke="${color}" stroke-width="${s * 0.2}"/>
    <path d="M ${cx - s * 0.46} ${cy + s * 0.86} Q ${cx} ${cy + s * 0.2} ${cx + s * 0.46} ${cy + s * 0.86}" fill="none" stroke="${color}" stroke-width="${s * 0.2}"/>
    <path d="M ${cx + s * 0.64} ${cy + s * 0.38} Q ${cx + s * 1.2} ${cy - s * 0.2} ${cx + s * 1.8} ${cy + s * 0.38}" fill="none" stroke="${color}" stroke-width="${s * 0.2}"/>
  `;
}

function iconEye(cx, cy, s, color) {
  return `
    <path d="M ${cx - s * 1.8} ${cy} Q ${cx} ${cy - s * 1.3} ${cx + s * 1.8} ${cy} Q ${cx} ${cy + s * 1.3} ${cx - s * 1.8} ${cy}" fill="none" stroke="${color}" stroke-width="${s * 0.26}" stroke-linecap="round"/>
    <circle cx="${cx}" cy="${cy}" r="${s * 0.58}" fill="${color}"/>
  `;
}

function iconWave(cx, cy, s, color) {
  return `
    <circle cx="${cx}" cy="${cy}" r="${s * 0.32}" fill="${color}"/>
    <path d="M ${cx - s * 0.9} ${cy - s * 0.9} A ${s * 1.26} ${s * 1.26} 0 0 0 ${cx - s * 0.9} ${cy + s * 0.9}" fill="none" stroke="${color}" stroke-width="${s * 0.18}" stroke-linecap="round"/>
    <path d="M ${cx + s * 0.9} ${cy - s * 0.9} A ${s * 1.26} ${s * 1.26} 0 0 1 ${cx + s * 0.9} ${cy + s * 0.9}" fill="none" stroke="${color}" stroke-width="${s * 0.18}" stroke-linecap="round"/>
    <path d="M ${cx - s * 1.52} ${cy - s * 1.34} A ${s * 2.08} ${s * 2.08} 0 0 0 ${cx - s * 1.52} ${cy + s * 1.34}" fill="none" stroke="${color}" stroke-width="${s * 0.14}" stroke-linecap="round"/>
    <path d="M ${cx + s * 1.52} ${cy - s * 1.34} A ${s * 2.08} ${s * 2.08} 0 0 1 ${cx + s * 1.52} ${cy + s * 1.34}" fill="none" stroke="${color}" stroke-width="${s * 0.14}" stroke-linecap="round"/>
  `;
}

function iconBoss(cx, cy, s, color) {
  return `
    <circle cx="${cx}" cy="${cy}" r="${s * 0.78}" fill="none" stroke="${color}" stroke-width="${s * 0.22}"/>
    <path d="M ${cx} ${cy - s * 1.4} L ${cx + s * 0.34} ${cy - s * 0.84} L ${cx + s * 1.18} ${cy - s * 1.18} L ${cx + s * 0.84} ${cy - s * 0.34} L ${cx + s * 1.4} ${cy} L ${cx + s * 0.84} ${cy + s * 0.34} L ${cx + s * 1.18} ${cy + s * 1.18} L ${cx + s * 0.34} ${cy + s * 0.84} L ${cx} ${cy + s * 1.4} L ${cx - s * 0.34} ${cy + s * 0.84} L ${cx - s * 1.18} ${cy + s * 1.18} L ${cx - s * 0.84} ${cy + s * 0.34} L ${cx - s * 1.4} ${cy} L ${cx - s * 0.84} ${cy - s * 0.34} L ${cx - s * 1.18} ${cy - s * 1.18} L ${cx - s * 0.34} ${cy - s * 0.84} Z" fill="${fade(color, 0.26)}" stroke="${color}" stroke-width="${s * 0.16}" stroke-linejoin="round"/>
  `;
}

function iconForest(cx, cy, s, color) {
  return `
    <polygon points="${cx},${cy - s * 1.7} ${cx - s * 1.14},${cy - s * 0.28} ${cx - s * 0.46},${cy - s * 0.28} ${cx - s * 1.32},${cy + s * 0.92} ${cx - s * 0.4},${cy + s * 0.92} ${cx - s * 0.4},${cy + s * 1.72} ${cx + s * 0.4},${cy + s * 1.72} ${cx + s * 0.4},${cy + s * 0.92} ${cx + s * 1.32},${cy + s * 0.92} ${cx + s * 0.46},${cy - s * 0.28} ${cx + s * 1.14},${cy - s * 0.28}" fill="${color}"/>
  `;
}

function iconPad(cx, cy, s, color) {
  return `
    <path d="M ${cx - s * 1.6} ${cy + s * 0.2} Q ${cx - s * 1.1} ${cy - s * 1.12} ${cx} ${cy - s * 1.12} Q ${cx + s * 1.1} ${cy - s * 1.12} ${cx + s * 1.6} ${cy + s * 0.2} Q ${cx + s * 1.38} ${cy + s * 1.14} ${cx + s * 0.7} ${cy + s * 1.14} L ${cx - s * 0.7} ${cy + s * 1.14} Q ${cx - s * 1.38} ${cy + s * 1.14} ${cx - s * 1.6} ${cy + s * 0.2} Z" fill="none" stroke="${color}" stroke-width="${s * 0.22}"/>
    <line x1="${cx - s * 0.76}" y1="${cy}" x2="${cx - s * 0.22}" y2="${cy}" stroke="${color}" stroke-width="${s * 0.22}" stroke-linecap="round"/>
    <line x1="${cx - s * 0.49}" y1="${cy - s * 0.28}" x2="${cx - s * 0.49}" y2="${cy + s * 0.28}" stroke="${color}" stroke-width="${s * 0.22}" stroke-linecap="round"/>
    <circle cx="${cx + s * 0.6}" cy="${cy - s * 0.2}" r="${s * 0.18}" fill="${color}"/>
    <circle cx="${cx + s * 0.92}" cy="${cy + s * 0.14}" r="${s * 0.18}" fill="${color}"/>
  `;
}

function iconMonitor(cx, cy, s, color) {
  return `
    <rect x="${cx - s * 1.4}" y="${cy - s}" width="${s * 2.8}" height="${s * 1.8}" rx="${s * 0.22}" fill="none" stroke="${color}" stroke-width="${s * 0.18}"/>
    <line x1="${cx}" y1="${cy + s * 0.84}" x2="${cx}" y2="${cy + s * 1.36}" stroke="${color}" stroke-width="${s * 0.18}" stroke-linecap="round"/>
    <line x1="${cx - s * 0.8}" y1="${cy + s * 1.36}" x2="${cx + s * 0.8}" y2="${cy + s * 1.36}" stroke="${color}" stroke-width="${s * 0.18}" stroke-linecap="round"/>
  `;
}

function iconEar(cx, cy, s, color) {
  return `
    <path d="M ${cx} ${cy - s * 1.2} C ${cx - s * 1.18} ${cy - s * 1.2}, ${cx - s * 1.18} ${cy + s * 1.2}, ${cx} ${cy + s * 1.2} C ${cx + s * 0.56} ${cy + s * 1.2}, ${cx + s * 1.02} ${cy + s * 0.72}, ${cx + s * 1.02} ${cy + s * 0.06} C ${cx + s * 1.02} ${cy - s * 0.44}, ${cx + s * 0.66} ${cy - s * 0.76}, ${cx + s * 0.26} ${cy - s * 0.76}" fill="none" stroke="${color}" stroke-width="${s * 0.2}" stroke-linecap="round"/>
    <path d="M ${cx + s * 0.22} ${cy - s * 0.2} C ${cx - s * 0.3} ${cy - s * 0.12}, ${cx - s * 0.34} ${cy + s * 0.54}, ${cx + s * 0.1} ${cy + s * 0.64}" fill="none" stroke="${color}" stroke-width="${s * 0.18}" stroke-linecap="round"/>
  `;
}

function iconLight(cx, cy, s, color) {
  return `
    <circle cx="${cx}" cy="${cy}" r="${s * 0.3}" fill="${color}"/>
    <circle cx="${cx}" cy="${cy}" r="${s * 0.88}" fill="none" stroke="${color}" stroke-width="${s * 0.18}"/>
    <circle cx="${cx}" cy="${cy}" r="${s * 1.42}" fill="none" stroke="${color}" stroke-width="${s * 0.12}"/>
  `;
}

function iconSpark(cx, cy, s, color) {
  return `
    <path d="M ${cx} ${cy - s * 1.4} L ${cx + s * 0.3} ${cy - s * 0.34} L ${cx + s * 1.2} ${cy - s * 0.82} L ${cx + s * 0.56} ${cy + s * 0.16} L ${cx + s * 1.34} ${cy + s * 0.44} L ${cx + s * 0.34} ${cy + s * 0.66} L ${cx + s * 0.12} ${cy + s * 1.4} L ${cx - s * 0.12} ${cy + s * 0.66} L ${cx - s} ${cy + s * 0.96} L ${cx - s * 0.56} ${cy + s * 0.12} L ${cx - s * 1.34} ${cy - s * 0.24} L ${cx - s * 0.38} ${cy - s * 0.42} Z" fill="${color}"/>
  `;
}

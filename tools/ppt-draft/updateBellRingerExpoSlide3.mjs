import fs from "node:fs";
import path from "node:path";
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, "..", "..");
const require = createRequire(import.meta.url);
const { Resvg } = require(path.join(repoRoot, "tools", "poster-draft", "node_modules", "@resvg", "resvg-js"));

const args = parseArgs(process.argv.slice(2));
if (!args.pptDir) {
  console.error("Usage: node updateBellRingerExpoSlide3.mjs --pptDir <extracted-ppt-dir> [--assetDir <dir>]");
  process.exit(1);
}

const extractedDir = path.resolve(args.pptDir);
const assetDir = path.resolve(args.assetDir ?? path.join(repoRoot, "Docs", "ExpoSlide3Assets"));
const slideXmlPath = path.join(extractedDir, "ppt", "slides", "slide3.xml");
const relsXmlPath = path.join(extractedDir, "ppt", "slides", "_rels", "slide3.xml.rels");
const mediaDir = path.join(extractedDir, "ppt", "media");

ensureFile(slideXmlPath);
ensureFile(relsXmlPath);
fs.mkdirSync(assetDir, { recursive: true });
fs.mkdirSync(mediaDir, { recursive: true });

const colors = {
  accent: "#1DAF88",
  title: "#173B39",
  ink: "#213847",
  muted: "#647786",
  line: "#D7E3E8",
  panel: "#FFFFFF",
  softPanel: "#F6FBFC",
  darkPanel: "#132128",
  bell: "#05FF1F",
  wall: "#05D9FF",
  rain: "#0514FF",
  tinnitus: "#750DFF",
  pad: "#F2B733",
  padRuntime: "#FF4705",
  heroTop: "#17363C",
  heroBottom: "#0B171B"
};

const copy = {
  overview: [
    "플레이어는 눈을 감거나 시야를 차단한 상태에서, 소리·빛·진동만으로 공간을 인지합니다.",
    "종소리를 따라 이동하고, 비와 바람 속에서도 목표 방향을 분리해 추적합니다.",
    "패드의 위치와 회전을 맞춰 이명을 정화하며 감각 규칙을 익히는 체험형 게임입니다."
  ],
  background: [
    "이 프로젝트는 접근성 보조보다, 시각을 거의 쓰지 않을 때 어떤 감각 규칙의 게임이 성립하는지에 대한 호기심에서 출발했습니다.",
    "소리, 눈앞 LED 빛, 패드 진동만으로도 선명하고 인상적인 플레이 경험을 만들 수 있는지 확인하고자 했습니다.",
    "이를 위해 ArUco 기반 패드 위치 추적, IMU 기반 머리·패드 회전 인식, 관람자 화면을 하나의 루프로 구성했습니다.",
    "폐안 상태에서 구분이 쉬운 빛 색을 직접 테스트해 종=초록, 벽=시안, 비=딥 블루, 이명=바이올렛, 패드=노랑·골드 계열로 반영했습니다."
  ],
  content: [
    "최종 데모의 진행 흐름과 감각 규칙을 한 장의 인포그래픽으로 정리했습니다.",
    "종 오리엔테이션 → 비바람 속 종 추적 → 종 바라보기 ×3 → 일반 이명 ×2 → 보스 이명 ×3 → 숲 엔딩"
  ],
  result: [
    "ArUco 기반 패드 위치 추적과 IMU 회전 추적을 결합했습니다.",
    "공간 음향, 눈앞 LED, 진동, 관람자 화면을 하나의 실시간 루프로 연결했습니다.",
    "아래 이미지는 실제 관람자 화면 예시, 패드 추적 구조, 현재 하드웨어 구성을 보여줍니다."
  ]
};

const sources = {
  heroOverride: path.join(repoRoot, "Docs", "ExpoSlide3Assets", "slide3_section02_hero_custom.png"),
  observerBell: path.join(repoRoot, "Temp", "ObserverVisualCaptures", "01_pad_bell.png"),
  observerRain: path.join(repoRoot, "Temp", "ObserverVisualCaptures", "02_rain.png"),
  observerForest: path.join(repoRoot, "Temp", "ObserverVisualCaptures", "05_forest.png"),
  controller: path.join(repoRoot, "Assets", "ThirdParty", "ObserverSource", "PolyPizza", "controller-poly-pizza.jpg"),
  esp32: path.join(repoRoot, "esp32-s3.jpg"),
  arucoBoard: path.join(repoRoot, "Docs", "Printables", "PadArucoBoard", "pad-aruco-v-board-assembly-schematic.svg")
};

const mediaSpecs = buildMediaSpecs();
writeMediaAssets(mediaSpecs, assetDir, mediaDir);

let slideXml = fs.readFileSync(slideXmlPath, "utf8");
let relsXml = fs.readFileSync(relsXmlPath, "utf8");

slideXml = replaceTextBoxByName(slideXml, "TextBox 31", buildBulletTextBody(copy.overview, 3300));
slideXml = replaceTextBoxByName(slideXml, "TextBox 5", buildBulletTextBody(copy.background, 2900));
slideXml = replaceTextBoxByName(slideXml, "TextBox 16", buildBulletTextBody(copy.content, 3000));
slideXml = replaceTextBoxByName(slideXml, "TextBox 11", buildBulletTextBody(copy.result, 2900));

slideXml = replacePictureEmbedById(slideXml, "14", "rId2");
slideXml = replacePictureEmbedById(slideXml, "18", "rId4");
slideXml = replacePictureEmbedById(slideXml, "15", "rId3");

if (!slideXml.includes('name="Image 119"')) {
  slideXml = insertBeforeClosingTag(
    slideXml,
    "</p:spTree>",
    createPictureBlock({
      shapeId: 119,
      shapeName: "Image 119",
      relId: "rId5",
      x: 2450718,
      y: 23890000,
      cx: 16350000,
      cy: 5350000
    })
  );
}

relsXml = ensureRelationship(relsXml, "rId2", "../media/slide3_section02_hero.png");
relsXml = ensureRelationship(relsXml, "rId3", "../media/slide3_section03_flow.png");
relsXml = ensureRelationship(relsXml, "rId4", "../media/slide3_section02_tech_strip.png");
relsXml = ensureRelationship(relsXml, "rId5", "../media/slide3_section04_result_panel.png");

fs.writeFileSync(slideXmlPath, slideXml, "utf8");
fs.writeFileSync(relsXmlPath, relsXml, "utf8");

console.log(`Updated ${slideXmlPath}`);
console.log(`Updated ${relsXmlPath}`);
for (const spec of mediaSpecs) {
  console.log(`Wrote ${path.join(assetDir, spec.fileName)}`);
}

function buildMediaSpecs() {
  return [
    {
      fileName: "slide3_section02_hero.png",
      svgName: "slide3_section02_hero.svg",
      width: 1800,
      height: 1800,
      svg: buildSection02HeroSvg()
    },
    {
      fileName: "slide3_section02_tech_strip.png",
      svgName: "slide3_section02_tech_strip.svg",
      width: 3200,
      height: 1100,
      svg: buildSection02TechStripSvg()
    },
    {
      fileName: "slide3_section03_flow.png",
      svgName: "slide3_section03_flow.svg",
      width: 4200,
      height: 1890,
      svg: buildSection03FlowSvg()
    },
    {
      fileName: "slide3_section04_result_panel.png",
      svgName: "slide3_section04_result_panel.svg",
      width: 4200,
      height: 1380,
      svg: buildSection04ResultSvg()
    }
  ];
}

function writeMediaAssets(specs, outDir, pptMediaDir) {
  for (const spec of specs) {
    const svgPath = path.join(outDir, spec.svgName);
    const pngPath = path.join(outDir, spec.fileName);

    if (spec.fileName === "slide3_section02_hero.png" && fs.existsSync(sources.heroOverride)) {
      fs.copyFileSync(sources.heroOverride, pngPath);
      fs.writeFileSync(svgPath, buildHeroOverrideNoteSvg(), "utf8");
    } else {
      fs.writeFileSync(svgPath, spec.svg, "utf8");
      const rendered = new Resvg(spec.svg, {
        fitTo: {
          mode: "width",
          value: spec.width
        }
      }).render();
      fs.writeFileSync(pngPath, rendered.asPng());
    }

    fs.copyFileSync(pngPath, path.join(pptMediaDir, spec.fileName));
  }
}

function buildSection02HeroSvg() {
  return wrapSvg(
    1800,
    1800,
    `
    <defs>
      <linearGradient id="heroBg" x1="0" x2="1" y1="0" y2="1">
        <stop offset="0%" stop-color="${colors.heroTop}"/>
        <stop offset="55%" stop-color="#11252A"/>
        <stop offset="100%" stop-color="${colors.heroBottom}"/>
      </linearGradient>
      <filter id="heroGlow" x="-30%" y="-30%" width="160%" height="160%">
        <feGaussianBlur stdDeviation="20" result="blur"/>
        <feMerge>
          <feMergeNode in="blur"/>
          <feMergeNode in="SourceGraphic"/>
        </feMerge>
      </filter>
      <linearGradient id="floorFade" x1="0" x2="0" y1="0" y2="1">
        <stop offset="0%" stop-color="#2A3E43" stop-opacity="0"/>
        <stop offset="100%" stop-color="#2A3E43" stop-opacity="0.82"/>
      </linearGradient>
    </defs>
    <rect width="1800" height="1800" rx="96" fill="url(#heroBg)"/>
    <circle cx="380" cy="360" r="210" fill="${colors.rain}" opacity="0.14" filter="url(#heroGlow)"/>
    <circle cx="1380" cy="350" r="180" fill="${colors.tinnitus}" opacity="0.12" filter="url(#heroGlow)"/>
    <circle cx="1300" cy="1280" r="220" fill="${colors.bell}" opacity="0.12" filter="url(#heroGlow)"/>
    <rect x="0" y="1120" width="1800" height="680" fill="url(#floorFade)"/>

    <path d="M124 1268 C 350 1182, 610 1176, 860 1250" fill="none" stroke="${colors.wall}" stroke-width="14" stroke-dasharray="10 24" opacity="0.7"/>
    <path d="M102 1324 C 318 1242, 618 1232, 928 1320" fill="none" stroke="${colors.wall}" stroke-width="8" stroke-dasharray="8 16" opacity="0.45"/>
    ${rainDrop(300, 1194, 22)}
    ${rainDrop(392, 1270, 14)}
    ${rainDrop(506, 1226, 18)}
    ${rainDrop(644, 1302, 12)}

    <circle cx="906" cy="792" r="432" fill="#112127" stroke="#2B4C50" stroke-width="3"/>
    <path d="M710 768 C 784 664, 868 614, 952 614 C 1036 614, 1120 664, 1194 768" fill="none" stroke="#F4F7FA" stroke-width="18" stroke-linecap="round"/>
    <path d="M756 866 C 808 816, 860 816, 912 866" fill="none" stroke="#F4F7FA" stroke-width="16" stroke-linecap="round"/>
    <path d="M994 866 C 1046 816, 1098 816, 1150 866" fill="none" stroke="#F4F7FA" stroke-width="16" stroke-linecap="round"/>
    <path d="M720 952 C 792 1020, 882 1062, 952 1062 C 1022 1062, 1112 1020, 1184 952" fill="none" stroke="#88A9AF" stroke-width="10" stroke-linecap="round"/>

    <path d="M1306 680 H 1382 L 1424 840 H 1264 Z" fill="#3C3720"/>
    <rect x="1330" y="624" width="28" height="54" rx="14" fill="#3C3720"/>
    <circle cx="1344" cy="792" r="78" fill="${colors.bell}" opacity="0.24" filter="url(#heroGlow)"/>
    <circle cx="1344" cy="792" r="42" fill="${colors.bell}"/>
    <path d="M1300 902 C 1300 934, 1324 956, 1356 956 C 1388 956, 1412 934, 1412 902" fill="none" stroke="${colors.bell}" stroke-width="12" stroke-linecap="round"/>
    <path d="M1282 760 C 1226 734, 1202 694, 1192 656" fill="none" stroke="${colors.bell}" stroke-width="8" opacity="0.68"/>
    <path d="M1416 760 C 1476 742, 1514 704, 1534 662" fill="none" stroke="${colors.bell}" stroke-width="8" opacity="0.68"/>

    <circle cx="1382" cy="420" r="58" fill="${colors.tinnitus}" opacity="0.22" filter="url(#heroGlow)"/>
    <circle cx="1382" cy="420" r="24" fill="${colors.tinnitus}"/>
    <path d="M1298 404 C 1242 394, 1196 414, 1160 458" fill="none" stroke="${colors.tinnitus}" stroke-width="8" opacity="0.56"/>

    <path d="M380 252 C 560 210, 704 230, 842 304" fill="none" stroke="${colors.wall}" stroke-width="8" opacity="0.25"/>
    <path d="M1048 266 C 1160 228, 1280 234, 1390 308" fill="none" stroke="${colors.tinnitus}" stroke-width="7" opacity="0.34"/>

    <rect x="1106" y="1268" width="322" height="146" rx="34" fill="${colors.pad}" opacity="0.18"/>
    <rect x="1140" y="1302" width="254" height="78" rx="24" fill="${colors.pad}"/>
    <rect x="1214" y="1328" width="108" height="26" rx="13" fill="#FFF3CB"/>
    <circle cx="1178" cy="1340" r="18" fill="#3C3F43"/>
    <circle cx="1358" cy="1340" r="18" fill="#3C3F43"/>
    <circle cx="1198" cy="1340" r="7" fill="${colors.padRuntime}" opacity="0.5"/>
    `
  );
}

function buildSection02TechStripSvg() {
  const chips = [
    ["ArUco 위치", "패드 위치 추적"],
    ["IMU 회전", "머리·패드 회전 인식"],
    ["폐안 LED 색", "종·벽·비·이명·패드 분리"],
    ["3D 공간 음향", "방향과 거리 인지"],
    ["진동 피드백", "lock·치유·보스 추적"],
    ["관람자 화면", "상태와 감각 규칙 확인"]
  ];
  const chipColors = [colors.bell, colors.pad, colors.wall, colors.rain, colors.tinnitus, colors.accent];
  const chipSvg = chips.map((chip, index) => {
    const x = 74 + index * 510;
    const color = chipColors[index];
    return `
      <rect x="${x}" y="196" width="430" height="246" rx="34" fill="#FFFFFF" stroke="#D8E5EA" stroke-width="3"/>
      <circle cx="${x + 66}" cy="258" r="28" fill="${color}" opacity="0.18"/>
      <circle cx="${x + 66}" cy="258" r="18" fill="${color}"/>
      <text x="${x + 112}" y="252" font-size="38" font-weight="700" fill="#1D3A45" font-family="Malgun Gothic, Pretendard, Arial">${chip[0]}</text>
      <text x="${x + 112}" y="312" font-size="28" fill="#56707C" font-family="Malgun Gothic, Pretendard, Arial">${chip[1]}</text>`;
  }).join("");

  return wrapSvg(
    3200,
    1100,
    `
    <rect width="3200" height="1100" rx="44" fill="#F8FBFC"/>
    <rect x="18" y="18" width="3164" height="1064" rx="34" fill="#FFFFFF" stroke="#D9E4EA" stroke-width="3"/>
    <text x="78" y="110" font-size="50" font-weight="700" fill="#1B3945" font-family="Malgun Gothic, Pretendard, Arial">구현 핵심</text>
    <text x="78" y="162" font-size="28" fill="#627583" font-family="Malgun Gothic, Pretendard, Arial">소리, 빛, 진동, 추적 장치를 하나의 플레이 루프로 묶기 위해 사용한 요소</text>
    ${chipSvg}
    <rect x="74" y="518" width="3052" height="480" rx="32" fill="#F4F9FA" stroke="#DCE7EC" stroke-width="3"/>
    <text x="118" y="588" font-size="34" font-weight="700" fill="#21424C" font-family="Malgun Gothic, Pretendard, Arial">색 선택 근거</text>
    <text x="118" y="638" font-size="26" fill="#627583" font-family="Malgun Gothic, Pretendard, Arial">폐안 상태에서 서로 겹쳐 보이지 않도록, 색보다도 위치와 패턴이 먼저 읽히도록 조합했습니다.</text>
    ${legendChip(122, 704, colors.bell, "종", "초록")}
    ${legendChip(622, 704, colors.wall, "벽", "시안")}
    ${legendChip(1122, 704, colors.rain, "비", "딥 블루")}
    ${legendChip(1622, 704, colors.tinnitus, "이명", "바이올렛")}
    ${legendChip(2122, 704, colors.pad, "패드", "노랑·골드")}
    <text x="120" y="906" font-size="24" fill="#60727E" font-family="Malgun Gothic, Pretendard, Arial">패드는 런타임 구현상 주황 계열 값도 존재하지만, 발표 및 관람자 화면에서는 노랑·골드 계열이 더 읽기 좋아 그 방향으로 설명합니다.</text>
    `
  );
}

function buildSection03FlowSvg() {
  const steps = [
    {
      number: 1,
      title: "종 오리엔테이션",
      lines: ["왼쪽 가까운 종으로 시작", "소리와 빛이 연결된 감각 소개"],
      color: colors.accent,
      icon: flowIconHead
    },
    {
      number: 2,
      title: "종 추적 / 비바람",
      lines: ["종 방향으로 이동", "비와 바람 속에서도 분리 추적"],
      color: colors.bell,
      icon: flowIconBell
    },
    {
      number: 3,
      title: "종 바라보기 ×3",
      lines: ["머리 회전으로 종을 응시", "잠깐 벗어나도 진행도 유지"],
      color: colors.rain,
      icon: flowIconRain
    },
    {
      number: 4,
      title: "종 획득",
      lines: ["짧은 정적과 진동", "정화 단계로 규칙 전환"],
      color: colors.pad,
      icon: flowIconGate
    },
    {
      number: 5,
      title: "일반 이명 ×2",
      lines: ["패드 위치 + 회전 pose 맞춤", "4초 유지로 정화"],
      color: colors.wall,
      icon: flowIconWave
    },
    {
      number: 6,
      title: "보스 이명 ×3",
      lines: ["2초 고정 + 느린 이동 추적", "실패 시 해당 패턴 리셋"],
      color: colors.tinnitus,
      icon: flowIconBoss
    },
    {
      number: 7,
      title: "숲 엔딩",
      lines: ["정적 후 숲 소리 전환", "앞쪽 종소리를 따라 마무리"],
      color: "#5DAD73",
      icon: flowIconForest
    }
  ];

  const cards = steps.map((step, index) => flowCard(step, index)).join("");

  return wrapSvg(
    4200,
    1890,
    `
    <rect width="4200" height="1890" rx="40" fill="#F7FBFC"/>
    <rect x="24" y="24" width="4152" height="1842" rx="28" fill="#FFFFFF" stroke="#D9E4EA" stroke-width="3"/>
    <text x="88" y="90" font-size="54" font-weight="700" fill="#1C3843" font-family="Malgun Gothic, Pretendard, Arial">데모 진행 흐름</text>
    <text x="88" y="142" font-size="28" fill="#627583" font-family="Malgun Gothic, Pretendard, Arial">종 탐색에서 숲 엔딩까지, 감각 규칙이 어떻게 단계별로 바뀌는지 한 장에 정리했습니다.</text>
    ${cards}
    <rect x="88" y="1188" width="4024" height="194" rx="26" fill="#F4F9FA" stroke="#DCE7EC" stroke-width="3"/>
    <text x="132" y="1260" font-size="34" font-weight="700" fill="#20404A" font-family="Malgun Gothic, Pretendard, Arial">빛과 색 규칙</text>
    ${legendChip(132, 1304, colors.bell, "종", "초록")}
    ${legendChip(672, 1304, colors.wall, "벽", "시안")}
    ${legendChip(1212, 1304, colors.rain, "비", "딥 블루")}
    ${legendChip(1752, 1304, colors.tinnitus, "이명", "바이올렛")}
    ${legendChip(2292, 1304, colors.pad, "패드", "노랑·골드")}

    <rect x="88" y="1416" width="4024" height="350" rx="26" fill="${colors.darkPanel}"/>
    <text x="132" y="1494" font-size="36" font-weight="700" fill="#F0F6F8" font-family="Malgun Gothic, Pretendard, Arial">입력과 피드백 구조</text>
    <text x="132" y="1558" font-size="28" fill="#C2D2D8" font-family="Malgun Gothic, Pretendard, Arial">입력: 종·비·바람·이명의 3D 공간 음향 / 머리 회전 / 패드 위치·yaw·pitch·roll</text>
    <text x="132" y="1616" font-size="28" fill="#C2D2D8" font-family="Malgun Gothic, Pretendard, Arial">피드백: 눈앞 LED, 패드 진동, 관람자 화면, 그리고 단계마다 바뀌는 종·이명·엔딩 사운드</text>
    <text x="132" y="1674" font-size="28" fill="#C2D2D8" font-family="Malgun Gothic, Pretendard, Arial">핵심 구현: ArUco 기반 패드 위치 추적 + IMU 회전 보정 + ESP32-S3 LED·진동 출력</text>
    `
  );
}

function buildSection04ResultSvg() {
  const rainData = fileToDataUri(requireFile(sources.observerRain));
  const bellData = fileToDataUri(requireFile(sources.observerBell));
  const forestData = fileToDataUri(requireFile(sources.observerForest));
  const esp32Data = fileToDataUri(requireFile(sources.esp32));
  const arucoData = fileToDataUri(requireFile(sources.arucoBoard));
  const controllerData = fileToDataUri(requireFile(sources.controller));

  return wrapSvg(
    4200,
    1380,
    `
    <rect width="4200" height="1380" rx="40" fill="#F8FBFC"/>
    <rect x="24" y="24" width="4152" height="1332" rx="28" fill="#FFFFFF" stroke="#D9E4EA" stroke-width="3"/>
    <text x="88" y="100" font-size="54" font-weight="700" fill="#1C3843" font-family="Malgun Gothic, Pretendard, Arial">프로토타입 결과</text>
    <text x="88" y="152" font-size="28" fill="#627583" font-family="Malgun Gothic, Pretendard, Arial">사운드, LED, 진동, 추적 장치를 실제 플레이 가능한 루프로 연결한 현재 구성</text>

    ${observerCard(88, 228, 1240, 980, rainData, bellData, forestData)}
    ${trackingCard(1492, 228, 1240, 980, arucoData, controllerData)}
    ${hardwareCard(2896, 228, 1216, 980, esp32Data)}
    `
  );
}

function observerCard(x, y, w, h, largeImage, bellImage, forestImage) {
  return `
    <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="30" fill="#FFFFFF" stroke="#DCE6EB" stroke-width="3"/>
    <text x="${x + 40}" y="${y + 62}" font-size="38" font-weight="700" fill="#1E3944" font-family="Malgun Gothic, Pretendard, Arial">관람자 화면</text>
    <text x="${x + 40}" y="${y + 102}" font-size="24" fill="#647786" font-family="Malgun Gothic, Pretendard, Arial">플레이어 상태와 감각 규칙을 함께 보여주는 실제 관람자 캡처 예시</text>

    <rect x="${x + 38}" y="${y + 130}" width="${w - 76}" height="456" rx="24" fill="#ECF5F7" stroke="#D7E3E8" stroke-width="3"/>
    <image href="${largeImage}" x="${x + 60}" y="${y + 152}" width="${w - 120}" height="412" preserveAspectRatio="xMidYMid meet"/>

    <rect x="${x + 38}" y="${y + 628}" width="${w - 76}" height="292" rx="24" fill="#F7FBFC" stroke="#DCE6EB" stroke-width="3"/>
    <text x="${x + 72}" y="${y + 684}" font-size="28" font-weight="700" fill="#21404A" font-family="Malgun Gothic, Pretendard, Arial">실제 장면 예시</text>
    <rect x="${x + 72}" y="${y + 714}" width="482" height="174" rx="18" fill="#ECF5F7"/>
    <image href="${bellImage}" x="${x + 84}" y="${y + 726}" width="458" height="150" preserveAspectRatio="xMidYMid meet"/>
    <text x="${x + 72}" y="${y + 910}" font-size="22" fill="#647786" font-family="Malgun Gothic, Pretendard, Arial">종/패드 연결 장면</text>
    <rect x="${x + 594}" y="${y + 714}" width="482" height="174" rx="18" fill="#ECF5F7"/>
    <image href="${forestImage}" x="${x + 606}" y="${y + 726}" width="458" height="150" preserveAspectRatio="xMidYMid meet"/>
    <text x="${x + 594}" y="${y + 910}" font-size="22" fill="#647786" font-family="Malgun Gothic, Pretendard, Arial">숲 엔딩 장면</text>
    `;
}

function trackingCard(x, y, w, h, arucoImage, controllerImage) {
  return `
    <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="30" fill="#FFFFFF" stroke="#DCE6EB" stroke-width="3"/>
    <text x="${x + 40}" y="${y + 62}" font-size="38" font-weight="700" fill="#1E3944" font-family="Malgun Gothic, Pretendard, Arial">패드 추적 구조</text>
    <text x="${x + 40}" y="${y + 102}" font-size="24" fill="#647786" font-family="Malgun Gothic, Pretendard, Arial">ArUco 위치 + 카메라 yaw, IMU pitch/roll을 정답 pose 판정에 결합</text>

    <rect x="${x + 38}" y="${y + 130}" width="${w - 76}" height="338" rx="24" fill="#ECF5F7" stroke="#D7E3E8" stroke-width="3"/>
    ${trackingNode(x + 116, y + 208, 222, 132, "카메라", "패드 위치·yaw")}
    ${trackingArrow(x + 342, y + 274, x + 432)}
    ${trackingNode(x + 448, y + 208, 246, 132, "ArUco V-board", "패드 전면 마커")}
    ${trackingArrow(x + 694, y + 274, x + 778)}
    ${trackingNode(x + 794, y + 208, 206, 132, "Pad IMU", "pitch·roll")}
    ${trackingArrow(x + 1000, y + 274, x + 1090)}
    ${trackingNode(x + 1106, y + 198, 184, 152, "Pose", "위치 + 회전", colors.pad)}

    <image href="${controllerImage}" x="${x + 88}" y="${y + 506}" width="420" height="226" preserveAspectRatio="xMidYMid meet"/>
    <rect x="${x + 548}" y="${y + 506}" width="${w - 588}" height="214" rx="22" fill="#F7FBFC" stroke="#DCE6EB" stroke-width="3"/>
    <image href="${arucoImage}" x="${x + 574}" y="${y + 528}" width="288" height="170" preserveAspectRatio="xMidYMid meet"/>
    <text x="${x + 892}" y="${y + 572}" font-size="28" font-weight="700" fill="#24424C" font-family="Malgun Gothic, Pretendard, Arial">Aruco V-board</text>
    <text x="${x + 892}" y="${y + 620}" font-size="24" fill="#627583" font-family="Malgun Gothic, Pretendard, Arial">패드 전면 마커로 위치와 yaw를 안정적으로 추적</text>
    <text x="${x + 892}" y="${y + 662}" font-size="24" fill="#627583" font-family="Malgun Gothic, Pretendard, Arial">pitch·roll은 IMU를 결합해 일반/보스 이명 정화에 사용</text>

    <rect x="${x + 38}" y="${y + 770}" width="${w - 76}" height="150" rx="22" fill="${colors.darkPanel}"/>
    ${bulletLine(x + 76, y + 834, colors.bell, "카메라: 패드 위치와 yaw 추적")}
    ${bulletLine(x + 76, y + 882, colors.pad, "IMU: pitch·roll 보정 및 motion intensity")}
    ${bulletLine(x + 610, y + 834, colors.wall, "정답 pose = 위치 + yaw + pitch + roll")}
    ${bulletLine(x + 610, y + 882, colors.tinnitus, "결과: 일반/보스 이명 정화 판정")}
    `;
}

function hardwareCard(x, y, w, h, esp32Image) {
  return `
    <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="30" fill="#FFFFFF" stroke="#DCE6EB" stroke-width="3"/>
    <text x="${x + 40}" y="${y + 62}" font-size="38" font-weight="700" fill="#1E3944" font-family="Malgun Gothic, Pretendard, Arial">하드웨어 구성</text>
    <text x="${x + 40}" y="${y + 102}" font-size="24" fill="#647786" font-family="Malgun Gothic, Pretendard, Arial">헤드 / 패드 장치를 실제 게임 루프와 직접 연결한 현재 구성</text>

    <rect x="${x + 40}" y="${y + 134}" width="${w - 80}" height="320" rx="24" fill="#EEF6F8" stroke="#D7E2E8" stroke-width="3"/>
    <image href="${esp32Image}" x="${x + 62}" y="${y + 156}" width="420" height="276" preserveAspectRatio="xMidYMid meet"/>
    <rect x="${x + 522}" y="${y + 156}" width="${w - 584}" height="74" rx="18" fill="#F4F9FA"/>
    <text x="${x + 554}" y="${y + 202}" font-size="30" font-weight="700" fill="#1E3B45" font-family="Malgun Gothic, Pretendard, Arial">ESP32-S3 기반 LED / IMU 제어</text>
    <text x="${x + 554}" y="${y + 256}" font-size="24" fill="#657988" font-family="Malgun Gothic, Pretendard, Arial">헤드 측 LED 2장과 머리 IMU, 패드 측 IMU와 진동 출력을 분리 연결</text>
    <text x="${x + 554}" y="${y + 304}" font-size="24" fill="#657988" font-family="Malgun Gothic, Pretendard, Arial">ArUco 추적과 결합해 폐안 상태에서도 패드 pose 판정이 가능하도록 구성</text>

    <rect x="${x + 40}" y="${y + 500}" width="${w - 80}" height="410" rx="24" fill="${colors.darkPanel}"/>
    <text x="${x + 78}" y="${y + 572}" font-size="34" font-weight="700" fill="#F1F7F8" font-family="Malgun Gothic, Pretendard, Arial">현재 발표에서 강조할 구현 포인트</text>
    ${bulletLine(x + 78, y + 640, colors.bell, "종, 비, 벽, 이명의 소리와 LED 색 규칙을 일관되게 연결")}
    ${bulletLine(x + 78, y + 700, colors.pad, "패드 위치 + 회전값을 정답 pose와 비교해 일반/보스 이명 정화 판정")}
    ${bulletLine(x + 78, y + 760, colors.wall, "ArUco 위치 추적과 IMU 보정을 결합해 폐안 상태에서도 위치 정보 유지")}
    ${bulletLine(x + 78, y + 820, colors.tinnitus, "관람자 화면에서 플레이어 상태와 게임 내부 오브젝트를 동시에 확인")}
    `;
}

function flowCard(step, index) {
  const cardWidth = 540;
  const gap = 28;
  const x = 88 + index * (cardWidth + gap);
  const y = 224;
  const lines = step.lines.map((line, lineIndex) => `
    <text x="${x + 44}" y="${y + 388 + lineIndex * 40}" font-size="24" fill="#627583" font-family="Malgun Gothic, Pretendard, Arial">${line}</text>
  `).join("");

  return `
    <rect x="${x}" y="${y}" width="${cardWidth}" height="902" rx="28" fill="#FFFFFF" stroke="#DCE6EB" stroke-width="3"/>
    <circle cx="${x + 46}" cy="${y + 46}" r="28" fill="${step.color}"/>
    <text x="${x + 36}" y="${y + 56}" font-size="28" font-weight="700" fill="#FFFFFF" font-family="Malgun Gothic, Pretendard, Arial">${step.number}</text>
    <text x="${x + 90}" y="${y + 58}" font-size="34" font-weight="700" fill="#1E3944" font-family="Malgun Gothic, Pretendard, Arial">${step.title}</text>
    <rect x="${x + 34}" y="${y + 98}" width="${cardWidth - 68}" height="244" rx="24" fill="#F4F9FA"/>
    ${step.icon(x + cardWidth / 2, y + 222)}
    ${lines}
    <rect x="${x + 34}" y="${y + 468}" width="${cardWidth - 68}" height="338" rx="22" fill="#F7FBFC" stroke="#DDE7EC" stroke-width="3"/>
    <text x="${x + 58}" y="${y + 532}" font-size="26" font-weight="700" fill="#20404A" font-family="Malgun Gothic, Pretendard, Arial">핵심 규칙</text>
    <text x="${x + 58}" y="${y + 584}" font-size="24" fill="#627583" font-family="Malgun Gothic, Pretendard, Arial">${flowRuleText(step.number)[0]}</text>
    <text x="${x + 58}" y="${y + 626}" font-size="24" fill="#627583" font-family="Malgun Gothic, Pretendard, Arial">${flowRuleText(step.number)[1]}</text>
    <text x="${x + 58}" y="${y + 668}" font-size="24" fill="#627583" font-family="Malgun Gothic, Pretendard, Arial">${flowRuleText(step.number)[2]}</text>
    <rect x="${x + 34}" y="${y + 828}" width="${cardWidth - 68}" height="66" rx="18" fill="${step.color}" opacity="0.12"/>
    <text x="${x + 58}" y="${y + 870}" font-size="23" fill="#365461" font-family="Malgun Gothic, Pretendard, Arial">${flowFooterText(step.number)}</text>
    `;
}

function flowRuleText(stepNumber) {
  switch (stepNumber) {
    case 1:
      return ["첫 종은 왼쪽 가까이에서 시작", "플레이어는 고개를 돌리며 종의 위치를 인지", "소리와 빛이 함께 반응한다는 규칙을 소개"];
    case 2:
      return ["왼쪽 스틱으로 실제 이동", "특정 구역에 들어가면 비·바람이 자동 시작", "패드를 흔들면 종소리가 다시 또렷해짐"];
    case 3:
      return ["머리 회전으로 종을 일정 시간 바라보기", "시선이 벗어나도 진행도는 유지", "총 3회 성공 + 2회 종 이동"];
    case 4:
      return ["짧은 정적과 진동으로 규칙 전환", "이후부터 패드가 정화 장치 역할 수행", "종은 길잡이이자 정화 감각의 상징"];
    case 5:
      return ["이명은 머리로 볼 때만 LED 표시", "패드 pose를 맞추면 4초 동안 정화", "범위를 벗어나면 일시정지되지만 초기화는 안 됨"];
    case 6:
      return ["2초 고정 후 아주 느리게 이동", "허용 범위를 벗어나면 패턴 처음으로 리셋", "총 3개 패턴(8.0 / 8.5 / 9.0초)"];
    default:
      return ["대왕 이명 해소 후 짧은 정적", "숲 소리와 새 소리가 서서히 등장", "앞쪽 종소리를 따라가거나 10초 뒤 자동 종료"];
  }
}

function flowFooterText(stepNumber) {
  switch (stepNumber) {
    case 1: return "도입";
    case 2: return "추적";
    case 3: return "시선";
    case 4: return "획득";
    case 5: return "정화";
    case 6: return "보스";
    default: return "엔딩";
  }
}

function legendChip(x, y, color, label, desc) {
  return `
    <rect x="${x}" y="${y}" width="420" height="104" rx="22" fill="#FFFFFF" stroke="#D9E4EA" stroke-width="3"/>
    <circle cx="${x + 42}" cy="${y + 52}" r="18" fill="${color}"/>
    <text x="${x + 78}" y="${y + 46}" font-size="30" font-weight="700" fill="#1D3A45" font-family="Malgun Gothic, Pretendard, Arial">${label}</text>
    <text x="${x + 78}" y="${y + 80}" font-size="22" fill="#627583" font-family="Malgun Gothic, Pretendard, Arial">${desc}</text>
  `;
}

function bulletLine(x, y, color, text) {
  return `
    <circle cx="${x}" cy="${y - 8}" r="12" fill="${color}"/>
    <text x="${x + 30}" y="${y}" font-size="27" fill="#C5D6DC" font-family="Malgun Gothic, Pretendard, Arial">${text}</text>
  `;
}

function trackingNode(x, y, w, h, title, desc, fill = "#FFFFFF") {
  return `
    <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="22" fill="${fill}" stroke="#D8E4E9" stroke-width="3"/>
    <text x="${x + 26}" y="${y + 56}" font-size="30" font-weight="700" fill="#1D3945" font-family="Malgun Gothic, Pretendard, Arial">${title}</text>
    <text x="${x + 26}" y="${y + 98}" font-size="22" fill="#627583" font-family="Malgun Gothic, Pretendard, Arial">${desc}</text>
  `;
}

function trackingArrow(x1, y, x2) {
  return `
    <line x1="${x1}" y1="${y}" x2="${x2}" y2="${y}" stroke="#8EA5AF" stroke-width="8" stroke-linecap="round"/>
    <path d="M ${x2 - 24} ${y - 18} L ${x2} ${y} L ${x2 - 24} ${y + 18}" fill="none" stroke="#8EA5AF" stroke-width="8" stroke-linecap="round" stroke-linejoin="round"/>
  `;
}

function rainDrop(cx, cy, r) {
  return `
    <circle cx="${cx}" cy="${cy}" r="${r}" fill="${colors.rain}" opacity="0.86"/>
    <path d="M ${cx} ${cy - r * 2.4} C ${cx - r * 0.9} ${cy - r * 1.3}, ${cx - r * 0.9} ${cy - r * 0.2}, ${cx} ${cy + r * 0.4} C ${cx + r * 0.9} ${cy - r * 0.2}, ${cx + r * 0.9} ${cy - r * 1.3}, ${cx} ${cy - r * 2.4}" fill="${colors.rain}" opacity="0.34"/>
  `;
}

function flowIconHead(cx, cy) {
  return `
    <circle cx="${cx}" cy="${cy}" r="82" fill="none" stroke="${colors.accent}" stroke-width="10"/>
    <path d="M ${cx - 52} ${cy - 16} C ${cx - 20} ${cy - 54}, ${cx + 20} ${cy - 54}, ${cx + 52} ${cy - 16}" fill="none" stroke="${colors.accent}" stroke-width="10" stroke-linecap="round"/>
    <path d="M ${cx - 42} ${cy + 36} C ${cx - 10} ${cy + 58}, ${cx + 10} ${cy + 58}, ${cx + 42} ${cy + 36}" fill="none" stroke="${colors.accent}" stroke-width="10" stroke-linecap="round"/>
    <path d="M ${cx + 110} ${cy - 44} L ${cx + 148} ${cy - 10} L ${cx + 110} ${cy + 24}" fill="none" stroke="${colors.accent}" stroke-width="10" stroke-linecap="round"/>
  `;
}

function flowIconBell(cx, cy) {
  return `
    <path d="M ${cx - 62} ${cy - 14} H ${cx + 62} L ${cx + 86} ${cy + 98} H ${cx - 86} Z" fill="${colors.bell}" opacity="0.92"/>
    <rect x="${cx - 18}" y="${cy - 82}" width="36" height="46" rx="16" fill="${colors.bell}" opacity="0.92"/>
    <path d="M ${cx - 84} ${cy + 112} C ${cx - 84} ${cy + 142}, ${cx - 42} ${cy + 162}, ${cx} ${cy + 162} C ${cx + 42} ${cy + 162}, ${cx + 84} ${cy + 142}, ${cx + 84} ${cy + 112}" fill="none" stroke="${colors.bell}" stroke-width="12" stroke-linecap="round"/>
  `;
}

function flowIconRain(cx, cy) {
  return `
    <ellipse cx="${cx}" cy="${cy + 20}" rx="96" ry="56" fill="${colors.rain}" opacity="0.20"/>
    <circle cx="${cx - 58}" cy="${cy - 10}" r="34" fill="${colors.rain}" opacity="0.86"/>
    <circle cx="${cx - 2}" cy="${cy - 28}" r="42" fill="${colors.rain}" opacity="0.86"/>
    <circle cx="${cx + 54}" cy="${cy - 8}" r="34" fill="${colors.rain}" opacity="0.86"/>
    <path d="M ${cx - 54} ${cy + 58} L ${cx - 74} ${cy + 118}" stroke="${colors.rain}" stroke-width="12" stroke-linecap="round"/>
    <path d="M ${cx} ${cy + 66} L ${cx - 18} ${cy + 126}" stroke="${colors.rain}" stroke-width="12" stroke-linecap="round"/>
    <path d="M ${cx + 54} ${cy + 58} L ${cx + 24} ${cy + 120}" stroke="${colors.rain}" stroke-width="12" stroke-linecap="round"/>
  `;
}

function flowIconGate(cx, cy) {
  return `
    <rect x="${cx - 74}" y="${cy - 108}" width="24" height="232" rx="12" fill="${colors.pad}"/>
    <rect x="${cx + 50}" y="${cy - 108}" width="24" height="232" rx="12" fill="${colors.pad}"/>
    <rect x="${cx - 86}" y="${cy - 128}" width="176" height="26" rx="12" fill="${colors.pad}"/>
    <path d="M ${cx - 42} ${cy - 16} H ${cx + 42} L ${cx + 60} ${cy + 90} H ${cx - 60} Z" fill="${colors.bell}" opacity="0.95"/>
  `;
}

function flowIconWave(cx, cy) {
  return `
    <line x1="${cx - 96}" y1="${cy}" x2="${cx - 42}" y2="${cy}" stroke="${colors.wall}" stroke-width="12" stroke-linecap="round"/>
    <line x1="${cx - 18}" y1="${cy - 42}" x2="${cx - 18}" y2="${cy + 42}" stroke="${colors.wall}" stroke-width="12" stroke-linecap="round"/>
    <line x1="${cx + 18}" y1="${cy - 74}" x2="${cx + 18}" y2="${cy + 74}" stroke="${colors.wall}" stroke-width="12" stroke-linecap="round"/>
    <line x1="${cx + 54}" y1="${cy - 36}" x2="${cx + 54}" y2="${cy + 36}" stroke="${colors.wall}" stroke-width="12" stroke-linecap="round"/>
    <line x1="${cx + 88}" y1="${cy}" x2="${cx + 120}" y2="${cy}" stroke="${colors.wall}" stroke-width="12" stroke-linecap="round"/>
  `;
}

function flowIconBoss(cx, cy) {
  return `
    <path d="M ${cx - 92} ${cy + 78} C ${cx - 118} ${cy - 20}, ${cx - 64} ${cy - 112}, ${cx} ${cy - 112} C ${cx + 64} ${cy - 112}, ${cx + 118} ${cy - 20}, ${cx + 92} ${cy + 78} Z" fill="${colors.tinnitus}" opacity="0.92"/>
    <path d="M ${cx - 42} ${cy - 10} L ${cx - 10} ${cy - 28} L ${cx - 2} ${cy + 10} Z" fill="#FFFFFF"/>
    <path d="M ${cx + 42} ${cy - 10} L ${cx + 10} ${cy - 28} L ${cx + 2} ${cy + 10} Z" fill="#FFFFFF"/>
    <path d="M ${cx - 36} ${cy + 46} C ${cx - 10} ${cy + 70}, ${cx + 10} ${cy + 70}, ${cx + 36} ${cy + 46}" fill="none" stroke="#FFFFFF" stroke-width="12" stroke-linecap="round"/>
  `;
}

function flowIconForest(cx, cy) {
  return `
    <path d="M ${cx - 72} ${cy + 110} L ${cx - 28} ${cy - 26} L ${cx + 18} ${cy + 110} Z" fill="#235B38"/>
    <path d="M ${cx - 18} ${cy + 110} L ${cx + 32} ${cy - 56} L ${cx + 82} ${cy + 110} Z" fill="#2B6B43"/>
    <rect x="${cx - 44}" y="${cy + 110}" width="18" height="48" rx="6" fill="#4B3824"/>
    <rect x="${cx + 20}" y="${cy + 110}" width="18" height="48" rx="6" fill="#4B3824"/>
    <path d="M ${cx + 122} ${cy + 142} C ${cx + 72} ${cy + 118}, ${cx + 32} ${cy + 88}, ${cx - 12} ${cy + 42} C ${cx - 46} ${cy + 6}, ${cx - 78} ${cy - 30}, ${cx - 126} ${cy - 66}" fill="none" stroke="#7DAA7A" stroke-width="14" stroke-linecap="round"/>
  `;
}

function buildHeroOverrideNoteSvg() {
  return wrapSvg(
    1800,
    1800,
    `
    <rect width="1800" height="1800" rx="96" fill="#112127"/>
    <text x="140" y="860" font-size="58" font-weight="700" fill="#F0F6F8" font-family="Malgun Gothic, Pretendard, Arial">Custom GPT image override used</text>
    <text x="140" y="940" font-size="34" fill="#C1D2D7" font-family="Malgun Gothic, Pretendard, Arial">slide3_section02_hero_custom.png</text>
    `
  );
}

function buildBulletTextBody(lines, size) {
  const paragraphs = lines.map((line) => {
    const runs = buildTextRuns(line, size);
    return `<a:p><a:pPr marL="420000" indent="-420000" algn="l" defTabSz="2159996"><a:spcAft><a:spcPts val="520"/></a:spcAft><a:buClr><a:srgbClr val="${colors.accent.replace("#", "")}"/></a:buClr><a:buSzPct val="100000"/><a:buFont typeface="Malgun Gothic" pitchFamily="34" charset="129"/><a:buChar char="•"/></a:pPr>${runs}</a:p>`;
  }).join("");
  return `<p:txBody><a:bodyPr wrap="square" lIns="72000" tIns="26000" rIns="72000" bIns="26000" rtlCol="0" anchor="t"><a:spAutoFit/></a:bodyPr><a:lstStyle/>${paragraphs}</p:txBody>`;
}

function buildTextRuns(line, size) {
  return `<a:r><a:rPr lang="ko-KR" altLang="en-US" sz="${size}"><a:solidFill><a:srgbClr val="2A3A46"/></a:solidFill><a:latin typeface="Malgun Gothic" pitchFamily="34" charset="129"/><a:ea typeface="Malgun Gothic" pitchFamily="34" charset="129"/></a:rPr><a:t>${escapeXml(line)}</a:t></a:r>`;
}

function replaceTextBoxByName(xml, name, txBody) {
  const escapedName = name.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const pattern = new RegExp(
    `(<p:cNvPr[^>]*name="${escapedName}"[\\s\\S]*?<\\/p:nvSpPr><p:spPr[\\s\\S]*?<\\/p:spPr>)<p:txBody>[\\s\\S]*?<\\/p:txBody>`,
    "u"
  );
  return xml.replace(pattern, `$1${txBody}`);
}

function replacePictureEmbedById(xml, shapeId, relId) {
  const pattern = new RegExp(
    `(<p:pic>[\\s\\S]*?<p:cNvPr[^>]*id="${shapeId}"[^>]*>[\\s\\S]*?<a:blip r:embed=")([^"]+)(")`,
    "u"
  );
  return xml.replace(pattern, `$1${relId}$3`);
}

function createPictureBlock({ shapeId, shapeName, relId, x, y, cx, cy }) {
  return `<p:pic><p:nvPicPr><p:cNvPr id="${shapeId}" name="${shapeName}"/><p:cNvPicPr><a:picLocks noChangeAspect="1"/></p:cNvPicPr><p:nvPr/></p:nvPicPr><p:blipFill><a:blip r:embed="${relId}"/><a:stretch><a:fillRect/></a:stretch></p:blipFill><p:spPr><a:xfrm><a:off x="${x}" y="${y}"/><a:ext cx="${cx}" cy="${cy}"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></p:spPr></p:pic>`;
}

function ensureRelationship(xml, id, target) {
  const escapedId = id.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const relPattern = new RegExp(`<Relationship Id="${escapedId}"[^>]*Target="[^"]+"\\/>`, "u");
  const replacement = `<Relationship Id="${id}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="${target}"/>`;
  if (relPattern.test(xml)) {
    return xml.replace(relPattern, replacement);
  }

  return insertBeforeClosingTag(xml, "</Relationships>", replacement);
}

function insertBeforeClosingTag(xml, tag, insert) {
  const index = xml.lastIndexOf(tag);
  if (index < 0) {
    throw new Error(`Could not find closing tag ${tag}`);
  }
  return `${xml.slice(0, index)}${insert}${xml.slice(index)}`;
}

function wrapSvg(width, height, body) {
  return `<?xml version="1.0" encoding="UTF-8"?>\n<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">${body}\n</svg>`;
}

function fileToDataUri(filePath) {
  const buffer = fs.readFileSync(filePath);
  const ext = path.extname(filePath).toLowerCase();
  const mime = ext === ".png"
    ? "image/png"
    : ext === ".jpg" || ext === ".jpeg"
      ? "image/jpeg"
      : ext === ".svg"
        ? "image/svg+xml"
        : "application/octet-stream";
  return `data:${mime};base64,${buffer.toString("base64")}`;
}

function requireFile(filePath) {
  ensureFile(filePath);
  return filePath;
}

function ensureFile(filePath) {
  if (!fs.existsSync(filePath)) {
    throw new Error(`Missing required file: ${filePath}`);
  }
}

function escapeXml(value) {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll("\"", "&quot;")
    .replaceAll("'", "&apos;");
}

function parseArgs(argv) {
  const parsed = {};
  for (let index = 0; index < argv.length; index += 1) {
    const key = argv[index];
    if (!key.startsWith("--")) {
      continue;
    }

    const value = argv[index + 1];
    if (!value || value.startsWith("--")) {
      parsed[key.slice(2)] = true;
      continue;
    }

    parsed[key.slice(2)] = value;
    index += 1;
  }

  return parsed;
}

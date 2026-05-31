import fs from "node:fs";
import path from "node:path";

if (process.argv.length < 3) {
  console.error("Usage: node generateBellRingerExpoDraft.mjs <extracted-ppt-dir> [asset-output-dir]");
  process.exit(1);
}

const extractedDir = process.argv[2];
const assetOutputDir =
  process.argv[3] || path.join(path.dirname(extractedDir), "expo_draft_assets");

const slideXmlPath = path.join(extractedDir, "ppt", "slides", "slide1.xml");
const slideRelsPath = path.join(extractedDir, "ppt", "slides", "_rels", "slide1.xml.rels");
const mediaDir = path.join(extractedDir, "ppt", "media");

const accent = "#1DAF88";
const bg = "#171A24";
const panel = "#202434";
const line = "#3A415A";
const white = "#F3F5F9";
const muted = "#A9B2C7";
const bell = "#F6D44A";
const rain = "#4BA7FF";
const tinnitus = "#A97DFF";
const wall = "#90D8E5";
const pad = "#F4C15A";

const imageSpecs = [
  {
    name: "bellringer_flow.svg",
    title: "플레이 흐름",
    content: buildFlowSvg(),
  },
  {
    name: "bellringer_closed_eye.svg",
    title: "폐안 플레이",
    content: buildClosedEyeSvg(),
  },
  {
    name: "bellringer_system.svg",
    title: "시스템 구성",
    content: buildSystemSvg(),
  },
  {
    name: "bellringer_cues.svg",
    title: "감각 매핑",
    content: buildCueSvg(),
  },
];

fs.mkdirSync(assetOutputDir, { recursive: true });
fs.mkdirSync(mediaDir, { recursive: true });

for (const spec of imageSpecs) {
  const assetPath = path.join(assetOutputDir, spec.name);
  const mediaPath = path.join(mediaDir, spec.name);
  fs.writeFileSync(assetPath, spec.content, "utf8");
  fs.writeFileSync(mediaPath, spec.content, "utf8");
}

let slideXml = fs.readFileSync(slideXmlPath, "utf8");
let relsXml = fs.readFileSync(slideRelsPath, "utf8");

slideXml = replacePlaceholderByIdx(
  slideXml,
  11,
  buildSimpleBody("눈을 감고 소리와 빛으로 길을 찾는 청각 중심 인터랙션", 2400, accent, {
    align: "l",
    bold: false,
  })
);
slideXml = replacePlaceholderByIdx(
  slideXml,
  12,
  buildSimpleBody("Bell Ringer", 3200, white, {
    align: "l",
    bold: true,
  })
);
slideXml = replacePlaceholderByIdx(slideXml, 13, buildSimpleBody("", 1800, muted));
slideXml = replacePlaceholderByIdx(slideXml, 14, buildSimpleBody("", 1800, muted));
slideXml = replacePlaceholderByIdx(slideXml, 15, buildSimpleBody("", 1800, muted));
slideXml = replacePlaceholderByIdx(slideXml, 16, buildSimpleBody("", 1800, muted));

slideXml = replaceTextBoxByName(slideXml, "TextBox 58", buildSectionTitleBody("프로젝트 개요"));
slideXml = replaceTextBoxByName(slideXml, "TextBox 60", buildBulletBody([
  "플레이어는 눈을 감은 상태에서 공간을 인지한다.",
  "종소리, LED 빛, 패드 진동, 머리 회전, 패드 위치·회전만으로 진행한다.",
  "종을 따라 이동하고, 이명을 정화하며, 마지막에는 숲으로 전환된다.",
  "관람자는 모니터를 통해 현재 상태와 상호작용을 동시에 확인할 수 있다.",
], 2150));

slideXml = replaceTextBoxByName(slideXml, "TextBox 54", buildSectionTitleBody("플레이 흐름"));
slideXml = replaceTextBoxByName(slideXml, "TextBox 56", buildBulletBody([
  "① 정적 뒤 왼쪽 귀 근처에서 종소리가 시작된다.",
  "② 플레이어는 종을 따라 두 개 이상의 목표 지점까지 이동한다.",
  "③ 비와 바람이 섞이며 종소리를 분리해서 추적해야 한다.",
  "④ 종을 세 번 바라보며 소리와 빛의 연결을 학습한다.",
  "⑤ 종을 획득한 뒤 일반 이명 2개를 정화한다.",
  "⑥ 마지막에는 움직이는 보스 이명을 추적 정화하고 숲 엔딩으로 넘어간다.",
], 2050));

slideXml = replaceTextBoxByName(slideXml, "TextBox 42", buildSectionTitleBody("폐안 감각 설계"));
slideXml = replaceTextBoxByName(slideXml, "TextBox 52", buildBulletBody([
  "핵심: 눈을 감은 상태에서도 다음 행동이 느껴져야 한다.",
  "공간 음향: 종 방향, 이명 위치, 엔딩 전환을 인지한다.",
  "빛: 초록 종, 보라 이명, 파란 비, 시안 벽 노이즈를 구분한다.",
  "햅틱: 종 탐지 보조, 이명 lock, 보스 이동 방향을 진동으로 전달한다.",
  "패턴: 큰 파동보다 짧고 선명한 점·ripple 중심으로 폐안 가독성을 높인다.",
], 2050));

slideXml = replaceTextBoxByName(slideXml, "TextBox 3", buildSectionTitleBody("시스템 / 관람 화면"));
slideXml = replaceTextBoxByName(slideXml, "TextBox 6", buildSimpleBody(
  "헤드 LED + IMU, 패드 IMU + 진동, ArUco 카메라, 관람자 화면이 하나의 루프로 동작",
  2100,
  white,
  { align: "l" }
));

const imageRels = ["rId4", "rId5", "rId6", "rId7"];
slideXml = replacePictureEmbeds(slideXml, imageRels);
relsXml = injectImageRelationships(relsXml, [
  { id: "rId4", target: "../media/bellringer_flow.svg" },
  { id: "rId5", target: "../media/bellringer_closed_eye.svg" },
  { id: "rId6", target: "../media/bellringer_system.svg" },
  { id: "rId7", target: "../media/bellringer_cues.svg" },
]);

fs.writeFileSync(slideXmlPath, slideXml, "utf8");
fs.writeFileSync(slideRelsPath, relsXml, "utf8");

console.log(`Updated ${slideXmlPath}`);
console.log(`Updated ${slideRelsPath}`);
for (const spec of imageSpecs) {
  console.log(`Wrote ${path.join(assetOutputDir, spec.name)}`);
}

function escapeXml(value) {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll("\"", "&quot;")
    .replaceAll("'", "&apos;");
}

function replacePlaceholderByIdx(xml, idx, txBody) {
  const pattern = new RegExp(
    `(<p:ph[^>]*idx="${idx}"\\/><\\/p:nvPr><\\/p:nvSpPr><p:spPr\\/?>(?:<\\/p:spPr>)?)<p:txBody>[\\s\\S]*?<\\/p:txBody>`,
    "u"
  );
  return xml.replace(pattern, `$1${txBody}`);
}

function replaceTextBoxByName(xml, name, txBody) {
  const escapedName = name.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const pattern = new RegExp(
    `(<p:cNvPr[^>]*name="${escapedName}"[\\s\\S]*?<\\/p:nvSpPr><p:spPr[\\s\\S]*?<\\/p:spPr>)<p:txBody>[\\s\\S]*?<\\/p:txBody>`,
    "u"
  );
  return xml.replace(pattern, `$1${txBody}`);
}

function buildSimpleBody(text, size, color, options = {}) {
  const align = options.align || "l";
  const boldFace = options.bold
    ? `<a:latin typeface="페이퍼로지 8 ExtraBold" pitchFamily="2" charset="-127"/><a:ea typeface="페이퍼로지 8 ExtraBold" pitchFamily="2" charset="-127"/>`
    : `<a:latin typeface="프리젠테이션 4 Regular" pitchFamily="2" charset="-127"/><a:ea typeface="프리젠테이션 4 Regular" pitchFamily="2" charset="-127"/>`;
  return `<p:txBody><a:bodyPr wrap="square" lIns="72000" tIns="36000" rIns="72000" bIns="36000" rtlCol="0"/><a:lstStyle/><a:p><a:pPr algn="${align}"/><a:r><a:rPr lang="ko-KR" altLang="en-US" sz="${size}"><a:solidFill><a:srgbClr val="${color.replace("#", "")}"/></a:solidFill>${boldFace}</a:rPr><a:t>${escapeXml(text)}</a:t></a:r></a:p></p:txBody>`;
}

function buildSectionTitleBody(text) {
  return `<p:txBody><a:bodyPr wrap="none" lIns="72000" tIns="36000" rIns="72000" bIns="36000" rtlCol="0"><a:noAutofit/></a:bodyPr><a:lstStyle/><a:p><a:r><a:rPr lang="ko-KR" altLang="en-US" sz="4400"><a:solidFill><a:schemeClr val="tx1"><a:lumMod val="85000"/><a:lumOff val="15000"/></a:schemeClr></a:solidFill><a:latin typeface="페이퍼로지 6 SemiBold" pitchFamily="2" charset="-127"/><a:ea typeface="페이퍼로지 6 SemiBold" pitchFamily="2" charset="-127"/></a:rPr><a:t>${escapeXml(text)}</a:t></a:r></a:p></p:txBody>`;
}

function buildBulletBody(lines, size) {
  const paras = lines.map((line) => {
    return `<a:p><a:pPr marL="420000" indent="-420000" algn="l" defTabSz="2159996"><a:spcAft><a:spcPts val="600"/></a:spcAft><a:buClr><a:srgbClr val="1DAF88"/></a:buClr><a:buSzPct val="100000"/><a:buFont typeface="프리젠테이션 4 Regular" pitchFamily="2" charset="-127"/><a:buChar char="•"/></a:pPr><a:r><a:rPr lang="ko-KR" altLang="en-US" sz="${size}"><a:solidFill><a:schemeClr val="tx1"><a:lumMod val="96000"/><a:lumOff val="4000"/></a:schemeClr></a:solidFill><a:latin typeface="프리젠테이션 4 Regular" pitchFamily="2" charset="-127"/><a:ea typeface="프리젠테이션 4 Regular" pitchFamily="2" charset="-127"/></a:rPr><a:t>${escapeXml(line)}</a:t></a:r></a:p>`;
  });
  return `<p:txBody><a:bodyPr wrap="square" lIns="72000" tIns="24000" rIns="72000" bIns="24000" rtlCol="0" anchor="t"><a:spAutoFit/></a:bodyPr><a:lstStyle/>${paras.join("")}</p:txBody>`;
}

function replacePictureEmbeds(xml, relIds) {
  let index = 0;
  return xml.replace(/<p:pic>[\s\S]*?<\/p:pic>/gu, (block) => {
    if (index >= relIds.length) {
      return block;
    }
    const relId = relIds[index++];
    return block.replace(/<a:blip[\s\S]*?<\/a:blip>/u, `<a:blip r:embed="${relId}"/>`);
  });
}

function injectImageRelationships(xml, relationships) {
  const tail = "</Relationships>";
  const insert = relationships
    .map(
      ({ id, target }) =>
        `<Relationship Id="${id}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="${target}"/>`
    )
    .join("");
  return xml.replace(tail, `${insert}${tail}`);
}

function panelFrame(title) {
  return `
  <rect x="0" y="0" width="1600" height="960" rx="42" fill="${bg}"/>
  <rect x="36" y="36" width="1528" height="888" rx="30" fill="${panel}" stroke="${line}" stroke-width="3"/>
  <text x="86" y="126" font-size="52" font-weight="700" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">${title}</text>
  <line x1="86" y1="154" x2="1514" y2="154" stroke="${line}" stroke-width="3"/>
  `;
}

function buildFlowSvg() {
  const steps = [
    { x: 140, color: bell, label1: "종 등장", label2: "왼쪽 귀 근처 시작", icon: "bell" },
    { x: 440, color: accent, label1: "종 따라 이동", label2: "공간 음향 추적", icon: "walk" },
    { x: 740, color: rain, label1: "비·바람", label2: "칵테일 파티 효과", icon: "rain" },
    { x: 1040, color: bell, label1: "종 바라보기", label2: "빛·소리 연결 학습", icon: "eye" },
    { x: 1340, color: tinnitus, label1: "이명 정화", label2: "패드 pose + 진동", icon: "wave" },
  ];
  const nodes = steps
    .map((step, i) => {
      const arrow =
        i < steps.length - 1
          ? `<line x1="${step.x + 90}" y1="370" x2="${steps[i + 1].x - 90}" y2="370" stroke="${muted}" stroke-width="8" stroke-linecap="round"/><polygon points="${steps[i + 1].x - 120},352 ${steps[i + 1].x - 70},370 ${steps[i + 1].x - 120},388" fill="${muted}"/>`
          : "";
      return `${arrow}
      <circle cx="${step.x}" cy="370" r="74" fill="${step.color}" opacity="0.16"/>
      <circle cx="${step.x}" cy="370" r="54" fill="${step.color}"/>
      ${iconSvg(step.icon, step.x, 370, 48, bg)}
      <text x="${step.x}" y="500" text-anchor="middle" font-size="32" font-weight="700" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">${step.label1}</text>
      <text x="${step.x}" y="544" text-anchor="middle" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">${step.label2}</text>`;
    })
    .join("");
  return wrapSvg(`
    ${panelFrame("게임 진행 흐름")}
    <text x="86" y="220" font-size="30" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">Bell Ringer는 종 추적에서 시작해, 폐안 상태의 감각 적응과 이명 정화, 숲 엔딩으로 이어진다.</text>
    ${nodes}
    <rect x="86" y="650" width="1428" height="188" rx="24" fill="#111520" stroke="${line}" stroke-width="2"/>
    <text x="126" y="720" font-size="28" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">핵심 포인트</text>
    <text x="126" y="772" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">• 사용자는 종소리의 방향을 듣고 실제로 이동한다.</text>
    <text x="126" y="814" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">• 비·바람 구간에서는 종소리를 분리해서 추적해야 한다.</text>
    <text x="126" y="856" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">• 종 획득 이후에는 패드의 위치·회전과 진동으로 이명을 정화한다.</text>
  `);
}

function buildClosedEyeSvg() {
  return wrapSvg(`
    ${panelFrame("눈을 감고 플레이하는 구조")}
    <text x="86" y="220" font-size="30" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">플레이어는 시각 UI가 아닌 소리, 빛, 진동, 머리 회전만으로 다음 행동을 결정한다.</text>
    <circle cx="360" cy="500" r="150" fill="#111520" stroke="${line}" stroke-width="4"/>
    <path d="M305 458 Q360 410 415 458" fill="none" stroke="${white}" stroke-width="10" stroke-linecap="round"/>
    <line x1="315" y1="465" x2="405" y2="445" stroke="${accent}" stroke-width="6" stroke-linecap="round"/>
    <rect x="248" y="438" width="30" height="120" rx="12" fill="${line}"/>
    <rect x="442" y="438" width="30" height="120" rx="12" fill="${line}"/>
    <path d="M250 436 Q360 330 470 436" fill="none" stroke="${line}" stroke-width="16"/>
    <circle cx="770" cy="320" r="84" fill="${bell}" opacity="0.18"/>
    <circle cx="770" cy="320" r="58" fill="${bell}"/>
    ${iconSvg("bell", 770, 320, 48, bg)}
    <text x="860" y="300" font-size="34" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">공간 음향</text>
    <text x="860" y="340" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">종 방향, 이명 위치, 숲 엔딩 전환</text>
    <circle cx="770" cy="500" r="84" fill="${accent}" opacity="0.18"/>
    <circle cx="770" cy="500" r="58" fill="${accent}"/>
    ${iconSvg("led", 770, 500, 48, bg)}
    <text x="860" y="480" font-size="34" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">LED 빛</text>
    <text x="860" y="520" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">초록 종, 보라 이명, 파란 비, 시안 벽 노이즈</text>
    <circle cx="770" cy="680" r="84" fill="${pad}" opacity="0.18"/>
    <circle cx="770" cy="680" r="58" fill="${pad}"/>
    ${iconSvg("pad", 770, 680, 48, bg)}
    <text x="860" y="660" font-size="34" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">패드 진동</text>
    <text x="860" y="700" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">종 탐지 보조, 이명 lock, 보스 이동 방향 안내</text>
    <rect x="86" y="780" width="1428" height="90" rx="20" fill="#111520" stroke="${line}" stroke-width="2"/>
    <text x="126" y="838" font-size="26" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">핵심: "눈을 감고도 다음 행동이 느껴지는가?"가 이 프로젝트의 가장 중요한 기준이다.</text>
  `);
}

function buildSystemSvg() {
  return wrapSvg(`
    ${panelFrame("하드웨어 / 트래킹 구조")}
    <text x="86" y="220" font-size="30" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">머리, 패드, 카메라, 관람 화면이 하나의 루프로 연결되어 플레이와 관람을 동시에 지원한다.</text>
    <rect x="120" y="320" width="300" height="170" rx="28" fill="#111520" stroke="${accent}" stroke-width="3"/>
    <text x="150" y="392" font-size="36" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">Head Module</text>
    <text x="150" y="438" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">LED 2장 + IMU</text>
    <rect x="520" y="320" width="300" height="170" rx="28" fill="#111520" stroke="${pad}" stroke-width="3"/>
    <text x="550" y="392" font-size="36" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">Pad</text>
    <text x="550" y="438" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">IMU + Vibration</text>
    <rect x="920" y="320" width="300" height="170" rx="28" fill="#111520" stroke="${rain}" stroke-width="3"/>
    <text x="950" y="392" font-size="36" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">Camera</text>
    <text x="950" y="438" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">ArUco pose tracking</text>
    <rect x="1320" y="320" width="170" height="170" rx="28" fill="#111520" stroke="${white}" stroke-width="3"/>
    <text x="1354" y="392" font-size="30" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">Observer</text>
    <text x="1354" y="432" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">Display</text>
    <line x1="420" y1="405" x2="520" y2="405" stroke="${muted}" stroke-width="8" stroke-linecap="round"/>
    <line x1="820" y1="405" x2="920" y2="405" stroke="${muted}" stroke-width="8" stroke-linecap="round"/>
    <line x1="1220" y1="405" x2="1320" y2="405" stroke="${muted}" stroke-width="8" stroke-linecap="round"/>
    <polygon points="500,388 520,405 500,422" fill="${muted}"/>
    <polygon points="900,388 920,405 900,422" fill="${muted}"/>
    <polygon points="1300,388 1320,405 1300,422" fill="${muted}"/>
    <rect x="160" y="610" width="1280" height="180" rx="28" fill="#111520" stroke="${line}" stroke-width="2"/>
    <text x="210" y="678" font-size="30" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">Unity Demo Loop</text>
    <text x="210" y="726" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">입력 수집 → 상태 판정 → 공간 음향 / 빛 / 진동 출력 → 관람 화면 갱신</text>
    <text x="210" y="766" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">테스트 씬에서 검증한 입력·오디오·빛·보스 추적 로직을 Final Demo에 통합한다.</text>
  `);
}

function buildCueSvg() {
  return wrapSvg(`
    ${panelFrame("감각 매핑 / 관람 포인트")}
    <text x="86" y="220" font-size="30" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">색과 감각은 장식이 아니라 플레이어가 다음 행동을 판단하는 실질적 정보로 사용된다.</text>
    <rect x="110" y="300" width="600" height="500" rx="28" fill="#111520" stroke="${line}" stroke-width="2"/>
    <circle cx="180" cy="380" r="28" fill="${bell}"/>
    <text x="230" y="392" font-size="30" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">종 / 초록 계열</text>
    <text x="230" y="428" font-size="22" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">소리 위치 점 + 짧은 ripple, 방향 추적의 기준</text>
    <circle cx="180" cy="500" r="28" fill="${tinnitus}"/>
    <text x="230" y="512" font-size="30" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">이명 / 보라 계열</text>
    <text x="230" y="548" font-size="22" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">패드 pose 정화 대상, 불안정한 소리와 결합</text>
    <circle cx="180" cy="620" r="28" fill="${rain}"/>
    <text x="230" y="632" font-size="30" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">비 / 파란 계열</text>
    <text x="230" y="668" font-size="22" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">바닥형 패턴, 종소리 분리 난이도 상승</text>
    <circle cx="180" cy="740" r="28" fill="${wall}"/>
    <text x="230" y="752" font-size="30" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">벽 / 시안 노이즈</text>
    <text x="230" y="788" font-size="22" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">일반 이명 구간의 경계와 공간 감각 보조</text>
    <rect x="840" y="300" width="650" height="500" rx="28" fill="#111520" stroke="${line}" stroke-width="2"/>
    <text x="890" y="374" font-size="30" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">관람자가 보는 포인트</text>
    <text x="890" y="432" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">• 플레이어는 눈을 감고 있지만, 관람자는 화면에서 종과 이명의 위치를 본다.</text>
    <text x="890" y="480" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">• 플레이어의 몸 회전, 패드 위치, 빛 반응, 진동 타이밍이 함께 보인다.</text>
    <text x="890" y="528" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">• 청각 중심 인터랙션이 실제로 어떤 몸 동작을 유도하는지 설명하기 쉽다.</text>
    <text x="890" y="616" font-size="28" fill="${white}" font-family="Malgun Gothic, Pretendard, Arial">전시 포인트</text>
    <text x="890" y="668" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">폐안 상태에서 소리, 빛, 진동이 하나의 감각 체계로 묶이는 경험을 전달한다.</text>
    <text x="890" y="716" font-size="24" fill="${muted}" font-family="Malgun Gothic, Pretendard, Arial">기존 시각 중심 게임과 달리 청각과 촉각을 주인공으로 세운다는 점이 차별점이다.</text>
  `);
}

function wrapSvg(content) {
  return `<?xml version="1.0" encoding="UTF-8" standalone="no"?>
<svg xmlns="http://www.w3.org/2000/svg" width="1600" height="960" viewBox="0 0 1600 960">
${content}
</svg>`;
}

function iconSvg(type, cx, cy, size, fillColor) {
  const s = size;
  switch (type) {
    case "bell":
      return `<path d="M ${cx} ${cy - s * 0.62} C ${cx - s * 0.45} ${cy - s * 0.62}, ${cx - s * 0.52} ${cy - s * 0.18}, ${cx - s * 0.52} ${cy + s * 0.18} L ${cx - s * 0.7} ${cy + s * 0.48} L ${cx + s * 0.7} ${cy + s * 0.48} L ${cx + s * 0.52} ${cy + s * 0.18} C ${cx + s * 0.52} ${cy - s * 0.18}, ${cx + s * 0.45} ${cy - s * 0.62}, ${cx} ${cy - s * 0.62} Z" fill="${fillColor}"/><circle cx="${cx}" cy="${cy + s * 0.6}" r="${s * 0.12}" fill="${fillColor}"/>`;
    case "walk":
      return `<circle cx="${cx}" cy="${cy - s * 0.52}" r="${s * 0.18}" fill="${fillColor}"/><line x1="${cx}" y1="${cy - s * 0.34}" x2="${cx}" y2="${cy + s * 0.1}" stroke="${fillColor}" stroke-width="${s * 0.16}" stroke-linecap="round"/><line x1="${cx}" y1="${cy - s * 0.1}" x2="${cx - s * 0.34}" y2="${cy + s * 0.14}" stroke="${fillColor}" stroke-width="${s * 0.14}" stroke-linecap="round"/><line x1="${cx}" y1="${cy - s * 0.1}" x2="${cx + s * 0.34}" y2="${cy + s * 0.06}" stroke="${fillColor}" stroke-width="${s * 0.14}" stroke-linecap="round"/><line x1="${cx}" y1="${cy + s * 0.1}" x2="${cx - s * 0.24}" y2="${cy + s * 0.5}" stroke="${fillColor}" stroke-width="${s * 0.14}" stroke-linecap="round"/><line x1="${cx}" y1="${cy + s * 0.1}" x2="${cx + s * 0.32}" y2="${cy + s * 0.54}" stroke="${fillColor}" stroke-width="${s * 0.14}" stroke-linecap="round"/>`;
    case "rain":
      return `<path d="M ${cx - s * 0.4} ${cy - s * 0.45} Q ${cx - s * 0.3} ${cy - s * 0.75} ${cx - s * 0.12} ${cy - s * 0.45} Q ${cx - s * 0.2} ${cy - s * 0.2} ${cx - s * 0.4} ${cy - s * 0.45} Z" fill="${fillColor}"/><path d="M ${cx + s * 0.06} ${cy - s * 0.3} Q ${cx + s * 0.18} ${cy - s * 0.68} ${cx + s * 0.34} ${cy - s * 0.3} Q ${cx + s * 0.24} ${cy - s * 0.02} ${cx + s * 0.06} ${cy - s * 0.3} Z" fill="${fillColor}"/><line x1="${cx - s * 0.48}" y1="${cy + s * 0.18}" x2="${cx + s * 0.48}" y2="${cy + s * 0.18}" stroke="${fillColor}" stroke-width="${s * 0.14}" stroke-linecap="round"/><line x1="${cx - s * 0.42}" y1="${cy + s * 0.42}" x2="${cx + s * 0.42}" y2="${cy + s * 0.42}" stroke="${fillColor}" stroke-width="${s * 0.12}" stroke-linecap="round"/>`;
    case "eye":
      return `<path d="M ${cx - s * 0.74} ${cy} Q ${cx} ${cy - s * 0.52} ${cx + s * 0.74} ${cy} Q ${cx} ${cy + s * 0.52} ${cx - s * 0.74} ${cy} Z" fill="none" stroke="${fillColor}" stroke-width="${s * 0.12}"/><circle cx="${cx}" cy="${cy}" r="${s * 0.18}" fill="${fillColor}"/>`;
    case "wave":
      return `<path d="M ${cx - s * 0.7} ${cy + s * 0.2} Q ${cx - s * 0.48} ${cy - s * 0.36} ${cx - s * 0.26} ${cy + s * 0.1} T ${cx + s * 0.18} ${cy + s * 0.08} T ${cx + s * 0.62} ${cy - s * 0.08}" fill="none" stroke="${fillColor}" stroke-width="${s * 0.14}" stroke-linecap="round"/><circle cx="${cx + s * 0.54}" cy="${cy - s * 0.06}" r="${s * 0.12}" fill="${fillColor}"/>`;
    case "led":
      return `<circle cx="${cx}" cy="${cy}" r="${s * 0.18}" fill="${fillColor}"/><circle cx="${cx}" cy="${cy}" r="${s * 0.42}" fill="none" stroke="${fillColor}" stroke-width="${s * 0.1}" opacity="0.8"/><circle cx="${cx}" cy="${cy}" r="${s * 0.64}" fill="none" stroke="${fillColor}" stroke-width="${s * 0.08}" opacity="0.45"/>`;
    case "pad":
      return `<rect x="${cx - s * 0.72}" y="${cy - s * 0.34}" width="${s * 1.44}" height="${s * 0.72}" rx="${s * 0.22}" fill="none" stroke="${fillColor}" stroke-width="${s * 0.12}"/><line x1="${cx - s * 0.32}" y1="${cy}" x2="${cx - s * 0.02}" y2="${cy}" stroke="${fillColor}" stroke-width="${s * 0.12}" stroke-linecap="round"/><line x1="${cx - s * 0.17}" y1="${cy - s * 0.15}" x2="${cx - s * 0.17}" y2="${cy + s * 0.15}" stroke="${fillColor}" stroke-width="${s * 0.12}" stroke-linecap="round"/><circle cx="${cx + s * 0.24}" cy="${cy - s * 0.08}" r="${s * 0.08}" fill="${fillColor}"/><circle cx="${cx + s * 0.4}" cy="${cy + s * 0.08}" r="${s * 0.08}" fill="${fillColor}"/>`;
    default:
      return "";
  }
}

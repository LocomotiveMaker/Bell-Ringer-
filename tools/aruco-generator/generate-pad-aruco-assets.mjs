import fs from 'node:fs';
import path from 'node:path';
import { arucoToSVGString } from 'aruco-marker';

const outputDir = path.resolve('Docs/Printables/PadArucoBoard');
fs.mkdirSync(outputDir, { recursive: true });

const markerFaceSizeMm = 60;
const markerImageSizeMm = 50;
const ids = {
  left: 23,
  right: 47,
};

function markerSvg(id, sizeMm) {
  return arucoToSVGString(id, `${sizeMm}mm`);
}

function markerSvgInner(id) {
  const fullSvg = markerSvg(id, markerImageSizeMm);
  const start = fullSvg.indexOf('>') + 1;
  const end = fullSvg.lastIndexOf('</svg>');
  return fullSvg.slice(start, end).trim();
}

function placeMarkerSvg(id, xMm, yMm, sizeMm) {
  const scale = sizeMm / 7;
  return `<g transform="translate(${xMm} ${yMm}) scale(${scale})">
  ${markerSvgInner(id)}
</g>`;
}

function writeFile(name, contents) {
  fs.writeFileSync(path.join(outputDir, name), contents, 'utf8');
}

function buildSingleMarkerSheet(label, id) {
  return `<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="${markerFaceSizeMm}mm" height="${markerFaceSizeMm}mm" viewBox="0 0 ${markerFaceSizeMm} ${markerFaceSizeMm}">
  <rect x="0" y="0" width="${markerFaceSizeMm}" height="${markerFaceSizeMm}" fill="white" stroke="black" stroke-width="0.5"/>
  ${placeMarkerSvg(id, 5, 5, markerImageSizeMm)}
</svg>
`;
}

function buildPrintSheet() {
  const tileX = [15, 85];
  const tileY = 28;

  return `<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="210mm" height="297mm" viewBox="0 0 210 297">
  <rect x="0" y="0" width="210" height="297" fill="white"/>
  <text x="15" y="14" font-family="Arial, sans-serif" font-size="7" font-weight="bold">Bell Ringer Pad ArUco V-Board Print Sheet</text>
  <text x="15" y="20" font-family="Arial, sans-serif" font-size="4.2">Print at 100% / actual size. Disable fit-to-page.</text>
  <text x="15" y="24" font-family="Arial, sans-serif" font-size="4.2">Dictionary for later tracking: DICT_ARUCO_ORIGINAL</text>
  <text x="15" y="28" font-family="Arial, sans-serif" font-size="4.2">Cut the full 60 x 60 mm square. Keep the 5 mm white border around each marker.</text>

  <g transform="translate(${tileX[0]} ${tileY + 6})">
    <rect x="0" y="0" width="${markerFaceSizeMm}" height="${markerFaceSizeMm}" fill="white" stroke="black" stroke-width="0.5"/>
    ${placeMarkerSvg(ids.left, 5, 5, markerImageSizeMm)}
  </g>
  <text x="${tileX[0] + markerFaceSizeMm / 2}" y="${tileY + 3.8}" text-anchor="middle" font-family="Arial, sans-serif" font-size="4">LEFT FACE / ID ${ids.left}</text>

  <g transform="translate(${tileX[1]} ${tileY + 6})">
    <rect x="0" y="0" width="${markerFaceSizeMm}" height="${markerFaceSizeMm}" fill="white" stroke="black" stroke-width="0.5"/>
    ${placeMarkerSvg(ids.right, 5, 5, markerImageSizeMm)}
  </g>
  <text x="${tileX[1] + markerFaceSizeMm / 2}" y="${tileY + 3.8}" text-anchor="middle" font-family="Arial, sans-serif" font-size="4">RIGHT FACE / ID ${ids.right}</text>

  <text x="15" y="108" font-family="Arial, sans-serif" font-size="5" font-weight="bold">Scale Check</text>
  <text x="15" y="115" font-family="Arial, sans-serif" font-size="4">These rulers must measure exactly 100 mm and 50 mm after printing.</text>
  <line x1="15" y1="124" x2="115" y2="124" stroke="black" stroke-width="0.5"/>
  <line x1="15" y1="121" x2="15" y2="127" stroke="black" stroke-width="0.5"/>
  <line x1="115" y1="121" x2="115" y2="127" stroke="black" stroke-width="0.5"/>
  <text x="65" y="132" text-anchor="middle" font-family="Arial, sans-serif" font-size="4">100 mm</text>

  <line x1="15" y1="144" x2="65" y2="144" stroke="black" stroke-width="0.5"/>
  <line x1="15" y1="141" x2="15" y2="147" stroke="black" stroke-width="0.5"/>
  <line x1="65" y1="141" x2="65" y2="147" stroke="black" stroke-width="0.5"/>
  <text x="40" y="152" text-anchor="middle" font-family="Arial, sans-serif" font-size="4">50 mm</text>

  <text x="15" y="171" font-family="Arial, sans-serif" font-size="5" font-weight="bold">Face Plate Cut Guide</text>
  <text x="15" y="178" font-family="Arial, sans-serif" font-size="4">Cut two rigid plates at exactly 60 x 60 mm. The printed tile should fully cover the front of each plate.</text>
  <rect x="15" y="184" width="60" height="60" fill="none" stroke="black" stroke-width="0.4" stroke-dasharray="2 1.5"/>
  <text x="45" y="251" text-anchor="middle" font-family="Arial, sans-serif" font-size="4">60 mm x 60 mm rigid plate</text>

  <text x="15" y="270" font-family="Arial, sans-serif" font-size="5" font-weight="bold">Assembly summary</text>
  <text x="15" y="277" font-family="Arial, sans-serif" font-size="4">1. Cut on the outer square border. Do not trim away the white margin.</text>
  <text x="15" y="283" font-family="Arial, sans-serif" font-size="4">2. Glue LEFT and RIGHT face tiles to two rigid plates.</text>
  <text x="15" y="289" font-family="Arial, sans-serif" font-size="4">3. Join the plates as a V with the opening facing the camera.</text>
</svg>
`;
}

function buildAssemblySchematic() {
  return `<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="210mm" height="148mm" viewBox="0 0 210 148">
  <rect x="0" y="0" width="210" height="148" fill="white"/>
  <text x="15" y="14" font-family="Arial, sans-serif" font-size="7" font-weight="bold">Bell Ringer Pad ArUco V-Board Assembly Schematic</text>
  <text x="15" y="20" font-family="Arial, sans-serif" font-size="4.2">Use this only as a build guide. The print sheet controls true marker size.</text>

  <text x="15" y="33" font-family="Arial, sans-serif" font-size="5" font-weight="bold">Top View</text>
  <line x1="105" y1="88" x2="70" y2="53" stroke="black" stroke-width="1.2"/>
  <line x1="105" y1="88" x2="140" y2="53" stroke="black" stroke-width="1.2"/>
  <text x="58" y="50" font-family="Arial, sans-serif" font-size="4">LEFT FACE</text>
  <text x="142" y="50" font-family="Arial, sans-serif" font-size="4">RIGHT FACE</text>
  <text x="93" y="96" font-family="Arial, sans-serif" font-size="4">join edge</text>
  <path d="M92 86 A18 18 0 0 1 118 86" fill="none" stroke="black" stroke-width="0.4"/>
  <text x="105" y="77" text-anchor="middle" font-family="Arial, sans-serif" font-size="4">90 deg target</text>
  <text x="15" y="108" font-family="Arial, sans-serif" font-size="4">Important: the V must open toward the camera, not toward the user.</text>

  <text x="15" y="122" font-family="Arial, sans-serif" font-size="5" font-weight="bold">Mounting position</text>
  <rect x="125" y="96" width="55" height="28" rx="3" ry="3" fill="none" stroke="black" stroke-width="0.8"/>
  <rect x="146" y="90" width="13" height="8" rx="1.5" ry="1.5" fill="none" stroke="black" stroke-width="0.8"/>
  <line x1="152.5" y1="90" x2="152.5" y2="66" stroke="black" stroke-width="0.8"/>
  <line x1="152.5" y1="66" x2="132" y2="46" stroke="black" stroke-width="1"/>
  <line x1="152.5" y1="66" x2="173" y2="46" stroke="black" stroke-width="1"/>
  <text x="125" y="134" font-family="Arial, sans-serif" font-size="4">Attach the V-board centered on the front / charging edge of the pad.</text>
</svg>
`;
}

writeFile(`aruco-original-left-id-${ids.left}.svg`, buildSingleMarkerSheet('LEFT FACE', ids.left));
writeFile(`aruco-original-right-id-${ids.right}.svg`, buildSingleMarkerSheet('RIGHT FACE', ids.right));
writeFile('pad-aruco-v-board-print-sheet.svg', buildPrintSheet());
writeFile('pad-aruco-v-board-assembly-schematic.svg', buildAssemblySchematic());

console.log(`Generated printable assets in ${outputDir}`);

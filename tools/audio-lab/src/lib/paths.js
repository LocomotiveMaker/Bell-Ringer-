import path from "node:path";

import { outputDir, tmpDir } from "../config.js";

export function resolveInputPath(filePath) {
  return path.resolve(process.cwd(), filePath);
}

export function createOutputPath(group, fileName) {
  return path.join(outputDir, group, fileName);
}

export function createTempPath(fileName) {
  return path.join(tmpDir, fileName);
}

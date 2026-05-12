import fs from "node:fs/promises";
import path from "node:path";

export const supportedAudioExtensions = new Set([
  ".aif",
  ".aiff",
  ".flac",
  ".m4a",
  ".mp3",
  ".ogg",
  ".opus",
  ".wav",
  ".wave",
  ".wma"
]);

export function isSupportedAudioFile(filePath) {
  return supportedAudioExtensions.has(path.extname(filePath).toLowerCase());
}

async function walkDirectory(dirPath, results) {
  const entries = await fs.readdir(dirPath, { withFileTypes: true });

  for (const entry of entries) {
    const entryPath = path.join(dirPath, entry.name);

    if (entry.isDirectory()) {
      await walkDirectory(entryPath, results);
      continue;
    }

    if (entry.isFile() && isSupportedAudioFile(entryPath)) {
      results.push(entryPath);
    }
  }
}

export async function listAudioFiles(inputPath) {
  const resolvedInput = path.resolve(inputPath);
  const stat = await fs.stat(resolvedInput);

  if (stat.isFile()) {
    return isSupportedAudioFile(resolvedInput) ? [resolvedInput] : [];
  }

  if (!stat.isDirectory()) {
    return [];
  }

  const results = [];
  await walkDirectory(resolvedInput, results);
  return results.sort((a, b) => a.localeCompare(b));
}

export function inferCategory(filePath, rootPath) {
  const resolvedRoot = path.resolve(rootPath);
  const relativePath = path.relative(resolvedRoot, filePath);
  const parts = relativePath.split(path.sep).filter(Boolean);

  if (parts.length > 1) {
    return parts[0];
  }

  return path.basename(resolvedRoot);
}

export function createMirroredOutputPath(inputRoot, filePath, outputRoot, extensionSuffix) {
  const relativePath = path.relative(path.resolve(inputRoot), filePath);
  const parsed = path.parse(relativePath);
  return path.join(outputRoot, parsed.dir, `${parsed.name}${extensionSuffix}`);
}

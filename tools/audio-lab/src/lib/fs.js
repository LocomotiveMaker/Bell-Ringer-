import fs from "node:fs/promises";
import path from "node:path";

export async function ensureDir(dirPath) {
  await fs.mkdir(dirPath, { recursive: true });
}

export async function readJsonFile(filePath) {
  const raw = await fs.readFile(filePath, "utf8");
  return JSON.parse(raw);
}

export async function writeJsonFile(filePath, value) {
  await ensureDir(path.dirname(filePath));
  await fs.writeFile(filePath, `${JSON.stringify(value, null, 2)}\n`, "utf8");
}

export async function writeBufferFile(filePath, buffer) {
  await ensureDir(path.dirname(filePath));
  await fs.writeFile(filePath, buffer);
}

export async function readTextOrJsonSpec(filePath) {
  if (filePath.toLowerCase().endsWith(".json")) {
    return readJsonFile(filePath);
  }

  return { input: await fs.readFile(filePath, "utf8") };
}

export function withExtension(filePath, newExtension) {
  const parsed = path.parse(filePath);
  return path.join(parsed.dir, `${parsed.name}${newExtension}`);
}

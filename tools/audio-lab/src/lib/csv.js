import { ensureDir } from "./fs.js";
import fs from "node:fs/promises";
import path from "node:path";

function escapeCsvCell(value) {
  if (value === null || value === undefined) {
    return "";
  }

  const text = String(value);
  if (!/[",\r\n]/.test(text)) {
    return text;
  }

  return `"${text.replaceAll('"', '""')}"`;
}

export async function writeCsvFile(filePath, rows, columns) {
  await ensureDir(path.dirname(filePath));

  const lines = [
    columns.map((column) => escapeCsvCell(column.header)).join(","),
    ...rows.map((row) =>
      columns.map((column) => escapeCsvCell(row[column.key])).join(",")
    )
  ];

  await fs.writeFile(filePath, `${lines.join("\n")}\n`, "utf8");
}

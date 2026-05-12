import fs from "node:fs/promises";
import path from "node:path";

import { parseCommandArgs, requireStringOption } from "../lib/args.js";
import { writeCsvFile } from "../lib/csv.js";
import { createOutputPath, resolveInputPath } from "../lib/paths.js";
import { reportToScoreRow, scorecardColumns, sortScoreRows } from "../lib/scorecard.js";

async function listJsonFiles(inputPath, results = []) {
  const stat = await fs.stat(inputPath);

  if (stat.isFile()) {
    if (inputPath.toLowerCase().endsWith(".json")) {
      results.push(inputPath);
    }
    return results;
  }

  const entries = await fs.readdir(inputPath, { withFileTypes: true });
  for (const entry of entries) {
    const entryPath = path.join(inputPath, entry.name);
    if (entry.isDirectory()) {
      await listJsonFiles(entryPath, results);
    } else if (entry.isFile() && entry.name.toLowerCase().endsWith(".json")) {
      results.push(entryPath);
    }
  }

  return results;
}

export async function runScorecardCommand(args) {
  const values = parseCommandArgs(args, {
    input: { type: "string" },
    output: { type: "string" }
  });

  const inputPath = resolveInputPath(
    requireStringOption(values, "input", "--input is required for scorecard.")
  );
  const jsonFiles = await listJsonFiles(inputPath);
  const rows = [];

  for (const jsonFile of jsonFiles) {
    const report = JSON.parse(await fs.readFile(jsonFile, "utf8"));
    if (report?.derived && report?.features && report?.input) {
      rows.push(reportToScoreRow(report));
    }
  }

  const outputPath = values.output
    ? resolveInputPath(values.output)
    : createOutputPath("scorecards", `scorecard-${Date.now()}.csv`);

  await writeCsvFile(outputPath, sortScoreRows(rows), scorecardColumns);
  console.log(`Scorecard written to ${outputPath}`);
  console.log(`rows=${rows.length}`);
}

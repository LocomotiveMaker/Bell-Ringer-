import fs from "node:fs/promises";
import path from "node:path";

import { parseCommandArgs, requireStringOption } from "../lib/args.js";
import {
  createMirroredOutputPath,
  inferCategory,
  listAudioFiles
} from "../lib/audioFiles.js";
import { writeCsvFile } from "../lib/csv.js";
import { writeJsonFile } from "../lib/fs.js";
import { prepareAudioFile } from "../lib/ffmpeg.js";
import { createOutputPath, resolveInputPath } from "../lib/paths.js";
import { reportToScoreRow, scorecardColumns, sortScoreRows } from "../lib/scorecard.js";
import { generateSpectrogramPng } from "../lib/spectrogram.js";
import { analyzeAudioFile } from "../lib/analysis.js";

function timestampForPath() {
  return new Date().toISOString().replaceAll(":", "").replace(/\..+$/, "").replace("T", "-");
}

export async function runBatchCommand(args) {
  const values = parseCommandArgs(args, {
    input: { type: "string" },
    output: { type: "string" },
    spectrograms: { type: "boolean", default: false },
    "unity-import": { type: "boolean", default: false },
    "unity-output": { type: "string" },
    normalize: { type: "boolean", default: false },
    "sample-rate": { type: "string" },
    channels: { type: "string" },
    "keep-temp": { type: "boolean", default: false }
  });

  const inputPath = resolveInputPath(
    requireStringOption(values, "input", "--input is required for batch.")
  );
  const outputRoot = values.output
    ? resolveInputPath(values.output)
    : createOutputPath("batch", `${path.basename(inputPath)}-${timestampForPath()}`);
  const analysisRoot = path.join(outputRoot, "analysis");
  const spectrogramRoot = path.join(outputRoot, "spectrograms");
  const unityOutputRoot = values["unity-output"]
    ? resolveInputPath(values["unity-output"])
    : path.join(outputRoot, "unity-import");
  const files = await listAudioFiles(inputPath);
  const mirrorRoot =
    files.length === 1 && path.resolve(files[0]) === path.resolve(inputPath)
      ? path.dirname(inputPath)
      : inputPath;
  const rows = [];
  const sampleRate = values["sample-rate"] ? Number(values["sample-rate"]) : 48000;
  const channels = values.channels ? Number(values.channels) : 1;

  console.log(`Batch input: ${inputPath}`);
  console.log(`Audio files: ${files.length}`);

  for (const file of files) {
    const report = await analyzeAudioFile(file);
    report.category = inferCategory(file, mirrorRoot);

    const tempPath = report.prepared.path;
    if (!values["keep-temp"] && tempPath) {
      await fs.rm(tempPath, { force: true });
      report.prepared.path = null;
    }

    const analysisPath = createMirroredOutputPath(mirrorRoot, file, analysisRoot, ".analysis.json");
    await writeJsonFile(analysisPath, report);
    rows.push(reportToScoreRow(report));

    if (values.spectrograms) {
      const spectrogramPath = createMirroredOutputPath(
        mirrorRoot,
        file,
        spectrogramRoot,
        ".spectrogram.png"
      );
      await generateSpectrogramPng(file, spectrogramPath);
    }

    if (values["unity-import"]) {
      const unityPath = createMirroredOutputPath(mirrorRoot, file, unityOutputRoot, ".wav");
      await prepareAudioFile(file, unityPath, {
        sampleRate,
        channels,
        normalize: values.normalize
      });
    }

    console.log(`Processed ${file}`);
  }

  const scorecardPath = path.join(outputRoot, "scorecard.csv");
  await writeCsvFile(scorecardPath, sortScoreRows(rows), scorecardColumns);

  console.log(`Batch complete. output=${outputRoot}`);
  console.log(`Scorecard written to ${scorecardPath}`);
}

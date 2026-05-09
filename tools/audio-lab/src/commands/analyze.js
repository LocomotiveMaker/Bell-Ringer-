import fs from "node:fs/promises";
import path from "node:path";

import { parseCommandArgs, requireStringOption } from "../lib/args.js";
import { analyzeAudioFile } from "../lib/analysis.js";
import { writeJsonFile } from "../lib/fs.js";
import { createOutputPath, resolveInputPath } from "../lib/paths.js";

export async function runAnalyzeCommand(args) {
  const values = parseCommandArgs(args, {
    input: { type: "string" },
    output: { type: "string" },
    "frame-size": { type: "string" },
    "hop-size": { type: "string" },
    "keep-temp": { type: "boolean", default: false }
  });

  const inputPath = resolveInputPath(
    requireStringOption(values, "input", "--input is required for analyze.")
  );

  const report = await analyzeAudioFile(inputPath, {
    frameSize: values["frame-size"] ? Number(values["frame-size"]) : 2048,
    hopSize: values["hop-size"] ? Number(values["hop-size"]) : 1024
  });

  const outputPath = values.output
    ? resolveInputPath(values.output)
    : createOutputPath("analysis", `${path.parse(inputPath).name}.analysis.json`);

  await writeJsonFile(outputPath, report);

  if (!values["keep-temp"]) {
    await fs.rm(report.prepared.path, { force: true });
    report.prepared.path = null;
    await writeJsonFile(outputPath, report);
  }

  console.log(`Analysis written to ${outputPath}`);
  console.log(
    `brightness=${report.derived.brightness.toFixed(3)} ` +
      `noisiness=${report.derived.noisiness.toFixed(3)} ` +
      `instability=${report.derived.instability.toFixed(3)} ` +
      `harshness=${report.derived.harshness.toFixed(3)}`
  );
}

import path from "node:path";

import { parseCommandArgs, requireStringOption } from "../lib/args.js";
import { createOutputPath, resolveInputPath } from "../lib/paths.js";
import { generateSpectrogramPng } from "../lib/spectrogram.js";

export async function runSpectrogramCommand(args) {
  const values = parseCommandArgs(args, {
    input: { type: "string" },
    output: { type: "string" },
    width: { type: "string" },
    height: { type: "string" },
    "frame-size": { type: "string" },
    "hop-size": { type: "string" },
    "min-db": { type: "string" },
    "max-db": { type: "string" },
    "linear-frequency": { type: "boolean", default: false },
    "keep-temp": { type: "boolean", default: false }
  });

  const inputPath = resolveInputPath(
    requireStringOption(values, "input", "--input is required for spectrogram.")
  );
  const outputPath = values.output
    ? resolveInputPath(values.output)
    : createOutputPath("spectrograms", `${path.parse(inputPath).name}.spectrogram.png`);

  const result = await generateSpectrogramPng(inputPath, outputPath, {
    width: values.width ? Number(values.width) : undefined,
    height: values.height ? Number(values.height) : undefined,
    frameSize: values["frame-size"] ? Number(values["frame-size"]) : undefined,
    hopSize: values["hop-size"] ? Number(values["hop-size"]) : undefined,
    minDb: values["min-db"] ? Number(values["min-db"]) : undefined,
    maxDb: values["max-db"] ? Number(values["max-db"]) : undefined,
    logFrequency: !values["linear-frequency"],
    keepTemp: values["keep-temp"]
  });

  console.log(`Spectrogram written to ${result.outputPath}`);
}

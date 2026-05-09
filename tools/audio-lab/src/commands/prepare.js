import path from "node:path";

import { parseCommandArgs, requireStringOption } from "../lib/args.js";
import { prepareAudioFile } from "../lib/ffmpeg.js";
import { createOutputPath, resolveInputPath } from "../lib/paths.js";

export async function runPrepareCommand(args) {
  const values = parseCommandArgs(args, {
    input: { type: "string" },
    output: { type: "string" },
    normalize: { type: "boolean", default: false },
    "sample-rate": { type: "string" },
    channels: { type: "string" },
    "trim-start": { type: "string" },
    duration: { type: "string" }
  });

  const inputPath = resolveInputPath(
    requireStringOption(values, "input", "--input is required for prepare.")
  );
  const outputPath = values.output
    ? resolveInputPath(values.output)
    : createOutputPath("prepared", `${path.parse(inputPath).name}.prepared.wav`);

  await prepareAudioFile(inputPath, outputPath, {
    sampleRate: values["sample-rate"] ? Number(values["sample-rate"]) : 48000,
    channels: values.channels ? Number(values.channels) : 1,
    normalize: values.normalize,
    trimStart: values["trim-start"] ? Number(values["trim-start"]) : undefined,
    duration: values.duration ? Number(values.duration) : undefined
  });

  console.log(`Prepared audio written to ${outputPath}`);
}

import fs from "node:fs/promises";
import path from "node:path";

import { repoRoot } from "../config.js";
import { parseCommandArgs, requireStringOption } from "../lib/args.js";
import { createMirroredOutputPath, listAudioFiles } from "../lib/audioFiles.js";
import { prepareAudioFile } from "../lib/ffmpeg.js";
import { resolveInputPath } from "../lib/paths.js";

export async function runUnityImportCommand(args) {
  const values = parseCommandArgs(args, {
    input: { type: "string" },
    output: { type: "string" },
    normalize: { type: "boolean", default: false },
    "sample-rate": { type: "string" },
    channels: { type: "string" },
    "dry-run": { type: "boolean", default: false }
  });

  const inputPath = resolveInputPath(
    requireStringOption(values, "input", "--input is required for unity-import.")
  );
  const outputRoot = values.output
    ? resolveInputPath(values.output)
    : path.join(repoRoot, "Assets", "Audio", "Processed");
  const files = await listAudioFiles(inputPath);
  const mirrorRoot =
    files.length === 1 && path.resolve(files[0]) === path.resolve(inputPath)
      ? path.dirname(inputPath)
      : inputPath;
  const sampleRate = values["sample-rate"] ? Number(values["sample-rate"]) : 48000;
  const channels = values.channels ? Number(values.channels) : 1;

  await fs.mkdir(outputRoot, { recursive: true });

  for (const file of files) {
    const outputPath = createMirroredOutputPath(mirrorRoot, file, outputRoot, ".wav");

    if (values["dry-run"]) {
      console.log(`${file} -> ${outputPath}`);
      continue;
    }

    await prepareAudioFile(file, outputPath, {
      sampleRate,
      channels,
      normalize: values.normalize
    });

    console.log(`Imported ${outputPath}`);
  }

  console.log(`Unity import complete. files=${files.length}`);
}

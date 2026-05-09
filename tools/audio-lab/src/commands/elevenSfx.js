import { defaults } from "../config.js";
import { parseCommandArgs } from "../lib/args.js";
import { readJsonFile, writeBufferFile } from "../lib/fs.js";
import { createOutputPath, resolveInputPath } from "../lib/paths.js";
import { createSoundEffect } from "../lib/providers/elevenlabs.js";

export async function runElevenSfxCommand(args) {
  const values = parseCommandArgs(args, {
    prompt: { type: "string" },
    spec: { type: "string" },
    output: { type: "string" },
    "output-format": { type: "string" },
    duration: { type: "string" },
    loop: { type: "boolean", default: false },
    "prompt-influence": { type: "string" },
    model: { type: "string" }
  });

  let spec = {};
  if (values.spec) {
    spec = await readJsonFile(resolveInputPath(values.spec));
  }

  const prompt = values.prompt || spec.text;
  if (!prompt) {
    throw new Error("Provide --prompt or --spec with a text field for eleven-sfx.");
  }

  const outputFormat = values["output-format"] || spec.outputFormat;
  const fileExtension = outputFormat?.startsWith("pcm") ? "pcm" : "mp3";
  const outputPath = values.output
    ? resolveInputPath(values.output)
    : createOutputPath("elevenlabs", `sfx-${Date.now()}.${fileExtension}`);

  const result = await createSoundEffect({
    text: prompt,
    modelId: values.model || spec.modelId || defaults.elevenLabsSfxModel,
    durationSeconds: values.duration ? Number(values.duration) : spec.durationSeconds,
    loop: values.loop || spec.loop || false,
    promptInfluence: values["prompt-influence"]
      ? Number(values["prompt-influence"])
      : spec.promptInfluence,
    outputFormat
  });

  await writeBufferFile(outputPath, result.audio);
  console.log(`ElevenLabs sound effect written to ${outputPath}`);
  if (result.characterCost) {
    console.log(`characterCost=${result.characterCost}`);
  }
}

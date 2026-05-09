import path from "node:path";

import { defaults } from "../config.js";
import { parseCommandArgs } from "../lib/args.js";
import { readJsonFile, readTextOrJsonSpec, writeBufferFile } from "../lib/fs.js";
import { createOutputPath, resolveInputPath } from "../lib/paths.js";
import { createSpeechAudio } from "../lib/providers/openai.js";

export async function runOpenAiTtsCommand(args) {
  const values = parseCommandArgs(args, {
    text: { type: "string" },
    spec: { type: "string" },
    output: { type: "string" },
    voice: { type: "string" },
    model: { type: "string" },
    instructions: { type: "string" },
    "response-format": { type: "string" },
    speed: { type: "string" }
  });

  let spec = {};
  if (values.spec) {
    spec = await readJsonFile(resolveInputPath(values.spec));
  }

  const input =
    values.text ||
    spec.input ||
    (spec.textFile ? (await readTextOrJsonSpec(resolveInputPath(spec.textFile))).input : null);

  if (!input) {
    throw new Error("Provide --text or --spec with an input field for openai-tts.");
  }

  const responseFormat = values["response-format"] || spec.responseFormat || "wav";
  const audio = await createSpeechAudio({
    input,
    instructions: values.instructions || spec.instructions,
    voice: values.voice || spec.voice || defaults.openAiTtsVoice,
    model: values.model || spec.model || defaults.openAiTtsModel,
    responseFormat,
    speed: values.speed ? Number(values.speed) : spec.speed
  });

  const outputPath = values.output
    ? resolveInputPath(values.output)
    : createOutputPath("openai", `tts-${Date.now()}.${responseFormat}`);

  await writeBufferFile(outputPath, audio);
  console.log(`OpenAI TTS audio written to ${outputPath}`);
}

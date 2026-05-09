import path from "node:path";

import { defaults } from "../config.js";
import { parseCommandArgs, requireStringOption } from "../lib/args.js";
import { writeBufferFile, writeJsonFile } from "../lib/fs.js";
import { createOutputPath, resolveInputPath } from "../lib/paths.js";
import { createTranscription } from "../lib/providers/openai.js";

export async function runOpenAiSttCommand(args) {
  const values = parseCommandArgs(args, {
    input: { type: "string" },
    output: { type: "string" },
    model: { type: "string" },
    prompt: { type: "string" },
    language: { type: "string" },
    "response-format": { type: "string" }
  });

  const inputPath = resolveInputPath(
    requireStringOption(values, "input", "--input is required for openai-stt.")
  );

  const responseFormat = values["response-format"] || "json";
  const result = await createTranscription({
    inputPath,
    model: values.model || defaults.openAiSttModel,
    prompt: values.prompt,
    language: values.language,
    responseFormat
  });

  if (responseFormat === "text" || responseFormat === "srt" || responseFormat === "vtt") {
    const outputPath = values.output
      ? resolveInputPath(values.output)
      : createOutputPath("openai", `${path.parse(inputPath).name}.${responseFormat}`);

    await writeBufferFile(outputPath, Buffer.from(result.text ?? String(result), "utf8"));
    console.log(`OpenAI transcription written to ${outputPath}`);
    return;
  }

  const outputPath = values.output
    ? resolveInputPath(values.output)
    : createOutputPath("openai", `${path.parse(inputPath).name}.transcription.json`);

  await writeJsonFile(outputPath, result);
  console.log(`OpenAI transcription written to ${outputPath}`);
}

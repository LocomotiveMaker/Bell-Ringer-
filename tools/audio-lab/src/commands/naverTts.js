import { parseCommandArgs } from "../lib/args.js";
import { readJsonFile, writeBufferFile } from "../lib/fs.js";
import { createOutputPath, resolveInputPath } from "../lib/paths.js";
import { createNaverClovaVoice } from "../lib/providers/naverClova.js";

export async function runNaverTtsCommand(args) {
  const values = parseCommandArgs(args, {
    text: { type: "string" },
    spec: { type: "string" },
    output: { type: "string" },
    speaker: { type: "string" },
    volume: { type: "string" },
    speed: { type: "string" },
    pitch: { type: "string" },
    emotion: { type: "string" },
    "emotion-strength": { type: "string" },
    format: { type: "string" },
    "sampling-rate": { type: "string" },
    alpha: { type: "string" },
    "end-pitch": { type: "string" }
  });

  let spec = {};
  if (values.spec) {
    spec = await readJsonFile(resolveInputPath(values.spec));
  }

  const text = values.text || spec.text;
  if (!text) {
    throw new Error("Provide --text or --spec with a text field for naver-tts.");
  }

  const format = values.format || spec.format || "wav";
  const outputPath = values.output
    ? resolveInputPath(values.output)
    : createOutputPath("naver-clova", `tts-${Date.now()}.${format}`);

  const audio = await createNaverClovaVoice({
    text,
    speaker: values.speaker || spec.speaker || "vgoeun",
    volume: numberOption(values.volume, spec.volume, -2),
    speed: numberOption(values.speed, spec.speed, 2),
    pitch: numberOption(values.pitch, spec.pitch, 1),
    emotion: numberOption(values.emotion, spec.emotion, 0),
    emotionStrength: numberOption(
      values["emotion-strength"],
      spec.emotionStrength,
      undefined
    ),
    format,
    samplingRate: numberOption(values["sampling-rate"], spec.samplingRate, 48000),
    alpha: numberOption(values.alpha, spec.alpha, -1),
    endPitch: numberOption(values["end-pitch"], spec.endPitch, undefined)
  });

  await writeBufferFile(outputPath, audio);
  console.log(`NAVER CLOVA Voice audio written to ${outputPath}`);
}

function numberOption(rawValue, specValue, fallback) {
  if (rawValue !== undefined) return Number(rawValue);
  if (specValue !== undefined) return Number(specValue);
  return fallback;
}

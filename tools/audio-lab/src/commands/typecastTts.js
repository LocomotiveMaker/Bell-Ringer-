import { defaults } from "../config.js";
import { parseCommandArgs } from "../lib/args.js";
import { readJsonFile, writeBufferFile } from "../lib/fs.js";
import { createOutputPath, resolveInputPath } from "../lib/paths.js";
import { createTypecastSpeech } from "../lib/providers/typecast.js";

export async function runTypecastTtsCommand(args) {
  const values = parseCommandArgs(args, {
    text: { type: "string" },
    spec: { type: "string" },
    output: { type: "string" },
    "voice-id": { type: "string" },
    model: { type: "string" },
    language: { type: "string" },
    "emotion-type": { type: "string" },
    "audio-tempo": { type: "string" },
    "audio-pitch": { type: "string" },
    volume: { type: "string" },
    seed: { type: "string" }
  });

  let spec = {};
  if (values.spec) {
    spec = await readJsonFile(resolveInputPath(values.spec));
  }

  const text = values.text || spec.text;
  if (!text) {
    throw new Error("Provide --text or --spec with a text field for typecast-tts.");
  }

  const voiceId = values["voice-id"] || spec.voiceId;
  if (!voiceId) {
    throw new Error("Provide --voice-id or a voiceId field in --spec for typecast-tts.");
  }

  const outputFormat = spec.output?.audio_format || "wav";
  const outputPath = values.output
    ? resolveInputPath(values.output)
    : createOutputPath("typecast", `tts-${Date.now()}.${outputFormat}`);

  const audio = await createTypecastSpeech({
    text,
    voiceId,
    model: values.model || spec.model || defaults.typecastTtsModel,
    language: values.language || spec.language || "KOR",
    prompt: spec.prompt || {
      emotion_type: values["emotion-type"] || "normal"
    },
    output: {
      volume: numberOption(values.volume, spec.output?.volume, 85),
      audio_pitch: numberOption(values["audio-pitch"], spec.output?.audio_pitch, -1),
      audio_tempo: numberOption(values["audio-tempo"], spec.output?.audio_tempo, 0.92),
      audio_format: outputFormat
    },
    seed: numberOption(values.seed, spec.seed, undefined)
  });

  await writeBufferFile(outputPath, audio);
  console.log(`Typecast TTS audio written to ${outputPath}`);
}

function numberOption(rawValue, specValue, fallback) {
  if (rawValue !== undefined) return Number(rawValue);
  if (specValue !== undefined) return Number(specValue);
  return fallback;
}

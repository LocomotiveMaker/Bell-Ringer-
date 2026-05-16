import { defaults } from "../config.js";
import { parseCommandArgs } from "../lib/args.js";
import { readJsonFile, writeBufferFile } from "../lib/fs.js";
import { createOutputPath, resolveInputPath } from "../lib/paths.js";
import { createSupertoneSpeech } from "../lib/providers/supertone.js";

export async function runSupertoneTtsCommand(args) {
  const values = parseCommandArgs(args, {
    text: { type: "string" },
    spec: { type: "string" },
    output: { type: "string" },
    "voice-id": { type: "string" },
    language: { type: "string" },
    style: { type: "string" },
    model: { type: "string" },
    "output-format": { type: "string" },
    "pitch-shift": { type: "string" },
    "pitch-variance": { type: "string" },
    speed: { type: "string" },
    duration: { type: "string" },
    similarity: { type: "string" },
    "text-guidance": { type: "string" },
    "subharmonic-amplitude-control": { type: "string" }
  });

  let spec = {};
  if (values.spec) {
    spec = await readJsonFile(resolveInputPath(values.spec));
  }

  const text = values.text || spec.text;
  if (!text) {
    throw new Error("Provide --text or --spec with a text field for supertone-tts.");
  }

  const voiceId = values["voice-id"] || spec.voiceId;
  if (!voiceId) {
    throw new Error("Provide --voice-id or a voiceId field in --spec for supertone-tts.");
  }

  const outputFormat = values["output-format"] || spec.outputFormat || "wav";
  const outputPath = values.output
    ? resolveInputPath(values.output)
    : createOutputPath("supertone", `tts-${Date.now()}.${outputFormat}`);

  const audio = await createSupertoneSpeech({
    text,
    voiceId,
    language: values.language || spec.language || "ko",
    style: values.style || spec.style || "serene",
    model: values.model || spec.model || defaults.supertoneTtsModel,
    outputFormat,
    voiceSettings: {
      pitch_shift: numberOption(values["pitch-shift"], spec.voiceSettings?.pitch_shift, -1),
      pitch_variance: numberOption(
        values["pitch-variance"],
        spec.voiceSettings?.pitch_variance,
        0.7
      ),
      speed: numberOption(values.speed, spec.voiceSettings?.speed, 0.9),
      duration: numberOption(values.duration, spec.voiceSettings?.duration, 0),
      similarity: numberOption(values.similarity, spec.voiceSettings?.similarity, 3),
      text_guidance: numberOption(values["text-guidance"], spec.voiceSettings?.text_guidance, 1),
      subharmonic_amplitude_control: numberOption(
        values["subharmonic-amplitude-control"],
        spec.voiceSettings?.subharmonic_amplitude_control,
        1
      )
    },
    normalizedText: spec.normalizedText
  });

  await writeBufferFile(outputPath, audio);
  console.log(`Supertone TTS audio written to ${outputPath}`);
}

function numberOption(rawValue, specValue, fallback) {
  if (rawValue !== undefined) return Number(rawValue);
  if (specValue !== undefined) return Number(specValue);
  return fallback;
}

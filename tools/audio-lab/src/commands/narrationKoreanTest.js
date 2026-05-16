import path from "node:path";

import { repoRoot } from "../config.js";
import { parseCommandArgs } from "../lib/args.js";
import { ensureDir, writeBufferFile } from "../lib/fs.js";
import { resolveInputPath } from "../lib/paths.js";
import { createNaverClovaVoice } from "../lib/providers/naverClova.js";
import { createSupertoneSpeech } from "../lib/providers/supertone.js";
import { createTypecastSpeech } from "../lib/providers/typecast.js";

const defaultText = "종소리를 따라, 천천히 이동하세요.";

export async function runNarrationKoreanTestCommand(args) {
  const values = parseCommandArgs(args, {
    text: { type: "string" },
    output: { type: "string" },
    providers: { type: "string" },
    "typecast-voice-ids": { type: "string" },
    "supertone-voice-ids": { type: "string" }
  });

  const text = values.text || defaultText;
  const outputDir = values.output
    ? resolveInputPath(values.output)
    : path.join(repoRoot, "Assets", "Audio", "RawCandidates", "Narration", "ProviderTests");

  await ensureDir(outputDir);

  const providers = new Set(
    (values.providers || "naver,typecast,supertone")
      .split(",")
      .map((item) => item.trim().toLowerCase())
      .filter(Boolean)
  );

  const results = [];

  if (providers.has("naver")) {
    results.push(...(await generateNaver(text, outputDir)));
  }

  if (providers.has("typecast")) {
    results.push(
      ...(await generateTypecast(
        text,
        outputDir,
        splitList(values["typecast-voice-ids"] || process.env.TYPECAST_VOICE_IDS)
      ))
    );
  }

  if (providers.has("supertone")) {
    results.push(
      ...(await generateSupertone(
        text,
        outputDir,
        splitList(values["supertone-voice-ids"] || process.env.SUPERTONE_VOICE_IDS)
      ))
    );
  }

  if (results.length === 0) {
    console.log("No files generated. Configure provider keys and voice IDs, then rerun.");
    return;
  }

  console.log("Generated files:");
  for (const result of results) {
    console.log(`${result.provider}\t${result.path}`);
  }
}

async function generateNaver(text, outputDir) {
  const variants = [
    { speaker: "vgoeun", name: "vgoeun_pro_calm", volume: -2, speed: 2, pitch: 1, alpha: -1 },
    { speaker: "vyuna", name: "vyuna_pro_calm", volume: -2, speed: 2, pitch: 1, alpha: -1 },
    { speaker: "vmikyung", name: "vmikyung_pro_calm", volume: -2, speed: 2, pitch: 1, alpha: -1 },
    { speaker: "nkyunglee", name: "nkyunglee_natural", volume: -2, speed: 2, pitch: 1, alpha: -1 },
    { speaker: "nminyoung", name: "nminyoung_natural", volume: -2, speed: 2, pitch: 1, alpha: -1 },
    { speaker: "njiwon", name: "njiwon_natural", volume: -2, speed: 2, pitch: 1, alpha: -1 }
  ];

  const results = [];
  for (const [index, variant] of variants.entries()) {
    const outputPath = path.join(
      outputDir,
      `${String(index + 1).padStart(2, "0")}_naver_${variant.name}.wav`
    );

    try {
      const audio = await createNaverClovaVoice({
        text,
        speaker: variant.speaker,
        volume: variant.volume,
        speed: variant.speed,
        pitch: variant.pitch,
        alpha: variant.alpha,
        emotion: 0,
        emotionStrength: 0,
        format: "wav",
        samplingRate: 48000
      });
      await writeBufferFile(outputPath, audio);
      results.push({ provider: "naver", path: outputPath });
    } catch (error) {
      console.log(`SKIP naver ${variant.speaker}: ${error.message}`);
      break;
    }
  }

  return results;
}

async function generateTypecast(text, outputDir, voiceIds) {
  if (voiceIds.length === 0) {
    console.log("SKIP typecast: TYPECAST_VOICE_IDS is not set.");
    return [];
  }

  const results = [];
  for (const [index, voiceId] of voiceIds.entries()) {
    const outputPath = path.join(
      outputDir,
      `${String(index + 1).padStart(2, "0")}_typecast_${safeName(voiceId)}.wav`
    );

    try {
      const audio = await createTypecastSpeech({
        text,
        voiceId,
        model: process.env.TYPECAST_TTS_MODEL?.trim() || "ssfm-v30",
        language: "KOR",
        prompt: { emotion_type: "normal" },
        output: {
          volume: 85,
          audio_pitch: -1,
          audio_tempo: 0.92,
          audio_format: "wav"
        },
        seed: 42
      });
      await writeBufferFile(outputPath, audio);
      results.push({ provider: "typecast", path: outputPath });
    } catch (error) {
      console.log(`SKIP typecast ${voiceId}: ${error.message}`);
    }
  }

  return results;
}

async function generateSupertone(text, outputDir, voiceIds) {
  if (voiceIds.length === 0) {
    console.log("SKIP supertone: SUPERTONE_VOICE_IDS is not set.");
    return [];
  }

  const results = [];
  for (const [index, voiceId] of voiceIds.entries()) {
    const outputPath = path.join(
      outputDir,
      `${String(index + 1).padStart(2, "0")}_supertone_${safeName(voiceId)}.wav`
    );

    try {
      const audio = await createSupertoneSpeech({
        text,
        voiceId,
        language: "ko",
        style: "serene",
        model: process.env.SUPERTONE_TTS_MODEL?.trim() || "sona_speech_2",
        outputFormat: "wav",
        voiceSettings: {
          pitch_shift: -1,
          pitch_variance: 0.7,
          speed: 0.9,
          duration: 0,
          similarity: 3,
          text_guidance: 1,
          subharmonic_amplitude_control: 1
        }
      });
      await writeBufferFile(outputPath, audio);
      results.push({ provider: "supertone", path: outputPath });
    } catch (error) {
      console.log(`SKIP supertone ${voiceId}: ${error.message}`);
    }
  }

  return results;
}

function splitList(value) {
  return (value || "")
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);
}

function safeName(value) {
  return value.replace(/[^a-z0-9_-]+/gi, "_").slice(0, 60);
}

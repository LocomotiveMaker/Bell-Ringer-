import path from "node:path";
import { fileURLToPath } from "node:url";

const sourceDir = path.dirname(fileURLToPath(import.meta.url));
export const packageRoot = path.resolve(sourceDir, "..");
export const repoRoot = path.resolve(packageRoot, "..", "..");

export function getConfiguredDir(envName, fallbackRelativePath) {
  const rawValue = process.env[envName]?.trim();
  if (rawValue) {
    return path.resolve(packageRoot, rawValue);
  }

  return path.join(packageRoot, fallbackRelativePath);
}

export const outputDir = getConfiguredDir("AUDIO_LAB_OUTPUT_DIR", "output");
export const tmpDir = getConfiguredDir("AUDIO_LAB_TMP_DIR", "tmp");

export const defaults = {
  openAiTtsModel: process.env.OPENAI_TTS_MODEL?.trim() || "gpt-4o-mini-tts",
  openAiTtsVoice: process.env.OPENAI_TTS_VOICE?.trim() || "coral",
  openAiSttModel: process.env.OPENAI_STT_MODEL?.trim() || "gpt-4o-transcribe",
  elevenLabsSfxModel: process.env.ELEVENLABS_SFX_MODEL?.trim() || "eleven_text_to_sound_v2",
  naverClovaVoiceUrl:
    process.env.NAVER_CLOVA_VOICE_URL?.trim() ||
    "https://naveropenapi.apigw.ntruss.com/tts-premium/v1/tts",
  typecastTtsModel: process.env.TYPECAST_TTS_MODEL?.trim() || "ssfm-v30",
  supertoneTtsModel: process.env.SUPERTONE_TTS_MODEL?.trim() || "sona_speech_2"
};

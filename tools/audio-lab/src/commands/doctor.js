import { defaults, outputDir, tmpDir } from "../config.js";
import { getFfmpegPath } from "../lib/ffmpeg.js";

export async function runDoctorCommand() {
  const checks = [
    {
      name: "OPENAI_API_KEY",
      ok: Boolean(process.env.OPENAI_API_KEY?.trim())
    },
    {
      name: "ELEVENLABS_API_KEY",
      ok: Boolean(process.env.ELEVENLABS_API_KEY?.trim())
    }
  ];

  let ffmpegBinary = null;
  try {
    ffmpegBinary = getFfmpegPath();
  } catch (error) {
    ffmpegBinary = `missing (${error instanceof Error ? error.message : String(error)})`;
  }

  console.log("Audio Lab doctor");
  console.log(`  outputDir: ${outputDir}`);
  console.log(`  tmpDir: ${tmpDir}`);
  console.log(`  ffmpeg: ${ffmpegBinary}`);
  console.log(`  openaiTtsModel: ${defaults.openAiTtsModel}`);
  console.log(`  openaiSttModel: ${defaults.openAiSttModel}`);
  console.log(`  elevenLabsSfxModel: ${defaults.elevenLabsSfxModel}`);

  for (const check of checks) {
    console.log(`  ${check.name}: ${check.ok ? "set" : "missing"}`);
  }
}

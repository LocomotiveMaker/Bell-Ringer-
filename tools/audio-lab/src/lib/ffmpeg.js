import { spawn } from "node:child_process";
import path from "node:path";

import ffmpegPath from "ffmpeg-static";

import { ensureDir } from "./fs.js";

export function getFfmpegPath() {
  if (!ffmpegPath) {
    throw new Error("ffmpeg-static is installed but no binary path was resolved.");
  }

  return ffmpegPath;
}

export async function runFfmpeg(args) {
  const binary = getFfmpegPath();

  await new Promise((resolve, reject) => {
    const child = spawn(binary, args, {
      stdio: ["ignore", "pipe", "pipe"]
    });

    let stderr = "";

    child.stderr.on("data", (chunk) => {
      stderr += chunk.toString();
    });

    child.on("error", reject);
    child.on("close", (code) => {
      if (code === 0) {
        resolve();
        return;
      }

      reject(new Error(stderr.trim() || `ffmpeg exited with code ${code}`));
    });
  });
}

export async function prepareAudioFile(inputPath, outputPath, options = {}) {
  const sampleRate = options.sampleRate ?? 48000;
  const channels = options.channels ?? 1;
  const normalize = options.normalize ?? false;
  const trimStart = options.trimStart;
  const duration = options.duration;

  await ensureDir(path.dirname(outputPath));

  const args = ["-y"];

  if (trimStart !== undefined) {
    args.push("-ss", String(trimStart));
  }

  args.push("-i", inputPath);

  if (duration !== undefined) {
    args.push("-t", String(duration));
  }

  args.push("-ac", String(channels), "-ar", String(sampleRate));

  if (normalize) {
    args.push("-af", "loudnorm=I=-16:TP=-1.5:LRA=11");
  }

  args.push("-c:a", "pcm_s16le", outputPath);

  await runFfmpeg(args);
}

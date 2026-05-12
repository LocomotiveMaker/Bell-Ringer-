import fs from "node:fs";
import path from "node:path";
import { randomUUID } from "node:crypto";

import Meyda from "meyda";
import wav from "node-wav";

import { ensureDir } from "./fs.js";
import { prepareAudioFile } from "./ffmpeg.js";
import { createTempPath } from "./paths.js";
import { encodeRgbaPng } from "./png.js";

function averageChannels(channels) {
  if (channels.length === 1) {
    return channels[0];
  }

  const output = new Float32Array(channels[0].length);

  for (const channel of channels) {
    for (let sampleIndex = 0; sampleIndex < channel.length; sampleIndex += 1) {
      output[sampleIndex] += channel[sampleIndex];
    }
  }

  for (let sampleIndex = 0; sampleIndex < output.length; sampleIndex += 1) {
    output[sampleIndex] /= channels.length;
  }

  return output;
}

function buildFrame(signal, offset, frameSize) {
  const frame = new Float32Array(frameSize);
  frame.set(signal.subarray(offset, Math.min(offset + frameSize, signal.length)));
  return frame;
}

function collectSpectra(signal, sampleRate, frameSize, hopSize) {
  const spectra = [];

  Meyda.sampleRate = sampleRate;
  Meyda.bufferSize = frameSize;
  Meyda.windowingFunction = "hanning";

  const lastOffset = Math.max(0, signal.length - frameSize);
  for (let offset = 0; offset <= lastOffset; offset += hopSize) {
    const features = Meyda.extract(["amplitudeSpectrum"], buildFrame(signal, offset, frameSize));
    if (features?.amplitudeSpectrum) {
      spectra.push(features.amplitudeSpectrum);
    }
  }

  return spectra.length > 0 ? spectra : [new Float32Array(frameSize / 2)];
}

function colorRamp(value) {
  const t = Math.max(0, Math.min(1, value));

  if (t < 0.25) {
    const k = t / 0.25;
    return [0, Math.round(20 * k), Math.round(55 + 80 * k)];
  }

  if (t < 0.5) {
    const k = (t - 0.25) / 0.25;
    return [Math.round(20 * k), Math.round(70 + 100 * k), Math.round(135 - 90 * k)];
  }

  if (t < 0.78) {
    const k = (t - 0.5) / 0.28;
    return [Math.round(20 + 210 * k), Math.round(170 + 65 * k), Math.round(45 - 20 * k)];
  }

  const k = (t - 0.78) / 0.22;
  return [Math.round(230 + 25 * k), Math.round(235 + 20 * k), Math.round(25 + 230 * k)];
}

function binForY(y, height, binCount, logFrequency) {
  const topToBottom = y / Math.max(1, height - 1);
  const highToLow = 1 - topToBottom;

  if (!logFrequency) {
    return Math.max(0, Math.min(binCount - 1, Math.round(highToLow * (binCount - 1))));
  }

  const minBin = 1;
  const maxBin = Math.max(minBin + 1, binCount - 1);
  const ratio = maxBin / minBin;
  const bin = Math.round(minBin * ratio ** highToLow);
  return Math.max(0, Math.min(binCount - 1, bin));
}

export async function generateSpectrogramPng(inputPath, outputPath, options = {}) {
  const width = options.width ?? 1200;
  const height = options.height ?? 512;
  const frameSize = options.frameSize ?? 2048;
  const hopSize = options.hopSize ?? 512;
  const logFrequency = options.logFrequency ?? true;
  const preparedWavPath =
    options.preparedWavPath ||
    createTempPath(`${path.parse(inputPath).name}-${randomUUID()}.spectrogram.wav`);

  await prepareAudioFile(inputPath, preparedWavPath, {
    sampleRate: 48000,
    channels: 1
  });

  const decoded = wav.decode(fs.readFileSync(preparedWavPath));
  const signal = averageChannels(decoded.channelData);
  const spectra = collectSpectra(signal, decoded.sampleRate, frameSize, hopSize);

  let peakDb = -Infinity;
  const dbSpectra = spectra.map((spectrum) => {
    const dbSpectrum = new Float32Array(spectrum.length);
    for (let index = 0; index < spectrum.length; index += 1) {
      const db = 20 * Math.log10(Math.max(1e-9, spectrum[index]));
      dbSpectrum[index] = db;
      peakDb = Math.max(peakDb, db);
    }
    return dbSpectrum;
  });

  const maxDb = Number.isFinite(options.maxDb) ? options.maxDb : peakDb;
  const minDb = Number.isFinite(options.minDb) ? options.minDb : maxDb - 80;
  const dbRange = Math.max(1, maxDb - minDb);
  const rgba = Buffer.alloc(width * height * 4);

  for (let x = 0; x < width; x += 1) {
    const spectrumIndex = Math.min(
      dbSpectra.length - 1,
      Math.floor((x / Math.max(1, width - 1)) * dbSpectra.length)
    );
    const spectrum = dbSpectra[spectrumIndex];

    for (let y = 0; y < height; y += 1) {
      const bin = binForY(y, height, spectrum.length, logFrequency);
      const normalized = (spectrum[bin] - minDb) / dbRange;
      const [red, green, blue] = colorRamp(normalized);
      const offset = (y * width + x) * 4;
      rgba[offset] = red;
      rgba[offset + 1] = green;
      rgba[offset + 2] = blue;
      rgba[offset + 3] = 255;
    }
  }

  await ensureDir(path.dirname(outputPath));
  fs.writeFileSync(outputPath, encodeRgbaPng(width, height, rgba));

  if (!options.keepTemp) {
    fs.rmSync(preparedWavPath, { force: true });
  }

  return {
    outputPath,
    width,
    height,
    frameSize,
    hopSize,
    minDb,
    maxDb,
    durationSeconds: signal.length / decoded.sampleRate
  };
}

import fs from "node:fs";
import path from "node:path";
import { randomUUID } from "node:crypto";

import Meyda from "meyda";
import { parseFile } from "music-metadata";
import wav from "node-wav";

import { createTempPath } from "./paths.js";
import { prepareAudioFile } from "./ffmpeg.js";

function aggregateSeries(values) {
  if (values.length === 0) {
    return {
      mean: 0,
      min: 0,
      max: 0,
      stdDev: 0
    };
  }

  const mean = values.reduce((sum, value) => sum + value, 0) / values.length;
  const variance =
    values.reduce((sum, value) => sum + (value - mean) ** 2, 0) / values.length;

  return {
    mean,
    min: Math.min(...values),
    max: Math.max(...values),
    stdDev: Math.sqrt(variance)
  };
}

function percentile(sortedValues, fraction) {
  if (sortedValues.length === 0) {
    return 0;
  }

  const index = Math.min(
    sortedValues.length - 1,
    Math.max(0, Math.floor(sortedValues.length * fraction))
  );

  return sortedValues[index];
}

function clamp01(value) {
  return Math.max(0, Math.min(1, value));
}

function averageChannels(channels) {
  if (channels.length === 1) {
    return channels[0];
  }

  const output = new Float32Array(channels[0].length);

  for (let channelIndex = 0; channelIndex < channels.length; channelIndex += 1) {
    const channel = channels[channelIndex];
    for (let sampleIndex = 0; sampleIndex < channel.length; sampleIndex += 1) {
      output[sampleIndex] += channel[sampleIndex];
    }
  }

  for (let sampleIndex = 0; sampleIndex < output.length; sampleIndex += 1) {
    output[sampleIndex] /= channels.length;
  }

  return output;
}

function buildFrames(signal, frameSize, hopSize) {
  if (signal.length <= frameSize) {
    const frame = new Float32Array(frameSize);
    frame.set(signal.subarray(0, Math.min(signal.length, frameSize)));
    return [frame];
  }

  const frames = [];
  for (let offset = 0; offset <= signal.length - frameSize; offset += hopSize) {
    frames.push(signal.slice(offset, offset + frameSize));
  }

  return frames;
}

function computeSignalStats(signal, rmsSeries) {
  let peak = 0;
  let mean = 0;

  for (let index = 0; index < signal.length; index += 1) {
    const sample = signal[index];
    peak = Math.max(peak, Math.abs(sample));
    mean += sample;
  }

  mean /= Math.max(1, signal.length);

  const sortedRms = [...rmsSeries].sort((a, b) => a - b);

  return {
    peak,
    dcOffset: mean,
    crestFactor: peak / Math.max(1e-6, aggregateSeries(rmsSeries).mean),
    rmsP10: percentile(sortedRms, 0.1),
    rmsP50: percentile(sortedRms, 0.5),
    rmsP90: percentile(sortedRms, 0.9)
  };
}

function computeDerivedScores(featureStats, sampleRate, frameSize, frameToFrameDiffs) {
  const nyquist = sampleRate / 2;
  const binWidth = sampleRate / frameSize;
  const centroidHz = featureStats.spectralCentroid.mean * binWidth;
  const rolloffHz = featureStats.spectralRolloff.mean;
  const centroidDiffHz = frameToFrameDiffs.centroid.mean * binWidth;
  const zcrStdRate = featureStats.zcr.stdDev / frameSize;

  const brightness = clamp01((centroidHz * 0.6 + rolloffHz * 0.4) / nyquist);
  const noisiness = clamp01(featureStats.spectralFlatness.mean);
  const instability = clamp01(
    centroidDiffHz / 250 +
      frameToFrameDiffs.rms.mean * 20 +
      zcrStdRate * 40
  );
  const transientDensity = clamp01(
    frameToFrameDiffs.rms.mean * 4 + featureStats.rms.stdDev * 6
  );
  const harshness = clamp01(
    brightness * 0.45 +
      noisiness * 0.25 +
      clamp01(rolloffHz / nyquist) * 0.2 +
      instability * 0.1
  );

  return {
    brightness,
    noisiness,
    instability,
    transientDensity,
    harshness
  };
}

function extractFrameFeatures(signal, sampleRate, frameSize, hopSize) {
  const featureNames = [
    "rms",
    "spectralCentroid",
    "spectralFlatness",
    "spectralRolloff",
    "spectralSpread",
    "zcr"
  ];

  const frames = buildFrames(signal, frameSize, hopSize);
  const featuresByName = new Map(featureNames.map((name) => [name, []]));
  const centroidDiffs = [];
  const rmsDiffs = [];

  let previousCentroid = null;
  let previousRms = null;

  Meyda.sampleRate = sampleRate;
  Meyda.bufferSize = frameSize;
  Meyda.windowingFunction = "hanning";

  for (const frame of frames) {
    const features = Meyda.extract(featureNames, frame);
    if (!features) {
      continue;
    }

    for (const name of featureNames) {
      featuresByName.get(name).push(features[name] ?? 0);
    }

    if (previousCentroid !== null) {
      centroidDiffs.push(Math.abs(features.spectralCentroid - previousCentroid));
    }

    if (previousRms !== null) {
      rmsDiffs.push(Math.abs(features.rms - previousRms));
    }

    previousCentroid = features.spectralCentroid;
    previousRms = features.rms;
  }

  const frameStats = Object.fromEntries(
    [...featuresByName.entries()].map(([name, values]) => [name, aggregateSeries(values)])
  );

  return {
    frameCount: frames.length,
    frameStats,
    frameToFrameDiffs: {
      centroid: aggregateSeries(centroidDiffs),
      rms: aggregateSeries(rmsDiffs)
    },
    rmsSeries: featuresByName.get("rms") ?? []
  };
}

export async function analyzeAudioFile(inputPath, options = {}) {
  const frameSize = options.frameSize ?? 2048;
  const hopSize = options.hopSize ?? 1024;

  const metadata = await parseFile(inputPath, { duration: true });
  const preparedWavPath =
    options.preparedWavPath ||
    createTempPath(`${path.parse(inputPath).name}-${randomUUID()}.analysis.wav`);

  await prepareAudioFile(inputPath, preparedWavPath, {
    sampleRate: 48000,
    channels: 1
  });

  const wavBuffer = fs.readFileSync(preparedWavPath);
  const decoded = wav.decode(wavBuffer);
  const monoSignal = averageChannels(decoded.channelData);

  const fileStats = fs.statSync(inputPath);
  const { frameCount, frameStats, frameToFrameDiffs, rmsSeries } = extractFrameFeatures(
    monoSignal,
    decoded.sampleRate,
    frameSize,
    hopSize
  );

  const signalStats = computeSignalStats(monoSignal, rmsSeries);

  const report = {
    generatedAt: new Date().toISOString(),
    input: {
      path: inputPath,
      sizeBytes: fileStats.size,
      container: metadata.format.container ?? null,
      codec: metadata.format.codec ?? null,
      sampleRate: metadata.format.sampleRate ?? null,
      numberOfChannels: metadata.format.numberOfChannels ?? null,
      bitsPerSample: metadata.format.bitsPerSample ?? null,
      durationSeconds: metadata.format.duration ?? null,
      bitrate: metadata.format.bitrate ?? null
    },
    prepared: {
      path: preparedWavPath,
      sampleRate: decoded.sampleRate,
      numberOfChannels: decoded.channelData.length,
      durationSeconds: monoSignal.length / decoded.sampleRate
    },
    analysis: {
      frameSize,
      hopSize,
      frameCount
    },
    features: frameStats,
    frameToFrameDiffs,
    signal: signalStats,
    derived: computeDerivedScores(frameStats, decoded.sampleRate, frameSize, frameToFrameDiffs)
  };

  return report;
}

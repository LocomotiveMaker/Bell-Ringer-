import path from "node:path";

export const scorecardColumns = [
  { key: "category", header: "category" },
  { key: "fileName", header: "file_name" },
  { key: "sourcePath", header: "source_path" },
  { key: "durationSeconds", header: "duration_seconds" },
  { key: "sampleRate", header: "sample_rate" },
  { key: "channels", header: "channels" },
  { key: "brightness", header: "brightness" },
  { key: "noisiness", header: "noisiness" },
  { key: "instability", header: "instability" },
  { key: "transientDensity", header: "transient_density" },
  { key: "harshness", header: "harshness" },
  { key: "peak", header: "peak" },
  { key: "rmsP50", header: "rms_p50" },
  { key: "spectralCentroidMean", header: "spectral_centroid_mean" },
  { key: "spectralRolloffMean", header: "spectral_rolloff_mean" }
];

function fixedNumber(value, digits = 4) {
  return Number.isFinite(value) ? Number(value).toFixed(digits) : "";
}

export function reportToScoreRow(report) {
  const sourcePath = report.input?.path ?? "";

  return {
    category: report.category ?? path.basename(path.dirname(sourcePath)),
    fileName: path.basename(sourcePath),
    sourcePath,
    durationSeconds: fixedNumber(report.input?.durationSeconds, 3),
    sampleRate: report.input?.sampleRate ?? "",
    channels: report.input?.numberOfChannels ?? "",
    brightness: fixedNumber(report.derived?.brightness),
    noisiness: fixedNumber(report.derived?.noisiness),
    instability: fixedNumber(report.derived?.instability),
    transientDensity: fixedNumber(report.derived?.transientDensity),
    harshness: fixedNumber(report.derived?.harshness),
    peak: fixedNumber(report.signal?.peak),
    rmsP50: fixedNumber(report.signal?.rmsP50),
    spectralCentroidMean: fixedNumber(report.features?.spectralCentroid?.mean),
    spectralRolloffMean: fixedNumber(report.features?.spectralRolloff?.mean)
  };
}

export function sortScoreRows(rows) {
  return [...rows].sort((a, b) => {
    const categoryCompare = a.category.localeCompare(b.category);
    if (categoryCompare !== 0) {
      return categoryCompare;
    }

    return a.fileName.localeCompare(b.fileName);
  });
}

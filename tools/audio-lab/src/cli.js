import "dotenv/config";

import { runAnalyzeCommand } from "./commands/analyze.js";
import { runBatchCommand } from "./commands/batch.js";
import { runDoctorCommand } from "./commands/doctor.js";
import { runElevenSfxCommand } from "./commands/elevenSfx.js";
import { runOpenAiSttCommand } from "./commands/openaiStt.js";
import { runOpenAiTtsCommand } from "./commands/openaiTts.js";
import { runPrepareCommand } from "./commands/prepare.js";
import { runScorecardCommand } from "./commands/scorecard.js";
import { runSpectrogramCommand } from "./commands/spectrogram.js";
import { runUnityImportCommand } from "./commands/unityImport.js";

const commands = new Map([
  ["doctor", runDoctorCommand],
  ["analyze", runAnalyzeCommand],
  ["batch", runBatchCommand],
  ["scorecard", runScorecardCommand],
  ["spectrogram", runSpectrogramCommand],
  ["prepare", runPrepareCommand],
  ["unity-import", runUnityImportCommand],
  ["openai-tts", runOpenAiTtsCommand],
  ["openai-stt", runOpenAiSttCommand],
  ["eleven-sfx", runElevenSfxCommand]
]);

function printHelp() {
  console.log(`Usage: node src/cli.js <command> [options]

Commands:
  doctor
  analyze
  batch
  scorecard
  spectrogram
  prepare
  unity-import
  openai-tts
  openai-stt
  eleven-sfx
`);
}

const [, , commandName, ...restArgs] = process.argv;

if (!commandName || commandName === "--help" || commandName === "-h") {
  printHelp();
  process.exit(commandName ? 0 : 1);
}

const command = commands.get(commandName);

if (!command) {
  console.error(`Unknown command: ${commandName}`);
  printHelp();
  process.exit(1);
}

try {
  await command(restArgs);
} catch (error) {
  console.error(error instanceof Error ? error.message : String(error));
  process.exit(1);
}

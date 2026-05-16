import { parseCommandArgs } from "../lib/args.js";
import { writeJsonFile } from "../lib/fs.js";
import { resolveInputPath } from "../lib/paths.js";
import { listTypecastVoices } from "../lib/providers/typecast.js";

export async function runTypecastVoicesCommand(args) {
  const values = parseCommandArgs(args, {
    model: { type: "string" },
    gender: { type: "string" },
    age: { type: "string" },
    "use-cases": { type: "string" },
    "voice-type": { type: "string" },
    output: { type: "string" }
  });

  const voices = await listTypecastVoices({
    model: values.model || "ssfm-v30",
    gender: values.gender || "female",
    age: values.age,
    useCases: values["use-cases"],
    voiceType: values["voice-type"] || "original"
  });

  if (values.output) {
    await writeJsonFile(resolveInputPath(values.output), voices);
  }

  for (const voice of voices) {
    console.log(
      [
        voice.voice_id,
        voice.voice_name,
        voice.gender,
        voice.age,
        Array.isArray(voice.use_cases) ? voice.use_cases.join("|") : ""
      ].join("\t")
    );
  }
}

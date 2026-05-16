import { parseCommandArgs } from "../lib/args.js";
import { writeJsonFile } from "../lib/fs.js";
import { resolveInputPath } from "../lib/paths.js";
import { searchSupertoneVoices } from "../lib/providers/supertone.js";

export async function runSupertoneVoicesCommand(args) {
  const values = parseCommandArgs(args, {
    name: { type: "string" },
    language: { type: "string" },
    gender: { type: "string" },
    age: { type: "string" },
    "use-case": { type: "string" },
    "page-size": { type: "string" },
    output: { type: "string" }
  });

  const result = await searchSupertoneVoices({
    name: values.name,
    language: values.language || "ko",
    gender: values.gender || "female",
    age: values.age,
    useCase: values["use-case"] || "narration",
    pageSize: values["page-size"] ? Number(values["page-size"]) : 100
  });

  if (values.output) {
    await writeJsonFile(resolveInputPath(values.output), result);
  }

  for (const voice of result.items || []) {
    console.log(
      [
        voice.voice_id,
        voice.name,
        voice.gender,
        voice.age,
        voice.use_case || "",
        Array.isArray(voice.styles) ? voice.styles.join("|") : ""
      ].join("\t")
    );
  }
}

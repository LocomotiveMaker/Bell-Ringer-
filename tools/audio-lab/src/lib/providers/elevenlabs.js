export async function createSoundEffect({
  text,
  modelId,
  durationSeconds,
  loop,
  promptInfluence,
  outputFormat
}) {
  const apiKey = process.env.ELEVENLABS_API_KEY?.trim();
  if (!apiKey) {
    throw new Error("ELEVENLABS_API_KEY is not set.");
  }

  const url = new URL("https://api.elevenlabs.io/v1/sound-generation");
  if (outputFormat) {
    url.searchParams.set("output_format", outputFormat);
  }

  const response = await fetch(url, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "xi-api-key": apiKey
    },
    body: JSON.stringify({
      text,
      model_id: modelId,
      duration_seconds: durationSeconds,
      loop,
      prompt_influence: promptInfluence
    })
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(`ElevenLabs sound generation failed (${response.status}): ${body}`);
  }

  return {
    audio: Buffer.from(await response.arrayBuffer()),
    characterCost: response.headers.get("character-cost")
  };
}

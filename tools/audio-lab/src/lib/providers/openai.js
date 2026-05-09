import fs from "node:fs";

import OpenAI from "openai";

function createClient() {
  if (!process.env.OPENAI_API_KEY?.trim()) {
    throw new Error("OPENAI_API_KEY is not set.");
  }

  return new OpenAI({
    apiKey: process.env.OPENAI_API_KEY
  });
}

export async function createSpeechAudio({
  input,
  instructions,
  voice,
  model,
  responseFormat,
  speed
}) {
  const client = createClient();

  const response = await client.audio.speech.create({
    model,
    voice,
    input,
    instructions,
    response_format: responseFormat,
    speed
  });

  return Buffer.from(await response.arrayBuffer());
}

export async function createTranscription({
  inputPath,
  model,
  prompt,
  language,
  responseFormat
}) {
  const client = createClient();

  return client.audio.transcriptions.create({
    file: fs.createReadStream(inputPath),
    model,
    prompt,
    language,
    response_format: responseFormat
  });
}

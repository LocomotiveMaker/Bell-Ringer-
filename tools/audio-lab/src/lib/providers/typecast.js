import { readBinaryResponse, readJsonResponse } from "./http.js";

const baseUrl = "https://api.typecast.ai";

function apiKey() {
  const value = process.env.TYPECAST_API_KEY?.trim();
  if (!value) {
    throw new Error("TYPECAST_API_KEY is not set.");
  }

  return value;
}

export async function listTypecastVoices({
  model,
  gender,
  age,
  useCases,
  voiceType
} = {}) {
  const url = new URL("/v2/voices", baseUrl);
  if (model) url.searchParams.set("model", model);
  if (gender) url.searchParams.set("gender", gender);
  if (age) url.searchParams.set("age", age);
  if (useCases) url.searchParams.set("use_cases", useCases);
  if (voiceType) url.searchParams.set("voice_type", voiceType);

  const response = await fetch(url, {
    headers: {
      "X-API-KEY": apiKey()
    }
  });

  return readJsonResponse(response, "Typecast voices");
}

export async function createTypecastSpeech({
  text,
  voiceId,
  model,
  language = "KOR",
  prompt,
  output,
  seed
}) {
  const response = await fetch(`${baseUrl}/v1/text-to-speech`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "X-API-KEY": apiKey()
    },
    body: JSON.stringify({
      voice_id: voiceId,
      text,
      model,
      language,
      prompt,
      output,
      seed
    })
  });

  return readBinaryResponse(response, "Typecast TTS");
}

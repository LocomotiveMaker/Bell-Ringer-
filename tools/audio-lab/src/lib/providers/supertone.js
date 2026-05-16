import { readBinaryResponse, readJsonResponse } from "./http.js";

const baseUrl = "https://supertoneapi.com";

function apiKey() {
  const value = process.env.SUPERTONE_API_KEY?.trim();
  if (!value) {
    throw new Error("SUPERTONE_API_KEY is not set.");
  }

  return value;
}

export async function searchSupertoneVoices({
  name,
  language,
  gender,
  age,
  useCase,
  pageSize = 100,
  nextPageToken
} = {}) {
  const url = new URL("/v1/voices/search", baseUrl);
  if (name) url.searchParams.set("name", name);
  if (language) url.searchParams.set("language", language);
  if (gender) url.searchParams.set("gender", gender);
  if (age) url.searchParams.set("age", age);
  if (useCase) url.searchParams.set("use_case", useCase);
  if (pageSize) url.searchParams.set("page_size", String(pageSize));
  if (nextPageToken) url.searchParams.set("next_page_token", nextPageToken);

  const response = await fetch(url, {
    headers: {
      "x-sup-api-key": apiKey()
    }
  });

  return readJsonResponse(response, "Supertone voices");
}

export async function createSupertoneSpeech({
  text,
  voiceId,
  language = "ko",
  style,
  model,
  outputFormat = "wav",
  voiceSettings,
  includePhonemes = false,
  normalizedText
}) {
  const response = await fetch(`${baseUrl}/v1/text-to-speech/${encodeURIComponent(voiceId)}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "x-sup-api-key": apiKey()
    },
    body: JSON.stringify({
      text,
      language,
      style,
      model,
      output_format: outputFormat,
      voice_settings: voiceSettings,
      include_phonemes: includePhonemes,
      normalized_text: normalizedText
    })
  });

  return readBinaryResponse(response, "Supertone TTS");
}

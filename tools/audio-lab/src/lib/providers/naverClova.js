import { defaults } from "../../config.js";
import { readBinaryResponse } from "./http.js";

function requireEnv(name) {
  const value = process.env[name]?.trim();
  if (!value) {
    throw new Error(`${name} is not set.`);
  }

  return value;
}

export async function createNaverClovaVoice({
  text,
  speaker,
  volume = -2,
  speed = 2,
  pitch = 1,
  emotion = 0,
  emotionStrength,
  format = "wav",
  samplingRate = 48000,
  alpha = -1,
  endPitch
}) {
  const body = new URLSearchParams();
  body.set("speaker", speaker);
  body.set("text", text);
  body.set("volume", String(volume));
  body.set("speed", String(speed));
  body.set("pitch", String(pitch));
  body.set("emotion", String(emotion));
  body.set("format", format);
  if (format === "wav" && samplingRate) {
    body.set("sampling-rate", String(samplingRate));
  }
  if (alpha !== undefined && alpha !== null) {
    body.set("alpha", String(alpha));
  }
  if (emotionStrength !== undefined && emotionStrength !== null) {
    body.set("emotion-strength", String(emotionStrength));
  }
  if (endPitch !== undefined && endPitch !== null) {
    body.set("end-pitch", String(endPitch));
  }

  const response = await fetch(defaults.naverClovaVoiceUrl, {
    method: "POST",
    headers: {
      "Content-Type": "application/x-www-form-urlencoded",
      "x-ncp-apigw-api-key-id": requireEnv("NAVER_CLOVA_VOICE_API_KEY_ID"),
      "x-ncp-apigw-api-key": requireEnv("NAVER_CLOVA_VOICE_API_KEY")
    },
    body
  });

  return readBinaryResponse(response, "NAVER CLOVA Voice");
}

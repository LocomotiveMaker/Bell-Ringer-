export async function readBinaryResponse(response, providerName) {
  if (!response.ok) {
    throw new Error(await createHttpError(response, providerName));
  }

  return Buffer.from(await response.arrayBuffer());
}

export async function readJsonResponse(response, providerName) {
  if (!response.ok) {
    throw new Error(await createHttpError(response, providerName));
  }

  return response.json();
}

async function createHttpError(response, providerName) {
  const text = await response.text().catch(() => "");
  const detail = text ? `: ${text.slice(0, 1000)}` : "";
  return `${providerName} request failed: ${response.status} ${response.statusText}${detail}`;
}

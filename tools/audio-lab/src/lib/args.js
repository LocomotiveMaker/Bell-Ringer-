import { parseArgs } from "node:util";

export function parseCommandArgs(args, options) {
  const parsed = parseArgs({
    args,
    options,
    allowPositionals: true,
    strict: true
  });

  return parsed.values;
}

export function requireStringOption(values, key, message) {
  const value = values[key];
  if (typeof value !== "string" || value.trim() === "") {
    throw new Error(message);
  }

  return value.trim();
}

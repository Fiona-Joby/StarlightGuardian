import http from "node:http";
import OpenAI from "openai";

const port = Number.parseInt(process.env.PORT ?? "3000", 10);
const maxBodyBytes = 16 * 1024;
const maxHistoryItems = 4;
const maxQuestionLength = 500;

const server = http.createServer(async (request, response) => {
  setCommonHeaders(response);

  if (request.method === "OPTIONS") {
    response.writeHead(204);
    response.end();
    return;
  }

  const pathname = new URL(request.url ?? "/", "http://localhost").pathname;
  if (request.method === "GET" && pathname === "/health") {
    sendJson(response, 200, { ok: true, service: "starlight-guardian-coach" });
    return;
  }

  if (request.method !== "POST" || pathname !== "/coach") {
    sendJson(response, 404, { error: "Not found" });
    return;
  }

  if (!process.env.OPENAI_API_KEY) {
    sendJson(response, 503, { error: "AI coaching is not configured." });
    return;
  }

  try {
    const body = await readJsonBody(request);
    const question = typeof body.question === "string" ? body.question.trim() : "";
    if (!question || question.length > maxQuestionLength) {
      sendJson(response, 400, { error: `Question must be between 1 and ${maxQuestionLength} characters.` });
      return;
    }

    const summary = normalizeSummary(body.summary);
    const history = normalizeHistory(body.history);
    const client = new OpenAI({ apiKey: process.env.OPENAI_API_KEY });
    const result = await client.responses.create({
      model: process.env.OPENAI_MODEL || "gpt-4o-mini",
      instructions: [
        "You are the friendly coach for Starlight Guardian, a short exhibition game.",
        "Use stageSummary.controlMode to choose the coaching topic: for gesture, focus on practical gesture timing and consistency using the gesture counts only; for keyboard, focus on gameplay choices, stage goals, score, and star collection, and do not discuss gestures.",
        "Do not claim to see the player, camera, hand pose, or movement form. Raw camera and joint data are not provided. Do not diagnose health conditions.",
        "Be specific to the supplied stage summary and player question; do not invent performance data.",
        "Keep each answer to at most two short sentences (about 45 words).",
      ].join(" "),
      input: JSON.stringify({
        stageSummary: summary,
        recentConversation: history,
        playerQuestion: question,
      }),
      max_output_tokens: 100,
    });

    const answer = result.output_text?.trim();
    if (!answer) {
      sendJson(response, 502, { error: "The coach did not return a reply." });
      return;
    }
    sendJson(response, 200, { answer });
  } catch (error) {
    const status = error instanceof RequestBodyError ? error.status : 502;
    const message = status === 413
      ? "Request is too large."
      : status === 400
        ? "Request body must be valid JSON."
        : "The coach is temporarily unavailable.";
    if (status === 502) console.error("OpenAI coaching request failed:", error?.message ?? error);
    sendJson(response, status, { error: message });
  }
});

server.listen(port, "0.0.0.0", () => {
  console.log(`Starlight Guardian Coach API listening on port ${port}`);
});

function setCommonHeaders(response) {
  response.setHeader("Content-Type", "application/json; charset=utf-8");
  response.setHeader("Access-Control-Allow-Origin", "*");
  response.setHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
  response.setHeader("Access-Control-Allow-Headers", "Content-Type");
  response.setHeader("Cache-Control", "no-store");
}

function sendJson(response, status, value) {
  response.writeHead(status);
  response.end(JSON.stringify(value));
}

async function readJsonBody(request) {
  let size = 0;
  const chunks = [];
  for await (const chunk of request) {
    size += chunk.length;
    if (size > maxBodyBytes) throw new RequestBodyError(413);
    chunks.push(chunk);
  }
  try {
    return JSON.parse(Buffer.concat(chunks).toString("utf8"));
  } catch {
    throw new RequestBodyError(400);
  }
}

function normalizeSummary(value) {
  const summary = value && typeof value === "object" ? value : {};
  return {
    stage: shortText(summary.stage, 30, "unknown"),
    controlMode: summary.controlMode === "gesture" ? "gesture" : "keyboard",
    durationSeconds: boundedNumber(summary.durationSeconds, 0, 3600),
    score: boundedNumber(summary.score, 0, 100000),
    starsCollected: boundedNumber(summary.starsCollected, 0, 10000),
    gestures: {
      left: boundedNumber(summary.gestures?.left, 0, 10000),
      right: boundedNumber(summary.gestures?.right, 0, 10000),
      jump: boundedNumber(summary.gestures?.jump, 0, 10000),
      reach: boundedNumber(summary.gestures?.reach, 0, 10000),
      collect: boundedNumber(summary.gestures?.collect, 0, 10000),
    },
  };
}

function normalizeHistory(value) {
  if (!Array.isArray(value)) return [];
  return value.slice(-maxHistoryItems).flatMap((item) => {
    if (!item || !["user", "assistant"].includes(item.role) || typeof item.content !== "string") return [];
    return [{ role: item.role, content: item.content.slice(0, maxQuestionLength) }];
  });
}

function shortText(value, maxLength, fallback) {
  return typeof value === "string" ? value.slice(0, maxLength) : fallback;
}

function boundedNumber(value, min, max) {
  const number = Number(value);
  return Number.isFinite(number) ? Math.min(max, Math.max(min, number)) : 0;
}

class RequestBodyError extends Error {
  constructor(status) {
    super("Invalid request body");
    this.status = status;
  }
}

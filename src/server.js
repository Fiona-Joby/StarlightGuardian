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
        "You are a friendly coach for Starlight Guardian, a short game shown at an exhibition.",
        "Use simple, everyday words that most players will understand. Keep sentences short. Avoid technical terms and explain advice as one clear action at a time.",
        "Every reply must mention at least one real detail from stageSummary.performance or another field that is relevant to this stage. Explain what that result means and give one useful next step. Avoid advice that could fit any player.",
        stageInstructions(summary.stage),
        "Use stageSummary.controlMode to tailor the next step: for gesture controls, give a simple gesture tip as well as referring to the gameplay result; for keyboard controls, focus the tip on gameplay. Do not talk about gestures when keyboard controls are active.",
        "Gesture counts show how often an action was used, not whether it was done correctly. Do not claim to see the player, camera, hand pose, or movement form. Raw camera and joint data are not provided. Do not diagnose health conditions.",
        "Use only facts in the stage summary. Do not guess what the player did. If a stat is missing, do not make one up.",
        "Answer the player's question in at most two short sentences and about 40 words.",
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

function stageInstructions(stage) {
  switch (stage) {
    case "Stage 1":
      return "Stage 1 objective: move and jump to collect the required falling stars. Star count and score are relevant. Do not give combat advice about Stage 2 or Stage 3 enemies.";
    case "Stage 2":
      return "Stage 2 objective: protect the star spirit by defeating the required turrets and blob beasts. Use enemy progress and hits for advice. This stage has no collectible-star objective; do not advise collecting stars or discuss a star score. Do not give Stage 3 drone, slime or hard-beast advice.";
    case "Stage 3":
      return "Stage 3 objective: defeat the required drones, slimes and hard beasts, then rescue the star spirit and restore the constellation. Use enemy progress and hits for advice. This stage has no collectible stars or score objective. Never advise collecting stars or discuss star counts or scores. Do not give Stage 1 star-collection or Stage 2 turret advice.";
    default:
      return "Use only the objectives named in stageSummary.performance. Never recommend objectives from another stage.";
  }
}

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
    performance: shortText(summary.performance, 240, ""),
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

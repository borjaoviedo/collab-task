const fs = require("node:fs");

const raw = fs.readFileSync("contracts/openapi.json", "utf8");
const doc = JSON.parse(raw);

if (!doc.paths || !doc.paths["/health"] || !doc.paths["/health"].get) {
  throw new Error("Incomplete contract: missing GET /health");
}

console.log("OpenAPI contract check passed.");
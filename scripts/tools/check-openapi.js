const fs = require("node:fs");

const raw = fs.readFileSync("contracts/openapi.json", "utf8");
const doc = JSON.parse(raw);

const fail = (m) => { throw new Error(m); };
const get = (o, p) => p.split(".").reduce((a, k) => (a && a[k] !== undefined ? a[k] : undefined), o);
const has = (p) => get(doc, p) !== undefined;

// Base contract
if (!doc.openapi || !String(doc.openapi).startsWith("3.")) fail("OpenAPI 3.x required");
if (!doc.paths) fail("Missing 'paths'");

// Required endpoints
[
  "paths./health.get",
  "paths./auth/register.post",
  "paths./auth/login.post",
  "paths./auth/me.get",
].forEach((p) => { if (!has(p)) fail(`Missing ${p.replace("paths.", "").toUpperCase()}`); });

// Security scheme
if (!has("components.securitySchemes.bearerAuth.scheme")) fail("Missing bearerAuth security scheme");

// Schemas existence
const requiredSchemas = [
  "UserCreateDto",
  "UserLoginDto",
  "UserReadDto",
  "UserSetRoleDto",
  "AuthTokenReadDto",
  "ProblemDetails",
];
requiredSchemas.forEach((s) => {
  if (!has(`components.schemas.${s}`)) fail(`Missing schema ${s}`);
});

// Validate request bodies
if (!has("paths./auth/register.post.requestBody.content.application/json.schema.$ref"))
  fail("POST /auth/register must define JSON requestBody with UserCreateDto");
if (!has("paths./auth/login.post.requestBody.content.application/json.schema.$ref"))
  fail("POST /auth/login must define JSON requestBody with UserLoginDto");

// Validate 200 responses
if (!has("paths./auth/register.post.responses.200.content.application/json.schema.$ref"))
  fail("POST /auth/register 200 must return AuthTokenReadDto");
if (!has("paths./auth/login.post.responses.200.content.application/json.schema.$ref"))
  fail("POST /auth/login 200 must return AuthTokenReadDto");
if (!has("paths./auth/me.get.responses.200.content.application/json.schema.$ref"))
  fail("GET /auth/me 200 must return UserReadDto");

// Problem responses present
[
  "paths./auth/register.post.responses.400.$ref",
  "paths./auth/register.post.responses.409.$ref",
  "paths./auth/login.post.responses.401.$ref",
  "paths./auth/me.get.responses.401.$ref",
].forEach((p) => { if (!has(p)) fail(`Missing problem response: ${p}`); });

// /auth/me requires bearerAuth
const meSec = get(doc, "paths./auth/me.get.security");
if (!Array.isArray(meSec) || !meSec.some((s) => s && Object.prototype.hasOwnProperty.call(s, "bearerAuth")))
  fail("GET /auth/me must require bearerAuth");

// Shape checks for key DTOs
const authProps = get(doc, "components.schemas.AuthTokenReadDto.properties") || {};
["accessToken", "tokenType", "expiresAtUtc", "userId", "email", "role"].forEach((p) => {
  if (!authProps[p]) fail(`AuthTokenReadDto missing property '${p}'`);
});

const userProps = get(doc, "components.schemas.UserReadDto.properties") || {};
["id", "email", "role", "createdAt", "updatedAt", "projectMembershipsCount"].forEach((p) => {
  if (!userProps[p]) fail(`UserReadDto missing property '${p}'`);
});

const setRoleProps = get(doc, "components.schemas.UserSetRoleDto.properties") || {};
["role", "rowVersion"].forEach((p) => {
  if (!setRoleProps[p]) fail(`UserSetRoleDto missing property '${p}'`);
});

// Type sanity with tolerance to allOf/properties
const inferType = (schemaNode) => {
  if (!schemaNode) return undefined;
  if (schemaNode.type) return schemaNode.type;
  if (schemaNode.allOf || schemaNode.oneOf || schemaNode.anyOf) return "object";
  if (schemaNode.properties) return "object";
  return undefined;
};

const ensureObjectSchema = (name) => {
  const node = get(doc, `components.schemas.${name}`);
  const t = inferType(node);
  if (t !== "object") {
    fail(`Type mismatch at components.schemas.${name}: expected 'object', got '${t}'`);
  }
};

["UserCreateDto", "UserLoginDto", "AuthTokenReadDto", "UserReadDto", "UserSetRoleDto", "ProblemDetails"]
  .forEach(ensureObjectSchema);

console.log("OpenAPI contract check passed.");

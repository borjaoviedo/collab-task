const fs = require("node:fs");

// ---------- helpers ----------
const raw = fs.readFileSync("contracts/openapi.json", "utf8");
const doc = JSON.parse(raw);

const fail = (m) => { throw new Error(m); };
const get = (o, p) => p.split(".").reduce((a, k) => (a && a[k] !== undefined ? a[k] : undefined), o);
const has = (p) => get(doc, p) !== undefined;

const lastRefName = (ref) => {
  if (!ref || typeof ref !== "string") return undefined;
  const i = ref.lastIndexOf("/");
  return i >= 0 ? ref.slice(i + 1) : ref;
};

const expectRefEndsWith = (path, ...expectedNames) => {
  const ref = get(doc, path);
  const name = lastRefName(ref);
  if (!name || !expectedNames.includes(name)) {
    fail(`Expected $ref at '${path}' to end with one of [${expectedNames.join(", ")}], got '${ref}'`);
  }
};

const expectStatus = (base, code) => {
  if (!has(`${base}.responses.${code}`)) fail(`Missing response ${code} at ${base.replace(/^paths\./, "")}`);
};

const hasBearerIn = (sec) =>
  Array.isArray(sec) && sec.some((s) => s && Object.prototype.hasOwnProperty.call(s, "bearerAuth"));

const expectBearer = (opPath) => {
  const opSec = get(doc, `${opPath}.security`);
  if (opSec === undefined) {
    if (!hasBearerIn(doc.security)) {
      fail(`Operation must require bearerAuth (root or op): ${opPath.replace(/^paths\./, "")}`);
    }
  } else {
    if (!hasBearerIn(opSec)) {
      fail(`Operation must require bearerAuth: ${opPath.replace(/^paths\./, "")}`);
    }
  }
};

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
  if (t !== "object") fail(`Type mismatch at components.schemas.${name}: expected 'object', got '${t}'`);
};

const hasProblemSchema = (base, code) => {
  if (has(`${base}.responses.${code}.$ref`)) return true;

  const pjsonRef = get(doc, `${base}.responses.${code}.content.application/problem+json.schema.$ref`);
  if (pjsonRef && lastRefName(pjsonRef) === "ProblemDetails") return true;

  const jsonRef = get(doc, `${base}.responses.${code}.content.application/json.schema.$ref`);
  if (jsonRef && lastRefName(jsonRef) === "ProblemDetails") return true;

  return false;
};

const expectProblem = (base, code) => {
  if (!hasProblemSchema(base, code)) {
    fail(`Missing problem response schema ${code} at ${base.replace(/^paths\./, "")}`);
  }
};

// ---------- base contract ----------
if (!doc.openapi || !String(doc.openapi).startsWith("3.")) fail("OpenAPI 3.x required");
if (!doc.paths) fail("Missing 'paths'");

// ---------- security scheme ----------
if (!has("components.securitySchemes.bearerAuth.scheme")) fail("Missing bearerAuth security scheme");

// ---------- required endpoints ----------
[
  "paths./health.get",
  "paths./auth/register.post",
  "paths./auth/login.post",
  "paths./auth/me.get",

  // Users
  "paths./users/{id}.get",
  "paths./users/{id}/name.patch",
  "paths./users/{id}/role.patch",
  "paths./users/{id}.delete",

  // Projects
  "paths./projects.get",
  "paths./projects.post",
  "paths./projects/{projectId}.get",
  "paths./projects/{projectId}/name.patch",
  "paths./projects/{projectId}.delete",

  // Project Members
  "paths./projects/{projectId}/members.get",
  "paths./projects/{projectId}/members.post",
  "paths./projects/{projectId}/members/{userId}/role.patch",
  "paths./projects/{projectId}/members/{userId}/remove.patch",
  "paths./projects/{projectId}/members/{userId}/restore.patch",
].forEach((p) => { if (!has(p)) fail(`Missing ${p.replace("paths.", "").toUpperCase()}`); });

// ---------- core schemas existence ----------
[
  "UserCreateDto",
  "UserLoginDto",
  "UserReadDto",
  "MeReadDto",
  "AuthTokenReadDto",
  "ProjectReadDto",
  "ProjectCreateDto",
  "ProjectMemberReadDto",
  "ProblemDetails",
].forEach((s) => { if (!has(`components.schemas.${s}`)) fail(`Missing schema ${s}`); });

// ---------- requests: auth ----------
expectRefEndsWith("paths./auth/register.post.requestBody.content.application/json.schema.$ref", "UserCreateDto");
expectRefEndsWith("paths./auth/login.post.requestBody.content.application/json.schema.$ref", "UserLoginDto");

// ---------- responses: auth ----------
expectRefEndsWith("paths./auth/register.post.responses.200.content.application/json.schema.$ref", "AuthTokenReadDto");
expectRefEndsWith("paths./auth/login.post.responses.200.content.application/json.schema.$ref", "AuthTokenReadDto");
expectRefEndsWith("paths./auth/me.get.responses.200.content.application/json.schema.$ref", "MeReadDto");

// ---------- problem responses: auth ----------
[
  ["paths./auth/register.post", 400],
  ["paths./auth/register.post", 409],
  ["paths./auth/login.post", 401],
  ["paths./auth/me.get", 401],
].forEach(([b, c]) => expectProblem(b, c));

// ---------- security: protected ops ----------
[
  "paths./auth/me.get",
  // Users
  "paths./users/{id}.get",
  "paths./users/{id}/name.patch",
  "paths./users/{id}/role.patch",
  "paths./users/{id}.delete",
  // Projects
  "paths./projects.get",
  "paths./projects.post",
  "paths./projects/{projectId}.get",
  "paths./projects/{projectId}/name.patch",
  "paths./projects/{projectId}.delete",
  // Project Members
  "paths./projects/{projectId}/members.get",
  "paths./projects/{projectId}/members.post",
  "paths./projects/{projectId}/members/{userId}/role.patch",
  "paths./projects/{projectId}/members/{userId}/remove.patch",
  "paths./projects/{projectId}/members/{userId}/restore.patch",
].forEach(expectBearer);

// ---------- requests: users ----------
expectRefEndsWith("paths./users/{id}/name.patch.requestBody.content.application/json.schema.$ref", "RenameUserDto");
expectRefEndsWith("paths./users/{id}/role.patch.requestBody.content.application/json.schema.$ref", "ChangeRoleDto");
// DELETE with body for concurrency (rowVersion)
expectRefEndsWith("paths./users/{id}.delete.requestBody.content.application/json.schema.$ref", "DeleteUserDto");

// ---------- responses: users ----------
expectRefEndsWith("paths./users/{id}.get.responses.200.content.application/json.schema.$ref", "UserReadDto");
[
  ["paths./users/{id}.get", 401],
  ["paths./users/{id}.get", 404],
  ["paths./users/{id}/name.patch", 204],
  ["paths./users/{id}/name.patch", 400],
  ["paths./users/{id}/name.patch", 401],
  ["paths./users/{id}/name.patch", 404],
  ["paths./users/{id}/name.patch", 409],
  ["paths./users/{id}/role.patch", 204],
  ["paths./users/{id}/role.patch", 400],
  ["paths./users/{id}/role.patch", 401],
  ["paths./users/{id}/role.patch", 403],
  ["paths./users/{id}/role.patch", 404],
  ["paths./users/{id}/role.patch", 409],
  ["paths./users/{id}.delete", 204],
  ["paths./users/{id}.delete", 400],
  ["paths./users/{id}.delete", 401],
  ["paths./users/{id}.delete", 403],
  ["paths./users/{id}.delete", 404],
  ["paths./users/{id}.delete", 409],
].forEach(([b, c]) => expectStatus(b, c));

// ---------- requests: projects ----------
expectRefEndsWith("paths./projects.post.requestBody.content.application/json.schema.$ref", "ProjectCreateDto");
expectRefEndsWith("paths./projects/{projectId}/name.patch.requestBody.content.application/json.schema.$ref", "RenameProjectDto");

// ---------- responses: projects ----------
expectRefEndsWith("paths./projects.get.responses.200.content.application/json.schema.items.$ref", "ProjectReadDto");
expectRefEndsWith("paths./projects/{projectId}.get.responses.200.content.application/json.schema.$ref", "ProjectReadDto");
expectRefEndsWith("paths./projects.post.responses.201.content.application/json.schema.$ref", "ProjectReadDto");
[
  ["paths./projects.get", 401],
  ["paths./projects/{projectId}.get", 401],
  ["paths./projects/{projectId}.get", 403],
  ["paths./projects/{projectId}.get", 404],
  ["paths./projects.post", 400],
  ["paths./projects.post", 401],
  ["paths./projects.post", 409],
  ["paths./projects/{projectId}/name.patch", 204],
  ["paths./projects/{projectId}/name.patch", 400],
  ["paths./projects/{projectId}/name.patch", 401],
  ["paths./projects/{projectId}/name.patch", 403],
  ["paths./projects/{projectId}/name.patch", 404],
  ["paths./projects/{projectId}/name.patch", 409],
  ["paths./projects/{projectId}.delete", 204],
  ["paths./projects/{projectId}.delete", 400],
  ["paths./projects/{projectId}.delete", 401],
  ["paths./projects/{projectId}.delete", 403],
  ["paths./projects/{projectId}.delete", 404],
  ["paths./projects/{projectId}.delete", 409],
].forEach(([b, c]) => expectStatus(b, c));

// ---------- requests: project members ----------
expectRefEndsWith("paths./projects/{projectId}/members.post.requestBody.content.application/json.schema.$ref", "AddMemberDto");
expectRefEndsWith("paths./projects/{projectId}/members/{userId}/role.patch.requestBody.content.application/json.schema.$ref", "ChangeMemberRoleDto");
expectRefEndsWith("paths./projects/{projectId}/members/{userId}/remove.patch.requestBody.content.application/json.schema.$ref", "RemoveMemberDto");
expectRefEndsWith("paths./projects/{projectId}/members/{userId}/restore.patch.requestBody.content.application/json.schema.$ref", "RestoreMemberDto");

// ---------- responses: project members ----------
expectRefEndsWith("paths./projects/{projectId}/members.get.responses.200.content.application/json.schema.items.$ref", "ProjectMemberReadDto");
[
  ["paths./projects/{projectId}/members.get", 401],
  ["paths./projects/{projectId}/members.get", 403],
  ["paths./projects/{projectId}/members.get", 404],
  ["paths./projects/{projectId}/members.post", 201],
  ["paths./projects/{projectId}/members.post", 400],
  ["paths./projects/{projectId}/members.post", 401],
  ["paths./projects/{projectId}/members.post", 403],
  ["paths./projects/{projectId}/members.post", 404],
  ["paths./projects/{projectId}/members.post", 409],
  ["paths./projects/{projectId}/members/{userId}/role.patch", 204],
  ["paths./projects/{projectId}/members/{userId}/role.patch", 400],
  ["paths./projects/{projectId}/members/{userId}/role.patch", 401],
  ["paths./projects/{projectId}/members/{userId}/role.patch", 403],
  ["paths./projects/{projectId}/members/{userId}/role.patch", 404],
  ["paths./projects/{projectId}/members/{userId}/role.patch", 409],
  ["paths./projects/{projectId}/members/{userId}/remove.patch", 204],
  ["paths./projects/{projectId}/members/{userId}/remove.patch", 400],
  ["paths./projects/{projectId}/members/{userId}/remove.patch", 401],
  ["paths./projects/{projectId}/members/{userId}/remove.patch", 403],
  ["paths./projects/{projectId}/members/{userId}/remove.patch", 404],
  ["paths./projects/{projectId}/members/{userId}/remove.patch", 409],
  ["paths./projects/{projectId}/members/{userId}/restore.patch", 204],
  ["paths./projects/{projectId}/members/{userId}/restore.patch", 400],
  ["paths./projects/{projectId}/members/{userId}/restore.patch", 401],
  ["paths./projects/{projectId}/members/{userId}/restore.patch", 403],
  ["paths./projects/{projectId}/members/{userId}/restore.patch", 404],
  ["paths./projects/{projectId}/members/{userId}/restore.patch", 409],
].forEach(([b, c]) => expectStatus(b, c));

// ---------- shape checks ----------
const authProps = get(doc, "components.schemas.AuthTokenReadDto.properties") || {};
["accessToken", "tokenType", "expiresAtUtc", "userId", "email", "name", "role"].forEach((p) => {
  if (!authProps[p]) fail(`AuthTokenReadDto missing property '${p}'`);
});

const meProps = get(doc, "components.schemas.MeReadDto.properties") || {};
["id", "email", "name", "role", "projectMembershipsCount"].forEach((p) => {
  if (!meProps[p]) fail(`MeReadDto missing property '${p}'`);
});

const userProps = get(doc, "components.schemas.UserReadDto.properties") || {};
["id", "email", "name", "role", "createdAt", "updatedAt", "projectMembershipsCount", "rowVersion"].forEach((p) => {
  if (!userProps[p]) fail(`UserReadDto missing property '${p}'`);
});

const projProps = get(doc, "components.schemas.ProjectReadDto.properties") || {};
["id", "name", "slug", "createdAt", "updatedAt", "rowVersion", "membersCount", "currentUserRole"].forEach((p) => {
  if (!projProps[p]) fail(`ProjectReadDto missing property '${p}'`);
});

const pmProps = get(doc, "components.schemas.ProjectMemberReadDto.properties") || {};
["projectId", "userId", "userName", "role", "joinedAt", "removedAt", "rowVersion"].forEach((p) => {
  if (!pmProps[p]) fail(`ProjectMemberReadDto missing property '${p}'`);
});

// ---------- type sanity ----------
[
  "UserCreateDto",
  "UserLoginDto",
  "AuthTokenReadDto",
  "MeReadDto",
  "UserReadDto",
  "ProjectReadDto",
  "ProjectCreateDto",
  "ProjectMemberReadDto",
  "ProblemDetails",
].forEach(ensureObjectSchema);

console.log("OpenAPI contract check passed.");

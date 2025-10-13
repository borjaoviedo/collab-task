// check-openapi.js — CollabTask API contract guard (v0.3.0)
const fs = require("node:fs");

// ---------- load ----------
const raw = fs.readFileSync("contracts/openapi.json", "utf8");
const doc = JSON.parse(raw);

// ---------- helpers ----------
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

// ========== REQUIRED ENDPOINTS ==========

// Health
["paths./health.get"].forEach((p) => { if (!has(p)) fail(`Missing ${p.replace("paths.", "").toUpperCase()}`); });

// Auth
[
  "paths./auth/register.post",
  "paths./auth/login.post",
  "paths./auth/me.get",
].forEach((p) => { if (!has(p)) fail(`Missing ${p.replace("paths.", "").toUpperCase()}`); });

// Users
[
  "paths./users.get",
  "paths./users/{userId}.get",
  "paths./users/by-email.get",
  "paths./users/{userId}/rename.patch",
  "paths./users/{userId}/role.patch",
  "paths./users/{userId}.delete",
].forEach((p) => { if (!has(p)) fail(`Missing ${p.replace("paths.", "").toUpperCase()}`); });

// Projects
[
  "paths./projects.get",
  "paths./projects.post",
  "paths./projects/{projectId}.get",
  "paths./projects/{projectId}/rename.patch",
  "paths./projects/{projectId}.delete",
].forEach((p) => { if (!has(p)) fail(`Missing ${p.replace("paths.", "").toUpperCase()}`); });

// Project Members
[
  "paths./projects/{projectId}/members.get",
  "paths./projects/{projectId}/members.post",
  "paths./projects/{projectId}/members/{userId}.get",
  "paths./projects/{projectId}/members/{userId}/role.get",
  "paths./projects/{projectId}/members/{userId}/role.patch",
  "paths./projects/{projectId}/members/{userId}/remove.patch",
  "paths./projects/{projectId}/members/{userId}/restore.patch",
].forEach((p) => { if (!has(p)) fail(`Missing ${p.replace("paths.", "").toUpperCase()}`); });

// Lanes
[
  "paths./projects/{projectId}/lanes.get",
  "paths./projects/{projectId}/lanes.post",
  "paths./projects/{projectId}/lanes/{laneId}/rename.put",
  "paths./projects/{projectId}/lanes/{laneId}/reorder.put",
  "paths./projects/{projectId}/lanes/{laneId}.delete",
].forEach((p) => { if (!has(p)) fail(`Missing ${p.replace("paths.", "").toUpperCase()}`); });

// Columns
[
  "paths./projects/{projectId}/lanes/{laneId}/columns.get",
  "paths./projects/{projectId}/lanes/{laneId}/columns.post",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/rename.put",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/reorder.put",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}.delete",
].forEach((p) => { if (!has(p)) fail(`Missing ${p.replace("paths.", "").toUpperCase()}`); });

// Tasks
[
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks.get",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks.post",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/edit.patch",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/move.put",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}.delete",
].forEach((p) => { if (!has(p)) fail(`Missing ${p.replace("paths.", "").toUpperCase()}`); });

// Task Notes
[
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes.get",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes.post",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}/edit.patch",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}.delete",
  "paths./notes/me.get",
  "paths./notes/users/{userId}.get",
].forEach((p) => { if (!has(p)) fail(`Missing ${p.replace("paths.", "").toUpperCase()}`); });

// Task Assignments
[
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments.get",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}.get",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments.post",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}/role.patch",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}.delete",
  "paths./assignments/me.get",
  "paths./assignments/users/{userId}.get",
].forEach((p) => { if (!has(p)) fail(`Missing ${p.replace("paths.", "").toUpperCase()}`); });

// Task Activities
[
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/activities.get",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/activities.post",
  "paths./activities/me.get",
].forEach((p) => { if (!has(p)) fail(`Missing ${p.replace("paths.", "").toUpperCase()}`); });

// ---------- core schemas existence ----------
[
  // Auth
  "UserRegisterDto",
  "UserLoginDto",
  "AuthTokenReadDto",
  "MeReadDto",

  // Users
  "UserReadDto",
  "UserRenameDto",
  "UserChangeRoleDto",

  // Projects
  "ProjectReadDto",
  "ProjectCreateDto",
  "ProjectRenameDto",

  // Project Members
  "ProjectMemberReadDto",
  "ProjectMemberCreateDto",
  "ProjectMemberChangeRoleDto",

  // Lanes
  "LaneReadDto",
  "LaneCreateDto",
  "LaneRenameDto",
  "LaneReorderDto",

  // Columns
  "ColumnReadDto",
  "ColumnCreateDto",
  "ColumnRenameDto",
  "ColumnReorderDto",

  // Tasks
  "TaskItemReadDto",
  "TaskItemCreateDto",
  "TaskItemEditDto",
  "TaskItemMoveDto",

  // Notes
  "TaskNoteReadDto",
  "TaskNoteCreateDto",
  "TaskNoteEditDto",

  // Assignments
  "TaskAssignmentReadDto",
  "TaskAssignmentCreateDto",
  "TaskAssignmentChangeRoleDto",

  // Activities
  "TaskActivityReadDto",
  "TaskActivityCreateDto",

  // Error
  "ProblemDetails",
].forEach((s) => { if (!has(`components.schemas.${s}`)) fail(`Missing schema ${s}`); });

// ---------- requests: auth ----------
expectRefEndsWith("paths./auth/register.post.requestBody.content.application/json.schema.$ref", "UserRegisterDto");
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
  // users
  "paths./users.get",
  "paths./users/{userId}.get",
  "paths./users/by-email.get",
  "paths./users/{userId}/rename.patch",
  "paths./users/{userId}/role.patch",
  "paths./users/{userId}.delete",

  // projects
  "paths./projects.get",
  "paths./projects.post",
  "paths./projects/{projectId}.get",
  "paths./projects/{projectId}/rename.patch",
  "paths./projects/{projectId}.delete",

  // members
  "paths./projects/{projectId}/members.get",
  "paths./projects/{projectId}/members.post",
  "paths./projects/{projectId}/members/{userId}.get",
  "paths./projects/{projectId}/members/{userId}/role.get",
  "paths./projects/{projectId}/members/{userId}/role.patch",
  "paths./projects/{projectId}/members/{userId}/remove.patch",
  "paths./projects/{projectId}/members/{userId}/restore.patch",

  // lanes
  "paths./projects/{projectId}/lanes.get",
  "paths./projects/{projectId}/lanes.post",
  "paths./projects/{projectId}/lanes/{laneId}/rename.put",
  "paths./projects/{projectId}/lanes/{laneId}/reorder.put",
  "paths./projects/{projectId}/lanes/{laneId}.delete",

  // columns
  "paths./projects/{projectId}/lanes/{laneId}/columns.get",
  "paths./projects/{projectId}/lanes/{laneId}/columns.post",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/rename.put",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/reorder.put",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}.delete",

  // tasks
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks.get",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks.post",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/edit.patch",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/move.put",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}.delete",

  // notes
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes.get",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes.post",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}/edit.patch",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}.delete",
  "paths./notes/me.get",
  "paths./notes/users/{userId}.get",

  // assignments
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments.get",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}.get",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments.post",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}/role.patch",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}.delete",
  "paths./assignments/me.get",
  "paths./assignments/users/{userId}.get",

  // activities
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/activities.get",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/activities.post",
  "paths./activities/me.get",
].forEach(expectBearer);

// ---------- requests & responses: USERS ----------
expectRefEndsWith("paths./users/{userId}/rename.patch.requestBody.content.application/json.schema.$ref", "UserRenameDto");
expectRefEndsWith("paths./users/{userId}/role.patch.requestBody.content.application/json.schema.$ref", "UserChangeRoleDto");
expectRefEndsWith("paths./users/{userId}.get.responses.200.content.application/json.schema.$ref", "UserReadDto");
expectRefEndsWith("paths./users.get.responses.200.content.application/json.schema.items.$ref", "UserReadDto");
expectRefEndsWith("paths./users/by-email.get.responses.200.content.application/json.schema.$ref", "UserReadDto");
expectRefEndsWith("paths./users/{userId}/role.patch.responses.200.content.application/json.schema.$ref", "UserReadDto");
[
  ["paths./users/{userId}/rename.patch", 200],
  ["paths./users/{userId}/rename.patch", 400],
  ["paths./users/{userId}/rename.patch", 401],
  ["paths./users/{userId}/rename.patch", 404],
  ["paths./users/{userId}/rename.patch", 409],
  ["paths./users/{userId}.delete", 204],
  ["paths./users/{userId}/role.patch", 200],
].forEach(([b, c]) => expectStatus(b, c));

// ---------- requests & responses: PROJECTS ----------
expectRefEndsWith("paths./projects.post.requestBody.content.application/json.schema.$ref", "ProjectCreateDto");
expectRefEndsWith("paths./projects/{projectId}/rename.patch.requestBody.content.application/json.schema.$ref", "ProjectRenameDto");
expectRefEndsWith("paths./projects.get.responses.200.content.application/json.schema.items.$ref", "ProjectReadDto");
expectRefEndsWith("paths./projects/{projectId}.get.responses.200.content.application/json.schema.$ref", "ProjectReadDto");
expectRefEndsWith("paths./projects.post.responses.201.content.application/json.schema.$ref", "ProjectReadDto");

// ---------- requests & responses: MEMBERS ----------
expectRefEndsWith("paths./projects/{projectId}/members.post.requestBody.content.application/json.schema.$ref", "ProjectMemberCreateDto");
expectRefEndsWith("paths./projects/{projectId}/members/{userId}/role.patch.requestBody.content.application/json.schema.$ref", "ProjectMemberChangeRoleDto");
expectRefEndsWith("paths./projects/{projectId}/members.get.responses.200.content.application/json.schema.items.$ref", "ProjectMemberReadDto");

// ---------- requests & responses: LANES ----------
expectRefEndsWith("paths./projects/{projectId}/lanes.post.requestBody.content.application/json.schema.$ref", "LaneCreateDto");
expectRefEndsWith("paths./projects/{projectId}/lanes.get.responses.200.content.application/json.schema.items.$ref", "LaneReadDto");
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/rename.put.requestBody.content.application/json.schema.$ref", "LaneRenameDto");
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/reorder.put.requestBody.content.application/json.schema.$ref", "LaneReorderDto");
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/rename.put.responses.200.content.application/json.schema.$ref", "LaneReadDto");

// ---------- requests & responses: COLUMNS ----------
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns.post.requestBody.content.application/json.schema.$ref", "ColumnCreateDto");
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns.get.responses.200.content.application/json.schema.items.$ref", "ColumnReadDto");
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/rename.put.requestBody.content.application/json.schema.$ref", "ColumnRenameDto");
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/reorder.put.requestBody.content.application/json.schema.$ref", "ColumnReorderDto");
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/rename.put.responses.200.content.application/json.schema.$ref", "ColumnReadDto");

// ---------- requests & responses: TASKS ----------
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks.post.requestBody.content.application/json.schema.$ref", "TaskItemCreateDto");
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks.get.responses.200.content.application/json.schema.items.$ref", "TaskItemReadDto");
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/edit.patch.requestBody.content.application/json.schema.$ref", "TaskItemEditDto");
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/move.put.requestBody.content.application/json.schema.$ref", "TaskItemMoveDto");
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/edit.patch.responses.200.content.application/json.schema.$ref", "TaskItemReadDto");

// ---------- requests & responses: NOTES ----------
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes.post.requestBody.content.application/json.schema.$ref", "TaskNoteCreateDto");
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes.get.responses.200.content.application/json.schema.items.$ref", "TaskNoteReadDto");
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}/edit.patch.requestBody.content.application/json.schema.$ref", "TaskNoteEditDto");

// ---------- requests & responses: ASSIGNMENTS ----------
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments.post.requestBody.content.application/json.schema.$ref", "TaskAssignmentCreateDto");
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments.get.responses.200.content.application/json.schema.items.$ref", "TaskAssignmentReadDto");
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}/role.patch.requestBody.content.application/json.schema.$ref", "TaskAssignmentChangeRoleDto");

// ---------- requests & responses: ACTIVITIES ----------
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/activities.post.requestBody.content.application/json.schema.$ref", "TaskActivityCreateDto");
expectRefEndsWith("paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/activities.get.responses.200.content.application/json.schema.items.$ref", "TaskActivityReadDto");

// ---------- type sanity ----------
[
  "UserRegisterDto",
  "UserLoginDto",
  "AuthTokenReadDto",
  "MeReadDto",
  "UserReadDto",
  "UserRenameDto",
  "UserChangeRoleDto",
  "ProjectReadDto",
  "ProjectCreateDto",
  "ProjectRenameDto",
  "ProjectMemberReadDto",
  "ProjectMemberCreateDto",
  "ProjectMemberChangeRoleDto",
  "LaneReadDto",
  "LaneCreateDto",
  "LaneRenameDto",
  "LaneReorderDto",
  "ColumnReadDto",
  "ColumnCreateDto",
  "ColumnRenameDto",
  "ColumnReorderDto",
  "TaskItemReadDto",
  "TaskItemCreateDto",
  "TaskItemEditDto",
  "TaskItemMoveDto",
  "TaskNoteReadDto",
  "TaskNoteCreateDto",
  "TaskNoteEditDto",
  "TaskAssignmentReadDto",
  "TaskAssignmentCreateDto",
  "TaskAssignmentChangeRoleDto",
  "TaskActivityReadDto",
  "TaskActivityCreateDto",
  "ProblemDetails",
].forEach(ensureObjectSchema);

// ---------- minimal status/problem checks for concurrency ----------
[
  "paths./projects/{projectId}/lanes/{laneId}/rename.put",
  "paths./projects/{projectId}/lanes/{laneId}/reorder.put",
  "paths./projects/{projectId}/lanes/{laneId}.delete",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/rename.put",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/reorder.put",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}.delete",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/edit.patch",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/move.put",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}.delete",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}/edit.patch",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}.delete",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}/role.patch",
  "paths./projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}.delete"
].forEach((b) => expectProblem(b, 412));

console.log("OpenAPI contract check passed.");

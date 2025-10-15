const { spawnSync } = require("child_process");
const path = require("path");
const isWin = process.platform === "win32";

const MAP = {
  dev: { dir: "dev", base: "dev" },
  prod: { dir: "dev", base: "prod" },
  "test:unit": { dir: "test", base: "test.unit" },
  "test:infra": { dir: "test", base: "test.infra" },
  "test:all": ["test:unit", "test:infra"],
};

function runFile(dir, base, extraArgs) {
  if (isWin) {
    const file = path.join(__dirname, dir, `${base}.ps1`);
    const res = spawnSync("powershell.exe",
      ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", file, ...extraArgs],
      { stdio: "inherit" });
    return res.status === 0;
  } else {
    const file = path.join(__dirname, dir, `${base}.sh`);
    const res = spawnSync("bash", [file, ...extraArgs], { stdio: "inherit" });
    return res.status === 0;
  }
}

function runTask(task, extraArgs) {
  const entry = MAP[task];
  if (!entry) {
    console.error(`Unknown task: ${task}`);
    process.exit(1);
  }
  if (Array.isArray(entry)) {
    for (const t of entry) if (!runTask(t, extraArgs)) return false;
    return true;
  }
  return runFile(entry.dir, entry.base, extraArgs);
}

const [, , task, ...args] = process.argv;
if (!task) {
  console.error(`Usage: node scripts/run.js <${Object.keys(MAP).join("|")}> [args...]`);
  process.exit(1);
}
process.exit(runTask(task, args) ? 0 : 1);

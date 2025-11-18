const { spawnSync } = require("child_process");
const path = require("path");
const isWin = process.platform === "win32";

// --------------------------------------------------------------------------------
// Task map: defines how each npm script maps to either a .ps1/.sh script
// or a direct dotnet command. Dotnet-based tasks are executed from the repo root.
// --------------------------------------------------------------------------------
const MAP = {
  dev: { dir: "dev", base: "dev" },
  prod: { dir: "dev", base: "prod" },

  // Run only unit tests (no coverage gate)
  "test:unit": {
    dotnetArgs: ["test", "./api", "--filter", "TestType=Unit", "/p:CollectCoverage=false"]
  },

  // Run only integration tests (no coverage gate)
  "test:integration": {
    dotnetArgs: ["test", "./api", "--filter", "TestType=Integration", "/p:CollectCoverage=false"]
  },

  // Run only SQL Server tests (no coverage gate)
  "test:sqlserver": {
    dotnetArgs: ["test", "./api", "--filter", "TestType=SqlServerContainer", "/p:CollectCoverage=false"]
  },

  // Runs all tests with coverage + threshold
  "test:all": { dotnetArgs: ["test", "./api"] },

  // Generate OpenAPI specification
  "gen:openapi": { dir: "tools", base: "export-openapi" }
};

// --------------------------------------------------------------------------------
// Executes PowerShell or Bash scripts depending on OS.
// --------------------------------------------------------------------------------
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

// --------------------------------------------------------------------------------
// Executes a dotnet command in the repository root (e.g., dotnet test).
// --------------------------------------------------------------------------------
function runDotnet(dotnetArgs, extraArgs) {
  const rootDir = path.join(__dirname, ".."); // ensure execution from repo root
  const res = spawnSync("dotnet", [...dotnetArgs, ...extraArgs], {
    stdio: "inherit",
    cwd: rootDir
  });
  return res.status === 0;
}

// --------------------------------------------------------------------------------
// Main dispatcher: determines which type of task to run and executes it.
// --------------------------------------------------------------------------------
function runTask(task, extraArgs) {
  const entry = MAP[task];
  if (!entry) {
    console.error(`Unknown task: ${task}`);
    process.exit(1);
  }

  // Composite task (array): runs multiple tasks sequentially
  if (Array.isArray(entry)) {
    for (const t of entry) if (!runTask(t, extraArgs)) return false;
    return true;
  }

  // dotnet-based tasks (use dotnet test / build / etc.)
  if (entry.dotnetArgs) {
    return runDotnet(entry.dotnetArgs, extraArgs);
  }

  // Fallback: OS-specific script file
  return runFile(entry.dir, entry.base, extraArgs);
}

// --------------------------------------------------------------------------------
// Entry point: node scripts/run.js <task> [args...]
// --------------------------------------------------------------------------------
const [, , task, ...args] = process.argv;
if (!task) {
  console.error(`Usage: node scripts/run.js <${Object.keys(MAP).join("|")}> [args...]`);
  process.exit(1);
}
process.exit(runTask(task, args) ? 0 : 1);

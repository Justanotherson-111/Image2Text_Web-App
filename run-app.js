#!/usr/bin/env node
const { spawn } = require("child_process");
const os = require("os");
const readline = require("readline");

// ---------- helpers ----------
function ask(question) {
  const rl = readline.createInterface({ input: process.stdin, output: process.stdout });
  return new Promise(resolve => {
    rl.question(question, answer => {
      rl.close();
      resolve(answer.trim());
    });
  });
}

async function confirm(question) {
  while (true) {
    const answer = (await ask(`${question} (y/n): `)).toLowerCase();

    if (answer === "y" || answer === "yes") return true;
    if (answer === "n" || answer === "no") return false;

    console.log("Please enter only 'y', 'yes', 'n', or 'no'.");
  }
}


function runCommand(command, args) {
  console.log(`\nRunning: ${command} ${args.join(" ")}\n`);
  const child = spawn(command, args, { stdio: "inherit" });

  child.on("exit", code => {
    console.log(`\nProcess exited with code ${code}`);
    showMenuAndAsk();
  });
}

// ---------- menu ----------
const options = [
  {
    key: "1",
    desc: "Start ALL services (background)",
    run: () =>
      runCommand("docker", ["compose", "--profile", "core", "--profile", "ocr", "up", "-d"]),
  },
  {
    key: "2",
    desc: "Stop ALL services",
    run: () => runCommand("docker", ["compose", "down"]),
  },
  {
    key: "3",
    desc: "Stop ALL services (-v, remove volumes)",
    run: () => runCommand("docker", ["compose", "down", "-v"]),
  },
  {
    key: "4",
    desc: "Build ALL services",
    run: () =>
      runCommand("docker", ["compose", "--profile", "core", "--profile", "ocr", "build"]),
  },
  {
    key: "5",
    desc: "Build CORE only",
    run: () => runCommand("docker", ["compose", "--profile", "core", "build"]),
  },
  {
    key: "6",
    desc: "Build OCR only",
    run: () => runCommand("docker", ["compose", "--profile", "ocr", "build"]),
  },
  {
    key: "7",
    desc: "FULL RESET (down -v + rebuild + up -d)",
    run: async () => {
      const ok = await confirm("⚠️ This will DELETE volumes and rebuild everything. Continue?");
      if (!ok) return showMenuAndAsk();

      runCommand("docker", [
        "compose",
        "--profile",
        "core",
        "--profile",
        "ocr",
        "down",
        "-v",
      ]);

      runCommand("docker", [
        "compose",
        "--profile",
        "core",
        "--profile",
        "ocr",
        "build",
      ]);

      runCommand("docker", [
        "compose",
        "--profile",
        "core",
        "--profile",
        "ocr",
        "up",
        "-d",
      ]);
    },
  },
  {
    key: "8",
    desc: "Clean build ALL (no cache)",
    run: async () => {
      const ok = await confirm("⚠️ Clean build with --no-cache. This will be slow. Continue?");
      if (!ok) return showMenuAndAsk();

      runCommand("docker", [
        "compose",
        "--profile",
        "core",
        "--profile",
        "ocr",
        "build",
        "--no-cache",
      ]);
    },
  },
  {
    key: "9",
    desc: "Show logs (choose container)",
    run: async () => {
      const name = await ask("Enter container name (e.g. project-backend): ");
      if (!name) {
        console.log("Container name required.");
        return showMenuAndAsk();
      }
      runCommand("docker", ["logs", "-f", name]);
    },
  },
  {
    key: "0",
    desc: "Exit",
    run: () => {
      console.log("Exiting...");
      process.exit(0);
    },
  },
];

// ---------- UI ----------
function showMenu() {
  console.log("\nSelect an option:");
  options.forEach(opt => console.log(`${opt.key}) ${opt.desc}`));
}

async function showMenuAndAsk() {
  showMenu();
  const choice = await ask("Enter your choice [0-9]: ");
  const selected = options.find(opt => opt.key === choice);

  if (!selected) {
    console.log("Invalid choice. Try again.");
    return showMenuAndAsk();
  }

  await selected.run();
}

// ---------- startup ----------
console.log(`Detected OS: ${os.platform()}`);
spawn("docker", ["--version"]).on("error", () => {
  console.error("Docker is not installed or not in PATH.");
  process.exit(1);
});

showMenuAndAsk();

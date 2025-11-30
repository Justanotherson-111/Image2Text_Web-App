#!/usr/bin/env node
const { spawn } = require("child_process");
const os = require("os");
const readline = require("readline");

// List of options
const options = [
  { key: "1", desc: "Start project (docker compose up)", cmd: ["docker", ["compose", "up"]] },
  { key: "2", desc: "Stop project (docker compose down)", cmd: ["docker", ["compose", "down"]] },
  { key: "3", desc: "Stop and remove volumes (docker compose down -v)", cmd: ["docker", ["compose", "down", "-v"]] },
  { key: "4", desc: "Build project (docker compose build)", cmd: ["docker", ["compose", "build"]] },
  { key: "5", desc: "Clean build (docker compose build --no-cache)", cmd: ["docker", ["compose", "build", "--no-cache"]] },
  { key: "6", desc: "Exit", cmd: null }
];

// Check if Docker is installed
function checkDocker() {
  const child = spawn("docker", ["--version"]);
  child.on("error", () => {
    console.error("Docker is not installed or not in PATH. Exiting...");
    process.exit(1);
  });
}

// Show menu
function showMenu() {
  console.log("\nSelect an option:");
  options.forEach(opt => console.log(`${opt.key}) ${opt.desc}`));
}

// Ask user for choice
function askChoice() {
  const rl = readline.createInterface({ input: process.stdin, output: process.stdout });
  rl.question("Enter your choice [1-6]: ", (answer) => {
    rl.close();
    const selected = options.find(opt => opt.key === answer);
    if (!selected) {
      console.log("Invalid choice. Try again.");
      return askChoice();
    }
    if (!selected.cmd) {
      console.log("Exiting...");
      process.exit(0);
    }
    runCommand(selected.cmd[0], selected.cmd[1]);
  });
}

// Run the command using spawn
function runCommand(command, args) {
  console.log(`\nRunning: ${command} ${args.join(" ")}\n`);
  const child = spawn(command, args, { stdio: "inherit" });

  // Handle Ctrl+C
  process.on("SIGINT", () => {
    console.log("\nInterrupted. Exiting...");
    child.kill("SIGINT");
    process.exit(0);
  });

  child.on("exit", code => {
    console.log(`\nProcess exited with code ${code}`);
    askChoice(); // Show menu again
  });
}

// Main
console.log(`Detected OS: ${os.platform()}`);
checkDocker();
showMenu();
askChoice();

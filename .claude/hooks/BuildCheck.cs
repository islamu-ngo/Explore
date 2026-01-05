using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

// C# Script to verify compilation for ISLAMU Event (.NET 10)
// NON-BLOCKING MODE: Reports errors without stopping Claude.

Console.WriteLine("🏗️  Checking compilation...");

// 1. Find the solution (Explore.sln)
string workingDir = Directory.GetCurrentDirectory();
string solutionPath = Path.Combine(workingDir, "Explore.sln");

if (!File.Exists(solutionPath))
{
    var found = Directory.GetFiles(workingDir, "*.sln", SearchOption.AllDirectories)
                         .Where(x => !x.Contains(".claude"))
                         .FirstOrDefault();
    if (found != null) solutionPath = found;
}

// 2. Build the command
string buildArgs = "build";
if (File.Exists(solutionPath))
{
    buildArgs += $" \"{solutionPath}\"";
    buildArgs += " --nologo --verbosity quiet";
}
else
{
    Console.WriteLine($"⚠️  Solution not found. Trying generic build...");
    buildArgs += " --nologo --verbosity quiet";
}

try
{
    var processInfo = new ProcessStartInfo("dotnet", buildArgs)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        WorkingDirectory = workingDir
    };

    var process = Process.Start(processInfo);
    if (process == null)
    {
        Console.WriteLine("⚠️  Unable to launch dotnet.");
        Environment.Exit(0);
    }

    string output = process.StandardOutput.ReadToEnd();
    string error = process.StandardError.ReadToEnd();

    process.WaitForExit();

    if (process.ExitCode == 0)
    {
        Console.WriteLine("✅  Compilation successful.");
        // Optional: Clean old logs on success
    }
    else
    {
        Console.WriteLine("⚠️  Compilation error detected (non-blocking).");

        // --- ADD TIMESTAMP ---
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var logFileName = $"error-{timestamp}.txt";

        var cachePath = Path.Combine(".claude", "build-cache");
        Directory.CreateDirectory(cachePath);

        var fullLog = $"DATE: {DateTime.Now}\nSOLUTION: {solutionPath}\n\nSTDOUT:\n{output}\n\nSTDERR:\n{error}";
        File.WriteAllText(Path.Combine(cachePath, logFileName), fullLog);

        // Update pointer to "last error" for auto-error-resolver agent
        File.WriteAllText(Path.Combine(cachePath, "last-errors.txt"), fullLog);

        // Display partial output
        var lines = fullLog.Split(Environment.NewLine)
                           .Where(l => l.Contains("error CS") || l.Contains(": error"))
                           .Take(5);

        Console.WriteLine($"📄  Log saved: {logFileName}");
        Console.WriteLine("--- Preview ---");
        foreach (var line in lines) Console.WriteLine(line);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️  Hook error: {ex.Message}");
}

// Always exit with 0 to avoid blocking the workflow
Environment.Exit(0);

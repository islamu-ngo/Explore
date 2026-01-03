using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

// Script C# pour vérifier la compilation ISLAMU Event (.NET 10)
// MODE NON-BLOQUANT : Signale les erreurs sans arrêter Claude.

Console.WriteLine("🏗️  Vérification de la compilation...");

// 1. Recherche de la solution (Explore.sln)
string workingDir = Directory.GetCurrentDirectory();
string solutionPath = Path.Combine(workingDir, "Explore.sln");

if (!File.Exists(solutionPath))
{
    var found = Directory.GetFiles(workingDir, "*.sln", SearchOption.AllDirectories)
                         .Where(x => !x.Contains(".claude"))
                         .FirstOrDefault();
    if (found != null) solutionPath = found;
}

// 2. Construction de la commande
string buildArgs = "build";
if (File.Exists(solutionPath))
{
    buildArgs += $" \"{solutionPath}\"";
    buildArgs += " --nologo --verbosity quiet";
}
else
{
    Console.WriteLine($"⚠️  Solution introuvable. Tentative générique...");
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
        Console.WriteLine("⚠️  Impossible de lancer dotnet.");
        Environment.Exit(0);
    }

    string output = process.StandardOutput.ReadToEnd();
    string error = process.StandardError.ReadToEnd();

    process.WaitForExit();

    if (process.ExitCode == 0)
    {
        Console.WriteLine("✅  Compilation réussie.");
        // Optionnel : Nettoyer les vieux logs si succès
    }
    else
    {
        Console.WriteLine("⚠️  Erreur de compilation détectée (non-bloquant).");

        // --- MODIFICATION DEMANDÉE : HORODATAGE ---
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var logFileName = $"error-{timestamp}.txt";

        var cachePath = Path.Combine(".claude", "build-cache");
        Directory.CreateDirectory(cachePath);

        var fullLog = $"DATE: {DateTime.Now}\nSOLUTION: {solutionPath}\n\nSTDOUT:\n{output}\n\nSTDERR:\n{error}";
        File.WriteAllText(Path.Combine(cachePath, logFileName), fullLog);

        // Mise à jour du pointeur "dernier erreur" pour l'agent auto-error-resolver
        File.WriteAllText(Path.Combine(cachePath, "last-errors.txt"), fullLog);

        // Affichage partiel
        var lines = fullLog.Split(Environment.NewLine)
                           .Where(l => l.Contains("error CS") || l.Contains(": error"))
                           .Take(5);

        Console.WriteLine($"📄  Log sauvegardé : {logFileName}");
        Console.WriteLine("--- Aperçu ---");
        foreach (var line in lines) Console.WriteLine(line);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️  Hook error: {ex.Message}");
}

// Toujours sortir avec 0 pour ne pas bloquer le workflow
Environment.Exit(0);

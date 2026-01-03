using System;
using System.Diagnostics;

// Script C# pour formater le code selon les standards GOVERNANCE.md
// Utilise: dotnet format

Console.WriteLine("🎨  Standardisation du code (dotnet format)...");

try
{
    // --include-generated permet de traiter certains fichiers Blazor si besoin,
    // mais généralement on l'évite. On reste simple.
    var processInfo = new ProcessStartInfo("dotnet", "format --verbosity quiet")
    {
        UseShellExecute = false,
        RedirectStandardError = true,
        RedirectStandardOutput = true,
        CreateNoWindow = true
    };

    var process = Process.Start(processInfo);

    if (process != null)
    {
        process.WaitForExit();
        if (process.ExitCode == 0)
        {
            Console.WriteLine("✨  Code formaté.");
        }
        else
        {
            // On ne bloque pas pour du formatage, mais on prévient
            Console.WriteLine("⚠️  Le formatage automatique a rencontré des warnings.");
        }
    }
}
catch (Exception)
{
    // Ignorer silencieusement si dotnet format n'est pas dispo ou plante
    Console.WriteLine("⚠️  Impossible de lancer le formatage.");
}

// Toujours succès pour ne pas bloquer Claude
Environment.Exit(0);

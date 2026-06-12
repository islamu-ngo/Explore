using System;
using System.Diagnostics;

// C# Script to format code according to .editorconfig standards
// Uses: dotnet format

Console.WriteLine("🎨  Formatting code (dotnet format)...");

try
{
    // --include-generated allows processing some Blazor files if needed,
    // but generally we avoid it. Keeping it simple.
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
            Console.WriteLine("✨  Code formatted.");
        }
        else
        {
            // Don't block for formatting, but warn
            Console.WriteLine("⚠️  Auto-formatting encountered warnings.");
        }
    }
}
catch (Exception)
{
    // Silently ignore if dotnet format is not available or crashes
    Console.WriteLine("⚠️  Unable to run formatter.");
}

// Always exit successfully to avoid blocking Claude
Environment.Exit(0);

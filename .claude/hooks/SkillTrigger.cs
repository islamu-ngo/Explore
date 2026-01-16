using System;
using System.IO;
using System.Collections.Generic;

// Skill/Agent trigger script
// ROBUST VERSION: Won't block if no input is provided.

// 1. ANTI-BLOCKING SAFETY
// If the script is run manually without a pipe (e.g., "dotnet SkillTrigger.cs"), it exits here.
if (!Console.IsInputRedirected)
{
    Environment.Exit(0);
}

try
{
    // 2. Read prompt (with implicit timeout safety via Reader)
    string prompt = "";
    using (var reader = new StreamReader(Console.OpenStandardInput()))
    {
        // Don't read everything at once to avoid blocking on very large streams
        // Peek checks if data is available
        if (reader.Peek() == -1) Environment.Exit(0);
        prompt = reader.ReadToEnd()?.ToLower() ?? "";
    }

    if (string.IsNullOrWhiteSpace(prompt)) Environment.Exit(0);

    // 3. Trigger rules
    var suggestions = new List<string>();

    // Authentication/Authorization
    if (prompt.Contains("401") || prompt.Contains("403") || prompt.Contains("keycloak") || prompt.Contains("token") || prompt.Contains("cerbos"))
        suggestions.Add("🔒 SUGGESTION: Use 'auth-route-debugger' agent for security issues.");

    // Frontend/UI
    if (prompt.Contains("blazor") || prompt.Contains("mudblazor") || prompt.Contains("css") || prompt.Contains("razor") || prompt.Contains("component"))
        suggestions.Add("🎨 SUGGESTION: Use 'frontend-error-fixer' agent for UI issues.");

    // Architecture/Refactoring
    if (prompt.Contains("refactor") || prompt.Contains("clean arch") || prompt.Contains("mediatr") || prompt.Contains("cqrs"))
        suggestions.Add("🏗️ SUGGESTION: Use 'code-refactor-master' agent or consult Clean Architecture skills.");

    // Build Errors
    if (prompt.Contains("error cs") || prompt.Contains("build fail") || prompt.Contains("compilation"))
        suggestions.Add("🛠️ SUGGESTION: Use 'auto-error-resolver' agent to fix compilation errors.");

    // Database/EF Core
    if (prompt.Contains("database") || prompt.Contains("ef core") || prompt.Contains("migration") || prompt.Contains("postgres"))
        suggestions.Add("💾 SUGGESTION: Consult 'dotnet-efcore-guidelines' skill for database patterns.");

    // Testing
    if (prompt.Contains("test") || prompt.Contains("xunit") || prompt.Contains("mock"))
        suggestions.Add("🧪 SUGGESTION: Use 'auth-route-tester' agent for API security testing.");

    // 4. Output
    if (suggestions.Count > 0)
    {
        Console.WriteLine("\n🎯  Suggested Skills/Agents:");
        foreach (var s in suggestions) Console.WriteLine(s);
        Console.WriteLine("");
    }
}
catch
{
    // Never fail the user prompt because of a hook
}

Environment.Exit(0);

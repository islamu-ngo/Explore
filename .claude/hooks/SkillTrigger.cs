using System;
using System.IO;
using System.Collections.Generic;

// Script déclencheur de Skills/Agents
// VERSION ROBUSTE : Ne bloque pas si aucune entrée n'est fournie.

// 1. SÉCURITÉ ANTI-BLOCAGE
// Si le script est lancé manuellement sans pipe (ex: "dotnet SkillTrigger.cs"), il s'arrête ici.
if (!Console.IsInputRedirected)
{
    Environment.Exit(0);
}

try
{
    // 2. Lecture du prompt (avec timeout de sécurité implicite via le Reader)
    string prompt = "";
    using (var reader = new StreamReader(Console.OpenStandardInput()))
    {
        // On ne lit pas tout d'un coup pour éviter les blocages sur de très gros flux
        // Peek vérifie s'il y a des données
        if (reader.Peek() == -1) Environment.Exit(0);
        prompt = reader.ReadToEnd()?.ToLower() ?? "";
    }

    if (string.IsNullOrWhiteSpace(prompt)) Environment.Exit(0);

    // 3. Règles de déclenchement
    var suggestions = new List<string>();

    // Auth
    if (prompt.Contains("401") || prompt.Contains("403") || prompt.Contains("keycloak") || prompt.Contains("token"))
        suggestions.Add("🔒 SUGGESTION: Agent 'auth-route-debugger' pour les problèmes de sécurité.");

    // Frontend
    if (prompt.Contains("blazor") || prompt.Contains("mudblazor") || prompt.Contains("css") || prompt.Contains("razor"))
        suggestions.Add("🎨 SUGGESTION: Agent 'frontend-error-fixer' pour l'UI.");

    // Architecture
    if (prompt.Contains("refactor") || prompt.Contains("clean arch") || prompt.Contains("mediatr"))
        suggestions.Add("🏗️ SUGGESTION: Agent 'code-refactor-master' ou 'backend-dev-guidelines'.");

    // Build Errors
    if (prompt.Contains("error cs") || prompt.Contains("build fail"))
        suggestions.Add("🛠️ SUGGESTION: Agent 'auto-error-resolver' pour fixer la compilation.");

    // 4. Sortie
    if (suggestions.Count > 0)
    {
        Console.WriteLine("\n🎯  Skills Suggérés :");
        foreach (var s in suggestions) Console.WriteLine(s);
        Console.WriteLine("");
    }
}
catch
{
    // Ne jamais faire échouer le prompt utilisateur à cause d'un hook
}

Environment.Exit(0);

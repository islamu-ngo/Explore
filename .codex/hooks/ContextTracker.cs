using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;

// ---------------------------------------------------------
// UNIFIED CONTEXT TRACKER (Build + Global State)
// GUARANTEE: Never blocks indefinitely. Never returns error code.
// ---------------------------------------------------------

try
{
    // --- PART 1: READ INPUT (Non-Blocking) ---
    string? inputJson = null;

    // Only read if input is actually being piped in (prevents hanging when running manually)
    if (Console.IsInputRedirected)
    {
        using (var reader = new StreamReader(Console.OpenStandardInput()))
        {
            // Add a timeout task just in case
            var readTask = reader.ReadToEndAsync();
            var timeoutTask = Task.Delay(1000); // 1 second max wait

            if (await Task.WhenAny(readTask, timeoutTask) == readTask)
            {
                inputJson = await readTask;
            }
        }
    }

    // Default values
    string currentDir = Directory.GetCurrentDirectory();
    string rootDir = currentDir;
    string sessionId = "default";
    string toolName = "";
    string? filePath = null;

    // Parse JSON if available
    if (!string.IsNullOrWhiteSpace(inputJson))
    {
        try
        {
            using var doc = JsonDocument.Parse(inputJson);
            var root = doc.RootElement;

            toolName = GetProperty(root, "tool_name") ?? "";
            rootDir = GetProperty(root, "project_dir") ?? currentDir;
            sessionId = GetProperty(root, "session_id") ?? "default";

            if (root.TryGetProperty("tool_input", out var inputElem) &&
                inputElem.TryGetProperty("file_path", out var fileElem))
            {
                filePath = fileElem.GetString();
            }
        }
        catch { /* Ignore JSON parse errors */ }
    }

    // Determine absolute paths
    rootDir = FindSolutionRoot(rootDir);

    // --- PART 2: INCREMENTAL BUILD TRACKING ---
    string layer = "Unknown";
    bool isEdit = IsEditTool(toolName) && !string.IsNullOrEmpty(filePath);

    if (isEdit && filePath != null && !IsIgnoredFile(filePath))
    {
        layer = DetectCleanArchLayer(filePath);

        var cacheDir = Path.Combine(rootDir, ".claude", "build-cache", sessionId);
        if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

        // 1. Log Edit
        string logEntry = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:{filePath}:{layer}{Environment.NewLine}";
        await File.AppendAllTextAsync(Path.Combine(cacheDir, "edited-files.log"), logEntry);

        // 2. Update Affected Layers
        var affectedPath = Path.Combine(cacheDir, "affected-layers.txt");
        var currentLayers = File.Exists(affectedPath)
            ? (await File.ReadAllLinesAsync(affectedPath)).ToHashSet()
            : new HashSet<string>();

        if (currentLayers.Add(layer))
        {
            await File.WriteAllLinesAsync(affectedPath, currentLayers);
        }

        // 3. Queue Build Command
        string buildCmd = GetBuildCommand(layer);
        if (!string.IsNullOrEmpty(buildCmd))
        {
            await File.AppendAllTextAsync(Path.Combine(cacheDir, "commands.txt"), $"{layer}:build:{buildCmd}{Environment.NewLine}");
        }
    }

    // --- PART 3: GLOBAL CONTEXT STATE ---
    var (frontendStatus, backendStatus, domainStatus, infraStatus) = ScanProjectCapabilities(rootDir);

    string recentFocus = isEdit && filePath != null ? $"{layer} ({Path.GetFileName(filePath)})" : "General";
    string safeRootDir = rootDir.Replace("\\", "\\\\");

    // Build Context JSON
    string jsonContent = $@"{{
  ""Project"": ""ISLAMU Event"",
  ""Stack"": "".NET 10 + Blazor + Aspire"",
  ""RootPath"": ""{safeRootDir}"",
  ""ActiveLayers"": {{
    ""Frontend"": ""{frontendStatus}"",
    ""Backend"": ""{backendStatus}"",
    ""Domain"": ""{domainStatus}"",
    ""Infra"": ""{infraStatus}""
  }},
  ""RecentFocus"": ""{recentFocus}"",
  ""LastUpdate"": ""{DateTime.Now:yyyy-MM-dd HH:mm:ss}""
}}";

    var contextFile = Path.Combine(rootDir, ".claude", "context-state.json");
    // Ensure .claude dir exists
    if (!Directory.Exists(Path.GetDirectoryName(contextFile))) Directory.CreateDirectory(Path.GetDirectoryName(contextFile)!);

    await File.WriteAllTextAsync(contextFile, jsonContent);

    // If running manually, print confirmation
    if (!Console.IsInputRedirected)
    {
        Console.WriteLine($"✅ Context updated. Focus: {recentFocus}");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ContextTracker] Handled error: {ex.Message}");
}

// ALWAYS EXIT 0
Environment.Exit(0);


// ---------------------------------------------------------
// HELPER FUNCTIONS
// ---------------------------------------------------------

static string? GetProperty(JsonElement element, string name)
{
    return element.TryGetProperty(name, out var prop) ? prop.GetString() : null;
}

static bool IsEditTool(string name)
{
    return new[] { "Edit", "MultiEdit", "Write" }.Contains(name, StringComparer.OrdinalIgnoreCase);
}

static bool IsIgnoredFile(string path)
{
    return Regex.IsMatch(path, @"\.(md|txt|json|yml|xml|editorconfig|gitkeep)$", RegexOptions.IgnoreCase);
}

static string FindSolutionRoot(string startPath)
{
    try
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Any()) return dir.FullName;
            dir = dir.Parent;
        }
    }
    catch { }
    return startPath;
}

static string DetectCleanArchLayer(string path)
{
    var p = path.Replace("\\", "/");
    if (p.Contains("/Domain/") || p.Contains(".Domain")) return "Domain";
    if (p.Contains("/Application/") || p.Contains(".Application")) return "Application";
    if (p.Contains("/Infrastructure/") || p.Contains(".Infrastructure")) return "Infrastructure";
    if (p.Contains("/Api/") || p.Contains(".Api") || p.Contains("Controllers")) return "API";
    if (p.Contains("/Blazor/") || p.Contains(".Client") || p.Contains(".Components")) return "Frontend";
    return "Shared";
}

static string GetBuildCommand(string layer)
{
    return layer switch
    {
        "Domain" => "dotnet build Explore.Domain --nologo --no-restore",
        "Application" => "dotnet build Explore.Application --nologo --no-restore",
        "Frontend" => "dotnet build Explore.Blazor --nologo --no-restore",
        "API" => "dotnet build Explore.Api --nologo --no-restore",
        _ => "dotnet build --nologo"
    };
}

static (string fe, string be, string dom, string inf) ScanProjectCapabilities(string rootDir)
{
    try
    {
        var projectFiles = Directory.GetFiles(rootDir, "*.csproj", SearchOption.AllDirectories)
             .Where(p => !p.Contains("obj") && !p.Contains("bin") && !p.Contains(".claude"))
             .Select(p => Path.GetFileNameWithoutExtension(p) ?? "")
             .ToList();

        bool hasFe = projectFiles.Any(p => p.Contains("Blazor") || p.Contains("Client"));
        bool hasApi = projectFiles.Any(p => p.Contains("Api") || p.Contains("API"));
        bool hasDom = projectFiles.Any(p => p.Contains("Domain"));
        bool hasInf = projectFiles.Any(p => p.Contains("Infrastructure"));

        return (
            hasFe ? "Active (MudBlazor)" : "Inactive",
            hasApi ? "Active (ASP.NET)" : "Inactive",
            hasDom ? "Active (DDD)" : "Inactive",
            hasInf ? "Active (EF Core)" : "Inactive"
        );
    }
    catch
    {
        return ("Unknown", "Unknown", "Unknown", "Unknown");
    }
}

// ABOUTME: PreToolUse hook that validates Bash commands for dangerous patterns.
// ABOUTME: Blocks commands containing "rm -rf", ".env" access, or other risky operations.

using System;
using System.IO;
using System.Text.Json;

// Exit silently if no input is piped (manual execution)
if (!Console.IsInputRedirected)
{
    Environment.Exit(0);
}

try
{
    string input;
    using (var reader = new StreamReader(Console.OpenStandardInput()))
    {
        if (reader.Peek() == -1)
        {
            Environment.Exit(0);
        }
        input = reader.ReadToEnd() ?? "";
    }

    if (string.IsNullOrWhiteSpace(input))
    {
        Environment.Exit(0);
    }

    // Parse the JSON input
    using var doc = JsonDocument.Parse(input);
    var root = doc.RootElement;

    // Check if this is a Bash tool call
    if (!root.TryGetProperty("tool_name", out var toolNameElement) ||
        toolNameElement.GetString() != "Bash")
    {
        // Not a Bash command, allow it
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            hookSpecificOutput = new { permissionDecision = "allow" }
        }));
        Environment.Exit(0);
    }

    // Get the command from tool_input
    string command = "";
    if (root.TryGetProperty("tool_input", out var toolInput))
    {
        if (toolInput.TryGetProperty("command", out var commandElement))
        {
            command = commandElement.GetString()?.ToLowerInvariant() ?? "";
        }
    }

    // Define dangerous patterns to block
    string[] dangerousPatterns =
    [
        "rm -rf /",           // Delete entire filesystem
        "rm -rf ~",           // Delete home directory
        "rm -rf",             // files that should be deleted should be reported and manually reviewed and deleted by amdmin
        "rm -rf .",           // Delete current directory recursively (dangerous)
        "> /dev/sda",         // Write to disk device
        "mkfs.",              // Format filesystem
        "dd if=/dev/zero",    // Overwrite with zeros
        ":(){:|:&};:",        // Fork bomb
        "chmod -r 777 /",     // Dangerous permissions on root
        ".env",               // Block .env file access (security rule)
        ".mcp.json"           // Block .mcp.json file access (security rule)
    ];

    // Check for dangerous patterns
    foreach (var pattern in dangerousPatterns)
    {
        if (command.Contains(pattern, StringComparison.OrdinalIgnoreCase))
        {
            // Block the command
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                hookSpecificOutput = new { permissionDecision = "deny" },
                systemMessage = $"Security violation: Command blocked because it contains '{pattern}'. This pattern is potentially dangerous."
            }));
            Environment.Exit(2);
        }
    }

    // Allow the command
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        hookSpecificOutput = new { permissionDecision = "allow" }
    }));
    Environment.Exit(0);
}
catch (Exception ex)
{
    // Log error but don't block the user - fail open with warning
    Console.Error.WriteLine($"SecurityCheck hook error: {ex.Message}");
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        hookSpecificOutput = new { permissionDecision = "allow" },
        systemMessage = "SecurityCheck hook encountered an error but allowed the command to proceed."
    }));
    Environment.Exit(0);
}

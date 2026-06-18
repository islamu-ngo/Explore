// ABOUTME: Writes SHA-256 checksum manifests for retained CI/CD evidence artifacts.
// ABOUTME: Keeps release evidence integrity checks in repository-owned C# instead of workflow shell logic.
#:property RestorePackagesWithLockFile=false

using System.Security.Cryptography;

var rootDirectory = args.Length > 0 ? args[0] : "artifacts/container";
var outputPath = args.Length > 1 ? args[1] : Path.Combine(rootDirectory, "checksums.sha256");

if (!Directory.Exists(rootDirectory))
{
    Console.Error.WriteLine($"Artifact directory does not exist: {rootDirectory}");
    return 1;
}

var rootFullPath = Path.GetFullPath(rootDirectory);
var outputFullPath = Path.GetFullPath(outputPath);

var files = Directory
    .EnumerateFiles(rootFullPath, "*", SearchOption.AllDirectories)
    .Select(Path.GetFullPath)
    .Where(path => !StringComparer.Ordinal.Equals(path, outputFullPath))
    .OrderBy(path => ToManifestPath(rootFullPath, path), StringComparer.Ordinal)
    .ToList();

if (files.Count == 0)
{
    Console.Error.WriteLine($"No artifact files found under: {rootDirectory}");
    return 1;
}

Directory.CreateDirectory(Path.GetDirectoryName(outputFullPath) ?? rootFullPath);

var lines = new List<string>(files.Count);
foreach (var file in files)
{
    using var stream = File.OpenRead(file);
    var hash = SHA256.HashData(stream);
    var relativePath = ToManifestPath(rootFullPath, file);
    lines.Add($"{Convert.ToHexString(hash).ToLowerInvariant()}  {relativePath}");
}

File.WriteAllLines(outputFullPath, lines);
Console.WriteLine($"Wrote {lines.Count} checksum entries to {outputPath}.");

return 0;

static string ToManifestPath(string rootFullPath, string fileFullPath)
{
    return Path.GetRelativePath(rootFullPath, fileFullPath)
        .Replace(Path.DirectorySeparatorChar, '/')
        .Replace(Path.AltDirectorySeparatorChar, '/');
}

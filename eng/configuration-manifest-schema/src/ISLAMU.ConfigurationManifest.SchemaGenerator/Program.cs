// ABOUTME: Provides write and drift-check commands for the governed ConfigurationManifest schema.
// ABOUTME: Returns nonzero without mutating files when checked bytes differ from deterministic output.

namespace ISLAMU.ConfigurationManifest.SchemaGenerator;

internal static class Program
{
    private const int UsageError = 64;

    public static int Main(string[] args)
    {
        if (args is ["--help"])
        {
            Console.WriteLine(
                "Usage: configuration-manifest-schema (--write|--check) <schema-path>");
            return 0;
        }

        if (args.Length != 2 || args[0] is not ("--write" or "--check"))
        {
            Console.Error.WriteLine(
                "Usage: configuration-manifest-schema (--write|--check) <schema-path>");
            return UsageError;
        }

        string artifactPath = Path.GetFullPath(args[1]);
        byte[] generated;
        try
        {
            generated = ConfigurationManifestJsonSchemaGenerator.Generate();
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }

        if (args[0] == "--check")
        {
            if (!File.Exists(artifactPath))
            {
                Console.Error.WriteLine("The configuration-manifest schema artifact is missing.");
                return 1;
            }

            byte[] existing = File.ReadAllBytes(artifactPath);
            if (!existing.AsSpan().SequenceEqual(generated))
            {
                Console.Error.WriteLine("The configuration-manifest schema artifact is stale.");
                return 1;
            }

            Console.WriteLine("Configuration-manifest schema artifact is current.");
            return 0;
        }

        string? directory = Path.GetDirectoryName(artifactPath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            Console.Error.WriteLine("The schema output directory does not exist.");
            return 1;
        }

        File.WriteAllBytes(artifactPath, generated);
        Console.WriteLine("Configuration-manifest schema artifact generated.");
        return 0;
    }
}

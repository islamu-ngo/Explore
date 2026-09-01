// ABOUTME: Exposes the standalone workstream validation command and closed JSON contract.
// ABOUTME: Parses explicit CLI arguments and maps typed domain outcomes to stable exit codes.

using System.Text.Json;
using ISLAMU.AgentWorkflow.Application;
using ISLAMU.AgentWorkflow.Domain;
using ISLAMU.AgentWorkflow.Infrastructure;

namespace ISLAMU.AgentWorkflow;

public static class Program
{
    private const int Success = 0;
    private const int ValidationFailure = 2;
    private const int UsageFailure = 64;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static int Main(string[] args)
    {
        if (args.Length == 0 || args is ["--help"] or ["-h"])
        {
            WriteHelp(Console.Out);
            return Success;
        }

        if (!TryParse(args, out ValidateArguments? arguments))
        {
            WriteHelp(Console.Error);
            return UsageFailure;
        }

        var command = new ValidateWorkstreamCommand(new YamlWorkstreamStore(), new GitHeadReader());
        WorkstreamResult result = command.Execute(arguments!.Manifest, arguments.Schema, arguments.Repository);
        if (result.Error is not null)
        {
            WriteJson(new FailureOutput("workstream-validation.v1", false, ToCode(result.Error.Code)));
            return ValidationFailure;
        }

        ValidatedWorkstream valid = result.Value!;
        WriteJson(new SuccessOutput(
            "workstream-validation.v1",
            true,
            "workstream_valid",
            valid.WorkstreamId,
            valid.PhaseId,
            valid.CurrentState,
            valid.NextTransition,
            new PacketOutput(valid.Paths, valid.VerificationCommands)));
        return Success;
    }

    private static bool TryParse(string[] args, out ValidateArguments? result)
    {
        result = null;
        if (args.Length != 9 || !string.Equals(args[0], "validate-workstream", StringComparison.Ordinal))
        {
            return false;
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int index = 1; index < args.Length; index += 2)
        {
            if (!values.TryAdd(args[index], args[index + 1]))
            {
                return false;
            }
        }

        if (values.Count != 4 || !values.TryGetValue("--manifest", out string? manifest) ||
            !values.TryGetValue("--schema", out string? schema) ||
            !values.TryGetValue("--repository", out string? repository) ||
            !values.TryGetValue("--output", out string? output) ||
            !string.Equals(output, "json", StringComparison.Ordinal) ||
            values.Keys.Any(key => key is not ("--manifest" or "--schema" or "--repository" or "--output")))
        {
            return false;
        }

        result = new ValidateArguments(manifest, schema, repository);
        return true;
    }

    private static string ToCode(WorkstreamErrorCode code) => code switch
    {
        WorkstreamErrorCode.StaleArtifactDigest => "stale_artifact_digest",
        WorkstreamErrorCode.CommitAuthorityRequired => "commit_authority_required",
        WorkstreamErrorCode.IllegalTransition => "illegal_transition",
        WorkstreamErrorCode.PhasePacketIncomplete => "phase_packet_incomplete",
        WorkstreamErrorCode.UnknownField => "unknown_field",
        WorkstreamErrorCode.UnsafePath => "unsafe_path",
        WorkstreamErrorCode.ExpectedHeadMismatch => "expected_head_mismatch",
        _ => throw new ArgumentOutOfRangeException(nameof(code)),
    };

    private static void WriteJson<T>(T value) => Console.Out.WriteLine(JsonSerializer.Serialize(value, JsonOptions));

    private static void WriteHelp(TextWriter output) => output.WriteLine(
        "usage: islamu-agent-workflow validate-workstream --manifest <path> --schema <path> --repository <path> --output json");

    private sealed record ValidateArguments(string Manifest, string Schema, string Repository);
    private sealed record PacketOutput(IReadOnlyList<string> Paths, IReadOnlyList<string> VerificationCommands);
    private sealed record SuccessOutput(
        string SchemaVersion,
        bool Ok,
        string Code,
        string WorkstreamId,
        string PhaseId,
        string CurrentState,
        string NextTransition,
        PacketOutput Packet);
    private sealed record FailureOutput(string SchemaVersion, bool Ok, string Code);
}

// ABOUTME: Command-line entry point for read-only platform diagnostics.
// ABOUTME: Runs non-mutating doctor checks and returns a hard-failure exit code only when FAIL checks are present.

using Explore.Diagnostic.Doctor;
using Explore.Diagnostic.Doctor.Checks;
using Explore.Diagnostic.Doctor.Infrastructure;

var options = DoctorCommandOptions.Parse(args);

if (options.ShowHelp)
{
    DoctorConsoleReporter.WriteHelp(Console.Out);
    return DoctorExitCodes.Success;
}

var repositoryRoot = options.RepositoryRoot ?? DoctorRepositoryLocator.LocateRepositoryRoot(Directory.GetCurrentDirectory());
var fileSystem = new PhysicalDoctorFileSystem();
var processRunner = new DefaultDoctorProcessRunner();

var checks = new IDoctorCheck[]
{
    new GlobalJsonSdkDoctorCheck(fileSystem, processRunner, repositoryRoot),
    new DockerDoctorCheck(processRunner),
    new AspireDoctorCheck(processRunner),
    new DockerComposeTopologyDoctorCheck(fileSystem, repositoryRoot),
    new BootstrapConfigurationDoctorCheck(fileSystem, repositoryRoot),
    new OperationsDocumentationDoctorCheck(fileSystem, repositoryRoot),
};

var runner = new DoctorRunner(checks);
var report = await runner.RunAsync(options.Timeout, CancellationToken.None);

DoctorConsoleReporter.WriteReport(Console.Out, report);

return DoctorExitCodes.FromReport(report);

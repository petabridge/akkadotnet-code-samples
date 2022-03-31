using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.DocFX.DocFXTasks;
using System.Text.Json;
using System.IO;
using static Nuke.Common.ChangeLog.ChangelogTasks;
using Nuke.Common.ChangeLog;
using Nuke.Common.Tools.DocFX;
using Nuke.Common.Tools.Docker;
using static Nuke.Common.Tools.SignClient.SignClientTasks;
using Nuke.Common.Tools.SignClient;
using static Nuke.Common.Tools.Git.GitTasks;
using Octokit;
using Nuke.Common.Utilities;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Install);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = Configuration.Release;

    [GitRepository] readonly GitRepository GitRepository;

     //usage:
    //.\build.cmd createnuget --NugetPrerelease {suffix}
    [Parameter] string NugetPrerelease;

    [Parameter][Secret] string GitHubToken;
    [Parameter][Secret] string NugetKey;

    // Directories
    AbsolutePath ToolsDir => RootDirectory / "tools";
    AbsolutePath Output => RootDirectory / "bin";
    AbsolutePath OutputNuget => Output / "nuget";
    AbsolutePath OutputTests => RootDirectory / "TestResults";
    AbsolutePath OutputPerfTests => RootDirectory / "PerfResults";
    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath DocSiteDirectory => RootDirectory / "docs" / "_site";
    public string ChangelogFile => RootDirectory / "RELEASE_NOTES.md";
    public AbsolutePath DocFxDir => RootDirectory / "docs";
    public AbsolutePath DocFxDirJson => DocFxDir / "docfx.json";

    readonly Solution[] Solutions = RootDirectory.GlobFiles("*.sln").Select(sln => ProjectModelTasks.ParseSolution(sln)).ToArray();

    static readonly JsonElement? _githubContext = string.IsNullOrWhiteSpace(EnvironmentInfo.GetVariable<string>("GITHUB_CONTEXT")) ?
        null
        : JsonSerializer.Deserialize<JsonElement>(EnvironmentInfo.GetVariable<string>("GITHUB_CONTEXT"));

    //let hasTeamCity = (not (buildNumber = "0")) // check if we have the TeamCity environment variable for build # set
    static readonly int BuildNumber = _githubContext.HasValue ? int.Parse(_githubContext.Value.GetProperty("run_number").GetString()) : 0;

    static readonly string PreReleaseVersionSuffix = "beta" + (BuildNumber > 0 ? BuildNumber : DateTime.UtcNow.Ticks.ToString());
    public ChangeLog Changelog => ReadChangelog(ChangelogFile);

    public ReleaseNotes ReleaseNotes => Changelog.ReleaseNotes.OrderByDescending(s => s.Version).FirstOrDefault() ?? throw new ArgumentException("Bad Changelog File. Version Should Exist");

    private string VersionFromReleaseNotes => ReleaseNotes.Version.IsPrerelease ? ReleaseNotes.Version.OriginalVersion : "";
    private string VersionSuffix => NugetPrerelease == "dev" ? PreReleaseVersionSuffix : NugetPrerelease == "" ? VersionFromReleaseNotes : NugetPrerelease;
    public string ReleaseVersion => ReleaseNotes.Version?.ToString() ?? throw new ArgumentException("Bad Changelog File. Define at least one version");
    GitHubClient GitHubClient;
    Target Clean => _ => _
        .Description("Cleans all the output directories")
        .Before(AssemblyInfo)
        .Executes(() =>
        {
            RootDirectory
            .GlobDirectories("src/**/bin", "src/**/obj", Output, OutputTests, OutputPerfTests, OutputNuget, DocSiteDirectory)
            .ForEach(DeleteDirectory);
            EnsureCleanDirectory(Output);
        });

   
    Target RunTests => _ => _
        .Description("Runs all the unit tests")
        .DependsOn(Compile)
        .Executes(() =>
        {
            foreach(var sln in Solutions)
            {
                var projects = sln.GetProjects("*.Tests");
                foreach (var project in projects)
                {
                    Information($"Running tests from {project}");
                    foreach (var fw in project.GetTargetFrameworks())
                    {
                        Information($"Running for {project} ({fw}) ...");
                        DotNetTest(c => c
                               .SetProjectFile(project)
                               .SetConfiguration(Configuration.ToString())
                               .SetFramework(fw)
                               .SetResultsDirectory(OutputTests)
                               .SetProcessWorkingDirectory(Directory.GetParent(project).FullName)
                               .SetLoggers("trx")
                               .SetVerbosity(verbosity: DotNetVerbosity.Normal)
                               .EnableNoBuild());
                    }
                }
            }
            
        });
  

    private AbsolutePath[] GetDockerProjects()
    {
        return SourceDirectory.GlobFiles("**/Dockerfile")// folders with Dockerfiles in it
            .ToArray();
    }

    Target PublishCode => _ => _
        .Unlisted()
        .Description("Publish project as release")
        .DependsOn(Compile)
        .Executes(() =>
        {
            var dockfiles = GetDockerProjects();
            foreach (var dockfile in dockfiles)
            {
                Information(dockfile.Parent.ToString());
                var project = dockfile.Parent.GlobFiles("*.csproj").First();
                DotNetPublish(s => s
                .SetProject(project)
                .SetConfiguration(Configuration.Release));
            }
        });
    Target All => _ => _
     .Description("Compiles all projects and runs all tests")
     .DependsOn(BuildRelease, RunTests);

   
    //--------------------------------------------------------------------------------
    // Documentation 
    //--------------------------------------------------------------------------------

    Target Compile => _ => _
        .Description("Builds all the projects in the solution")
        .DependsOn(AssemblyInfo)
        .Executes(() =>
        {
            var version = ReleaseNotes.Version.ToString();

            foreach(var sln in Solutions){
                DotNetBuild(s => s
                .SetProjectFile(sln)
                .SetConfiguration(Configuration));
            }
        });

    Target BuildRelease => _ => _
    .DependsOn(Compile);

    Target AssemblyInfo => _ => _
        .Executes(() =>
        {
            XmlTasks.XmlPoke(SourceDirectory / "Directory.Build.props", "//Project/PropertyGroup/PackageReleaseNotes", GetNuGetReleaseNotes(ChangelogFile));
            XmlTasks.XmlPoke(SourceDirectory / "Directory.Build.props", "//Project/PropertyGroup/VersionPrefix", ReleaseVersion);

        });

    Target Install => _ => _
        .Description("Install `Nuke.GlobalTool`")
        .Executes(() =>
        {
            DotNet($"tool install Nuke.GlobalTool --global");
        });

    static void Information(string info)
    {
        Serilog.Log.Information(info);
    }
}

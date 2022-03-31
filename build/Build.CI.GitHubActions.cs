// Copyright 2021 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.GitHubActions.Configuration;
using Nuke.Common.Execution;
using Nuke.Common.Utilities;

[CustomGitHubActions("pr_validation",
    GitHubActionsImage.WindowsLatest,
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = false,
    OnPushBranches = new[] { "master", "dev" },
    OnPullRequestBranches = new[] { "master", "dev" },
    InvokedTargets = new[] { nameof(All) },
    PublishArtifacts = true,
    EnableGitHubContext = true)
]

[CustomGitHubActions("Windows_release",
    GitHubActionsImage.WindowsLatest,
    OnPushTags = new[] { "*" },
    AutoGenerate = false,
    InvokedTargets = new[] { nameof(NuGet) },
    ImportSecrets = new[] { "Nuget_Key", "GITHUB_TOKEN" },
    EnableGitHubContext = true,
    PublishArtifacts = true)
]

partial class Build
{
}
class CustomGitHubActionsAttribute : GitHubActionsAttribute
{
    public CustomGitHubActionsAttribute(string name, GitHubActionsImage image, params GitHubActionsImage[] images) : base(name, image, images)
    {
    }

    protected override GitHubActionsJob GetJobs(GitHubActionsImage image, IReadOnlyCollection<ExecutableTarget> relevantTargets)
    {
        var job = base.GetJobs(image, relevantTargets);
        var newSteps = new List<GitHubActionsStep>(job.Steps);
        foreach (var version in new[] { "6.0.*", "5.0.*" })
        {
            newSteps.Insert(1, new GitHubActionsSetupDotNetStep
            {
                Version = version
            });
        }
        newSteps.Insert(1, new GitHubActionsSetupChmod
        {
            File = "build.cmd"
        });
        newSteps.Insert(1, new GitHubActionsSetupChmod
        {
            File = "build.sh"
        });
        job.Steps = newSteps.ToArray();
        return job;
    }
}

class GitHubActionsSetupDotNetStep : GitHubActionsStep
{
    public string Version { get; init; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- uses: actions/setup-dotnet@v1");

        using (writer.Indent())
        {
            writer.WriteLine("with:");
            using (writer.Indent())
            {
                writer.WriteLine($"dotnet-version: {Version}");
            }
        }
    }
}

class GitHubActionsSetupChmod : GitHubActionsStep
{
    public string File { get; init; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine($"- name: Make {File} executable");
        using (writer.Indent())
        {
            writer.WriteLine($"run: chmod +x ./{File}");
        }
    }
}
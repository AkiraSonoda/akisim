using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using System;
using System.IO;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution("Akisim.sln")] readonly Solution Solution;

    AbsolutePath SourceDirectory => RootDirectory / "OpenSim";
    AbsolutePath BinDirectory => RootDirectory / "bin";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    
    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            if (Directory.Exists(BinDirectory))
            {
                Directory.Delete(BinDirectory, true);
            }
            Directory.CreateDirectory(BinDirectory);
            
            // Restore all files in current directory
            RestoreFiles(ArtifactsDirectory, BinDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution)
                .SetProperty("DisableParallel", "true"));
        });

    Target BuildThirdParty => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(RootDirectory / "ThirdParty" / "SmartThreadPool" / "SmartThreadPool.csproj")
                .SetConfiguration(Configuration)
                .SetProperty("DisableParallel", "true")
                .EnableNoRestore());

            DotNetBuild(s => s
                .SetProjectFile(RootDirectory / "ThirdParty" / "ThreadedClasses" / "ThreadedClasses.csproj")
                .SetConfiguration(Configuration)
                .SetProperty("DisableParallel", "true")
                .EnableNoRestore());
        });

    Target BuildOpenSim => _ => _
        .DependsOn(BuildThirdParty)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution.GetProject("OpenSim"))
                .SetConfiguration(Configuration)
                .SetProperty("DisableParallel", "true")
                .EnableNoRestore());
        });

    Target Compile => _ => _
        .DependsOn(BuildOpenSim)
        .Executes(() =>
        {
            // Build remaining projects one by one
            foreach (var project in Solution.AllProjects)
            {
                if (project.Name == "SmartThreadPool" || 
                    project.Name == "ThreadedClasses" || 
                    project.Name == "OpenSim.Framework")
                    continue;

                try
                {
                    DotNetBuild(s => s
                        .SetProjectFile(project)
                        .SetConfiguration(Configuration)
                        .SetProperty("DisableParallel", "true")
                        .EnableNoRestore());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to build {project.Name}: {ex.Message}");
                    // Continue with other projects
                }
            }
        });

    void SetCommonProperties(DotNetBuildSettings settings)
    {
        settings
            .SetConfiguration(Configuration)
            .SetCopyright("OpenSimulator")
            .AddProperty("AllowUnsafe", "true")
            .AddProperty("WarningLevel", "4")
            .AddProperty("WarningsAsErrors", "false")
            .AddProperty("SuppressWarnings", "CA1416,SYSLIB0011,SYSLIB0014,SYSLIB0039")
            .AddProperty("UseCommonOutputDirectory", "true")
            .AddProperty("AppendTargetFrameworkToOutputPath", "false")
            .AddProperty("AppendRuntimeIdentifierToOutputPath", "false");
    }
    
    void RestoreFiles(AbsolutePath tempDir, AbsolutePath targetDir)
    {
        // Create the target directory if it doesn't exist
        Directory.CreateDirectory(targetDir);

        // Restore all files in current directory
        foreach (var file in Directory.GetFiles(tempDir))
        {
            var relativePath = Path.GetRelativePath(tempDir, file);
            var targetFile = targetDir / relativePath;
        
            // Create target directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
            File.Copy(file, targetFile);
        }

        // Recursively process all directories
        foreach (var dir in Directory.GetDirectories(tempDir))
        {
            var relativePath = Path.GetRelativePath(tempDir, dir);
            var newTargetDir = targetDir / relativePath;
            RestoreFiles(dir, newTargetDir);
        }
    }
}
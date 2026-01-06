using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    public static int Main()
    {
        // Completely isolate the _build project from the rest of the build
        var originalOutputPath = Environment.GetEnvironmentVariable("NUKE_OUTPUT_DIRECTORY");
        var buildAssemblyPath = Assembly.GetExecutingAssembly().Location;
        var buildDirectory = Path.GetDirectoryName(buildAssemblyPath);
        
        Console.WriteLine($"Build is executing from: {buildDirectory}");
        
        // Create a permanent isolation directory for _build artifacts
        var isolationDir = Path.Combine(Path.GetTempPath(), "NukeBuild_Isolation", Guid.NewGuid().ToString());
        Directory.CreateDirectory(isolationDir);
        Console.WriteLine($"Created isolation directory for _build: {isolationDir}");
        
        try
        {
            // Set environment variable to isolate NUKE's output
            Environment.SetEnvironmentVariable("NUKE_OUTPUT_DIRECTORY", isolationDir);
            
            // Set MSBuildLocator to use a different directory to prevent files from being loaded
            Environment.SetEnvironmentVariable("MSBUILD_DISABLE_SHARED_RESOLVER", "1");
            
            // Execute the build
            return Execute<Build>(x => x.Compile);
        }
        finally
        {
            // Restore environment variables
            Environment.SetEnvironmentVariable("MSBUILD_DISABLE_SHARED_RESOLVER", null);
            
            if (originalOutputPath != null)
                Environment.SetEnvironmentVariable("NUKE_OUTPUT_DIRECTORY", originalOutputPath);
            else
                Environment.SetEnvironmentVariable("NUKE_OUTPUT_DIRECTORY", null);
                
            // Try to clean up temp directory, but don't fail if it can't be deleted
            try
            {
                if (Directory.Exists(isolationDir))
                {
                    Directory.Delete(isolationDir, true);
                    Console.WriteLine($"Cleaned up isolation directory: {isolationDir}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to clean up isolation directory: {ex.Message}");
            }
        }
    }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution("Akisim.sln")] readonly Solution Solution;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath BinDirectory => RootDirectory / "bin";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath TempDirectory => RootDirectory / "temp" / ".nuke";
    
    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            if (Directory.Exists(BinDirectory))
            {
                Directory.Delete(BinDirectory, true);
            }
            Directory.CreateDirectory(BinDirectory);

            // Restore all files in current directory but skip everything related to _build
            RestoreFiles(ArtifactsDirectory, BinDirectory);

            // Create symlink for platform-specific System.Drawing.Common.dll
            string targetFileName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                targetFileName = "System.Drawing.Common.dll.linux";
                Console.WriteLine("Detected Linux platform");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                targetFileName = "System.Drawing.Common.dll.win";
                Console.WriteLine("Detected Windows platform");
            }
            else
            {
                Console.WriteLine("Warning: Unsupported platform for System.Drawing.Common.dll symlink");
                return;
            }

            string symlinkPath = Path.Combine(BinDirectory, "System.Drawing.Common.dll");
            string targetPath = Path.Combine(BinDirectory, targetFileName);

            // Delete existing symlink or file if it exists
            if (File.Exists(symlinkPath))
            {
                File.Delete(symlinkPath);
            }

            if (File.Exists(targetPath))
            {
                // Create symbolic link (relative path)
                File.CreateSymbolicLink(symlinkPath, targetFileName);
                Console.WriteLine($"Created symlink: {symlinkPath} -> {targetFileName}");
            }
            else
            {
                Console.WriteLine($"Warning: Platform-specific file not found: {targetPath}");
            }
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
            // Show the source directory contents for debugging
            Console.WriteLine($"Contents of source directory: {SourceDirectory}");
            if (Directory.Exists(SourceDirectory))
            {
                foreach (var dir in Directory.GetDirectories(SourceDirectory))
                {
                    Console.WriteLine($"- {Path.GetFileName(dir)}");
                }
            }
            
            // Define the ThirdParty project paths with proper folder names
            var smartThreadPoolPath = SourceDirectory / "SmartThreadPool" / "SmartThreadPool.csproj";
            var threadedClassesPath = SourceDirectory / "ThreadedClasses" / "ThreadedClasses.csproj";
            
            Console.WriteLine($"Looking for SmartThreadPool project at: {smartThreadPoolPath}");
            Console.WriteLine($"Looking for ThreadedClasses project at: {threadedClassesPath}");
            
            // Verify the project files exist
            if (!File.Exists(smartThreadPoolPath))
            {
                Console.WriteLine($"ERROR: SmartThreadPool project file not found at: {smartThreadPoolPath}");
                throw new FileNotFoundException($"Could not find project file: {smartThreadPoolPath}");
            }
            
            if (!File.Exists(threadedClassesPath))
            {
                Console.WriteLine($"ERROR: ThreadedClasses project file not found at: {threadedClassesPath}");
                throw new FileNotFoundException($"Could not find project file: {threadedClassesPath}");
            }
            
            // Build SmartThreadPool
            Console.WriteLine("Building SmartThreadPool...");
            DotNetBuild(s => s
                .SetProjectFile(smartThreadPoolPath)
                .SetConfiguration(Configuration)
                .SetProperty("DisableParallel", "true")
                .SetProperty("OutputPath", RootDirectory / "bin")
                .EnableNoRestore());

            // Build ThreadedClasses
            Console.WriteLine("Building ThreadedClasses...");
            DotNetBuild(s => s
                .SetProjectFile(threadedClassesPath)
                .SetConfiguration(Configuration)
                .SetProperty("DisableParallel", "true")
                .SetProperty("OutputPath", RootDirectory / "bin")
                .EnableNoRestore());
        });

    Target BuildOpenSim => _ => _
        .DependsOn(BuildThirdParty)
        .Executes(() =>
        {
            // Try to find OpenSim.Framework project
            var openSimProject = Solution.AllProjects
                .FirstOrDefault(p => p.Name == "OpenSim.Framework");
            
            if (openSimProject == null)
            {
                Console.WriteLine("OpenSim.Framework project not found, checking for other OpenSim projects...");
                openSimProject = Solution.AllProjects
                    .FirstOrDefault(p => p.Name.StartsWith("OpenSim"));
                
                if (openSimProject == null)
                {
                    Console.WriteLine("ERROR: No OpenSim projects found in solution");
                    Console.WriteLine("Available projects:");
                    foreach (var project in Solution.AllProjects)
                    {
                        Console.WriteLine($"- {project.Name}");
                    }
                    throw new InvalidOperationException("No OpenSim projects found in solution");
                }
            }
            
            Console.WriteLine($"Building OpenSim project: {openSimProject.Name}");
            DotNetBuild(s => s
                .SetProjectFile(openSimProject)
                .SetConfiguration(Configuration)
                .SetProperty("DisableParallel", "true")
                .SetProperty("OutputPath", RootDirectory / "bin")
                .EnableNoRestore());
        });

    Target CopyNuGetDependencies => _ => _
        .Executes(() =>
        {
            Console.WriteLine("Copying NuGet dependencies to bin directory...");
            
            // Get the home directory and NuGet packages folder
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var nugetPackages = Path.Combine(home, ".nuget", "packages");
            
            // Define packages we want to ensure are copied
            var packages = new Dictionary<string, string[]>
            {
                { "log4net", new[] { "log4net.dll" } },
                { "bouncycastle.cryptography", new[] { "BouncyCastle.Cryptography.dll" } },
                { "mono.addins", new[] { "Mono.Addins.dll" } },
                { "mono.addins.cecilreflector", new[] { "Mono.Addins.CecilReflector.dll" } },
                { "mono.addins.setup", new[] { "Mono.Addins.Setup.dll" } },
                { "mysqlconnector", new[] { "MySqlConnector.dll" } },
                { "system.configuration.configurationmanager", new[] { "System.Configuration.ConfigurationManager.dll" } },
                { "system.text.json", new[] { "System.Text.Json.dll" } },
                { "npgsql", new[] { "Npgsql.dll" } },
                { "system.drawing.common", new[] { "System.Drawing.Common.dll" } },
                { "system.data.sqlclient", new[] { "System.Data.SqlClient.dll" } },
                { "system.data.sqlite", new[] { "System.Data.SQLite.dll" } },
                { "system.runtime.caching", new[] { "System.Runtime.Caching.dll" } },
                { "ionic.zlib.core", new[] { "Ionic.Zlib.Core.dll" } },
                // Add other packages you need here in the format:
                // { "packageName", new[] { "file1.dll", "file2.dll" } }
            };
            
            foreach (var package in packages)
            {
                string packageName = package.Key;
                string[] files = package.Value;
                
                // Check if package directory exists
                string packageDir = Path.Combine(nugetPackages, packageName);
                if (!Directory.Exists(packageDir))
                {
                    Console.WriteLine($"Package directory for {packageName} not found at {packageDir}");
                    continue;
                }
                
                // Get all versions and sort to get the latest
                var versions = Directory.GetDirectories(packageDir)
                    .Select(Path.GetFileName)
                    .OrderByDescending(v => v)
                    .ToList();
                    
                if (versions.Count == 0)
                {
                    Console.WriteLine($"No versions found for package {packageName}");
                    continue;
                }
                
                string latestVersion = versions.First();
                string packageVersionDir = Path.Combine(packageDir, latestVersion);
                
                // Look for the lib directory
                string libDir = Path.Combine(packageVersionDir, "lib");
                if (!Directory.Exists(libDir))
                {
                    Console.WriteLine($"Lib directory not found for package {packageName} version {latestVersion}");
                    continue;
                }
                
                // Find all target framework directories in the lib folder
                var frameworkDirs = Directory.GetDirectories(libDir);
                
                // Priority order for framework selection
                var frameworkPriority = new[] 
                { 
                    "netstandard2.1", "netstandard2.0", "netstandard1.3", "netstandard", 
                    "net8.0", "net7.0", "net6.0", "net5.0", "netcoreapp3.1", "netcoreapp",
                    "net48", "net472", "net47", "net462", "net461", "net46", "net45", "net40"
                };
                
                string selectedFramework = null;
                
                // Find the highest priority framework that exists
                foreach (var framework in frameworkPriority)
                {
                    var match = frameworkDirs.FirstOrDefault(d => 
                        Path.GetFileName(d).Equals(framework, StringComparison.OrdinalIgnoreCase));
                        
                    if (match != null)
                    {
                        selectedFramework = match;
                        break;
                    }
                }
                
                // If no prioritized framework found, just take the first one
                if (selectedFramework == null && frameworkDirs.Length > 0)
                {
                    selectedFramework = frameworkDirs[0];
                }
                
                if (selectedFramework == null)
                {
                    Console.WriteLine($"No compatible framework found for package {packageName}");
                    continue;
                }
                
                // Copy each requested file
                foreach (var file in files)
                {
                    string sourceFile = Path.Combine(selectedFramework, file);
                    if (File.Exists(sourceFile))
                    {
                        string targetFile = Path.Combine(BinDirectory, file);
                        Console.WriteLine($"Copying {sourceFile} to {targetFile}");
                        File.Copy(sourceFile, targetFile, true);
                    }
                    else
                    {
                        Console.WriteLine($"File {file} not found in {selectedFramework}");
                    }
                }
            }
        });

    Target Compile => _ => _
        .DependsOn(BuildOpenSim)
        .DependsOn(CopyNuGetDependencies)
        .Executes(() =>
        {
            // Build remaining projects one by one
            var builtProjects = new List<string> { "SmartThreadPool", "ThreadedClasses" };
            if (Solution.AllProjects.Any(p => p.Name == "OpenSim.Framework"))
                builtProjects.Add("OpenSim.Framework");
                
            Console.WriteLine("Building remaining projects...");
            
            foreach (var project in Solution.AllProjects)
            {
                // Skip _build project and already built projects
                if (IsBuildInfrastructureProject(project.Name) || 
                    builtProjects.Contains(project.Name) || 
                    project.Name.EndsWith(".Tests") ||
                    project.Name.EndsWith(".Test"))
                    continue;

                Console.WriteLine($"Building {project.Name}...");
                try
                {
                    DotNetBuild(s => s
                        .SetProjectFile(project)
                        .SetConfiguration(Configuration)
                        .SetProperty("DisableParallel", "true")
                        .SetProperty("OutputPath", RootDirectory / "bin")
                        .EnableNoRestore());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to build {project.Name}: {ex.Message}");
                    // Continue with other projects
                }
            }
        });

    // Helper method to check if a project is related to build infrastructure
    bool IsBuildInfrastructureProject(string projectName)
    {
        return projectName == "_build" || 
               projectName.Contains("Nuke") ||
               projectName.Contains("Build") ||
               projectName.Contains("MSBuild") ||
               projectName.Contains("Azure") ||
               projectName.StartsWith("_");
    }

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
        // Skip if source directory doesn't exist
        if (!Directory.Exists(tempDir))
        {
            Console.WriteLine($"Skipping restore from {tempDir} (directory doesn't exist)");
            return;
        }
            
        // Create the target directory if it doesn't exist
        Directory.CreateDirectory(targetDir);
        Console.WriteLine($"Restoring files from {tempDir} to {targetDir}");

        // Restore all files in current directory
        foreach (var file in Directory.GetFiles(tempDir))
        {
            var relativePath = Path.GetRelativePath(tempDir, file);
            string fileName = Path.GetFileName(file);
            
            var targetFile = Path.Combine(targetDir, relativePath);
            var targetFileDir = Path.GetDirectoryName(targetFile);
            
            // Create target directory if it doesn't exist
            Directory.CreateDirectory(targetFileDir);
            File.Copy(file, targetFile, true);
            Console.WriteLine($"Restored file: {relativePath}");
        }

        // Recursively process all directories
        foreach (var dir in Directory.GetDirectories(tempDir))
        {
            var dirName = Path.GetFileName(dir);
            
            var relativePath = Path.GetRelativePath(tempDir, dir);
            var newTargetDir = Path.Combine(targetDir, relativePath);
            RestoreFiles((AbsolutePath)dir, (AbsolutePath)newTargetDir);
        }
    }
}
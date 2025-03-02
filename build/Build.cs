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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution("Akisim.sln")] readonly Solution Solution;

    AbsolutePath SourceDirectory => RootDirectory / "src";
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
                .EnableNoRestore());

            // Copy SmartThreadPool DLL to bin directory
            Console.WriteLine("Copying SmartThreadPool to bin...");
            CopyOutputToBin(smartThreadPoolPath, "SmartThreadPool");

            // Build ThreadedClasses
            Console.WriteLine("Building ThreadedClasses...");
            DotNetBuild(s => s
                .SetProjectFile(threadedClassesPath)
                .SetConfiguration(Configuration)
                .SetProperty("DisableParallel", "true")
                .EnableNoRestore());

            // Copy ThreadedClasses DLL to bin directory
            Console.WriteLine("Copying ThreadedClasses to bin...");
            CopyOutputToBin(threadedClassesPath, "ThreadedClasses");
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
                .EnableNoRestore());

            // Copy OpenSim.Framework DLL to bin directory
            Console.WriteLine("Copying OpenSim.Framework to bin...");
            CopyOutputToBin(openSimProject.Path, openSimProject.Name);
        });

    Target Compile => _ => _
        .DependsOn(BuildOpenSim)
        .Executes(() =>
        {
            // Build remaining projects one by one
            var builtProjects = new List<string> { "SmartThreadPool", "ThreadedClasses" };
            if (Solution.AllProjects.Any(p => p.Name == "OpenSim.Framework"))
                builtProjects.Add("OpenSim.Framework");
                
            Console.WriteLine("Building remaining projects...");
            
            foreach (var project in Solution.AllProjects)
            {
                if (builtProjects.Contains(project.Name) || 
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
                        .EnableNoRestore());

                    // Copy project output to bin directory
                    CopyOutputToBin(project.Path, project.Name);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to build {project.Name}: {ex.Message}");
                    // Continue with other projects
                }
            }
        });

    void CopyOutputToBin(string projectPath, string projectName)
    {
        try
        {
            // Ensure bin directory exists
            Directory.CreateDirectory(BinDirectory);
            
            // Calculate output directory for the project
            var projectDir = Path.GetDirectoryName(projectPath);
            
            // For .NET 8.0 projects, the output is likely in a net8.0 subdirectory
            var outputDir = Path.Combine(projectDir, "bin", Configuration, "net8.0");
            
            Console.WriteLine($"Looking for {projectName} output in: {outputDir}");
            
            if (!Directory.Exists(outputDir))
            {
                Console.WriteLine($"WARNING: .NET 8.0 output directory not found: {outputDir}");
                
                // Try alternate locations
                var alternateOutputDirs = new string[]
                {
                    Path.Combine(projectDir, "bin", Configuration),
                    Path.Combine(projectDir, "bin", Configuration.ToString()),
                    Path.Combine(projectDir, "bin", Configuration, "netstandard2.0"),
                    Path.Combine(projectDir, "bin", Configuration, "netcoreapp3.1")
                };
                
                foreach (var altDir in alternateOutputDirs)
                {
                    Console.WriteLine($"Trying alternate path: {altDir}");
                    if (Directory.Exists(altDir))
                    {
                        outputDir = altDir;
                        Console.WriteLine($"Found alternate output directory: {outputDir}");
                        break;
                    }
                }
                
                if (!Directory.Exists(outputDir))
                {
                    Console.WriteLine($"WARNING: No output directory found for {projectName}. Trying recursive search...");
                    
                    // Last resort: search recursively for the DLL
                    if (Directory.Exists(Path.Combine(projectDir, "bin")))
                    {
                        var foundFiles = Directory.GetFiles(
                            Path.Combine(projectDir, "bin"), 
                            $"{projectName}.dll", 
                            SearchOption.AllDirectories);
                            
                        if (foundFiles.Length > 0)
                        {
                            // Use the directory of the first found file
                            outputDir = Path.GetDirectoryName(foundFiles[0]);
                            Console.WriteLine($"Found DLL via recursive search: {foundFiles[0]}");
                        }
                        else
                        {
                            Console.WriteLine($"WARNING: Could not find any output for {projectName}. Skipping copy.");
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"WARNING: Project bin directory not found. Skipping copy for {projectName}");
                        return;
                    }
                }
            }

            // Try to check if this is an executable project
            bool isExecutableProject = false;
            try 
            {
                var projectFile = ProjectModelTasks.ParseProject(projectPath);
                isExecutableProject = projectFile.GetPropertyValue("OutputType")?.Equals("Exe", StringComparison.OrdinalIgnoreCase) == true;
                if (isExecutableProject)
                {
                    Console.WriteLine($"Project {projectName} is an executable project.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: Could not determine if {projectName} is an executable project: {ex.Message}");
            }

            // Get all files from the output directory
            var allFilesInOutput = Directory.GetFiles(outputDir);
            
            // First, collect all DLLs, PDBs, and EXEs
            var filesToCopy = new List<string>();
            filesToCopy.AddRange(Directory.GetFiles(outputDir, "*.dll"));
            filesToCopy.AddRange(Directory.GetFiles(outputDir, "*.pdb"));
            filesToCopy.AddRange(Directory.GetFiles(outputDir, "*.exe"));

            // For executable projects, also look for files without extension matching the project name
            if (isExecutableProject)
            {
                var executableName = Path.GetFileNameWithoutExtension(projectName);
                var potentialExecutable = Path.Combine(outputDir, executableName);
                
                if (File.Exists(potentialExecutable))
                {
                    Console.WriteLine($"Found executable: {executableName}");
                    filesToCopy.Add(potentialExecutable);
                }
            }

            // Also look for any other files without extensions that might be executables
            foreach (var file in allFilesInOutput)
            {
                if (string.IsNullOrEmpty(Path.GetExtension(file)) && !filesToCopy.Contains(file))
                {
                    Console.WriteLine($"Found potential executable: {Path.GetFileName(file)}");
                    filesToCopy.Add(file);
                }
            }

            Console.WriteLine($"Found {filesToCopy.Count} files to copy from {outputDir}");
            
            // Copy only new or updated files
            foreach (var file in filesToCopy)
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(BinDirectory, fileName);
                
                bool shouldCopy = false;
                
                // Check if destination file exists
                if (!File.Exists(destFile))
                {
                    shouldCopy = true;
                    Console.WriteLine($"New file: {fileName}");
                }
                else 
                {
                    // Check if source file is newer than destination
                    var sourceTime = File.GetLastWriteTime(file);
                    var destTime = File.GetLastWriteTime(destFile);
                    
                    if (sourceTime > destTime)
                    {
                        shouldCopy = true;
                        Console.WriteLine($"Updated file: {fileName}");
                    }
                }
                
                // Copy only if file is new or updated
                if (shouldCopy)
                {
                    File.Copy(file, destFile, true);
                    Console.WriteLine($"Copied {fileName} to bin");
                }
                else
                {
                    Console.WriteLine($"Skipped {fileName} (unchanged)");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR copying output for {projectName}: {ex.Message}");
        }
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
            var relativePath = Path.GetRelativePath(tempDir, dir);
            var newTargetDir = Path.Combine(targetDir, relativePath);
            RestoreFiles((AbsolutePath)dir, (AbsolutePath)newTargetDir);
        }
    }
}
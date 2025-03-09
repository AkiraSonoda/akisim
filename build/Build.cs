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
using System.Reflection;
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
            
            // Verify no _build files were copied
            VerifyNoBuildFiles();
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
            // Disable any references to _build
            DisableBuildReferences();
            
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
                        .EnableNoRestore());

                    // Copy project output to bin directory with careful filtering
                    CopyOutputToBin(project.Path, project.Name);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to build {project.Name}: {ex.Message}");
                    // Continue with other projects
                }
            }
            
            // Final verification and cleanup
            VerifyNoBuildFiles();
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
    
    // Helper method to disable any references to _build
    void DisableBuildReferences()
    {
        try
        {
            // Find the _build project
            var buildProject = Solution.AllProjects.FirstOrDefault(p => p.Name == "_build");
            if (buildProject != null)
            {
                Console.WriteLine("Found _build project, disabling its references...");
                
                // Load the project file to examine it
                var projectFile = ProjectModelTasks.ParseProject(buildProject.Path);
                
                // Get all referenced packages
                var packageReferences = projectFile.GetItems("PackageReference")
                    .Select(i => i.GetMetadataValue("Include"))
                    .ToList();
                    
                Console.WriteLine($"_build project references the following packages:");
                foreach (var package in packageReferences)
                {
                    Console.WriteLine($"  - {package}");
                }
                
                // These are the packages to ensure aren't copied to output
                Console.WriteLine("Any files from these packages will be explicitly filtered from the output");
            }
            else
            {
                Console.WriteLine("_build project not found in solution");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error analyzing _build project: {ex.Message}");
        }
    }
    
    void CopyOutputToBin(string projectPath, string projectName)
    {
        // Skip any build infrastructure artifacts
        if (IsBuildInfrastructureProject(projectName))
        {
            Console.WriteLine($"Skipping copying of build infrastructure artifacts: {projectName}");
            return;
        }
        
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

            // Examine output directory contents
            Console.WriteLine($"Examining directory: {outputDir}");
            var allFilesInOutput = Directory.GetFiles(outputDir);
            Console.WriteLine($"Found {allFilesInOutput.Length} total files in output directory:");
            foreach (var file in allFilesInOutput)
            {
                Console.WriteLine($"  - {Path.GetFileName(file)}");
            }
            
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

            // Also look for any other files without extensions or with non-standard filenames
            foreach (var file in allFilesInOutput)
            {
                string fileName = Path.GetFileName(file);
                
                // Skip if we already plan to copy this file
                if (filesToCopy.Contains(file))
                    continue;
                
                // Check if it has a proper extension or not
                bool isExecutable = false;
                
                // Get the part after the last dot (if any)
                int lastDotIndex = fileName.LastIndexOf('.');
                if (lastDotIndex == -1)
                {
                    // No dot at all - consider it an executable
                    isExecutable = true;
                }
                else
                {
                    // There's a dot - check if what follows is a proper extension
                    string extension = fileName.Substring(lastDotIndex + 1);
                    
                    // If more than 3 characters after the dot, it's probably not an extension
                    // but part of the filename (like "executable.config123")
                    if (extension.Length > 3)
                    {
                        isExecutable = true;
                        Console.WriteLine($"Found file with non-standard name: {fileName} (>3 chars after dot)");
                    }
                    // Skip common non-executable extensions
                    else if (extension == "xml" || extension == "config" || extension == "json" ||
                             extension == "txt" || extension == "md" || extension == "log")
                    {
                        isExecutable = false;
                    }
                    else if (extension == "dll" || extension == "pdb" || extension == "exe")
                    {
                        // These should have been caught earlier, but double check
                        isExecutable = true;
                    }
                    else
                    {
                        // Unknown extension with 3 or fewer chars - treat as potential executable
                        isExecutable = true;
                        Console.WriteLine($"Found file with unknown extension: {fileName} (treating as executable)");
                    }
                }
                
                if (isExecutable)
                {
                    Console.WriteLine($"Found potential executable: {fileName}");
                    filesToCopy.Add(file);
                }
            }

            // Filter out Azure and other build infrastructure-related files
            var filteredFiles = new List<string>();
            foreach (var file in filesToCopy)
            {
                string fileName = Path.GetFileName(file);
                
                // Check all potential build infrastructure files and skip them
                if (IsBuildInfrastructureFile(fileName))
                {
                    Console.WriteLine($"Skipping build-related file: {fileName}");
                    continue;
                }
                
                filteredFiles.Add(file);
            }

            Console.WriteLine($"Found {filteredFiles.Count} files to copy from {outputDir} after filtering");
            
            // Print the list of files that will be copied
            Console.WriteLine("Files to be copied:");
            foreach (var file in filteredFiles)
            {
                Console.WriteLine($"  - {Path.GetFileName(file)}");
            }
            
            // Copy only new or updated files
            foreach (var file in filteredFiles)
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
    
    // Verify that absolutely no build-related files exist in the bin directory
    void VerifyNoBuildFiles()
    {
        if (!Directory.Exists(BinDirectory))
            return;
            
        Console.WriteLine("Verifying bin directory for build-related files...");
        
        // List of patterns that should never be found in bin directory
        var forbiddenPatterns = new string[] 
        {
            "_build*",
            "Nuke.*",
            "Microsoft.Build*",
            "Microsoft.TeamFoundation*",
            "Microsoft.VisualStudio*",
            "MSBuild*",
            "Azure*",
            "System.Collections.Immutable*",  // Often used by build tools
            "Newtonsoft.Json*",              // Often used by build tools
            "NETStandard.Library*",           // References, not needed in output
            "JetBrains*"                      // JetBrains annotations often used by NUKE
        };
        
        bool foundForbiddenFiles = false;
        
        foreach (var pattern in forbiddenPatterns)
        {
            var matchingFiles = Directory.GetFiles(BinDirectory, pattern);
            foreach (var file in matchingFiles)
            {
                Console.WriteLine($"ERROR: Found forbidden build file: {Path.GetFileName(file)}");
                File.Delete(file);  // Forcibly delete it
                foundForbiddenFiles = true;
            }
        }
        
        // Advanced: Search all files for mentions of build-related terms
        foreach (var file in Directory.GetFiles(BinDirectory, "*.dll"))
        {
            string fileName = Path.GetFileName(file);
            
            // Check for _build and other patterns embedded in filenames
            if (fileName.IndexOf("build", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fileName.IndexOf("azure", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fileName.IndexOf("msbuild", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fileName.IndexOf("nuke", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Console.WriteLine($"ERROR: Found suspicious file name: {fileName}");
                File.Delete(file);  // Forcibly delete it
                foundForbiddenFiles = true;
            }
        }
        
        if (foundForbiddenFiles)
        {
            Console.WriteLine("WARNING: Found and removed build-related files in bin directory");
        }
        else
        {
            Console.WriteLine("Verification passed: No build-related files found in bin directory");
        }
    }
    
    // Comprehensive check to identify any build infrastructure related file
    bool IsBuildInfrastructureFile(string fileName)
    {
        // List of specific keywords that indicate build infrastructure
        var buildKeywords = new[]
        {
            "Azure", "Nuke", "Build", "MSBuild", "TeamFoundation", "VisualStudio",
            "Compiler", "CodeAnalysis", "Roslyn", "JetBrains", "Newtonsoft.Json",
            "_build", "Nuget", "Artifacts", "NETStandard.Library"
        };
        
        // List of prefixes that indicate build or system infrastructure
        var systemPrefixes = new[]
        {
            "System.", "Microsoft.Build", "Microsoft.Team", "Microsoft.Visual", "Microsoft.CodeAnalysis",
            "Microsoft.Extensions", "Microsoft.Net"
        };
        
        // Check for any exact match in keywords (case-insensitive)
        foreach (var keyword in buildKeywords)
        {
            if (fileName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        
        // Check for any matching prefixes
        foreach (var prefix in systemPrefixes)
        {
            if (fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        // Check for any special patterns
        if (fileName.StartsWith("_") || // Files starting with underscore
            (fileName.Contains(".") && fileName.IndexOf("build", StringComparison.OrdinalIgnoreCase) >= 0) || // Any file with 'build' in name
            fileName.EndsWith(".props") || // MSBuild props
            fileName.EndsWith(".targets")) // MSBuild targets
        {
            return true;
        }
        
        return false;
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
            
            // Skip any build infrastructure files
            if (IsBuildInfrastructureFile(fileName))
            {
                Console.WriteLine($"Skipping build infrastructure file: {relativePath}");
                continue;
            }
            
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
            
            // Skip build infrastructure directories
            if (IsBuildInfrastructureFile(dirName))
            {
                Console.WriteLine($"Skipping build infrastructure directory: {dirName}");
                continue;
            }
            
            var relativePath = Path.GetRelativePath(tempDir, dir);
            var newTargetDir = Path.Combine(targetDir, relativePath);
            RestoreFiles((AbsolutePath)dir, (AbsolutePath)newTargetDir);
        }
    }
}
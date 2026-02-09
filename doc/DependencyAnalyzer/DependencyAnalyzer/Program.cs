using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

public class ProjectDependencyAnalyzer
{
    private class Project
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
    }

    private readonly Dictionary<string, Project> _projects = new Dictionary<string, Project>();
    private readonly string _solutionDirectory;

    public ProjectDependencyAnalyzer(string solutionDirectory)
    {
        _solutionDirectory = solutionDirectory;
    }

    public void AnalyzeDependencies()
    {
        // Find all .csproj files
        var projectFiles = Directory.GetFiles(_solutionDirectory, "*.csproj", SearchOption.AllDirectories);
        
        // Load project information
        foreach (var projectFile in projectFiles)
        {
            LoadProject(projectFile);
        }

        // Find circular dependencies
        var circularDependencies = FindCircularDependencies();
        
        // Report results
        if (circularDependencies.Any())
        {
            Console.WriteLine("Circular Dependencies Found:");
            foreach (var cycle in circularDependencies)
            {
                Console.WriteLine($"Cycle: {string.Join(" -> ", cycle)} -> {cycle[0]}");
            }
        }
        else
        {
            Console.WriteLine("No circular dependencies found.");
        }
    }

    private void LoadProject(string projectPath)
    {
        var doc = XDocument.Load(projectPath);
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        
        var project = new Project
        {
            Name = projectName,
            Path = projectPath,
            Dependencies = new List<string>()
        };

        // Get project references
        var projectReferences = doc.Descendants()
            .Where(x => x.Name.LocalName == "ProjectReference")
            .Select(x => Path.GetFileNameWithoutExtension(x.Attribute("Include").Value))
            .ToList();

        project.Dependencies.AddRange(projectReferences);
        _projects[projectName] = project;
    }

    private List<List<string>> FindCircularDependencies()
    {
        var cycles = new List<List<string>>();
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        // Print all projects
        Console.WriteLine("Projects:");
        foreach (var project in _projects.Keys)
        {
            Console.WriteLine(project);
        }

        foreach (var project in _projects.Keys)
        {
            if (!visited.Contains(project))
            {
                var currentPath = new List<string>();
                DetectCycle(project, visited, recursionStack, currentPath, cycles);
            }
        }

        // Print cycles to console
        foreach (var cycle in cycles)
        {
            Console.WriteLine($"Cycle: {string.Join(" -> ", cycle)}");
        }

        return cycles;
        
    }

    private void DetectCycle(
        string currentProject,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        List<string> currentPath,
        List<List<string>> cycles)
    {
        visited.Add(currentProject);
        recursionStack.Add(currentProject);
        currentPath.Add(currentProject);

        foreach (var dependency in _projects[currentProject].Dependencies)
        {
            if (!_projects.ContainsKey(dependency))
                continue;

            if (!visited.Contains(dependency))
            {
                DetectCycle(dependency, visited, recursionStack, currentPath, cycles);
            }
            else if (recursionStack.Contains(dependency))
            {
                // Found a cycle
                var cycleStart = currentPath.IndexOf(dependency);
                var cycle = currentPath.Skip(cycleStart).ToList();
                if (!CycleAlreadyFound(cycles, cycle))
                {
                    cycles.Add(new List<string>(cycle));
                }
            }
        }

        currentPath.RemoveAt(currentPath.Count - 1);
        recursionStack.Remove(currentProject);
    }

    private bool CycleAlreadyFound(List<List<string>> cycles, List<string> newCycle)
    {
        foreach (var existingCycle in cycles)
        {
            if (existingCycle.Count != newCycle.Count)
                continue;

            // Check if cycles are the same (allowing for different starting points)
            for (int i = 0; i < existingCycle.Count; i++)
            {
                var rotated = newCycle.Skip(i)
                    .Concat(newCycle.Take(i))
                    .ToList();
                
                if (existingCycle.SequenceEqual(rotated))
                    return true;
            }
        }
        return false;
    }
}

// Usage example:
public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide the solution directory path as an argument.");
            return;
        }

        var analyzer = new ProjectDependencyAnalyzer(args[0]);
        analyzer.AnalyzeDependencies();
    }
}
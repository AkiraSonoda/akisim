using System;
using System.Reflection;

class Program {
    static void Main() {
        try {
            var asm = Assembly.LoadFrom("System.Diagnostics.DiagnosticSource.dll");
            Console.WriteLine($"Assembly: {asm.FullName}");
            Console.WriteLine($"Version: {asm.GetName().Version}");
            Console.WriteLine($"Location: {asm.Location}");
        } catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

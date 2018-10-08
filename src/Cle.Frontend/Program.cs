﻿using System;
using System.IO;
using Cle.Compiler;

namespace Cle.Frontend
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("Cle Compiler");
            Console.WriteLine("WIP: This executable currently only parses the Cle files in current directory.");
            Console.WriteLine();

            // TODO: Read command line parameters

            // TODO: Create a logger for diagnostics and output messages as they are produced

            // TODO: Create a file interface
            var fileProvider = new SourceFileProvider(Directory.GetCurrentDirectory());

            // TODO: Initialize a compiler instance and call it
            var result = CompilerDriver.Compile(".", new CompilationOptions(), fileProvider);

            // Write an output summary
            PrintDiagnostics(result);
            
            if (result.Diagnostics.Count > 0)
            {
                // Separate the diagnostics list from the summary
                Console.WriteLine();
            }

            if (result.FailedCount == 0)
            {
                Console.WriteLine("Build succeeded.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Build failed.");
                Console.ResetColor();
            }
            Console.WriteLine($"Succeeded modules: {result.SucceededCount}, Failed modules: {result.FailedCount}");

            // TODO: Return an according return value
            return 0;
        }

        private static void PrintDiagnostics(CompilationResult result)
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                // TODO: Display paths relative to module root, including subdirectories
                Console.WriteLine($"{Path.GetFileName(diagnostic.Filename)} " +
                                  $"({diagnostic.Position.Line},{diagnostic.Position.ByteInLine}): " +
                                  DiagnosticMessages.GetMessage(diagnostic));
            }
        }
    }
}

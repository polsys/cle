using System.Collections.Generic;
using CommandLine;

namespace Cle.Frontend
{
    /// <summary>
    /// Possible command line arguments.
    /// </summary>
    internal class CommandLineOptions
    {
        [Option("dump", HelpText = "If specified, compiler debug information for all methods " + 
            "where the name contains the parameter string will be logged to cle-dump.txt. " +
            "Specify * to dump all methods. More complex wildcard syntax is not supported.")]
        public string? DumpRegex { get; set; }
        
        [Option("disasm", HelpText = "If specified, an assembly language listing for the program " +
                                     "will be emitted in the output directory.")]
        public bool Disassembly { get; set; }

        [Value(0, Required = false,
            HelpText = "Main module path relative to current directory.", MetaName = "module")]
        public IEnumerable<string>? MainModule { get; set; }
    }
}

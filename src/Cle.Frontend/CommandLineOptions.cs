using System.Collections.Generic;
using CommandLine;
using JetBrains.Annotations;

namespace Cle.Frontend
{
    /// <summary>
    /// Possible command line arguments.
    /// </summary>
    internal class CommandLineOptions
    {
        [CanBeNull]
        [Option("dump", HelpText = "If specified, debug information for all methods " + 
            "matching the regex parameter will be dumped to cle-dump.txt.")]
        public string DumpRegex { get; set; }

        [CanBeNull, ItemNotNull]
        [Value(0, Required = false, Max = 1,
            HelpText = "Main module path relative to current directory.", MetaName = "module")]
        public IEnumerable<string> MainModule { get; set; }
    }
}

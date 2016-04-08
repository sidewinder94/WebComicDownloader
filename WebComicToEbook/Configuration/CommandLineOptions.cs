using System;

using CommandLine;
using CommandLine.Text;

namespace WebComicToEbook.Configuration
{
    public class CommandLineOptions
    {
        [Option('o', "overwrite", DefaultValue = false, HelpText = "If the application should overwrite existing ebooks files")]
        public bool Overwrite { get; set; }

        [Option('i', "config", DefaultValue = "config.json", HelpText = "The path to the configuration file to use")]
        public String ConfigFilePath { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText()
            {
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddOptions(this);
            return help;
        }
    }
}
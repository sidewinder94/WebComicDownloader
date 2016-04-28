using System;

using CommandLine;
using CommandLine.Text;

namespace WebComicToEbook.Configuration
{
    public class CommandLineOptions
    {
        [Option('o', "overwrite", DefaultValue = false, HelpText = "If the application should overwrite existing ebooks files")]
        public bool Overwrite { get; set; }

        [Option('d', "download-again", DefaultValue = false, HelpText = "Download again if the file already exists ?")]
        public bool Redownload { get; set; }

        [Option('i', "config", DefaultValue = "config.json", HelpText = "The path to the configuration file to use")]
        public String ConfigFilePath { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
    (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
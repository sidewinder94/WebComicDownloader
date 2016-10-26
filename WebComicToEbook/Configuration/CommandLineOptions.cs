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
        public string ConfigFilePath { get; set; }

        [Option(longName: "save-progress", DefaultValue = "", HelpText = "The path to save the current progress, temporary directory by default (won't be able to resume)")]
        public string SaveProgressFolder { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(
                this,
                current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
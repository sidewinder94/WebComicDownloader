using System;
using System.IO;
using System.Linq;

using CommandLine;

using WebComicToEbook.Configuration;
using WebComicToEbook.Properties;
using WebComicToEbook.Scraper;
using WebComicToEbook.Utils;

namespace WebComicToEbook
{
    class Program
    {
        private static readonly ConsoleDisplay Display = new ConsoleDisplay();

        static void Main(string[] args)
        {
            if (Parser.Default.ParseArguments(args, Settings.Instance.CommandLineOptions))
            {
                if (File.Exists(Settings.Instance.CommandLineOptions.ConfigFilePath ?? Settings.DefaultConfigFile))
                {
                    Settings.Instance.Load();

                    BaseWebComicScraper scraper;
                    Settings.Instance.Entries.AsParallel().ForAll(
                        entry =>
                        {
                            try
                            {


                                if (entry.Parser == WebComicEntry.Parsers.XPath)
                                {
                                    scraper = new HAPWebComicScraper();
                                }
                                else if (entry.Parser == WebComicEntry.Parsers.RegExp)
                                {
                                    scraper = new RegExpWebComicScraper();
                                }
                                else
                                {
                                    ConsoleDisplay.AppendLine(
                                        $"Unknown scraper type for entry {entry.Title} - {entry.BaseAddress}");
                                    return;
                                }

                                scraper.StartScraping(entry);
                            }
                            catch (NotSupportedException ex)
                            {
                                ConsoleDisplay.AppendLine($"[{entry.Title}][Exception] : {ex.Message}");
                            }
                        });
                }
            }
            else
            {
                Display.Halted = true;
                Console.WriteLine(Resources.ErrorNoConfigFileFound);
                Settings.Instance.Entries.Add(new WebComicEntry());
                Settings.Instance.Save();
            }
        }
    }
}

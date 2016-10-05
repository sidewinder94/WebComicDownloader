using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml.Linq;

using WebComicToEbook.Configuration;
using WebComicToEbook.Properties;
using WebComicToEbook.Scraper;
using WebComicToEbook.Utils;
using CommandLine;
using CommandLine.Text;
using static Utils.Text.Comparison;


namespace WebComicToEbook
{
    class Program
    {
        private static ConsoleDisplay _display = new ConsoleDisplay();

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
                _display.Halted = true;
                Console.WriteLine("No parameters given and no config file found ! Creating one now...");
                Settings.Instance.Entries.Add(new WebComicEntry());
                Settings.Instance.Save();
                return;
            }
        }
    }
}

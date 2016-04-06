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

using static Utils.Text.Comparison;


namespace WebComicToEbook
{
    class Program
    {
        private static ConsoleDisplay _display = new ConsoleDisplay();

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                if (File.Exists(Settings.DefaultConfigFile))
                {
                    Settings.Instance.Load();
                    BaseWebComicScraper scraper;
                    Settings.Instance.Entries.AsParallel().ForAll(
                        entry =>
                            {

                                if (CaseInsensitiveComparison(entry.Parser, "XPath"))
                                {
                                    scraper = new HAPWebComicScraper();
                                }
                                else if (CaseInsensitiveComparison(entry.Parser, "RegExp"))
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
                                
                            });
                    Settings.Instance.Save();
                }
                else
                {
                    _display.Halted = true;
                    Console.WriteLine("No parameters given and no config file found ! Creating one now...");
                    Settings.Instance.Entries.Add(new WebComicEntry());
                    Settings.Instance.Save();
                    PrintUsage();
                    return;
                }
            }



        }


        static void PrintUsage()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Usage : ");
        }
    }
}

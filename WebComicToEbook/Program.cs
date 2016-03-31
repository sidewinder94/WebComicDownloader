using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

using WebComicToEbook.Configuration;
using WebComicToEbook.Properties;
using WebComicToEbook.Scraper;
using static Utils.Text.Comparison;


namespace WebComicToEbook
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                if (File.Exists(Settings.DefaultConfigFile))
                {
                    Settings.Instance.Load();
                    BaseWebComicScraper scraper; 
                    foreach (var entry in Settings.Instance.Entries)
                    {
                        if (CaseInsensitiveComparison(entry.Parser,"XPath"))
                        {
                            scraper = new HAPWebComicScraper();
                        }
                        else if (CaseInsensitiveComparison(entry.Parser, "RegExp"))
                        {
                            continue;
                            scraper = new RegExpWebComicScraper();
                        }
                        else
                        {
                            Console.WriteLine($"Unknown scraper type for entry {entry.Title} - {entry.BaseAddress}");
                            continue;
                        }
                        scraper.StartScraping(entry);
                    }
                    Settings.Instance.Save();
                }
                else
                {
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

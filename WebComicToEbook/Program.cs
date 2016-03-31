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
                    int counter = 0;
                    Settings.Instance.Entries.AsParallel().ForAll(
                        entry =>
                            {

                                var lineCounter = counter;
                                Interlocked.Increment(ref counter);
                                if (CaseInsensitiveComparison(entry.Parser, "XPath"))
                                {
                                    scraper = new HAPWebComicScraper(lineCounter);
                                }
                                else if (CaseInsensitiveComparison(entry.Parser, "RegExp"))
                                {
                                    scraper = new RegExpWebComicScraper(lineCounter);
                                }
                                else
                                {
                                    Console.WriteLine(
                                        $"Unknown scraper type for entry {entry.Title} - {entry.BaseAddress}");
                                    return;
                                }
                                scraper.StartScraping(entry);
                                
                            });
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

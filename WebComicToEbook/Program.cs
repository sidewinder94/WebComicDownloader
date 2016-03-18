using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

using WebComicToEbook.Configuration;
using WebComicToEbook.Properties;

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

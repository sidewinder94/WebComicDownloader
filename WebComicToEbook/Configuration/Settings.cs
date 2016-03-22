using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace WebComicToEbook.Configuration
{
    public class Settings
    {
        public const string DefaultConfigFile = "config.json";

        public static Settings Instance => Holder.settings;

        private Settings()
        {
            this.Entries = new List<WebComicEntry>();
        }

        public List<WebComicEntry> Entries;


        public void Load(string configFilePath = DefaultConfigFile)
        {
            var jsonList = JsonConvert.DeserializeObject<List<WebComicEntry>>(File.ReadAllText(configFilePath));
            this.Entries.AddRange(jsonList);
        }

        public void Save(string configFilePath = DefaultConfigFile)
        {
            File.WriteAllText(configFilePath, JsonConvert.SerializeObject(this.Entries, Formatting.Indented));
        }

        private static class Holder
        {
            public static readonly Settings settings = new Settings();
        }
    }
}
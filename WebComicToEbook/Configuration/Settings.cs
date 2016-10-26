using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace WebComicToEbook.Configuration
{
    public class Settings
    {
        public const string DefaultConfigFile = "config.json";

        public static Settings Instance => Holder.Settings;

        private Settings()
        {
            this.Entries = new List<WebComicEntry>();
            this.CommandLineOptions = new CommandLineOptions();
        }

        public List<WebComicEntry> Entries;

        [JsonIgnore]
        public CommandLineOptions CommandLineOptions;

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
            public static readonly Settings Settings = new Settings();
        }
    }
}
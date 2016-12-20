using System;
using System.Collections.Generic;
using System.ComponentModel;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WebComicToEbook.Configuration
{
    public class WebComicEntry
    {
        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Parsers.XPath)]
        public Parsers Parser = Parsers.XPath;

        public string BaseAddress = string.Empty;

        public string AddressPattern = "{0}";

        public string NextButtonSelector = string.Empty;

        public string ChapterTitleSelector = string.Empty;

        public string ChapterContentSelector = string.Empty;

        public string[] ImageTags = new[] { "img" };

        public Dictionary<string, string> ImageSourceAttributes = new Dictionary<string, string> { { "img", "src" } };

        public string[] IncludeTags = new string[0];

        public string InteruptAtTag = string.Empty;

        public string Author;

        public DateTime Date = DateTime.Now;

        public string Title;

        public string Description;

        public bool IgnoreMissingChapterName = false;

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(ContentType.Text)]
        public ContentType Content = ContentType.Text;

        public enum Parsers
        {
            XPath,
            RegExp
        }

        public enum ContentType
        {
            Text,
            Image,
            Mixed
        }
    }
}
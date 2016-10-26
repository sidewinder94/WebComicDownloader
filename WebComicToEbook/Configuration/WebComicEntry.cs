using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WebComicToEbook.Configuration
{
    public class WebComicEntry
    {
        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Parsers.XPath)]
        public Parsers Parser = Parsers.XPath;

        public String BaseAddress = "";

        public String NextButtonSelector = "";

        public String ChapterTitleSelector = "";

        public String ChapterContentSelector = "";

        public String[] ImageTags = new[] { "img" };

        public Dictionary<String, String> ImageSourceAttributes = new Dictionary<string, string> { { "img", "src" } };

        public String[] IncludeTags = new String[0];

        public String InteruptAtTag = "";

        public String Author;

        public DateTime Date = DateTime.Now;

        public String Title;

        public String Description;

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
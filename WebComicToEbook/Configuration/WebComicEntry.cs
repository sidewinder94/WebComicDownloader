using System;
using System.ComponentModel;
using System.Data.SqlTypes;
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
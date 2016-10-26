using System.Collections.Generic;
using System.ComponentModel;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using WebComicToEbook.Configuration;

namespace WebComicToEbook.Scraper
{
    using ContentType = WebComicEntry.ContentType;
    public class Page
    {

        public string Path { get; set; }

        /// <summary>
        /// Used only when <see cref="ContentType"/> is set to <see cref="ContentType.Mixed"/>
        /// </summary>
        public List<string> ImagesPath { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(ContentType.Text)]
        public ContentType Type { get; set; }

        public string Title { get; set; }

        public string PageUrl { get; set; }

        public int Order { get; set; }
    }
}
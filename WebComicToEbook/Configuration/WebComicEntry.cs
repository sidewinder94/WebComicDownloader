using System;
using System.Data.SqlTypes;

namespace WebComicToEbook.Configuration
{
    public class WebComicEntry
    {
        public String Parser = "XPath";

        public String BaseAddress = "";

        public String NextButtonSelector = "";

        public String ChapterTitleSelector = "";

        public String ChapterContentSelector = "";

        public String Author;

        public DateTime Date = DateTime.Now;

        public String Title;

        public String Description;
    }
}
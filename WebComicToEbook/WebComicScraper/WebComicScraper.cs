using System;
using System.Reflection;

using WebComicToEbook.Configuration;
using WebComicToEbook.Properties;

using EPubDocument = Epub.Document;
namespace WebComicToEbook.WebComicScraper
{
    public class WebComicScraper
    {
        private int _navCounter = 1;

        private int _pageCounter = 1;

        public void Scrape(WebComicEntry entry)
        {
            var ebook = new EPubDocument();
            ebook.AddStylesheetData("style.css", Resources.style);


        }

        private void SetMetadata(WebComicEntry entry, EPubDocument document)
        {
            document.AddAuthor(entry.Author);
            document.AddDescription(entry.Description);
            document.AddTitle(entry.Title);
        }
    }
}
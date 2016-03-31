using System;

using Epub;

using WebComicToEbook.Configuration;
using WebComicToEbook.EmbeddedResources;
using WebComicToEbook.Properties;
using HtmlAgilityPack;

namespace WebComicToEbook.Scraper
{
    public class HAPWebComicScraper : IWebComicScraper
    {
        private int _navCounter = 1;

        private int _pageCounter = 1;

        private string _pageTemplate = Resources.page.AsString();

        public void StartScraping(WebComicEntry entry)
        {
            var ebook = new Document();
            ebook.AddStylesheetData("style.css", Resources.style);
            ScrapeWebPage(entry, ebook);
            ebook.Generate($"{entry.Title}.epub");
            Console.WriteLine($"\nFinished Compiling book {entry.Title}");

        }

        private void ScrapeWebPage(WebComicEntry entry, Document ebook, string nextPageUrl = null)
        {
             //http://htmlagilitypack.codeplex.com/wikipage?title=Examples
            throw new NotImplementedException();
        }
    }
}
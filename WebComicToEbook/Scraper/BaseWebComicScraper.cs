using System;
using System.IO;
using System.Net;

using Epub;

using Utils.Text;

using WebComicToEbook.Configuration;
using WebComicToEbook.EmbeddedResources;
using WebComicToEbook.Properties;
using WebComicToEbook.Utils;

using EPubDocument = Epub.Document;

namespace WebComicToEbook.Scraper
{
    public abstract class BaseWebComicScraper
    {
        protected int _navCounter = 1;

        protected int _pageCounter = 1;

        protected string _pageTemplate = Resources.page.AsString();

        private WebComicEntry _entry;

        public void StartScraping(WebComicEntry entry)
        {
            this._entry = entry;
            var ebook = new Document();
            ebook.AddStylesheetData("style.css", Resources.style);
            SetMetadata(ebook);
            ScrapeWebPage(this._entry, ebook);
            String outputName = DetectBestName(this._entry.Title);
            ebook.Generate(outputName);
            ConsoleDisplay.AppendLine($"[{this._entry}] : Finished Compiling book");
        }

        private void SetMetadata(EPubDocument document)
        {
            document.AddAuthor(this._entry.Author);
            document.AddDescription(this._entry.Description);
            document.AddTitle(this._entry.Title);
        }

        protected void AddPage(EPubDocument ebook, string content, string title)
        {
            String page = this._pageTemplate.Replace("%%TITLE%%", title).Replace("%%CONTENT%%", content);

            title = WebUtility.HtmlDecode(title);
            String pageName = $"page{this._pageCounter}.xhtml";
            ebook.AddXhtmlData(pageName, page);
            ebook.AddNavPoint(title.IsEmpty() ? $"Chapter {this._pageCounter}" : title, pageName, this._navCounter++);

            ConsoleDisplay.AddMessageDisplay(this._entry, $"Completed Page {this._pageCounter}");
            this._pageCounter++;
        }

        private string DetectBestName(string baseName, int iter = 0)
        {
            String suffix = iter == 0 ? "" : $" ({iter})";
            String path = $"{baseName}{suffix}.epub";
            if (File.Exists(path))
            {
                if (Settings.Instance.Overwrite)
                {
                    File.Delete(path);
                }
                return DetectBestName(baseName, ++iter);
            }
            return path;
        }

        protected abstract void ScrapeWebPage(WebComicEntry entry, EPubDocument ebook, string nextPageUrl = null);
    }
}
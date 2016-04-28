using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Configuration;
using System.Threading.Tasks;
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

        protected int _imageCounter = 1;

        protected string _pageTemplate = Resources.page.AsString();

        private WebComicEntry _entry;

        public void StartScraping(WebComicEntry entry)
        {

            this._entry = entry;
            var ebook = new Document();
            bool existing = false;
            String outputName = DetectBestName(this._entry.Title, out existing);
            if (!existing || Settings.Instance.CommandLineOptions.Redownload)
            {
                ebook.AddStylesheetData("style.css", Resources.style);
                SetMetadata(ebook);
                ScrapeWebPage(this._entry, ebook);
                ebook.Generate(outputName);
                ConsoleDisplay.MainMessage(entry, "Finished Compiling book");
            }
            else
            {
                ConsoleDisplay.MainMessage(entry, "Book already compiled");
            }
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

            ConsoleDisplay.MainMessage(this._entry, $"Completed Page {this._pageCounter}");
            this._pageCounter++;
        }

        protected Bitmap DownloadImage(WebClient wc, MemoryStream ms, string url)
        {
            while (true)
            {
                try
                {
                    ms = new MemoryStream(wc.DownloadData(url));
                    var bm = new Bitmap(ms);
                    return bm;
                }
                catch (WebException ex)
                {
                    Task.Delay(1000).Wait();
                    ConsoleDisplay.AddAdditionalMessageDisplay(_entry, $"{url} : {ex.Message}");
                }
            }
        }

        protected void AddImage(EPubDocument ebook, WebClient wc, string url, string title = "")
        {
            using (MemoryStream memImg = new MemoryStream())
            {
                using (var ms = new MemoryStream())
                {
                    var img = this.DownloadImage(wc, ms, url);
                    img.Save(memImg, ImageFormat.Png);
                }
                ebook.AddImageData($"image{this._imageCounter}.png", memImg.GetBuffer());
            }
            String page = this._pageTemplate.Replace("%%TITLE%%", "").Replace("%%CONTENT%%", $"<img src=\"image{this._imageCounter}.png\" alt=\"\"/>");
            String pageName = $"page{this._pageCounter}.xhtml";
            ebook.AddXhtmlData(pageName, page);
            ebook.AddNavPoint(title.IsEmpty() ? $"Page {this._pageCounter}" : title, pageName, this._navCounter++);
            ConsoleDisplay.MainMessage(this._entry, $"Completed Page {this._pageCounter}");
            this._pageCounter++;
        }

        private string DetectBestName(string baseName, out bool existing, int iter = 0)
        {
            String suffix = iter == 0 ? "" : $" ({iter})";
            String path = $"{baseName}{suffix}.epub";
            if (File.Exists(path))
            {
                if (Settings.Instance.CommandLineOptions.Overwrite)
                {
                    File.Delete(path);
                    existing = false;
                    return path;
                }
                return DetectBestName(baseName, out existing, ++iter);
            }
            existing = iter != 0;
            return path;
        }

        protected abstract void ScrapeWebPage(WebComicEntry entry, EPubDocument ebook, string nextPageUrl = null);
    }
}
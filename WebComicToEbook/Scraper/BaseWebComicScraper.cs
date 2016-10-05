using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Text;
using System.Threading.Tasks;
using Epub;

using Microsoft.Win32.SafeHandles;

using Newtonsoft.Json;

using Utils.Text;
using static Utils.Misc.Misc;

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

        protected List<Page> Pages = new List<Page>();

        private string _workingDirPath;

        public void StartScraping(WebComicEntry entry)
        {

            this._entry = entry;
            var ebook = new Document();
            bool existing = false;
            string nextPageUrl = null;
            String outputName = DetectBestName(this._entry.Title, out existing);

            this._workingDirPath = Path.Combine(Settings.Instance.CommandLineOptions.SaveProgressFolder, entry.Title);

            if (!Settings.Instance.CommandLineOptions.SaveProgressFolder.IsEmpty() &&
                !Directory.Exists(this._workingDirPath))
            {
                Directory.CreateDirectory(
                    Path.Combine(this._workingDirPath));
            }

            if (File.Exists(Path.Combine(this._workingDirPath, "Pages.json")))
            {
                var temp =
                    JsonConvert.DeserializeObject<List<Page>>(
                        File.ReadAllText(Path.Combine(this._workingDirPath, "Pages.json")));
                if (temp.Any())
                {
                    this.Pages = temp;
                    nextPageUrl = this.RecoverProgress(ebook);
                }
            }

            if (!existing || Settings.Instance.CommandLineOptions.Redownload)
            {
                ebook.AddStylesheetData("style.css", Resources.style);
                SetMetadata(ebook);
                ScrapeWebPage(this._entry, ebook, nextPageUrl);
                ebook.Generate(outputName);
                ConsoleDisplay.MainMessage(entry, "Finished Compiling book");
            }
            else
            {
                ConsoleDisplay.MainMessage(entry, "Book already compiled");
            }
        }

        private string RecoverProgress(EPubDocument ebook)
        {
            var orderedPages = this.Pages.OrderBy(p => p.Order).ToList();
            foreach (var page in orderedPages)
            {
                if (page == orderedPages.Last())
                {
                    return page.PageUrl;
                }

                switch (page.Type)
                {
                    case WebComicEntry.ContentType.Image:
                        RecoverImagePage(ebook, page);
                        break;
                    case WebComicEntry.ContentType.Text:
                        RecoverTextPage(ebook, page);
                        break;
                    case WebComicEntry.ContentType.Mixed:
                        throw new NotImplementedException();//TODO : Implement Mixed Recovery
                    default:
                        break;
                }
            }

            return null;
        }

        private void RecoverTextPage(EPubDocument ebook, Page page)
        {
            String pageName = $"page{this._pageCounter}.xhtml";
            ebook.AddXhtmlData(pageName, File.ReadAllText(page.Path));
            ebook.AddNavPoint(page.Title.IsEmpty() ? $"Chapter {this._pageCounter}" : page.Title, pageName, this._navCounter++);

            ConsoleDisplay.MainMessage(this._entry, $"Completed Page {this._pageCounter}");
            this._pageCounter++;
        }

        private void RecoverImagePage(EPubDocument ebook, Page page)
        {
            var imageBytes = File.ReadAllBytes(page.Path);
            ebook.AddImageData($"image{this._imageCounter}.png", imageBytes);
            String p = this._pageTemplate.Replace("%%TITLE%%", "").Replace("%%CONTENT%%", $"<img src=\"image{this._imageCounter}.png\" alt=\"\"/>");
            String pageName = $"page{this._pageCounter}.xhtml";
            ebook.AddXhtmlData(pageName, p);
            ebook.AddNavPoint(page.Title.IsEmpty() ? $"Page {this._pageCounter}" : page.Title, pageName, this._navCounter++);
            ConsoleDisplay.MainMessage(this._entry, $"Completed Page {this._pageCounter}");
            this._pageCounter++;
            this._imageCounter++;
        }

        private void SetMetadata(EPubDocument document)
        {
            document.AddAuthor(this._entry.Author);
            document.AddDescription(this._entry.Description);
            document.AddTitle(this._entry.Title);
        }

        protected void AddPage(EPubDocument ebook, string content, string title, string currentUrl)
        {
            String page = this._pageTemplate.Replace("%%TITLE%%", title).Replace("%%CONTENT%%", content);

            title = WebUtility.HtmlDecode(title);
            String pageName = $"page{this._pageCounter}.xhtml";
            ebook.AddXhtmlData(pageName, page);
            ebook.AddNavPoint(title.IsEmpty() ? $"Chapter {this._pageCounter}" : title, pageName, this._navCounter++);

            ConsoleDisplay.MainMessage(this._entry, $"Completed Page {this._pageCounter}");


            Unless(Settings.Instance.CommandLineOptions.SaveProgressFolder.IsEmpty(),
                () =>
                    {
                        var pagesDir = Path.Combine(this._workingDirPath, this._entry.Title, "Pages");
                        Unless(Directory.Exists(pagesDir), () => Directory.CreateDirectory(pagesDir));

                        var pagePath = Path.Combine(pagesDir, pageName);

                        this.Pages.Add(new Page()
                        {
                            Title = title,
                            Path = pagePath,
                            Type = WebComicEntry.ContentType.Text,
                            PageUrl = currentUrl,
                            Order = this._pageCounter
                        });

                        File.WriteAllText(pagePath, page);

                        File.WriteAllText(
                            Path.Combine(this._workingDirPath, "Pages.json"),
                            JsonConvert.SerializeObject(this.Pages));
                    });

            this._pageCounter++;
        }

        protected void DownloadImage(WebClient wc, MemoryStream destinationStream, string url)
        {
            while (true)
            {
                try
                {
                    using (var ms = new MemoryStream(wc.DownloadData(url)))
                    {
                        var bm = new Bitmap(ms);
                        bm.Save(destinationStream, ImageFormat.Png);
                        return;
                    }
                }
                catch (WebException ex)
                {
                    Task.Delay(1000).Wait();
                    ConsoleDisplay.AddAdditionalMessageDisplay(_entry, $"{url} : {ex.Message}");
                }
            }
        }

        protected void AddImage(EPubDocument ebook, WebClient wc, string url, string pageUrl, string title = "")
        {
            using (MemoryStream memImg = new MemoryStream())
            {
                this.DownloadImage(wc, memImg, url);
                ebook.AddImageData($"image{this._imageCounter}.png", memImg.GetBuffer());

                if (!Settings.Instance.CommandLineOptions.SaveProgressFolder.IsEmpty())
                {
                    var pagesDir = Path.Combine(this._workingDirPath, this._entry.Title, "Images");
                    Unless(Directory.Exists(pagesDir), () => Directory.CreateDirectory(pagesDir));

                    var pagePath = Path.Combine(pagesDir, $"image{this._imageCounter}.png");

                    this.Pages.Add(new Page()
                    {
                        Title = title,
                        Path = pagePath,
                        Type = WebComicEntry.ContentType.Image,
                        PageUrl = pageUrl,
                        Order = this._pageCounter
                    });

                    File.WriteAllBytes(pagePath, memImg.GetBuffer());

                    File.WriteAllText(
                            Path.Combine(this._workingDirPath, "Pages.json"),
                            JsonConvert.SerializeObject(this.Pages));
                }
            }
            String page = this._pageTemplate.Replace("%%TITLE%%", "").Replace("%%CONTENT%%", $"<img src=\"image{this._imageCounter}.png\" alt=\"\"/>");
            String pageName = $"page{this._pageCounter}.xhtml";
            ebook.AddXhtmlData(pageName, page);
            ebook.AddNavPoint(title.IsEmpty() ? $"Page {this._pageCounter}" : title, pageName, this._navCounter++);
            ConsoleDisplay.MainMessage(this._entry, $"Completed Page {this._pageCounter}");
            this._pageCounter++;
            this._imageCounter++;
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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Epub;

using Newtonsoft.Json;

using Utils.Text;

using WebComicToEbook.Configuration;
using WebComicToEbook.EmbeddedResources;
using WebComicToEbook.Properties;
using WebComicToEbook.Utils;

using static Utils.Misc.Misc;

using EPubDocument = Epub.Document;

namespace WebComicToEbook.Scraper
{
    public abstract class BaseWebComicScraper
    {
        protected int NavCounter = 1;

        protected int PageCounter = 1;

        protected int ImageCounter = 1;

        protected readonly string PageTemplate = Resources.page.AsString();

        private WebComicEntry _entry;

        protected List<Page> Pages = new List<Page>();

        private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars().Union(Path.GetInvalidPathChars()).ToArray();

        private string _workingDirPath;

        protected string WorkingDirPath => this._workingDirPath;

        private string _sanitizedName;

        public void StartScraping(WebComicEntry entry)
        {

            this._entry = entry;
            var ebook = new Document();
            bool existing;
            string nextPageUrl = null;

            this._sanitizedName = new string(entry.Title.Where(c => !InvalidChars.Contains(c)).ToArray());

            string outputName = this.DetectBestName(this._sanitizedName, out existing);

            this._workingDirPath = Path.Combine(Settings.Instance.CommandLineOptions.SaveProgressFolder, this._sanitizedName);

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
                this.SetMetadata(ebook);
                this.ScrapeWebPage(this._entry, ebook, nextPageUrl);
                ebook.Generate(outputName);
                ConsoleDisplay.MainMessage(entry, "Finished Compiling book");
            }
            else
            {
                ConsoleDisplay.MainMessage(entry, "Book already compiled");
            }
        }

        private string RecoverProgress(Document ebook)
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
                        this.RecoverImagePage(ebook, page);
                        break;
                    case WebComicEntry.ContentType.Text:
                        this.RecoverTextPage(ebook, page);
                        break;
                    case WebComicEntry.ContentType.Mixed:
                        this.RecoverCompositePage(ebook, page);
                        break;
                }
            }

            return null;
        }

        private void RecoverCompositePage(Document ebook, Page page)
        {
            string pageName = $"page{this.PageCounter}.xhtml";
            ebook.AddXhtmlData(pageName, File.ReadAllText(page.Path));
            ebook.AddNavPoint(page.Title.IsEmpty() ? $"Chapter {this.PageCounter}" : page.Title, pageName, this.NavCounter++);

            page.ImagesPath.ForEach(ip =>
            {
                var imageBytes = File.ReadAllBytes(ip);
                ebook.AddImageData($"image{this.ImageCounter}.png", imageBytes);
                this.ImageCounter++;
            });

            ConsoleDisplay.MainMessage(this._entry, $"Completed Page {this.PageCounter}");
            this.PageCounter++;
        }

        private void RecoverTextPage(Document ebook, Page page)
        {
            string pageName = $"page{this.PageCounter}.xhtml";
            ebook.AddXhtmlData(pageName, File.ReadAllText(page.Path));
            ebook.AddNavPoint(page.Title.IsEmpty() ? $"Chapter {this.PageCounter}" : page.Title, pageName, this.NavCounter++);

            ConsoleDisplay.MainMessage(this._entry, $"Completed Page {this.PageCounter}");
            this.PageCounter++;
        }

        private void RecoverImagePage(Document ebook, Page page)
        {
            var imageBytes = File.ReadAllBytes(page.Path);
            ebook.AddImageData($"image{this.ImageCounter}.png", imageBytes);
            string p = this.PageTemplate.Replace("%%TITLE%%", string.Empty).Replace("%%CONTENT%%", $"<img src=\"image{this.ImageCounter}.png\" alt=\"\"/>");
            string pageName = $"page{this.PageCounter}.xhtml";
            ebook.AddXhtmlData(pageName, p);
            ebook.AddNavPoint(page.Title.IsEmpty() ? $"Page {this.PageCounter}" : page.Title, pageName, this.NavCounter++);
            ConsoleDisplay.MainMessage(this._entry, $"Completed Page {this.PageCounter}");
            this.PageCounter++;
            this.ImageCounter++;
        }

        private void SetMetadata(Document document)
        {
            document.AddAuthor(this._entry.Author);
            document.AddDescription(this._entry.Description);
            document.AddTitle(this._entry.Title);
        }

        protected void AddPage(Document ebook, string content, string title, string currentUrl, bool ignoreMissingChapterName = false)
        {
            string page = this.PageTemplate.Replace("%%TITLE%%", title).Replace("%%CONTENT%%", content);

            title = WebUtility.HtmlDecode(title);
            string pageName = $"page{this.PageCounter}.xhtml";
            ebook.AddXhtmlData(pageName, page);

            if ((title == null && !ignoreMissingChapterName) || title != null)
            {
                ebook.AddNavPoint(title.IsEmpty() ? $"Chapter {this.PageCounter}" : title, pageName, this.NavCounter++);
            }

            ConsoleDisplay.MainMessage(this._entry, $"Completed Page {this.PageCounter}");


            Unless(Settings.Instance.CommandLineOptions.SaveProgressFolder.IsEmpty(),
                () =>
                    {
                        var pagesDir = Path.Combine(this._workingDirPath, this._sanitizedName, "Pages");
                        Unless(Directory.Exists(pagesDir), () => Directory.CreateDirectory(pagesDir));

                        var pagePath = Path.Combine(pagesDir, pageName);

                        this.Pages.Add(new Page()
                        {
                            Title = title,
                            Path = pagePath,
                            Type = WebComicEntry.ContentType.Text,
                            PageUrl = currentUrl,
                            Order = this.PageCounter
                        });

                        File.WriteAllText(pagePath, page);

                        File.WriteAllText(
                            Path.Combine(this._workingDirPath, "Pages.json"),
                            JsonConvert.SerializeObject(this.Pages));
                    });

            this.PageCounter++;
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
                    ConsoleDisplay.AddAdditionalMessageDisplay(this._entry, $"{url} : {ex.Message}");
                }
            }
        }

        protected void AddImage(Document ebook, WebClient wc, string url, string pageUrl, string title = "")
        {
            using (MemoryStream memImg = new MemoryStream())
            {
                this.DownloadImage(wc, memImg, url);
                ebook.AddImageData($"image{this.ImageCounter}.png", memImg.GetBuffer());

                if (!Settings.Instance.CommandLineOptions.SaveProgressFolder.IsEmpty())
                {
                    var pagesDir = Path.Combine(this._workingDirPath, this._sanitizedName, "Images");
                    Unless(Directory.Exists(pagesDir), () => Directory.CreateDirectory(pagesDir));

                    var pagePath = Path.Combine(pagesDir, $"image{this.ImageCounter}.png");

                    this.Pages.Add(new Page()
                    {
                        Title = title,
                        Path = pagePath,
                        Type = WebComicEntry.ContentType.Image,
                        PageUrl = pageUrl,
                        Order = this.PageCounter
                    });

                    File.WriteAllBytes(pagePath, memImg.GetBuffer());

                    File.WriteAllText(
                            Path.Combine(this._workingDirPath, "Pages.json"),
                            JsonConvert.SerializeObject(this.Pages));
                }
            }

            string page = this.PageTemplate.Replace("%%TITLE%%", string.Empty).Replace("%%CONTENT%%", $"<img src=\"image{this.ImageCounter}.png\" alt=\"\"/>");
            string pageName = $"page{this.PageCounter}.xhtml";
            ebook.AddXhtmlData(pageName, page);
            ebook.AddNavPoint(title.IsEmpty() ? $"Page {this.PageCounter}" : title, pageName, this.NavCounter++);
            ConsoleDisplay.MainMessage(this._entry, $"Completed Page {this.PageCounter}");
            this.PageCounter++;
            this.ImageCounter++;
        }

        private string DetectBestName(string baseName, out bool existing, int iter = 0)
        {
            string suffix = iter == 0 ? string.Empty : $" ({iter})";
            string path = $"{baseName}{suffix}.epub";
            if (File.Exists(path))
            {
                if (Settings.Instance.CommandLineOptions.Overwrite)
                {
                    File.Delete(path);
                    existing = false;
                    return path;
                }

                return this.DetectBestName(baseName, out existing, ++iter);
            }

            existing = iter != 0;
            return path;
        }

        protected abstract void ScrapeWebPage(WebComicEntry entry, Document ebook, string nextPageUrl = null);
    }
}
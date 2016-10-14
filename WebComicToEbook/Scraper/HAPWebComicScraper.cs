using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.XPath;

using Epub;

using HtmlAgilityPack;
using Newtonsoft.Json;
using Utils.Text;

using WebComicToEbook.Configuration;
using WebComicToEbook.EmbeddedResources;
using WebComicToEbook.Properties;
using WebComicToEbook.Utils;
using EPubDocument = Epub.Document;

using static Utils.Misc.Misc;

namespace WebComicToEbook.Scraper
{
    public class HAPWebComicScraper : BaseWebComicScraper
    {
        // Valid JSON configuration for this class
        // {
        // "Parser": "XPath",
        // "BaseAddress": "http://beyondtheimpossible.org/comic/1-before-the-beginning-2/",
        // "NextButtonSelector": "//@href[@class='comic-nav-base comic-nav-next']",
        // "ChapterTitleSelector": "//*[@class='post-title']",
        // "ChapterContentSelector": "//*[@class='entry']",
        // "Author": "Ffurla",
        // "Date": "2016-03-18T13:24:36.2855417+01:00",
        // "Title": "Beyond the Impossible",
        // "Description": null
        // }
        protected override void ScrapeWebPage(WebComicEntry entry, Document ebook, string nextPageUrl = null)
        {
            // http://htmlagilitypack.codeplex.com/wikipage?title=Examples
            do
            {
                string content = string.Empty;
                string title;
                var currentUrl = nextPageUrl ?? entry.BaseAddress;
                try
                {
                    using (var wc = new WebClient())
                    {
                        using (var ms = new MemoryStream(wc.DownloadData(currentUrl)))
                        {
                            HtmlDocument hDoc = new HtmlDocument();
                            hDoc.Load(ms, true);
                            XPathNavigator xNav = hDoc.CreateNavigator();

                            try
                            {
                                title = xNav.SelectSingleNode(entry.ChapterTitleSelector).Value;
                            }
                            catch
                            {
                                ConsoleDisplay.AddAdditionalMessageDisplay(
                                    entry,
                                    $"Title not found for page {this._pageCounter}, replacing with default value");
                                title = WebUtility.HtmlEncode($"Chapter - {this._pageCounter}");
                            }

                            XPathNodeIterator xIter = xNav.Select(entry.ChapterContentSelector);

                            if (entry.Content == WebComicEntry.ContentType.Text)
                            {
                                content += $"<h1>{title}</h1>";

                                while (xIter.MoveNext())
                                {
                                    var temp = $"<{xIter.Current.Name}>{xIter.Current.Value}</{xIter.Current.Name}>";
                                    content += temp;
                                }

                                this.AddPage(ebook, content, title, currentUrl);
                            }
                            else if (entry.Content == WebComicEntry.ContentType.Image)
                            {
                                while (xIter.MoveNext())
                                {
                                    this.AddImage(ebook, wc, xIter.Current.Value, currentUrl);
                                }
                            }
                            else if (entry.Content == WebComicEntry.ContentType.Mixed)
                            {
                                while (xIter.MoveNext())
                                {
                                    var subIter = xIter.Current.SelectChildren(XPathNodeType.Element);
                                    this.AddCompositePage(ebook, subIter, title, wc, currentUrl, entry);
                                }
                            }

                            nextPageUrl = xNav.SelectSingleNode(entry.NextButtonSelector)?.Value;
                        }
                    }
                }
                catch (WebException ex)
                {
                    if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotFound)
                    {
                        return;
                    }
                }
            }
            while (!nextPageUrl.IsEmpty());
        }

        protected void AddCompositePage(EPubDocument ebook, XPathNodeIterator iter, string title, WebClient wc, string currentUrl, WebComicEntry entry)
        {
            var page = this._pageTemplate.Replace("%%TITLE%%", title);
            var content = string.Empty;
            title = WebUtility.HtmlDecode(title);

            var pagesDir = Path.Combine(this.WorkingDirPath, entry.Title, "Pages");
            var imagesDir = Path.Combine(this.WorkingDirPath, entry.Title, "Images");
            var p = new Page()
            {
                Title = title,
                Order = this._pageCounter,
                Type = WebComicEntry.ContentType.Mixed,
                PageUrl = currentUrl,
                ImagesPath = new List<string>()
            };

            while (iter.MoveNext())
            {
                if (entry.IncludeTags.Contains(iter.Current.Name))
                {
                    content = HandleMixedContent(ebook, iter, wc, imagesDir, p, content, entry);
                }
                else if (entry.InteruptAtTag != iter.Current.Name)
                {
                    var exprBuilder = new StringBuilder();
                    foreach (var tag in entry.IncludeTags)
                    {
                        exprBuilder.Append($".//{tag}|");
                    }

                    var subIter = iter.Current.Select(exprBuilder.ToString().TrimEnd('|'));

                    while (subIter.MoveNext())
                    {
                        content = HandleMixedContent(ebook, subIter, wc, imagesDir, p, content, entry);
                    }
                }

                //On break sur le tag indiqué
                if (entry.InteruptAtTag == iter.Current.Name) break;
            }

            page = page.Replace("%%CONTENT%%", content);

            String pageName = $"page{this._pageCounter}.xhtml";
            if (!Settings.Instance.CommandLineOptions.SaveProgressFolder.IsEmpty())
            {
                Unless(Directory.Exists(pagesDir), () => Directory.CreateDirectory(pagesDir));
                var pagePath = Path.Combine(pagesDir, pageName);
                p.Path = pagePath;
                File.WriteAllText(pagePath, page);

                this.Pages.Add(p);

                File.WriteAllText(
                    Path.Combine(this.WorkingDirPath, "Pages.json"),
                    JsonConvert.SerializeObject(this.Pages));
            }

            ebook.AddXhtmlData(pageName, page);
            ebook.AddNavPoint(title.IsEmpty() ? $"Page {this._pageCounter}" : title, pageName, this._navCounter++);
            ConsoleDisplay.MainMessage(entry, $"Completed Page {this._pageCounter}");
            this._pageCounter++;
        }

        private string HandleMixedContent(Document ebook, XPathNodeIterator iter, WebClient wc, string imagesDir, Page p, string content, WebComicEntry entry)
        {
            if (entry.ImageTags.Contains(iter.Current.Name))
            {
                string url = null;
                if (entry.ImageSourceAttributes[iter.Current.Name] == ".")
                {
                    url = iter.Current.Value;
                }
                else
                {
                    url = iter.Current.GetAttribute(entry.ImageSourceAttributes[iter.Current.Name], "");
                }

                using (MemoryStream memImg = new MemoryStream())
                {
                    this.DownloadImage(wc, memImg, url);
                    ebook.AddImageData($"image{this._imageCounter}.png", memImg.GetBuffer());

                    if (!Settings.Instance.CommandLineOptions.SaveProgressFolder.IsEmpty())
                    {
                        Unless(Directory.Exists(imagesDir), () => Directory.CreateDirectory(imagesDir));
                        var imagePath = Path.Combine(
                            imagesDir,
                            $"image{this._imageCounter}.png");
                        File.WriteAllBytes(imagePath, memImg.GetBuffer());
                        p.ImagesPath.Add(imagePath
                        );
                    }
                }

                //Image processing
                var temp = $"<img src=\"image{this._imageCounter}.png\" alt=\"\"/>";
                content += temp;
                this._imageCounter++;
            }
            else
            {
                //Text processing
                var temp = $"<{iter.Current.Name}>{iter.Current.Value}</{iter.Current.Name}>";
                content += temp;
            }
            return content;
        }
    }
}
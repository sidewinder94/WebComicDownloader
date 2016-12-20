using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.XPath;

using Epub;

using HtmlAgilityPack;

using Newtonsoft.Json;

using static Utils.Misc.Misc;
using Utils.Text;

using WebComicToEbook.Configuration;
using WebComicToEbook.Utils;

using EPubDocument = Epub.Document;

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
                                if (entry.IgnoreMissingChapterName)
                                {
                                    title = null;
                                }
                                else
                                {
                                    ConsoleDisplay.AddAdditionalMessageDisplay(
                                        entry,
                                        $"Title not found for page {this.PageCounter}, replacing with default value");
                                    title = WebUtility.HtmlEncode($"Chapter - {this.PageCounter}");
                                }
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

                                this.AddPage(ebook, content, title, currentUrl, entry.IgnoreMissingChapterName);
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

                            var tempNextPageUrl = xNav.SelectSingleNode(entry.NextButtonSelector)?.Value;

                            try
                            {
                                var uri = new Uri(tempNextPageUrl);
                                nextPageUrl = tempNextPageUrl;
                            }
                            catch (UriFormatException)
                            {
                                nextPageUrl = string.Format(entry.AddressPattern, tempNextPageUrl);
                            }
                            catch (ArgumentNullException)
                            {
                                //The end of the book.....
                                return;
                            }
                            catch (NullReferenceException)
                            {
                                //The end of the book.....
                                return;
                            }
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

        protected void AddCompositePage(
            Document ebook,
            XPathNodeIterator iter,
            string title,
            WebClient wc,
            string currentUrl,
            WebComicEntry entry)
        {
            var page = this.PageTemplate.Replace("%%TITLE%%", title);
            var content = string.Empty;
            title = WebUtility.HtmlDecode(title);

            var pagesDir = Path.Combine(this.WorkingDirPath, entry.Title, "Pages");
            var imagesDir = Path.Combine(this.WorkingDirPath, entry.Title, "Images");
            var p = new Page()
            {
                Title = title,
                Order = this.PageCounter,
                Type = WebComicEntry.ContentType.Mixed,
                PageUrl = currentUrl,
                ImagesPath = new List<string>()
            };

            while (iter.MoveNext())
            {
                if (entry.IncludeTags.Contains(iter.Current.Name))
                {
                    content = this.HandleMixedContent(ebook, iter, wc, imagesDir, p, content, entry);
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
                        content = this.HandleMixedContent(ebook, subIter, wc, imagesDir, p, content, entry);
                    }
                }

                // On break sur le tag indiqué
                if (entry.InteruptAtTag == iter.Current.Name) break;
            }

            page = page.Replace("%%CONTENT%%", content);

            string pageName = $"page{this.PageCounter}.xhtml";
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
            ebook.AddNavPoint(title.IsEmpty() ? $"Page {this.PageCounter}" : title, pageName, this.NavCounter++);
            ConsoleDisplay.MainMessage(entry, $"Completed Page {this.PageCounter}");
            this.PageCounter++;
        }

        private string HandleMixedContent(
            Document ebook,
            XPathNodeIterator iter,
            WebClient wc,
            string imagesDir,
            Page p,
            string content,
            WebComicEntry entry)
        {
            if (entry.ImageTags.Contains(iter.Current.Name))
            {
                string url;

                // If the image link is in the value of the tag instead of an attibute, a dot should be used to indicate that
                if (entry.ImageSourceAttributes[iter.Current.Name] == ".")
                {
                    url = iter.Current.Value;
                }
                else
                {
                    url = iter.Current.GetAttribute(entry.ImageSourceAttributes[iter.Current.Name], string.Empty);
                }

                using (MemoryStream memImg = new MemoryStream())
                {
                    this.DownloadImage(wc, memImg, url);
                    ebook.AddImageData($"image{this.ImageCounter}.png", memImg.GetBuffer());

                    if (!Settings.Instance.CommandLineOptions.SaveProgressFolder.IsEmpty())
                    {
                        Unless(Directory.Exists(imagesDir), () => Directory.CreateDirectory(imagesDir));
                        var imagePath = Path.Combine(imagesDir, $"image{this.ImageCounter}.png");
                        File.WriteAllBytes(imagePath, memImg.GetBuffer());
                        p.ImagesPath.Add(imagePath);
                    }
                }

                // Image processing
                var temp = $"<img src=\"image{this.ImageCounter}.png\" alt=\"\"/>";
                content += temp;
                this.ImageCounter++;
            }
            else
            {
                // Text processing
                var temp = $"<{iter.Current.Name}>{iter.Current.Value}</{iter.Current.Name}>";
                content += temp;
            }

            return content;
        }
    }
}
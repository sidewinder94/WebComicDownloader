using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Xml.XPath;

using Epub;

using HtmlAgilityPack;

using Utils.Text;

using WebComicToEbook.Configuration;
using WebComicToEbook.EmbeddedResources;
using WebComicToEbook.Properties;
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
                                    this.AddCompositePage(ebook, subIter, title, wc, currentUrl);
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
    }
}
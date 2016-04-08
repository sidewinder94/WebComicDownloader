using System;
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
                try
                {
                    using (var wc = new WebClient())
                    {
                        using (var ms = new MemoryStream(wc.DownloadData(nextPageUrl ?? entry.BaseAddress)))
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
                                    $"Title not found for page {_pageCounter}, replacing with default value");
                                title = WebUtility.HtmlEncode($"Chapter - {_pageCounter}");
                            }

                            content += $"<h1>{title}</h1>";
                            XPathNodeIterator xIter = xNav.Select(entry.ChapterContentSelector);
                            while (xIter.MoveNext())
                            {
                                var temp = $"<{xIter.Current.Name}>{xIter.Current.Value}</{xIter.Current.Name}>";
                                content += temp;
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
                        continue;
                }

                AddPage(ebook, content, title);
            }
            while (!nextPageUrl.IsEmpty());
        }
    }
}
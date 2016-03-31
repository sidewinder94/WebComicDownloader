using System;
using System.IO;
using System.Net;
using System.Xml.XPath;

using Epub;

using WebComicToEbook.Configuration;
using WebComicToEbook.EmbeddedResources;
using WebComicToEbook.Properties;
using HtmlAgilityPack;

using Utils.Text;

using static Utils.Misc.Misc;
using EPubDocument = Epub.Document;

namespace WebComicToEbook.Scraper
{
    public class HAPWebComicScraper : BaseWebComicScraper
    {
        //Valid JSON configuration for this class
        //{
        //    "Parser": "XPath",
        //    "BaseAddress": "http://beyondtheimpossible.org/comic/1-before-the-beginning-2/",
        //    "NextButtonSelector": "//@href[@class='comic-nav-base comic-nav-next']",
        //    "ChapterTitleSelector": "//*[@class='post-title']",
        //    "ChapterContentSelector": "//*[@class='entry']",
        //    "Author": "Ffurla",
        //    "Date": "2016-03-18T13:24:36.2855417+01:00",
        //    "Title": "Beyond the Impossible",
        //    "Description": null
        //}

        protected override void ScrapeWebPage(WebComicEntry entry, Document ebook, string nextPageUrl = null)
        {
            //http://htmlagilitypack.codeplex.com/wikipage?title=Examples
            HtmlDocument hDoc = new HtmlDocument();
            String content = "";
            String title = "";
            using (var wc = new WebClient())
            {
                using (var ms = new MemoryStream(wc.DownloadData(nextPageUrl ?? entry.BaseAddress)))
                {
                    hDoc.Load(ms, true);
                    XPathNavigator xNav = hDoc.CreateNavigator();
                    title = xNav.SelectSingleNode(entry.ChapterTitleSelector).Value;
                    content += $"<h1>{title}</h1>";
                    XPathNodeIterator xIter= xNav.Select(entry.ChapterContentSelector);
                    while (xIter.MoveNext())
                    {
                        var temp = $"<{xIter.Current.Name}>{xIter.Current.Value}</{xIter.Current.Name}>";
                        content += temp;
                    }
                    nextPageUrl = xNav.SelectSingleNode(entry.NextButtonSelector).Value;
                }
            }

            AddPage(ebook, content, title);


            Unless(nextPageUrl.IsEmpty(), () => ScrapeWebPage(entry, ebook, nextPageUrl));
        }
    }
}
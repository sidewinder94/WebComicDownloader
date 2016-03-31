using System;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

using Utils.Text;
using WebComicToEbook.Configuration;
using WebComicToEbook.EmbeddedResources;
using WebComicToEbook.Properties;
using static Utils.Misc.Misc;

using EPubDocument = Epub.Document;
namespace WebComicToEbook.Scraper
{
    public class RegExpWebComicScraper : BaseWebComicScraper
    {
        //Example Valid JSON configuration for the class
        //       {
        //    "Parser": "RegExp",
        //    "BaseAddress": "http://beyondtheimpossible.org/comic/1-before-the-beginning-2/",
        //    "NextButtonSelector": "(?:href=\"(\\S+)\")? class=\"comic-nav-base comic-nav-next\">",
        //    "ChapterTitleSelector": "class=\"post-title\">([^<]*)<",
        //    "ChapterContentSelector": "<div class=\"entry\">((?:.|\n)*)<div class=\"post-extras\">",
        //    "Author": "Ffurla",
        //    "Date": "2016-03-18T13:24:36.2855417+01:00",
        //    "Title": "Beyond the Impossible",
        //    "Description": null
        //},

        protected override void ScrapeWebPage(WebComicEntry entry, EPubDocument ebook, string nextPageUrl = null)
        {
            String title = "";
            String content = "";
            String nextUrl = "";

            using (var wc = new WebClient()
            {
                Encoding = Encoding.UTF8
            })
            {
                var s = WebUtility.HtmlDecode(wc.DownloadString(nextPageUrl ?? entry.BaseAddress));

                var m = Regex.Match(s, entry.NextButtonSelector);
                if (m.Groups[1].Success)
                {
                    nextUrl = m.Groups[1].Value;
                }
                m = Regex.Match(s, entry.ChapterTitleSelector);
                if (m.Groups[1].Success)
                {
                    title = m.Groups[1].Value;
                    content = $"<h1>{title}</h1>";
                }

                m = Regex.Match(s, entry.ChapterContentSelector);
                if (m.Groups[1].Success)
                {
                    var v = m.Groups[1].Value;
                    content += v.Remove(v.LastIndexOf("</div>"));
                }

                this.AddPage(ebook, content, title);

                Unless(nextUrl.IsEmpty(), () => ScrapeWebPage(entry, ebook, nextUrl));
            }
        }

        
    }
}
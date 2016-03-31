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
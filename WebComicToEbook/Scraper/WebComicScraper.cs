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
    public class WebComicScraper
    {
        private int _navCounter = 1;

        private int _pageCounter = 1;

        private string _pageTemplate = Resources.page.AsString();

        public void StartScraping(WebComicEntry entry)
        {
            var ebook = new EPubDocument();
            ebook.AddStylesheetData("style.css", Resources.style);
            ScrapeWebPage(entry, ebook);
            ebook.Generate($"{entry.Title}.epub");
            Console.WriteLine($"\nFinished Compiling book {entry.Title}");

        }

        public void ScrapeWebPage(WebComicEntry entry, EPubDocument ebook, string nextPageUrl = null)
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

                String page = _pageTemplate.Replace("%%CONTENT%%", content);
                String pageName = $"page{_pageCounter}.xhtml";
                ebook.AddXhtmlData(pageName, page);
                ebook.AddNavPoint(title.IsEmpty() ? $"Chapter {_pageCounter}" : title, pageName, _navCounter++);
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"Completed Page {_pageCounter}");
                _pageCounter++;


                Unless(nextUrl.IsEmpty(), () => ScrapeWebPage(entry, ebook, nextUrl));
            }
        }


        private void SetMetadata(WebComicEntry entry, EPubDocument document)
        {
            document.AddAuthor(entry.Author);
            document.AddDescription(entry.Description);
            document.AddTitle(entry.Title);
        }
    }
}
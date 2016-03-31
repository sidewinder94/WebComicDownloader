using WebComicToEbook.Configuration;

namespace WebComicToEbook.Scraper
{
    public interface IWebComicScraper
    {
        void StartScraping(WebComicEntry entry);
    }
}
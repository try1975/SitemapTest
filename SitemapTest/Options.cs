using Plossum.CommandLine;

namespace SitemapTest
{
    [CommandLineManager(ApplicationName = "Link collector",
        Copyright = "Copyright (c) Kaukf")]
    internal class Options
    {
        private string _url;

        [CommandLineOption(Description = "Displays this help text")] public bool Help = false;

        [CommandLineOption(Description = "Skip crawl links")] public bool SkipCrawl = false;

        [CommandLineOption(Description = "Skip load links from sitemap")] public bool SkipSitemap = false;

        [CommandLineOption(Description = "Use Simple Crawler")] public bool UseSimpleCrawler = false;

        [CommandLineOption(Description = "Specifies the input url", MinOccurs = 1)]
        public string Url
        {
            get { return _url; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new InvalidOptionValueException(
                        "The url must not be empty", false);
                _url = value;
            }
        }
    }
}
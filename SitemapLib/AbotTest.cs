using System;
using System.Net;
using Abot.Crawler;

namespace SitemapLib
{
    public class AbotTest
    {
        private readonly ILinkStorage _linkStorage;
        private readonly string _url;

        public AbotTest(string url, ILinkStorage linkStorage)
        {
            _linkStorage = linkStorage ?? new ConsoleLinkStorage();
            _url = url;
        }

        public void Execute()
        {
            var crawler = new PoliteWebCrawler();
            crawler.PageCrawlStartingAsync += crawler_ProcessPageCrawlStarting;
            crawler.PageCrawlCompletedAsync += crawler_ProcessPageCrawlCompleted;
            //crawler.PageCrawlDisallowedAsync += crawler_PageCrawlDisallowed;
            //crawler.PageLinksCrawlDisallowedAsync += crawler_PageLinksCrawlDisallowed;

            var result = crawler.Crawl(new Uri(_url));
            //This is synchronous, it will not go to the next line until the crawl has completed

            Console.WriteLine(
                result.ErrorOccurred
                    ? $"Crawl of {result.RootUri.AbsoluteUri} completed with error: {result.ErrorException.Message}"
                    : $"Crawl of {result.RootUri.AbsoluteUri} completed without error.");
        }

        private void crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            var pageToCrawl = e.PageToCrawl;
            //Console.WriteLine("About to crawl link {0} which was found on page {1}", pageToCrawl.Uri.AbsoluteUri, pageToCrawl.ParentUri.AbsoluteUri);
        }

        private void crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            var crawledPage = e.CrawledPage;

            if (crawledPage.WebException != null || crawledPage.HttpWebResponse.StatusCode != HttpStatusCode.OK)
            {
                //Console.WriteLine("Crawl of page failed {0}", crawledPage.Uri.AbsoluteUri);
            }

            if (string.IsNullOrEmpty(crawledPage.Content.Text))
            {
                //Console.WriteLine("Page had no content {0}", crawledPage.Uri.AbsoluteUri);
            }
            else
            {
                //uriList.Add(crawledPage.Uri.AbsoluteUri);
                _linkStorage.TryAdd(crawledPage.Uri.AbsoluteUri);
            }

            //var htmlAgilityPackDocument = crawledPage.HtmlDocument; //Html Agility Pack parser
            //var angleSharpHtmlDocument = crawledPage.AngleSharpHtmlDocument; //AngleSharp parser
        }

        private void crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            var pageToCrawl = e.PageToCrawl;
            Console.WriteLine("Did not crawl page {0} due to {1}", pageToCrawl.Uri.AbsoluteUri, e.DisallowedReason);
        }

        private void crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            var crawledPage = e.CrawledPage;
            Console.WriteLine("Did not crawl the links on page {0} due to {1}", crawledPage.Uri.AbsoluteUri,
                e.DisallowedReason);
        }
    }
}
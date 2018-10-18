using SimpleCrawler;

namespace SitemapLib
{
    public class SimpleCrawlerTest
    {
        private static readonly CrawlSettings Settings = new CrawlSettings();
        private readonly ILinkStorage _linkStorage;
        private readonly CrawlMaster _master;

        public SimpleCrawlerTest(string url, ILinkStorage linkStorage)
        {
            _linkStorage = linkStorage ?? new ConsoleLinkStorage();
            Settings.SeedsAddress.Add(url);
            //Settings.ThreadCount = 20;
            Settings.Depth = 5;
            Settings.EscapeLinks.Add(".jpg");
            Settings.EscapeLinks.Add(".gif");
            Settings.EscapeLinks.Add(".png");
            Settings.EscapeLinks.Add(".pdf");
            Settings.EscapeLinks.Add(".doc");
            Settings.EscapeLinks.Add(".xls");
            Settings.AutoSpeedLimit = true;
            _master = new CrawlMaster(Settings);
            _master.AddUrlEvent += MasterAddUrlEvent;
            _master.DataReceivedEvent += MasterDataReceivedEvent;
        }

        public void Execute()
        {
            _master.Crawl();
        }

        private bool MasterAddUrlEvent(AddUrlEventArgs args)
        {
            return _linkStorage.TryAdd(args.Url);
        }

        /// <summary>
        ///     The master data received event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        private void MasterDataReceivedEvent(DataReceivedEventArgs args)
        {
            //var doc = new HtmlDocument();
            //doc.LoadHtml(args.Html);
            //var name = "";
            //var price = "";
            //// //div[@class="wrap"]/div[@class="content content-reg content-shop clearfix"]/div//div/div[@class="product-p"]/div[@class="clearfix"]/div[@class="descr"]/table[@class="buy"]/tbody/tr/td/div[@class="p"]/div[@class="pb"]/span[@class="price"]
            //foreach (var node in doc.DocumentNode.SelectNodes("//span/a/ins"))
            //{
            //    name = node.ChildNodes[0].InnerHtml;
            //}
            //foreach (var node in doc.DocumentNode.SelectNodes("//span[@class='price']"))
            //{
            //    price =node.ChildNodes[0].InnerHtml;
            //}
            //name = name.Trim();
            //price= price.Trim();
            //if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(price))
            //{
            //    Console.WriteLine($"{name} {price} {args.Url}");
            //}
        }
    }
}
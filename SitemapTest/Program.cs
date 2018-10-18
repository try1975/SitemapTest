using System;
using Plossum.CommandLine;
using SitemapLib;

namespace SitemapTest
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var options = new Options();
            var parser = new CommandLineParser(options);
            parser.Parse();

            if (options.Help)
            {
                Console.WriteLine(parser.UsageInfo.GetOptionsAsString(78));
                return 0;
            }
            if (parser.HasErrors)
            {
                Console.WriteLine(parser.UsageInfo.GetErrorsAsString(78));
                return -1;
            }
            var linkStorage = new ConsoleLinkStorage();
            if (!options.SkipSitemap) TestLoew(options.Url, linkStorage);
            if (linkStorage.LinkCount != 0) return 0;
            if (!options.SkipCrawl)
            {
                if (options.UseSimpleCrawler)
                {
                    TestSimpleCrawler(options.Url, linkStorage);
                }
                else
                {
                    TestAbot(options.Url, linkStorage);
                }
            }
            return 0;
        }

        private static void TestLoew(string url, ILinkStorage linkStorage = null)
        {
            var loew = new LoewTest(url, linkStorage);
            loew.Execute();
        }

        private static void TestAbot(string url, ILinkStorage linkStorage = null)
        {
            var abotTest = new AbotTest(url, linkStorage);
            abotTest.Execute();
        }

        private static void TestSimpleCrawler(string url, ILinkStorage linkStorage = null)
        {
            var abotTest = new SimpleCrawlerTest(url, linkStorage);
            abotTest.Execute();
        }
    }
}
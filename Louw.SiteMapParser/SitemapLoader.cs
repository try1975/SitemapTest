using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Louw.SitemapParser
{
    public class SitemapLoader
    {
        private readonly ISitemapFetcher _fetcher;
        private readonly IRobotsTxtParser _robotsParser;
        private readonly ISitemapParser _sitemapParser;

        public SitemapLoader(ISitemapFetcher fetcher = null, ISitemapParser sitemapParser = null,
            IRobotsTxtParser robotsParser = null)
        {
            _fetcher = fetcher ?? new WebSitemapFetcher();
            _sitemapParser = sitemapParser ?? new SitemapParser();
            _robotsParser = robotsParser ?? new RobotsTxtParser();
        }

        public async Task<Sitemap> LoadFromRobotsTxtAsync(Uri websiteLocation)
        {
            var robotsTxtLocation = new Uri(websiteLocation, "/robots.txt");
            var robotsTxtContent = await _fetcher.Fetch(robotsTxtLocation);
            var sitemapLocations = _robotsParser.Parse(robotsTxtContent, robotsTxtLocation);
            var sitemaps = sitemapLocations.Select(x => new Sitemap(x));
            return new Sitemap(sitemaps, robotsTxtLocation);
        }

        private static byte[] Decompress(byte[] gzip)
        {
            // Create a GZIP stream with decompression mode.
            // ... Then create a buffer and write into while reading from the GZIP stream.
            using (var stream = new GZipStream(new MemoryStream(gzip),
                CompressionMode.Decompress))
            {
                const int size = 4096;
                var buffer = new byte[size];
                using (var memory = new MemoryStream())
                {
                    int count;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        public async Task<Sitemap> LoadAsync(Uri sitemapLocation)
        {
            string sitemapContent;
            var extension = Path.GetExtension(sitemapLocation.AbsolutePath).ToLower();
            if (extension.Equals(".gz"))
            {
                var currentPath = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                var directoryName = Path.GetDirectoryName(currentPath);
                var gzFilename = new Uri(Path.Combine(directoryName, Path.GetFileName(sitemapLocation.AbsolutePath)))
                    .LocalPath;

                using (var wc = new WebClient())
                {
                    wc.DownloadFile(sitemapLocation, gzFilename);
                }
                var file = File.ReadAllBytes(gzFilename);
                var decompressed = Decompress(file);
                var xmlFilename = Path.GetFileNameWithoutExtension(sitemapLocation.AbsolutePath);
                File.WriteAllBytes(xmlFilename, decompressed);
                sitemapContent = File.ReadAllText(xmlFilename);
            }
            else
            {
                sitemapContent = await _fetcher.Fetch(sitemapLocation);
            }
            var sitemap = _sitemapParser.Parse(sitemapContent, sitemapLocation);
            return sitemap;
        }

        public async Task<Sitemap> LoadAsync(Sitemap sitemap)
        {
            if (sitemap == null)
                throw new ArgumentNullException(nameof(sitemap));
            return await LoadAsync(sitemap.SitemapLocation);
        }
    }
}
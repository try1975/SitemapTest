using System;
using System.Collections.Generic;
using System.Linq;
using Louw.SitemapParser;

namespace SitemapLib
{
    public class LoewTest
    {
        private readonly ILinkStorage _linkStorage;
        //private readonly List<Uri> _locationList = new List<Uri>();
        private readonly List<Sitemap> _loadedSitemaps = new List<Sitemap>();
        private readonly SitemapLoader _loader;
        private readonly List<Uri> _sitemapUris = new List<Uri>();
        private readonly string _url;

        public LoewTest(string url, ILinkStorage linkStorage)
        {
            _linkStorage = linkStorage ?? new ConsoleLinkStorage();
            _url = url;
            _loader = new SitemapLoader();
        }

        public void Execute()
        {
            LoadRobots(_url);
        }

        private void LoadRobots(string url)
        {
            var robotSitemap = _loader.LoadFromRobotsTxtAsync(new Uri(url)).Result;
            if (robotSitemap.SitemapType != SitemapType.RobotsTxt) return;
            // загрузка всех sitemaps из robots.txt
            foreach (var sitemap in robotSitemap.Sitemaps)
            {
                var uri = sitemap.SitemapLocation;
                if (_sitemapUris.Any(z => z == uri)) continue;
                _sitemapUris.Add(uri);
                var loadedSitemap = _loader.LoadAsync(uri).Result;
                _loadedSitemaps.Add(loadedSitemap);
            }
            var cnt = _loadedSitemaps.Count;
            for (var i = 0; i < cnt; i++)
            {
                var loadedSitemap = _loadedSitemaps[i];

                if (loadedSitemap.SitemapType == SitemapType.Items)
                {
                    foreach (var item in loadedSitemap.Items)
                    {
                        var uri = item.Location;
                        //if (_locationList.Any(z => z == uri)) continue;
                        //_locationList.Add(uri);
                        _linkStorage.TryAdd(uri.AbsoluteUri);
                    }
                }

                if (loadedSitemap.SitemapType == SitemapType.Index)
                {
                    foreach (var sitemap1 in loadedSitemap.Sitemaps)
                    {
                        var uri = sitemap1.SitemapLocation;
                        if (_sitemapUris.Any(z => z == uri)) continue;
                        _sitemapUris.Add(uri);
                        var item = _loader.LoadAsync(uri).Result;
                        _loadedSitemaps.Add(item);
                    }
                }
                cnt = _loadedSitemaps.Count;
            }
        }
    }
}
using System;
using Abot.Util;

namespace SitemapLib
{
    public class ConsoleLinkStorage : ILinkStorage
    {
        private static BloomFilter<string> _filter;
        private readonly int _maxLinkCount;


        public ConsoleLinkStorage(int maxLinkCount = 200000)
        {
            _maxLinkCount = maxLinkCount;
            _filter = new BloomFilter<string>(_maxLinkCount);
        }

        public int LinkCount { get; private set; }

        public bool TryAdd(string url)
        {
            if (_filter.Contains(url)) return false;
            if (LinkCount >= _maxLinkCount) Environment.Exit(0);
            LinkCount++;
            _filter.Add(url);
            Console.WriteLine(url);
            return true;
        }
    }
}
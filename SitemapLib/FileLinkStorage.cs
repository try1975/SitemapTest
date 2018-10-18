using System;
using System.ComponentModel;
using System.IO;
using Abot.Util;

namespace SitemapLib
{
    public class FileLinkStorage : ILinkStorage
    {
        private static BloomFilter<string> _filter;
        private readonly int _maxLinkCount;
        private readonly BackgroundWorker _bw;
        private readonly string _fileName;
        private int _linkCount;

        public FileLinkStorage(string fileName, BackgroundWorker bw, int maxLinkCount = 200000)
        {
            _fileName = fileName;
            _maxLinkCount = maxLinkCount;
            _filter = new BloomFilter<string>(_maxLinkCount);
            _bw = bw;
        }

        public int LinkCount
        {
            get { return _linkCount; }
            private set
            {
                _linkCount = value;
                if (_linkCount%100 == 0 && _linkCount != 0)
                {
                    _bw.ReportProgress(0, $"Найдено {_linkCount} ссылок ...");
                }
            }
        }

        public bool TryAdd(string url)
        {
            if (_filter.Contains(url)) return false;
            if (LinkCount >= _maxLinkCount) return false;
            LinkCount++;
            _filter.Add(url);
            File.AppendAllText(_fileName, $"{url}{Environment.NewLine}");

            return true;
        }
    }
}
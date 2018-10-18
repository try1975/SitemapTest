// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CrawlMaster.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The crawl master.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SimpleCrawler
{
    /// <summary>
    ///     The crawl master.
    /// </summary>
    public class CrawlMaster
    {
        #region Constants

        /// <summary>
        ///     The web url regular expressions.
        /// </summary>
        private const string WebUrlRegularExpressions = @"^(http|https)://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?";

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="CrawlMaster" /> class.
        /// </summary>
        /// <param name="settings">
        ///     The settings.
        /// </param>
        public CrawlMaster(CrawlSettings settings)
        {
            _cookieContainer = new CookieContainer();
            _random = new Random();

            Settings = settings;
            _threads = new Thread[settings.ThreadCount];
            _threadStatus = new bool[settings.ThreadCount];
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the settings.
        /// </summary>
        private CrawlSettings Settings { get; }

        #endregion

        #region Fields

        /// <summary>
        ///     The cookie container.
        /// </summary>
        private readonly CookieContainer _cookieContainer;

        /// <summary>
        ///     The random.
        /// </summary>
        private readonly Random _random;

        /// <summary>
        ///     The thread status.
        /// </summary>
        private readonly bool[] _threadStatus;

        /// <summary>
        ///     The threads.
        /// </summary>
        private readonly Thread[] _threads;

        #endregion

        #region Public Events

        /// <summary>
        ///     The add url event.
        /// </summary>
        public event AddUrlEventHandler AddUrlEvent;

        /// <summary>
        ///     The crawl error event.
        /// </summary>
        public event CrawlErrorEventHandler CrawlErrorEvent;

        /// <summary>
        ///     The data received event.
        /// </summary>
        public event DataReceivedEventHandler DataReceivedEvent;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The crawl.
        /// </summary>
        public void Crawl()
        {
            Initialize();

            for (var i = 0; i < _threads.Length; i++)
            {
                _threads[i].Start(i);
                _threadStatus[i] = false;
            }
        }

        /// <summary>
        ///     The stop.
        /// </summary>
        public void Stop()
        {
            foreach (var thread in _threads)
            {
                thread.Abort();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     The config request.
        /// </summary>
        /// <param name="request">
        ///     The request.
        /// </param>
        private void ConfigRequest(HttpWebRequest request)
        {
            request.UserAgent = Settings.UserAgent;
            request.CookieContainer = _cookieContainer;
            request.AllowAutoRedirect = true;
            request.MediaType = "text/html";
            //request.Headers["Accept-Language"] = "zh-CN,zh;q=0.8";

            if (Settings.Timeout > 0)
            {
                request.Timeout = Settings.Timeout;
            }
        }

        /// <summary>
        ///     The crawl process.
        /// </summary>
        /// <param name="threadIndex">
        ///     The thread index.
        /// </param>
        private void CrawlProcess(object threadIndex)
        {
            var currentThreadIndex = (int) threadIndex;
            while (true)
            {
                // 根据队列中的 Url 数量和空闲线程的数量，判断线程是睡眠还是退出
                if (UrlQueue.Instance.Count == 0)
                {
                    _threadStatus[currentThreadIndex] = true;
                    if (!_threadStatus.Any(t => t == false))
                    {
                        break;
                    }

                    Thread.Sleep(2000);
                    continue;
                }

                _threadStatus[currentThreadIndex] = false;

                if (UrlQueue.Instance.Count == 0)
                {
                    continue;
                }

                var urlInfo = UrlQueue.Instance.DeQueue();

                HttpWebRequest request = null;
                HttpWebResponse response = null;

                try
                {
                    if (urlInfo == null)
                    {
                        continue;
                    }

                    // 1~5 秒随机间隔的自动限速
                    if (Settings.AutoSpeedLimit)
                    {
                        var span = _random.Next(1000, 5000);
                        Thread.Sleep(span);
                    }

                    // 创建并配置Web请求
                    request = WebRequest.Create(urlInfo.UrlString) as HttpWebRequest;
                    ConfigRequest(request);

                    if (request != null)
                    {
                        response = request.GetResponse() as HttpWebResponse;
                    }

                    if (response != null)
                    {
                        PersistenceCookie(response);

                        Stream stream = null;

                        // 如果页面压缩，则解压数据流
                        if (response.ContentEncoding == "gzip")
                        {
                            var responseStream = response.GetResponseStream();
                            if (responseStream != null)
                            {
                                stream = new GZipStream(responseStream, CompressionMode.Decompress);
                            }
                        }
                        else
                        {
                            stream = response.GetResponseStream();
                        }

                        using (stream)
                        {
                            var html = ParseContent(stream, response.CharacterSet);

                            ParseLinks(urlInfo, html);

                            DataReceivedEvent?.Invoke(
                                new DataReceivedEventArgs
                                {
                                    Url = urlInfo.UrlString,
                                    Depth = urlInfo.Depth,
                                    Html = html
                                });

                            stream?.Close();
                        }
                    }
                }
                catch (Exception exception)
                {
                    if (CrawlErrorEvent != null)
                    {
                        if (urlInfo != null)
                        {
                            CrawlErrorEvent(
                                new CrawlErrorEventArgs {Url = urlInfo.UrlString, Exception = exception});
                        }
                    }
                }
                finally
                {
                    request?.Abort();
                    response?.Close();
                }
            }
        }

        /// <summary>
        ///     The initialize.
        /// </summary>
        private void Initialize()
        {
            if (Settings.SeedsAddress != null && Settings.SeedsAddress.Count > 0)
            {
                foreach (var seed in Settings.SeedsAddress)
                {
                    if (Regex.IsMatch(seed, WebUrlRegularExpressions, RegexOptions.IgnoreCase))
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(seed) {Depth = 1});
                    }
                }
            }

            for (var i = 0; i < Settings.ThreadCount; i++)
            {
                var threadStart = new ParameterizedThreadStart(CrawlProcess);

                _threads[i] = new Thread(threadStart);
            }

            ServicePointManager.DefaultConnectionLimit = 256;
        }

        /// <summary>
        ///     The is match regular.
        /// </summary>
        /// <param name="url">
        ///     The url.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool IsMatchRegular(string url)
        {
            var result = false;

            if (Settings.RegularFilterExpressions != null && Settings.RegularFilterExpressions.Count > 0)
            {
                if (
                    Settings.RegularFilterExpressions.Any(
                        pattern => Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase)))
                {
                    result = true;
                }
            }
            else
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        ///     The parse content.
        /// </summary>
        /// <param name="stream">
        ///     The stream.
        /// </param>
        /// <param name="characterSet">
        ///     The character set.
        /// </param>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        private static string ParseContent(Stream stream, string characterSet)
        {
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            var buffer = memoryStream.ToArray();

            var encode = Encoding.ASCII;
            var html = encode.GetString(buffer);

            var localCharacterSet = characterSet;

            var match = Regex.Match(html, "<meta([^<]*)charset=([^<]*)\"", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                localCharacterSet = match.Groups[2].Value;

                var stringBuilder = new StringBuilder();
                foreach (var item in localCharacterSet)
                {
                    if (item == ' ')
                    {
                        break;
                    }

                    if (item != '\"')
                    {
                        stringBuilder.Append(item);
                    }
                }

                localCharacterSet = stringBuilder.ToString();
            }

            if (string.IsNullOrEmpty(localCharacterSet))
            {
                localCharacterSet = characterSet;
            }

            if (!string.IsNullOrEmpty(localCharacterSet))
            {
                encode = Encoding.GetEncoding(localCharacterSet);
            }

            memoryStream.Close();

            return encode.GetString(buffer);
        }

        /// <summary>
        ///     The parse links.
        /// </summary>
        /// <param name="urlInfo">
        ///     The url info.
        /// </param>
        /// <param name="html">
        ///     The html.
        /// </param>
        private void ParseLinks(UrlInfo urlInfo, string html)
        {
            if (Settings.Depth > 0 && urlInfo.Depth >= Settings.Depth)
            {
                return;
            }

            var urlDictionary = new Dictionary<string, string>();

            var match = Regex.Match(html, "(?i)<a .*?href=\"([^\"]+)\"[^>]*>(.*?)</a>");
            while (match.Success)
            {
                // 以 href 作为 key
                var urlKey = match.Groups[1].Value;

                // 以 text 作为 value
                var urlValue = Regex.Replace(match.Groups[2].Value, "(?i)<.*?>", string.Empty);

                urlDictionary[urlKey] = urlValue;
                match = match.NextMatch();
            }

            foreach (var item in urlDictionary)
            {
                var href = item.Key;
                var text = item.Value;

                if (string.IsNullOrEmpty(href)) continue;
                var canBeAdd = true;

                if (Settings.EscapeLinks != null && Settings.EscapeLinks.Count > 0)
                {
                    if (Settings.EscapeLinks.Any(suffix => href.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
                    {
                        canBeAdd = false;
                    }
                }

                if (Settings.HrefKeywords != null && Settings.HrefKeywords.Count > 0)
                {
                    if (!Settings.HrefKeywords.Any(href.Contains))
                    {
                        canBeAdd = false;
                    }
                }

                if (!canBeAdd) continue;
                var url = href.Replace("%3f", "?")
                    .Replace("%3d", "=")
                    .Replace("%2f", "/")
                    .Replace("&amp;", "&");

                if (string.IsNullOrEmpty(url) || url.StartsWith("#")
                    || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                    || url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)
                    || url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var baseUri = new Uri(urlInfo.UrlString);
                var currentUri = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? new Uri(url)
                    : new Uri(baseUri, url);

                url = currentUri.AbsoluteUri;

                if (Settings.LockHost)
                {
                    // 去除二级域名后，判断域名是否相等，相等则认为是同一个站点
                    // 例如：mail.pzcast.com 和 www.pzcast.com
                    if (baseUri.Host.Split('.').Skip(1).Aggregate((a, b) => a + "." + b)
                        != currentUri.Host.Split('.').Skip(1).Aggregate((a, b) => a + "." + b))
                    {
                        continue;
                    }
                }

                if (!IsMatchRegular(url))
                {
                    continue;
                }

                var addUrlEventArgs = new AddUrlEventArgs {Title = text, Depth = urlInfo.Depth + 1, Url = url};
                if (AddUrlEvent != null && !AddUrlEvent(addUrlEventArgs))
                {
                    continue;
                }

                UrlQueue.Instance.EnQueue(new UrlInfo(url) {Depth = urlInfo.Depth + 1});
            }
        }

        /// <summary>
        ///     The persistence cookie.
        /// </summary>
        /// <param name="response">
        ///     The response.
        /// </param>
        private void PersistenceCookie(HttpWebResponse response)
        {
            if (!Settings.KeepCookie)
            {
                return;
            }

            var cookies = response.Headers["Set-Cookie"];
            if (string.IsNullOrEmpty(cookies)) return;
            var cookieUri =
                new Uri($"{response.ResponseUri.Scheme}://{response.ResponseUri.Host}:{response.ResponseUri.Port}/");
            _cookieContainer.SetCookies(cookieUri, cookies);
        }

        #endregion
    }
}
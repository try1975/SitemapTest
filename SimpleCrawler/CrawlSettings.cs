// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CrawlSettings.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The crawl settings.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SimpleCrawler
{
    /// <summary>
    ///     The crawl settings.
    /// </summary>
    [Serializable]
    public class CrawlSettings
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="CrawlSettings" /> class.
        /// </summary>
        public CrawlSettings()
        {
            AutoSpeedLimit = false;
            EscapeLinks = new List<string>();
            KeepCookie = true;
            HrefKeywords = new List<string>();
            LockHost = true;
            RegularFilterExpressions = new List<string>();
            SeedsAddress = new List<string>();
        }

        #endregion

        #region Fields

        /// <summary>
        ///     The depth.
        /// </summary>
        private byte _depth = 3;

        /// <summary>
        ///     The lock host.
        /// </summary>
        private bool _lockHost = true;

        /// <summary>
        ///     The thread count.
        /// </summary>
        private byte _threadCount = 1;

        /// <summary>
        ///     The timeout.
        /// </summary>
        private int _timeout = 15000;

        /// <summary>
        ///     The user agent.
        /// </summary>
        private string _userAgent =
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.97 Safari/537.11";

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets a value indicating whether auto speed limit.
        /// </summary>
        public bool AutoSpeedLimit { get; set; }

        /// <summary>
        ///     Gets or sets the depth.
        /// </summary>
        public byte Depth
        {
            get { return _depth; }

            set { _depth = value; }
        }

        /// <summary>
        ///     Gets the escape links.
        /// </summary>
        public List<string> EscapeLinks { get; private set; }

        /// <summary>
        ///     Gets or sets a value indicating whether keep cookie.
        /// </summary>
        public bool KeepCookie { get; set; }

        /// <summary>
        ///     Gets the href keywords.
        /// </summary>
        public List<string> HrefKeywords { get; private set; }

        /// <summary>
        ///     Gets or sets a value indicating whether lock host.
        /// </summary>
        public bool LockHost
        {
            get { return _lockHost; }

            set { _lockHost = value; }
        }

        /// <summary>
        ///     Gets the regular filter expressions.
        /// </summary>
        public List<string> RegularFilterExpressions { get; private set; }

        /// <summary>
        ///     Gets  the seeds address.
        /// </summary>
        public List<string> SeedsAddress { get; private set; }

        /// <summary>
        ///     Gets or sets the thread count.
        /// </summary>
        public byte ThreadCount
        {
            get { return _threadCount; }

            set { _threadCount = value; }
        }

        /// <summary>
        ///     Gets or sets the timeout.
        /// </summary>
        public int Timeout
        {
            get { return _timeout; }

            set { _timeout = value; }
        }

        /// <summary>
        ///     Gets or sets the user agent.
        /// </summary>
        public string UserAgent
        {
            get { return _userAgent; }

            set { _userAgent = value; }
        }

        #endregion
    }
}
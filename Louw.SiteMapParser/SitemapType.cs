namespace Louw.SitemapParser
{
    public enum SitemapType
    {
        /// <summary>
        ///     Type is currently unknown.
        ///     All that is known is the location to the sitemap file.
        ///     Use the LoadAsync method to return a parsed version of sitemap.
        /// </summary>
        NotLoaded,

        /// <summary>
        ///     Sitemap was created by parsing of Robots.txt file
        /// </summary>
        RobotsTxt,

        /// <summary>
        ///     Sitemap contains references to other sitemaps
        /// </summary>
        Index,

        /// <summary>
        ///     Sitemap contains references to content (like text, images or video)
        /// </summary>
        Items
    }
}
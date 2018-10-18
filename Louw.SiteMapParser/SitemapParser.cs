using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Louw.SitemapParser
{
    public class SitemapParser : ISitemapParser
    {
        public Sitemap Parse(string sitemapContent, Uri sitemapLocation = null)
        {
            if (string.IsNullOrWhiteSpace(sitemapContent))
                return null;

            try
            {
                var sitemapXElement = XElement.Parse(sitemapContent);

                //Check if this is Index Sitemap
                if (sitemapXElement.Name.EqualsAnyNs(_sitemapIndexName))
                {
                    return ParseIndexSitemap(sitemapXElement, sitemapLocation);
                }

                //Check if this is Normal Sitemap with items
                if (sitemapXElement.Name.EqualsAnyNs(_urlSetName))
                {
                    return ParseSitemapItems(sitemapXElement, sitemapLocation);
                }
            }
            catch (Exception ex)
            {
                //TODO: Handle only expected exceptions here
                Console.WriteLine(ex.Message);
                throw;
            }

            return null;
        }

        public static Sitemap ParseSitemapFields(Uri baseUri, string sitemapLocation, string lastModified)
        {
            if (string.IsNullOrEmpty(sitemapLocation))
                return null;

            var parsedSitemapLocation = SafeUriParse(baseUri, sitemapLocation);
            if (parsedSitemapLocation == null)
                return null;

            var parsedLastModified = SafeDateTimeParse(lastModified);

            return new Sitemap(parsedSitemapLocation, parsedLastModified);
        }

        public static SitemapItem ParseSitemapItemFields(Uri baseUri, string location, string lastModified = null,
            string changeFrequency = null, string priority = null)
        {
            if (string.IsNullOrEmpty(location))
                return null;

            var parsedLocation = SafeUriParse(baseUri, location);
            if (parsedLocation == null)
                return null;

            var parsedLastModified = SafeDateTimeParse(lastModified);

            SitemapChangeFrequency? parsedChangeFrequency = null;
            SitemapChangeFrequency triedChangeFrequency;
            if (Enum.TryParse(changeFrequency, true, out triedChangeFrequency))
                parsedChangeFrequency = triedChangeFrequency;

            double? parsedPriority = null;
            double triedPriority;
            if (double.TryParse(priority, out triedPriority))
            {
                triedPriority = Math.Max(0.0, triedPriority);
                triedPriority = Math.Min(1.0, triedPriority);
                parsedPriority = triedPriority;
            }

            return new SitemapItem(parsedLocation, parsedLastModified, parsedChangeFrequency, parsedPriority);
        }

        #region Element Names

        private const string SitemapSchema = "http://www.sitemaps.org/schemas/sitemap/0.9";
        //Note: Namespace not consistently being used. So we actually ignore namespaces

        private readonly XName _sitemapIndexName = XName.Get("sitemapindex", SitemapSchema);
        private readonly XName _sitemapName = XName.Get("sitemap", SitemapSchema);

        private readonly XName _urlSetName = XName.Get("urlset", SitemapSchema);
        private readonly XName _urlName = XName.Get("url", SitemapSchema);
        private readonly XName _locationName = XName.Get("loc", SitemapSchema);
        private readonly XName _lastModifiedName = XName.Get("lastmod", SitemapSchema);
        private readonly XName _changeFrequencyName = XName.Get("changefreq", SitemapSchema);
        private readonly XName _priorityName = XName.Get("priority", SitemapSchema);

        #endregion

        #region Private methods

        private Sitemap ParseIndexSitemap(XElement sitemapXElement, Uri sitemapLocation)
        {
            var sitemaps = new List<Sitemap>();
            foreach (var urlElement in sitemapXElement.ElementsAnyNs(_sitemapName))
            {
                var locElement = urlElement.ElementAnyNs(_locationName);
                if (string.IsNullOrWhiteSpace(locElement?.Value))
                    continue;

                var lastmodElement = urlElement.ElementAnyNs(_lastModifiedName);

                var sitemap = ParseSitemapFields(sitemapLocation, locElement.Value, lastmodElement?.Value);
                if (sitemap != null)
                    sitemaps.Add(sitemap);
            }

            var lastModified = SafeMaxDate(sitemaps.Select(x => x.LastModified));

            return new Sitemap(sitemaps, sitemapLocation, lastModified);
        }

        private Sitemap ParseSitemapItems(XElement sitemapXElement, Uri sitemapLocation)
        {
            var sitemapItems = new List<SitemapItem>();
            foreach (var urlElement in sitemapXElement.ElementsAnyNs(_urlName))
            {
                var locElement = urlElement.ElementAnyNs(_locationName);
                if (string.IsNullOrWhiteSpace(locElement?.Value))
                    continue;

                var lastmodElement = urlElement.ElementAnyNs(_lastModifiedName);
                var changefreqElement = urlElement.ElementAnyNs(_changeFrequencyName);
                var priorityElement = urlElement.ElementAnyNs(_priorityName);
                var sitemapItem = ParseSitemapItemFields(sitemapLocation, locElement.Value, lastmodElement?.Value,
                    changefreqElement?.Value, priorityElement?.Value);
                sitemapItems.Add(sitemapItem);
            }

            var lastModified = SafeMaxDate(sitemapItems.Select(x => x.LastModified));

            return new Sitemap(sitemapItems, sitemapLocation, lastModified);
        }

        private static Uri SafeUriParse(Uri baseUri, string location)
        {
            Uri parsedLocation;
            if (baseUri != null)
            {
                if (!Uri.TryCreate(baseUri, location, out parsedLocation))
                    return null;
            }
            else
            {
                if (!Uri.TryCreate(location, UriKind.Absolute, out parsedLocation))
                    return null;
            }

            return parsedLocation;
        }

        private static DateTime? SafeDateTimeParse(string dateTimeString)
        {
            if (string.IsNullOrWhiteSpace(dateTimeString))
                return null;

            DateTime result;
            if (DateTime.TryParseExact(dateTimeString,
                new[]
                {
                    "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",
                    "yyyy-MM-dd'T'HH:mm:ss.fffffffzzz",
                    "yyyy-MM-dd'T'HH:mm:sszzz",
                    "yyyy-MM-dd HH:mm:ss",
                    "yyyy-MM-dd"
                },
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result))
                return result;

            //Do our best to also parse "out of spec" dates
            if (DateTime.TryParse(dateTimeString, out result))
            {
                if (result.Kind == DateTimeKind.Unspecified)
                    result = DateTime.SpecifyKind(result, DateTimeKind.Utc);
                return result;
            }

            return null;
        }

        private static DateTime? SafeMaxDate(IEnumerable<DateTime?> dates)
        {
            var dateList = dates
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .ToList();

            if (dateList.Count == 0)
                return null;

            var maxDate = dateList[0];
            for (var i = 1; i < dateList.Count; i++)
            {
                if (maxDate < dateList[i])
                    maxDate = dateList[i];
            }

            return maxDate;
        }

        #endregion
    }

    //Helps us ignore namespaces
    //See http://stackoverflow.com/questions/1145659/ignore-namespaces-in-linq-to-xml
    public static class XContainerExtensions
    {
        public static IEnumerable<XElement> ElementsAnyNs<T>(this T source, string localName) where T : XContainer
        {
            return source.Elements().Where(e => e.Name.LocalName == localName);
        }

        public static IEnumerable<XElement> ElementsAnyNs<T>(this T source, XName xName) where T : XContainer
        {
            return source.ElementsAnyNs(xName.LocalName);
        }

        public static XElement ElementAnyNs<T>(this T source, string localName) where T : XContainer
        {
            return source.ElementsAnyNs(localName).FirstOrDefault();
        }

        public static XElement ElementAnyNs<T>(this T source, XName xName) where T : XContainer
        {
            return source.ElementAnyNs(xName.LocalName);
        }

        public static bool EqualsAnyNs(this XName name, string compareName)
        {
            if (name == null) return false;
            if (compareName == null) return false;

            return name.LocalName.Equals(compareName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool EqualsAnyNs(this XName name, XName compareName)
        {
            if (name == null) return false;
            if (compareName == null) return false;

            return name.EqualsAnyNs(compareName.LocalName);
        }
    }
}
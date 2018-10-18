namespace SitemapLib
{
    public interface ILinkStorage
    {
        bool TryAdd(string url);
    }
}
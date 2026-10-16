namespace VideoHub.Api.Services;

// Only external YouTube/Vimeo links are accepted in this phase (no file upload).
public static class VideoUrl
{
    private static readonly HashSet<string> YouTubeHosts =
        ["youtube.com", "www.youtube.com", "m.youtube.com", "youtube-nocookie.com", "www.youtube-nocookie.com"];

    private static readonly HashSet<string> VimeoHosts = ["vimeo.com", "www.vimeo.com", "player.vimeo.com"];

    public static bool TryParse(string? value, out Uri uri)
    {
        uri = default!;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var host = parsed.Host.ToLowerInvariant();
        if (host != "youtu.be" && !YouTubeHosts.Contains(host) && !VimeoHosts.Contains(host))
        {
            return false;
        }

        uri = parsed;
        return true;
    }

    public static string? ToEmbedUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var host = uri.Host.ToLowerInvariant();
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (host == "youtu.be" && segments.Length == 1)
        {
            return YouTubeEmbed(segments[0]);
        }

        if (YouTubeHosts.Contains(host))
        {
            if (segments is ["embed", var embedId])
            {
                return YouTubeEmbed(embedId);
            }

            var id = System.Web.HttpUtility.ParseQueryString(uri.Query)["v"];
            return segments is ["watch"] && id is not null ? YouTubeEmbed(id) : null;
        }

        if (VimeoHosts.Contains(host))
        {
            var id = segments.LastOrDefault();
            return id is not null && id.All(char.IsAsciiDigit) ? $"https://player.vimeo.com/video/{id}" : null;
        }

        return null;
    }

    private static string? YouTubeEmbed(string id) =>
        id.Length is > 0 and <= 20 && id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            ? $"https://www.youtube-nocookie.com/embed/{id}"
            : null;
}

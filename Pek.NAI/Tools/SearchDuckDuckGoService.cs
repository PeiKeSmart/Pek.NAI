using System.Net;
using System.Text.RegularExpressions;

namespace NewLife.AI.Tools;

/// <summary>DuckDuckGo HTML 搜索实现。无需密钥，功能完全免费，适合海外环境兜底</summary>
/// <remarks>通过 html.duckduckgo.com 搜索页解析真实网页搜索结果，替代仅返回摘要卡片的即时问答 API</remarks>
/// <remarks>初始化 DuckDuckGo 搜索服务</remarks>
/// <param name="httpClient">HTTP 客户端；为 null 时自动创建默认实例</param>
public class SearchDuckDuckGoService(HttpClient? httpClient = null) : ISearchService
{
    private readonly HttpClient _http = httpClient ?? ToolHelper.CreateDefaultHttpClient();

    /// <summary>使用 DuckDuckGo 搜索页检索互联网信息</summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="count">返回结果数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功返回搜索结果；失败或无结果返回 null</returns>
    public async Task<SearchModel?> SearchAsync(String query, Int32 count = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            // 独立 8 秒超时，避免单提供者不可用时拖慢整个降级链
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(8000);

            var encoded = Uri.EscapeDataString(query);
            var resp = await _http.GetAsync(
                $"https://html.duckduckgo.com/html/?q={encoded}",
                cts.Token).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return null;

            var html = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            return ParseHtml(html, count);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>解析 DuckDuckGo 搜索结果页 HTML</summary>
    /// <param name="html">搜索页 HTML 内容</param>
    /// <param name="count">最大返回条数</param>
    /// <returns>搜索结果；无结果返回 null</returns>
    private static SearchModel? ParseHtml(String html, Int32 count)
    {
        var model = new SearchModel();
        var titles = Regex.Matches(html, @"<a[^>]*class=""result__a""[^>]*href=""(?<url>[^""]*)""[^>]*>(?<title>[\s\S]*?)</a>", RegexOptions.IgnoreCase);
        var snippets = Regex.Matches(html, @"<a[^>]*class=""result__snippet""[^>]*>(?<snippet>[\s\S]*?)</a>", RegexOptions.IgnoreCase);

        for (var i = 0; i < titles.Count; i++)
        {
            var m = titles[i];
            var title = Clean(m.Groups["title"].Value);
            var snippet = i < snippets.Count ? Clean(snippets[i].Groups["snippet"].Value) : null;
            if (String.IsNullOrEmpty(title) && String.IsNullOrEmpty(snippet)) continue;

            model.Items.Add(new SearchItem { Title = title, Url = ResolveUrl(m.Groups["url"].Value), Snippet = snippet });
            if (model.Items.Count >= count) break;
        }

        return model.Items.Count > 0 ? model : null;
    }

    /// <summary>去除 HTML 标签并反转义实体</summary>
    /// <param name="html">原始片段</param>
    /// <returns>纯文本</returns>
    private static String Clean(String html) => WebUtility.HtmlDecode(Regex.Replace(html, @"<[^>]+>", "")).Trim();

    /// <summary>解析结果链接，提取 DuckDuckGo 跳转地址中的真实 URL</summary>
    /// <param name="url">原始链接，可能是 //duckduckgo.com/l/?uddg=xxx 跳转</param>
    /// <returns>真实 URL</returns>
    private static String? ResolveUrl(String? url)
    {
        if (String.IsNullOrEmpty(url)) return null;

        var u = url!.Trim();
        if (u.StartsWith("//")) u = "https:" + u;

        var idx = u.IndexOf("uddg=", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            var val = u.Substring(idx + 5);
            var end = val.IndexOf('&');
            if (end >= 0) val = val.Substring(0, end);
            u = Uri.UnescapeDataString(val);
        }

        return u;
    }
}

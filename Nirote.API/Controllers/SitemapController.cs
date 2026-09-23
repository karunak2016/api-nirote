using System.Text;
using Dapper;
using Nirote.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("sitemap.xml")]
public class SitemapController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly IConfiguration _config;

    public SitemapController(IDbConnectionFactory db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> GetSitemap()
    {
        var baseUrl = _config["SiteBaseUrl"]?.TrimEnd('/') ?? "https://nirote.com";

        using var conn = _db.Create();

        var products = await conn.QueryAsync<(int Id, string Name, DateTime? UpdatedAt, DateTime CreatedAt)>(
            "SELECT Id, Name, UpdatedAt, CreatedAt FROM dbo.Products WHERE IsActive = 1 ORDER BY Id");

        var categories = await conn.QueryAsync<(string Slug, string Name)>(
            "SELECT Slug, Name FROM dbo.Categories WHERE IsActive = 1 ORDER BY DisplayOrder");

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        // Static pages
        AppendUrl(sb, $"{baseUrl}/", "daily", "1.0");
        AppendUrl(sb, $"{baseUrl}/products", "daily", "0.9");
        AppendUrl(sb, $"{baseUrl}/track", "monthly", "0.4");

        // Category pages
        foreach (var cat in categories)
        {
            AppendUrl(sb, $"{baseUrl}/products/category/{cat.Slug}", "weekly", "0.8");
        }

        // Product detail pages
        foreach (var p in products)
        {
            var lastMod = (p.UpdatedAt ?? p.CreatedAt).ToString("yyyy-MM-dd");
            sb.AppendLine($"  <url>");
            sb.AppendLine($"    <loc>{baseUrl}/products/{p.Id}</loc>");
            sb.AppendLine($"    <lastmod>{lastMod}</lastmod>");
            sb.AppendLine($"    <changefreq>weekly</changefreq>");
            sb.AppendLine($"    <priority>0.7</priority>");
            sb.AppendLine($"  </url>");
        }

        sb.AppendLine("</urlset>");

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    private static void AppendUrl(StringBuilder sb, string loc, string changefreq, string priority)
    {
        sb.AppendLine($"  <url>");
        sb.AppendLine($"    <loc>{loc}</loc>");
        sb.AppendLine($"    <changefreq>{changefreq}</changefreq>");
        sb.AppendLine($"    <priority>{priority}</priority>");
        sb.AppendLine($"  </url>");
    }
}

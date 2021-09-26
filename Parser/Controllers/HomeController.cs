using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Parser.Models;

namespace Parser.Controllers
{
    public class HomeController : Controller
    {
        private const string SiteUrl = "https://habr.com";
        
        private readonly IWebHostEnvironment _appEnvironment;
        public HomeController(IWebHostEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }
        
        [HttpPost]
        public IActionResult FindArticles(string search)
        {
            var url = SiteUrl + "/ru/search/?q=" + search + "&target_type=posts&order=relevance";
            var response = CallUrl(url).Result;
            var articles = ParseArticlesHtml(response);
            WriteArticlesToCsv(articles);
            
            var path = Path.Combine(_appEnvironment.ContentRootPath, "articles.txt");
            var mas = System.IO.File.ReadAllBytes(path);
            return File(mas, "text/plain", "articles.txt");
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static async Task<string> CallUrl(string fullUrl)
        {
            var client = new HttpClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
            client.DefaultRequestHeaders.Accept.Clear();
            var response = client.GetStringAsync(fullUrl);
            return await response;
        }

        private List<Article> ParseArticlesHtml(string html)
        {
            var links = GetLinksToArticles(html);
            var articles = new List<Article>();

            foreach (var link in links)
            {
                var response = CallUrl(SiteUrl + link).Result;
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(response);
                
                var tagItems = htmlDoc.DocumentNode
                    .Descendants("div")
                    .First(node => node.HasClass("tm-article-body__tags-links"))
                    .Descendants("a");
                
                
                var hubItems = htmlDoc.DocumentNode
                    .Descendants("div")
                    .Last(node => node.HasClass("tm-article-body__tags-links"))
                    .Descendants("a");

                var contentBlock = htmlDoc.GetElementbyId("post-content-body").InnerText;
                var tags = tagItems.Select(item => item.InnerText).ToList();
                var hubs = hubItems.Select(item => item.InnerText.Replace("\n", string.Empty).Trim()).ToList();
                var title = htmlDoc.DocumentNode.Descendants("h1").First().InnerText;
                
                var article = new Article
                {
                    Title = title,
                    Content = contentBlock,
                    Tags = tags,
                    Hubs = hubs
                };
                
                articles.Add(article);
            }
            
            return articles;
        }
        
        private List<string> GetLinksToArticles(string html)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var articles = htmlDoc.DocumentNode.Descendants("article");
            
            var articleLink = new List<string>();

            foreach (var link in articles)
            { 
                articleLink.Add(link.Descendants("a")
                    .First(node => node.HasClass("tm-article-snippet__readmore"))
                    .GetAttributeValue("href", ""));
            }

            return articleLink;
        }

        private void WriteArticlesToCsv(List<Article> articles)
        {
            var sb = new StringBuilder();
            foreach (var article in articles)
            {
                var tags = string.Join(", ", article.Tags);
                var hubs = string.Join(", ", article.Hubs);
                sb.AppendLine(article.Title);
                sb.AppendLine(article.Content);
                sb.AppendLine("Теги: " + tags);
                sb.AppendLine("Хабы: " +  hubs);
                sb.AppendLine("---------------------------------");
            }

            System.IO.File.WriteAllText("articles.txt", sb.ToString());
        }
    }
}
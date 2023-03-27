using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using System.Text.Json;
using SendEmailViaSMTP.DAL_Services;
using SendEmailViaSMTP.Models;
using System.Globalization;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml.Linq;

namespace SendEmailViaSMTP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsControllerController : ControllerBase
    {
        readonly CultureInfo culture = new("en-US");
        private readonly IConfiguration _configuration;
        private static readonly object _lockObj = new();
        private readonly IDistributedCache _cache;
        private readonly DAL _dal;
        private readonly string str = "server=localhost;port=3306;uid=root;pwd=sobiazafar@2023;database=mvc_crud";


        public AnalyticsControllerController(IConfiguration configuration, IDistributedCache cache)
        {
            _configuration = configuration;
            _cache = cache;
            _dal = new DAL();
        }
        [HttpPost]
        [Route("createpost/{authorid}")]
        public async Task<bool> Createpost(string authorid)
        {
            try
            {
                XDocument doc = XDocument.Load("https://www.c-sharpcorner.com/members/" + authorid + "/rss");
                if (doc == null)
                {
                    return false;
                }
                var entries = from item in doc.Root.Descendants().First(i => i.Name.LocalName == "channel").Elements().Where(i => i.Name.LocalName == "item")

                              select new Feed
                              {
                                  Content = item.Elements().First(i => i.Name.LocalName == "description").Value,
                                  Link = (item.Elements().First(i => i.Name.LocalName == "link").Value).StartsWith("/") ? "https://www.c-sharpcorner.com" + item.Elements().First(i => i.Name.LocalName == "link").Value : item.Elements().First(i => i.Name.LocalName == "link").Value,
                                  PubDate = Convert.ToDateTime(item.Elements().First(i => i.Name.LocalName == "pubDate").Value, culture),
                                  Title = item.Elements().First(i => i.Name.LocalName == "title").Value,
                                  FeedType = (item.Elements().First(i => i.Name.LocalName == "link").Value).ToLowerInvariant().Contains("blog") ? "Blog" : (item.Elements().First(i => i.Name.LocalName == "link").Value).ToLowerInvariant().Contains("news") ? "News" : "Article",
                                  Author = item.Elements().First(i => i.Name.LocalName == "author").Value
                              };
                List<Feed> feeds= entries.OrderByDescending(o=>o.PubDate).ToList();
                string urlAddress = string.Empty;
                List<ArticleMatrix> articelmatrix = new();
                _ = int.TryParse(_configuration["ParallelTasksCount"], out int parallelTasksCount);
                Parallel.ForEach(feeds, new ParallelOptions { MaxDegreeOfParallelism = parallelTasksCount }, feed =>
                {
                urlAddress = feed.Link;
                var httpclinet = new HttpClient
                {
                    BaseAddress = new Uri(urlAddress)
                };
                var resutl = httpclinet.GetAsync("").Result;
                string strData = "";
                if (resutl.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    strData = resutl.Content.ReadAsStringAsync().Result;
                    HtmlDocument htmlDocument = new();
                    htmlDocument.LoadHtml(strData);
                    ArticleMatrix articelmatric = new()
                    {
                        AuthorId = authorid,
                        Author = feed.Author,
                        Type = feed.FeedType,
                        Link = feed.Link,
                        Title = feed.Title,
                        PubDate = feed.PubDate
                    };
                    string category = "Videos";
                    if (htmlDocument.GetElementbyId("ImgCategory") != null)
                    {
                        category = htmlDocument.GetElementbyId("ImgCategory").GetAttributeValue("title", "");
                    }
                    articelmatric.Category = category;

                    var view = htmlDocument.DocumentNode.SelectSingleNode("//span[@id='ViewCounts']");
                    if (view != null)
                    {
                        articelmatric.Views = view.InnerText;
                        if (articelmatric.Views.Contains('m'))
                        {
                            articelmatric.ViewsCount = decimal.Parse(articelmatric.Views[0..^1]) * 1000000;
                        }
                        else if (articelmatric.Views.Contains('k'))
                        {
                            articelmatric.ViewsCount = decimal.Parse(articelmatric.Views[0..^1]) * 1000;
                        }
                        else
                        {
                            _ = decimal.TryParse(articelmatric.Views, out decimal viewcount);
                            articelmatric.ViewsCount = viewcount;
                        }
                    }
                    else
                    {
                        var newsView = htmlDocument.DocumentNode.SelectSingleNode("\"//span[@id='spanNewsViews']\"");
                        if (newsView != null)
                        {
                            articelmatric.Views = newsView.InnerText;

                            if (articelmatric.Views.Contains('m'))
                            {
                                articelmatric.ViewsCount = decimal.Parse(articelmatric.Views[0..^1]) * 1000000;
                            }
                            else if (articelmatric.Views.Contains('k'))
                            {
                                articelmatric.ViewsCount = decimal.Parse(articelmatric.Views[0..^1]) * 1000;
                            }
                            else
                            {
                                _ = decimal.TryParse(articelmatric.Views, out decimal viewCount);
                                articelmatric.ViewsCount = viewCount;
                            }
                        }
                        else
                        {
                            articelmatric.ViewsCount = 0;
                        }
                    }
                    var like = htmlDocument.DocumentNode.SelectSingleNode("//span[@id='LabelLikeCount']");
                    if (like != null)
                    {
                        _ = int.TryParse(like.InnerText, out int likes);
                        articelmatric.Likes = likes;

                    }
                    lock (_lockObj)
                    {
                        articelmatrix.Add(articelmatric);
                    }
                    
                    }

                });
                using(MySqlConnection conn=new MySqlConnection(str))
                {

                   await conn.OpenAsync();
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM ArticleMatrices WHERE AuthorId = @AuthorId",conn))
                    {
                        cmd.Parameters.AddWithValue("@AuthorId", authorid);
                        cmd.ExecuteNonQueryAsync();
                    }
                    // Insert new ArticleMatrices
                    string insertQuery = "INSERT INTO ArticleMatrices (AuthorId, Author, Type, Link, Title, PubDate, Category, Views, ViewsCount, Likes) VALUES (@AuthorId, @Author, @Type, @Link, @Title, @PubDate, @Category, @Views, @ViewsCount, @Likes)";
                    using(MySqlCommand cmd2=new MySqlCommand(insertQuery,conn)) 
                    {
                        foreach(ArticleMatrix articlmtrx in articelmatrix)
                        {
                            if(articlmtrx.Category== "Videos")
                            {
                                articlmtrx.Type = "Video";
                            }
                            articlmtrx.Category = articlmtrx.Category.Replace("&amp;", "&");
                            cmd2.Parameters.Clear();
                            cmd2.Parameters.AddWithValue("@AuthorId", articlmtrx.AuthorId);
                            cmd2.Parameters.AddWithValue("@Author", articlmtrx.Author);
                            cmd2.Parameters.AddWithValue("@Type", articlmtrx.Type);
                            cmd2.Parameters.AddWithValue("@Link", articlmtrx.Link);
                            cmd2.Parameters.AddWithValue("@Title", articlmtrx.Title);
                            cmd2.Parameters.AddWithValue("@PubDate", articlmtrx.PubDate);
                            cmd2.Parameters.AddWithValue("@Category", articlmtrx.Category);
                            cmd2.Parameters.AddWithValue("@Views", articlmtrx.Views);
                            cmd2.Parameters.AddWithValue("@ViewsCount", articlmtrx.ViewsCount);
                            cmd2.Parameters.AddWithValue("@Likes", articlmtrx.Likes);

                          await  cmd2.ExecuteNonQueryAsync();

                        }
                    }
                }
                await _cache.RemoveAsync(authorid);
                return true;
            }
            catch
            {
                return false;
            }           
        }

        [HttpGet]
        [Route("getall/{authorid}/{enablecache}")]
        public async Task<List<ArticleMatrix>> GetAllMatrix(string authorid,bool enablecache)
        {
            if (!enablecache)
            {
                var lst = _dal.ListOfMatrix(authorid);
                return lst;
            }
            string cachekey = authorid;

            //trying to get data from cache key first if awailable
            // Trying to get data from the Redis cache
            byte[] cachedData = await _cache.GetAsync(cachekey);
            List<ArticleMatrix> articleMatrices = new();
            if (cachedData != null)
            {
                // If the data is found in the cache, encode and deserialize cached data.
               // var cachedDataString = Encoding.UTF8.GetString(cachedData);
                var cachedDataSting = Encoding.UTF8.GetString(cachedData);
                articleMatrices = System.Text.Json.JsonSerializer.Deserialize<List<ArticleMatrix>>(cachedDataSting);
                    //var serilizaer = new JsonSerializer();
                    //articleMatrices = serilizaer.Deserialize<List<ArticleMatrix>>(jsonreaer);
                
              }
            else
            {
                // If the data is not found in the cache, then fetch data from database

               articleMatrices = _dal.ListOfMatrix(authorid);
                //serilize the data into cache
                string cachedDataString = JsonSerializer.Serialize(articleMatrices);

                var dataTocahce = Encoding.UTF8.GetBytes(cachedDataString);

                // Setting up the cache options

                DistributedCacheEntryOptions option = new DistributedCacheEntryOptions()
                     .SetAbsoluteExpiration(DateTime.Now.AddMinutes(5))
                     .SetSlidingExpiration(TimeSpan.FromMinutes(3));

                // Add the data into the cache

                await _cache.SetAsync(cachekey, dataTocahce,option);
            }
            return articleMatrices;
        }
    }
}

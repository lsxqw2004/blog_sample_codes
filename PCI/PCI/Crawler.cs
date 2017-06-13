using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using HtmlAgilityPack;

namespace ConsoleApplication1
{
    public class Crawler : IDisposable
    {
        private HttpClient _httpClient;
        private IDbConnection _connection;

        private static readonly HashSet<string> IgnoreWords
            = new HashSet<string>(new[] { "the", "of", "to", "and", "a", "in", "is", "it" });

        //构造函数，接收数据库名作为参数
        public Crawler(string dbname)
        {
            _httpClient = new HttpClient();
            _connection = GetConn(dbname);
        }

        //辅助函数，用于获取条目Id，且如果条目不存在，就将其加入数据库中
        public int GetEntryId(string table, string field, string value, bool createnew = true)
        {
            var id = _connection.Query<int>($"select rowid from {table} where {field}='{value}'").FirstOrDefault();
            if (id == 0)
            {
                var lastrowid = _connection
                    .ExecuteScalar<int>($"insert into {table} ({field}) values ('{value}');SELECT last_insert_rowid();");
                return lastrowid;
            }
            return id;
        }

        //为每个网页建立索引
        public async Task AddtoIndex(string url, HtmlDocument soup)
        {
            if (IsIndexed(url)) return;
            Console.WriteLine("Indexing " + url);

            // 获取每个单词
            var text = GetTextOnly(soup);
            var words = SeparateWords(text);

            // 得到URL的id
            var urlid = GetEntryId("urllist", "url", url);

            // 将每个单词与该url关联
            for (int i = 0; i < words.Count; i++)
            {
                Console.WriteLine(words[i]);
                var word = words[i];
                if (IgnoreWords.Contains(word)) continue;
                var wordid = GetEntryId("wordlist", "word", word);
                _connection.Execute(@"insert into wordlocation(urlid,wordid,location) 
                                        values (@urlid, @wordid, @i)", new { urlid, wordid, i });
            }
        }

        //从一个HTML网页提取文字（不带html标签）
        public string GetTextOnly(HtmlDocument soup)
        {
            if (soup == null) return string.Empty;
            return soup.DocumentNode.InnerText;
        }

        //分词
        public List<string> SeparateWords(string text)
        {
            return Regex.Split(text, "\\W")
                .Where(s => s.Length > 1)
                .Select(s => s.ToLower()).ToList();
        }

        //如果Url已经建立索引，则返回true
        public bool IsIndexed(string url)
        {
            Console.WriteLine(url);
            var u = _connection
                .QueryFirstOrDefault<int>($"select rowid from urllist where url='{url}'");
            if (u > 0)
            {
                //检查它是否已经被检索过
                var v = _connection.Query($"select * from wordlocation where urlid={u}");
                if (v.Any())
                    return true;
            }
            return false;
        }


        public void AddLinkref(string urlFrom, string urlTo, string linkText)
        {
            var words = SeparateWords(linkText);
            var fromid = GetEntryId("urllist", "url", urlFrom);
            var toid = GetEntryId("urllist", "url", urlTo);
            if (fromid == toid) return;
            var linkid = _connection
                    .ExecuteScalar<int>($"insert into link(fromid,toid) values ({fromid},{toid});SELECT last_insert_rowid();");
            foreach (var word in words)
            {
                if (IgnoreWords.Contains(word)) continue;
                var wordid = GetEntryId("wordlist", "word", word);
                _connection.Execute($"insert into linkwords(linkid,wordid) values ({linkid},{wordid})");
            }
        }

        //从一小组网页开始进行广度优先搜索，直至某一给定深度，期间为网页建立索引
        public async Task Crawl(List<string> pages, int depth = 2)
        {
            for (int i = 0; i < depth; i++)
            {
                var newpages = new List<string>();
                foreach (var page in pages.ToList())
                {
                    var doc = await GetHtmlDoc(page);
                    if (doc == null) continue;
                    await AddtoIndex(page, doc);

                    var links = doc.DocumentNode.Descendants("a");
                    foreach (var link in links)
                    {
                        var attr = link.Attributes["href"];
                        if (attr != null && !attr.Value.StartsWith("#"))
                        {
                            var url = UrlJoin(page, attr.Value);

                            if (url.Contains("'"))
                                continue;

                            if (string.IsNullOrEmpty(url))
                                continue;
                            url = url.Split('#')[0];
                            if (url.StartsWith("http") && !IsIndexed(url))
                                newpages.Add(url);
                            var linkText = link.InnerText;
                            AddLinkref(page, url, linkText);
                        }
                    }
                }
                pages.Clear();
                pages.AddRange(newpages);
            }
        }

        public async Task<HtmlDocument> GetHtmlDoc(string url)
        {
            var content = default(string);
            try
            {
                content = await _httpClient.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not open: {url} , ex: {ex.Message}");
                return null;
            }
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(content);
            return doc;
        }

        //创建数据库表
        public void CreateIndexTables()
        {
            using (var trans = _connection.BeginTransaction())
            {
                try
                {
                    trans.Connection.Execute("create table urllist(url)");
                    trans.Connection.Execute("create table wordlist(word)");
                    trans.Connection.Execute("create table wordlocation(urlid,wordid,location)");
                    trans.Connection.Execute("create table link(fromid integer,toid integer)");
                    trans.Connection.Execute("create table linkwords(wordid,linkid)");
                    trans.Connection.Execute("create index wordidx on wordlist(word)");
                    trans.Connection.Execute("create index urlidx on urllist(url)");
                    trans.Connection.Execute("create index wordurlidx on wordlocation(wordid)");
                    trans.Connection.Execute("create index urltoidx on link(toid)");
                    trans.Connection.Execute("create index urlfromidx on link(fromid)");
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    trans.Rollback();
                }
            }
        }

        public static string UrlJoin(string urlBase, string urlRel)
        {
            Uri result = null;
            if (Uri.TryCreate(new Uri(urlBase), urlRel, out result))
            {
                return result.ToString();
            }
            return null;
        }

        public void CalculatePageRank(int iterations = 20)
        {
            _connection.Execute("drop table if exists pagerank");
            _connection.Execute("create table pagerank(urlid primary key, score)");

            //初始化所有url，将其PageRank设为1
            _connection.Execute("insert into pagerank select rowid, 1.0 from urllist");

            for (int i = 0; i < iterations; i++)
            {
                Console.WriteLine($"Iterations {i+1}");
                var urlids = _connection.Query<int>("select rowid from urllist");
                foreach (var urlid in urlids)
                {
                    var pr = 0.15f;
                    //循环遍历指向当前网页的所有其他网页
                    var fromids = _connection.Query<int>($"select distinct fromid from link where toid={urlid}");
                    foreach (var linker in fromids)
                    {
                        //得到链接对应网页的PageRank值
                        var linkingpr =
                            _connection.QueryFirstOrDefault<float>($"select score from pagerank where urlid={linker}");
                        //查询链接对应网页所有href的数目
                        var linkingcount =
                            _connection.ExecuteScalar<int>($"select count(*) from link where fromid={linker}");
                        pr += 0.85f*(linkingpr/linkingcount);//能进入这个循环，linkingcount就不会为0
                    }
                    _connection.Execute($"update pagerank set score={pr} where urlid={urlid}");
                }
            }
        }

        public void TopPage()
        {
            var tops = _connection.Query("select * from pagerank order by score desc").Take(3).ToList();
            foreach (var cur in tops)
                Console.WriteLine($"{cur.urlid} - {cur.score}");
        }

        public IDbConnection GetConn(string dbname)
        {
            DbProviderFactory fact = DbProviderFactories.GetFactory("System.Data.SQLite");
            DbConnection cnn = fact.CreateConnection();
            cnn.ConnectionString = $"Data Source={dbname}";
            cnn.Open();
            return cnn;
        }

        public void Dispose()
        {
            _connection.Close();
        }
    }
}

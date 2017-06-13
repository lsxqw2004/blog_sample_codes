using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace ConsoleApplication1
{
    public class NewsFeatures
    {
        private static readonly List<string> Feedlist = new List<string>()
        {
            "http://feeds.reuters.com/reuters/topNews",
            "http://feeds.reuters.com/Reuters/domesticNews",
            "http://feeds.reuters.com/Reuters/worldNews",
            "http://hosted.ap.org/lineups/TOPHEADS-rss_2.0.xml",
            "http://hosted.ap.org/lineups/USHEADS-rss_2.0.xml",
            "http://hosted.ap.org/lineups/WORLDHEADS-rss_2.0.xml",
            "http://hosted.ap.org/lineups/POLITICSHEADS-rss_2.0.xml",
            "http://rss.nytimes.com/services/xml/rss/nyt/HomePage.xml",
            "http://rss.nytimes.com/services/xml/rss/nyt/International.xml",
            "https://news.google.com/?output=rss",
            "http://www.foxnews.com/xmlfeed/rss/0,4313,0,00.rss",
            "http://rss.cnn.com/rss/edition.rss",
            "http://rss.cnn.com/rss/edition_world.rss",
            "http://rss.cnn.com/rss/edition_us.rss"
        };


        public string StripHtml(string h)
        {
            var psb = new StringBuilder();
            var s = 0;
            foreach (var c in h)
            {
                if (c == '<')
                    s = 1;
                else if (c == '>')
                {
                    s = 0;
                    psb.Append(" ");
                }
                else if (s == 0)
                    psb.Append(c);
            }
            return psb.ToString();
        }

        public List<string> SeparateWords(string text)
        {
            return Regex.Split(text, "\\W")
                .Where(s => s.Length > 3)
                .Select(s => s.ToLower()).ToList();
        }

        public Tuple<Dictionary<string, int>, 
            Dictionary<int, Dictionary<string, int>>, 
            List<string>> GetArticleWords()
        {
            var allWords = new Dictionary<string, int>();
            var articleWords = new Dictionary<int, Dictionary<string, int>>();
            var articleTitles = new List<string>();
            var ec = 0;
            // 遍历所有RssFeed
            foreach (var feed in Feedlist)
            {
                var f = FeedParser.Parse(feed);

                // 遍历每篇文章
                foreach (var e in f.Items)
                {
                    //跳过标题相同的文章
                    if (articleTitles.Contains(e.Title.Text))
                        continue;
                    // 提取单词
                    var txt = e.Title.Text + StripHtml(e.Summary.Text);
                    var words = SeparateWords(txt);
                    articleTitles.Add(e.Title.Text);

                    // 在allWords和articleWords中增加针对当前单词的计数
                    foreach (var word in words)
                    {
                        if (!allWords.ContainsKey(word))
                            allWords.Add(word, 0);
                        allWords[word] += 1;
                        if (!articleWords.ContainsKey(ec))
                            articleWords.Add(ec, new Dictionary<string, int>());
                        if (!articleWords[ec].ContainsKey(word))
                            articleWords[ec].Add(word, 0);
                        articleWords[ec][word] += 1;
                    }
                    ec += 1;
                }
            }
            return Tuple.Create(allWords, articleWords, articleTitles);
        }

        public Tuple<List<List<int>>, List<string>> MakeMatrix(Dictionary<string, int> allW, Dictionary<int, Dictionary<string, int>> articleW)
        {
            var wordVec = new List<string>();

            // 只考虑普通的但又不是非常普通的单词
            foreach (var kvp in allW)
            {
                var w = kvp.Key;
                var c = kvp.Value;
                if (c > 3 && c < articleW.Count * 0.6)
                    wordVec.Add(w);
            }

            // 构造单词矩阵
            var ll = new List<List<int>>();
            foreach (var f in articleW.Values)
            {
                var r= wordVec.Select(word =>
                    {
                        if (f.ContainsKey(word)) return f[word];
                        return 0;
                    })
                    .ToList();
                ll.Add(r);
            }
            return Tuple.Create(ll, wordVec);
        }

    }

    public class FeedParser
    {
        public static SyndicationFeed Parse(string url)
        {
            using (XmlReader reader = XmlReader.Create(url))
            {
                try
                {
                    SyndicationFeed feed = SyndicationFeed.Load(reader);
                    return feed;
                }
                catch (Exception e)
                {
                    return null;
                }
                finally
                {
                    reader.Close();
                }
            }

        }
    }
}


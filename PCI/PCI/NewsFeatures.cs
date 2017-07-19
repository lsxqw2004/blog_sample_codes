using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CodeHollow.FeedReader;
using MathNet.Numerics.LinearAlgebra;
using Newtonsoft.Json;

namespace PCI
{
    public class NewsFeatures
    {
        private static readonly List<string> Feedlist = new List<string>()
        {
            "http://feeds.reuters.com/reuters/topNews",
            "http://feeds.reuters.com/Reuters/domesticNews",
            "http://feeds.reuters.com/Reuters/worldNews",
            //"http://hosted.ap.org/lineups/TOPHEADS-rss_2.0.xml",
            //"http://hosted.ap.org/lineups/USHEADS-rss_2.0.xml",
            //"http://hosted.ap.org/lineups/WORLDHEADS-rss_2.0.xml",
            //"http://hosted.ap.org/lineups/POLITICSHEADS-rss_2.0.xml",
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

        public (Dictionary<string, int>, Dictionary<int, Dictionary<string, int>>,
            List<string>) GetArticleWords()
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
                    if (articleTitles.Contains(e.Title))
                        continue;
                    // 提取单词
                    var txt = e.Title + StripHtml(e.Description??string.Empty);
                    var words = SeparateWords(txt);
                    articleTitles.Add(e.Title);

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
            return (allWords, articleWords, articleTitles);
        }

        public (List<List<double>>, List<string>) MakeMatrix(Dictionary<string, int> allW, 
            Dictionary<int, Dictionary<string, int>> articleW)
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
            var ll = new List<List<double>>();
            foreach (var f in articleW.Values)
            {
                var r = wordVec.Select(word =>
                     {
                         if (f.ContainsKey(word)) return f[word];
                         return 0d;
                     })
                    .ToList();
                ll.Add(r);
            }
            return (ll, wordVec);
        }

        public (List<List<ArticleAndFeature>>, List<string>) ShowFeatures(Matrix<double> w, Matrix<double> h,
            List<string> titles, List<string> wordvec, string @out = "features.txt")
        {
            using (var fs = File.Create(@out))
            {
                var sw = new StreamWriter(fs);
                var pc = h.RowCount;
                var wc = h.ColumnCount;
                var topPatterns = Enumerable.Repeat(new List<ArticleAndFeature>(), titles.Count).ToList();
                var patternNames = new List<string>();

                //遍历所有特征
                for (int i = 0; i < pc; i++)
                {
                    var slist = new List<ValueTuple<double, string>>();
                    //构造包含单词及其权重数据的列表
                    for (int j = 0; j < wc; j++)
                    {
                        slist.Add((h[i, j], wordvec[j]));
                    }
                    //按单词对特征贡献度值倒叙排序
                    slist.Sort((x,y)=>y.Item1.CompareTo(x.Item1));
                    //打印开始的6个元素
                    var n = string.Join(",", slist.Take(6));
                    sw.WriteLine($"[{n}]");
                    patternNames.Add(n);

                    //构造针对该特征的文章列表
                    var flist = new List<ValueTuple<double, string>>();
                    for (int j = 0; j < titles.Count; j++)
                    {
                        //加入文章及权重数据
                        flist.Add((w[j, i], titles[j]));
                        topPatterns[j].Add(new ArticleAndFeature(w[j, i], i, titles[j]));
                    }
                    // 按特征对于文章的匹配程度有高到低排列
                    flist.Sort((x,y)=>y.Item1.CompareTo(x.Item1));
                    //显示前3篇文章
                    foreach (var kvp in flist.Take(3))
                    {
                        sw.WriteLine($"({kvp.Item1},{kvp.Item2})");
                        sw.WriteLine();
                    }
                }
                sw.Close();
                // 返回模式名称，后面要用
                return (topPatterns, patternNames);
            }
        }

        public void ShowArticles(List<string> titles,
            List<List<ArticleAndFeature>> topPatterns,
            List<string> patternNames, string @out = "articles.txt")
        {
            using (var fs = File.Create(@out))
            {
                var sw = new StreamWriter(fs);

                // 遍历所有文章
                for (int j = 0; j < titles.Count; j++)
                {
                    sw.WriteLine(titles[j]);

                    // 针对当前文章，获得排位最靠前(倒序下)的几个特征
                    topPatterns[j].Sort();

                    // 打印前3个模式
                    for (int i = 0; i < 3; i++)
                    {
                        sw.WriteLine($@"{topPatterns[j][i].Weight} 
                            {JsonConvert.SerializeObject(patternNames[topPatterns[j][i].FeatureIdx])}");
                    }
                    sw.WriteLine();
                }
                sw.Close();
            }
        }

        public class ArticleAndFeature : IComparable<ArticleAndFeature>
        {
            public ArticleAndFeature(double weight, int featureIdx, string articleTitle)
            {
                Weight = weight;
                FeatureIdx = featureIdx;
                ArticleTitle = articleTitle;
            }

            public double Weight { get; set; }

            public int FeatureIdx { get; set; }

            public string ArticleTitle { get; set; }

            public int CompareTo(ArticleAndFeature right)
            {
                return right.Weight.CompareTo(Weight);//由大到小排序
            }
        }
    }

    public class FeedParser
    {
        public static Feed Parse(string url)
        {
            try
            {
                // 这种反异步的方式应该只用于测试中
                var feed = FeedReader.ReadAsync(url).GetAwaiter().GetResult();
                return feed;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}


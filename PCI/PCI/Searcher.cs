using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace ConsoleApplication1
{
    public class Searcher
    {
        private IDbConnection _connection;

        public Searcher(string dbname)
        {
            _connection = GetConn(dbname);
        }

        public Tuple<List<List<int>>, List<int>> GetMatchRows(string q)
        {
            //构造查询的字符串
            var fieldlist = "w0.urlid";
            var tablelist = "";
            var clauselist = "";
            List<int> wordids = new List<int>();

            //根据空格拆分单词
            var words = q.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int tablenumber = 0;

            foreach (var word in words)
            {
                //获取单词的Id
                var wordrow = _connection.QueryFirstOrDefault<int>($"select rowid from wordlist where word='{word}'");
                if (wordrow != 0)
                {
                    var wordid = wordrow;
                    wordids.Add(wordid);
                    if (tablenumber > 0)
                    {
                        tablelist += ",";
                        clauselist += " and ";
                        clauselist += $"w{tablenumber - 1}.urlid=w{tablenumber}.urlid and ";
                    }
                    fieldlist += $",w{tablenumber}.location as loc{ tablenumber}";
                    tablelist += $"wordlocation w{tablenumber}";
                    clauselist += $"w{tablenumber}.wordid={wordid}";
                    ++tablenumber;
                }
            }
            //根据各个分组，建立查询
            var fullquery = $"select {fieldlist} from {tablelist} where {clauselist}";
            var rows = _connection.Query(fullquery).ToList().Select(r =>
            {
                var rowDic = (IDictionary<string, object>)r;
                var intLst = new List<int>();
                intLst.Add(Convert.ToInt32(rowDic["urlid"]));
                for (int i = 0; i < tablenumber; i++)
                {
                    intLst.Add(Convert.ToInt32(rowDic[$"loc{i}"]));
                }
                return intLst;
            }).ToList();

            return Tuple.Create(rows, wordids);
        }

        public Dictionary<int, float> GetScoredList(List<List<int>> rows, List<int> wordids)
        {
            var totalscores = rows.Select(r => r[0]).Distinct().ToDictionary(r => r, r => 0f);

            var weights = GetWeights(rows, wordids);

            foreach (var tuple in weights)
            {
                var weight = tuple.Item1;
                var scores = tuple.Item2;
                foreach (var tsKey in totalscores.Keys.ToList())
                {
                    totalscores[tsKey] += weight * scores[tsKey];
                }
            }
            return totalscores;
        }

        public string GetUrlName(int id)
        {
            return _connection.QueryFirstOrDefault<string>($"select url from urllist where rowid={id}");
        }

        public Tuple<List<int>, List<int>> Query(string q)
        {
            var matchRows = GetMatchRows(q);
            var rows = matchRows.Item1;
            var wordids = matchRows.Item2;
            var scores = GetScoredList(rows, wordids);
            var rankedscores = new SortedList<float, int>(new RankComparer());
            foreach (var score in scores)
            {
                rankedscores.Add(score.Value, score.Key);
            }
            foreach (var scoreKvp in rankedscores.Take(10))
            {
                Console.WriteLine($"{scoreKvp.Key}\t{GetUrlName(scoreKvp.Value)}");
            }
            return Tuple.Create(wordids,
                                rankedscores.Take(10).Select(r => r.Value).ToList());
        }

        class RankComparer : IComparer<float>
        {
            public int Compare(float x, float y)
            {
                if (x == y)//这样可以让SortedList保存重复的key
                    return 1;
                return y.CompareTo(x);
            }
        }

        public Dictionary<int, float> FrequencyScore(List<List<int>> rows)
        {
            var counts = new Dictionary<int, float>();
            foreach (var row in rows)
            {
                if (counts.ContainsKey(row[0]))
                    counts[row[0]] += 1f;
                else
                    counts.Add(row[0], 1f);
            }
            return NormalizeScores(counts);
        }

        public Dictionary<int, float> LocationScore(List<List<int>> rows)
        {
            var locations = rows.Select(r => r[0]).Distinct().ToDictionary(r => r, r => 1000000f);
            foreach (var row in rows)
            {
                var loc = row.Skip(1).Sum();
                if (loc < locations[row[0]])
                    locations[row[0]] = loc;
            }
            return NormalizeScores(locations, smallIsBetter: true);
        }

        public Dictionary<int, float> DistanceScore(List<List<int>> rows)
        {
            //如果只有一个单词，则所有网页的得分一样
            if (rows[0].Count <= 2)
                return rows.Select(r => r[0]).Distinct().ToDictionary(r => r, r => 1f);

            var mindistance = rows.Select(r => r[0]).Distinct().ToDictionary(r => r, r => 1000000f);
            foreach (var row in rows)
            {
                var dist = row.Skip(2).Select((r, i) => Math.Abs(r - row[i + 1])).Sum();
                if (dist < mindistance[row[0]])
                    mindistance[row[0]] = dist;
            }
            return NormalizeScores(mindistance, smallIsBetter: true);
        }

        public Dictionary<int, float> PageRankscore(List<List<int>> rows)
        {
            //由于本例中pagerank表与urllist表的urlid相同，构建pagerank dic可以简化为这样
            var pageranks = _connection.Query("select urlid, score from pagerank").ToList()
                .ToDictionary(r => (int)r.urlid, r => (float)r.score);
            var maxrank = pageranks.Values.Max();
            var normalizedscores = NormalizeScores(pageranks.ToDictionary(pr => pr.Key, pr => pr.Value / maxrank));
            return normalizedscores;
        }

        public Dictionary<int, float> InboundLinkScore(List<List<int>> rows)
        {
            var uniqueUrls = rows.Select(r => r[0]).Distinct().ToList();
            var inboundCount = uniqueUrls.ToDictionary(
                u => u,
                u => (float)_connection.ExecuteScalar<int>($"select count(*) from link where toid={u}"));
            return NormalizeScores(inboundCount);
        }

        public Dictionary<int, float> NnScore(List<List<int>> rows, List<int> wordids)
        {
            var myNet = new SearchNet("searchindex.db3");
            //获得要给由唯一Url Id构成的有序列表
            var urlids = rows.Select(r => r[0]).Distinct().ToList();
            var nnres = myNet.GetResult(wordids, urlids);
            var scores = nnres.Select((n, i) => Tuple.Create(urlids[i], n)).ToDictionary(t => t.Item1, t => t.Item2);
            return NormalizeScores(scores);
        }

        public List<Tuple<float, Dictionary<int, float>>> GetWeights(List<List<int>> rows, List<int> wordids)
        {
            return new List<Tuple<float, Dictionary<int, float>>>()
            {
                Tuple.Create(1.0f,LocationScore(rows)),
                Tuple.Create(1.0f, PageRankscore(rows)),
                Tuple.Create(1.0f, NnScore(rows,wordids))
            };
        }

        public Dictionary<int, float> LinkTextScore(List<List<int>> rows, List<int> wordids)
        {
            var linkscores = rows.Select(r => r[0]).Distinct().ToDictionary(r => r, r => 0f);
            foreach (var wordid in wordids)
            {
                var cur = _connection.Query(@"select link.fromid, link.toid from linkwords, link 
                                                where wordid=@wordid and linkwords.linkid = link.rowid", new { wordid });
                foreach (var c in cur)
                {
                    var fromid = (int)c.fromid;
                    var toid = (int)c.toid;
                    if (linkscores.ContainsKey(toid))
                    {
                        var pr = _connection.QueryFirstOrDefault<float>(@"select score from pagerank where urlid=@fromid", new { fromid });
                        linkscores[toid] += pr;
                    }
                }
            }
            var maxscore = linkscores.Values.Max();
            var normalizedscores = NormalizeScores(linkscores.ToDictionary(pr => pr.Key, pr => pr.Value / maxscore));
            return normalizedscores;
        }

        public Dictionary<int, float> NormalizeScores(Dictionary<int, float> scores, bool smallIsBetter = false)
        {
            var vsmall = 0.00001f; //避免被0除
            if (smallIsBetter)
            {
                var minScore = scores.Values.Min();
                return scores.ToDictionary(s => s.Key, s => minScore / Math.Max(vsmall, s.Value));
            }
            else
            {
                var maxScore = scores.Values.Max();
                if (maxScore == 0)
                    maxScore = vsmall;
                return scores.ToDictionary(s => s.Key, s => s.Value / maxScore);
            }
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

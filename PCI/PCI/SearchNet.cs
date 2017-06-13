using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace ConsoleApplication1
{
    public class SearchNet
    {
        private IDbConnection _connection;

        public SearchNet(string dbname)
        {
            _connection = GetConn(dbname);
        }

        public void MakeTables()
        {
            _connection.Execute("create table hiddennode(create_key)");
            _connection.Execute("create table wordhidden(fromid, toid,strength)");
            _connection.Execute("create table hiddenurl (fromid,toid,strength)");
        }

        public float GetStrength(int fromid, int toid, int layer)
        {
            string table;
            if (layer == 0) table = "wordhidden";
            else table = "hiddenurl";
            var res = _connection.QueryFirstOrDefault<float?>
                ($"select strength from {table} where fromid={fromid} and toid={toid}");
            if (!res.HasValue)
            {
                if (layer == 0) return -0.2f;
                if (layer == 1) return 0f;
            }
            return res.Value;
        }

        public void SetStrength(int fromid, int toid, int layer, float strength)
        {
            string table;
            if (layer == 0) table = "wordhidden";
            else table = "hiddenurl";
            var res = _connection.QueryFirstOrDefault<int?>
                ($"select rowid from {table} where fromid={fromid} and toid={toid}");
            if (!res.HasValue)
            {
                _connection.Execute($@"insert into {table} (fromid,toid,strength) 
                                        values ({fromid},{toid},{strength})");
            }
            else
            {
                var rowid = res.Value;
                _connection.Execute($@"update {table} set strength={strength} 
                                        where rowid={rowid}");
            }
        }

        public void GenerateHiddenNode(List<int> wordids, List<int> urls)
        {
            if (wordids.Count > 3) return;
            //检查是否已经为这组单词建好了一个节点
            var createkey = string.Join("_", wordids.OrderBy(w => w));
            var res = _connection.QueryFirstOrDefault<int?>(
                $"select rowid from hiddennode where create_key = '{createkey}'");
            //如果不存在则建立
            if (!res.HasValue)
            {
                var hiddenid = _connection.ExecuteScalar<int>(
                    $@"insert into hiddennode (create_key) values ('{createkey}');
                                        SELECT last_insert_rowid();");
                //设置默认权重
                foreach (var wordid in wordids)
                    SetStrength(wordid, hiddenid, 0,1.0f/wordids.Count);
                foreach (var urlid in urls)
                    SetStrength(hiddenid,urlid,1,0.1f);
            }
        }

        public void ShowHiddens()
        {
            var wordHiddens = _connection.Query("select * from wordhidden");
            foreach (var wh in wordHiddens)
                Console.WriteLine($"({wh.fromid}, {wh.toid}, {wh.strength})");
            var hiddenUrls = _connection.Query("select * from hiddenurl");
            foreach (var hu in hiddenUrls)
                Console.WriteLine($"({hu.fromid}, {hu.toid}, {hu.strength})");
        }

        public List<int> GetAllHiddenIds(List<int> wordids, List<int> urlids)
        {
            var l1 = new HashSet<int>();
            foreach (var wordid in wordids)
            {
                var cur = _connection.Query<int>($"select toid from wordhidden where fromid = {wordid}");
                l1.UnionWith(cur);
            }
            foreach (var urlid in urlids)
            {
                var cur = _connection.Query<int>($"select fromid from hiddenurl where toid={urlid}");
                l1.UnionWith(cur);
            }
            return l1.ToList();
        }

        private List<int> _wordids;
        private List<int> _hiddenids;
        private List<int> _urlids;

        private List<float> _ai;
        private List<float> _ah;
        private List<float> _ao;

        private List<List<float>> _wi;
        private List<List<float>> _wo;

        public void SetupNetwork(List<int> wordids, List<int> urlids)
        {
            //值列表
            _wordids = wordids;
            _hiddenids = GetAllHiddenIds(wordids, urlids);
            _urlids = urlids;
            //节点输出
            _ai = ArrayList.Repeat(1.0f, _wordids.Count).Cast<float>().ToList();
            _ah = ArrayList.Repeat(1.0f, _hiddenids.Count).Cast<float>().ToList();
            _ao = ArrayList.Repeat(1.0f, _urlids.Count).Cast<float>().ToList();
            //建立权重矩阵
            _wi = _wordids
                .Select(w=>_hiddenids.Select(h=>GetStrength(w,h,0)).ToList())
                .ToList();
            _wo = _hiddenids
                .Select(h => _urlids.Select(u => GetStrength(h, u, 1)).ToList())
                .ToList();
        }

        public List<float> FeedForward()
        {
            //查询单词是仅有的输入（这一步好像有点多余）
            for (int i = 0; i < _wordids.Count; i++)
                _ai[i] = 1.0f;
            //隐藏层节点的活跃程度
            for (int j = 0; j < _hiddenids.Count; j++)
            {
                var sum = 0f;
                for (int i = 0; i < _wordids.Count; i++)
                    sum = sum + _ai[i]*_wi[i][j];
                _ah[j] = (float)Math.Tanh(sum);
            }
            //输出层节点活跃程度
            for (int k = 0; k < _urlids.Count; k++)
            {
                var sum = 0f;
                for (int j = 0; j < _hiddenids.Count; j++)
                    sum = sum + _ah[j] * _wo[j][k];
                _ao[k] = (float)Math.Tanh(sum);
            }

            return _ao.ToList();
        }

        public List<float> GetResult(List<int> wordids, List<int> urlids)
        {
            SetupNetwork(wordids,urlids);
            return FeedForward();
        }

        public float Dtanh(float y)
        {
            return 1.0f - y*y;
        }

        public void BackPropagate(List<float> targes, float N = 0.5f)
        {
            //计算输出层的误差
            var out_deltas = ArrayList.Repeat(0.0f, _urlids.Count).Cast<float>().ToList();
            for (int k = 0; k < _urlids.Count; k++)
            {
                var error = targes[k] - _ao[k];
                out_deltas[k] = Dtanh(_ao[k])*error;
            }
            //计算隐藏层的误差
            var hidden_deltas = ArrayList.Repeat(0.0f, _wordids.Count).Cast<float>().ToList();
            for (int j = 0; j < _hiddenids.Count; j++)
            {
                var error = 0f;
                for (int k = 0; k < _urlids.Count; k++)
                    error += out_deltas[k]*_wo[j][k];
                hidden_deltas[j] = Dtanh(_ah[j]) * error;
            }
            //更新输出权重
            for (int j = 0; j < _hiddenids.Count; j++)
            {
                for (int k = 0; k < _urlids.Count; k++)
                {
                    var change = out_deltas[k]*_ah[j];
                    _wo[j][k] += N*change;
                }
            }
            //更新输入权重
            for (int i = 0; i < _wordids.Count; i++)
            {
                for (int j = 0; j < _hiddenids.Count; j++)
                {
                    var change = hidden_deltas[j] * _ai[i];
                    _wi[i][j] += N * change;
                }
            }
        }

        public void TrainQuery(List<int> wordids, List<int> urlids, int selectedUrl)
        {
            //如有必要，生成一个隐藏节点
            //GenerateHiddenNode(wordids, urlids);

            SetupNetwork(wordids,urlids);
            FeedForward();
            var targets = ArrayList.Repeat(0.0f, _urlids.Count).Cast<float>().ToList();
            targets[urlids.IndexOf(selectedUrl)] = 1.0f;
            BackPropagate(targets);
            UpdateDatabase();
        }

        public void UpdateDatabase()
        {
            //将值存入数据库中
            for (int i = 0; i < _wordids.Count; i++)
            {
                for (int j = 0; j < _hiddenids.Count; j++)
                {
                    SetStrength(_wordids[i],_hiddenids[j],0,_wi[i][j]);
                }
            }
            for (int j = 0; j < _hiddenids.Count; j++)
            {
                for (int k = 0; k < _urlids.Count; k++)
                {
                    SetStrength(_hiddenids[j],_urlids[k],1,_wo[j][k]);
                }
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
    }
}

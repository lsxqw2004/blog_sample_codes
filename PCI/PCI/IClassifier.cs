using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Dapper;

namespace ConsoleApplication1
{
    public interface IClassifier
    {
        int CatCount(Cat cat);
        List<Cat> Categories();
        int Fcount(string f, Cat cat);
        float Fprob(string f, Cat cat);
        void Incc(Cat cat);
        void Incf(string f, Cat cat);
        int TotalCount();
        void Train(string item, Cat cat);
        float WeightedProb(string f, Cat cat, Func<string, Cat, float> prf, float weight = 1, float ap = 0.5F);
    }

    public class SqliteClassifier : IClassifier
    {
        private IDbConnection _connection;

        public SqliteClassifier(Func<string, Dictionary<string, int>> getFeatures, string dbname)
        {
            _getFeatures = getFeatures;
            _connection = GetConn(dbname);
            SetDb();
        }

        public void SetDb()
        {
            _connection.Execute("create table if not exists fc(feature, category, count)");
            _connection.Execute("create table if not exists cc(category,count)");
        }

        public IDbConnection GetConn(string dbname)
        {
            DbProviderFactory fact = DbProviderFactories.GetFactory("System.Data.SQLite");
            DbConnection cnn = fact.CreateConnection();
            cnn.ConnectionString = $"Data Source={dbname}";
            cnn.Open();
            return cnn;
        }

        protected Func<string, Dictionary<string, int>> _getFeatures;

        public void Incf(string f, Cat cat)
        {
            var count = Fcount(f, cat);
            _connection.Execute(count == 0
                ? $"insert into fc values ('{f}','{cat}', 1)"
                : $"update fc set count={count + 1} where feature='{f}' and category='{cat}'");
        }

        public int Fcount(string f, Cat cat)
        {
            var res = _connection.ExecuteScalar<int>($"select count from fc where feature='{f}' and category='{cat}'");
            return res;
        }

        public void Incc(Cat cat)
        {
            var count = CatCount(cat);
            if (count == 0) _connection.Execute($"insert into cc values ('{cat}', 1)");
            else _connection.Execute($"update cc set count={count + 1} where category='{cat}'");
        }

        public int CatCount(Cat cat)
        {
            var res = _connection.ExecuteScalar<int>($"select count from cc where category='{cat}'");
            return res;    
        }

        public List<Cat> Categories()
        {
            return _connection.Query<string>("select category from cc")
                .ToList()
                .Select(cs => Enum.Parse(typeof(Cat), cs))
                .Cast<Cat>()
                .ToList();
        }

        public int TotalCount()
        {
            var res = _connection.ExecuteScalar<int>("select sum(count) from cc");
            return res;
        }

        public void Train(string item, Cat cat)
        {
            var features = _getFeatures(item);
            //针对该分类为每个特征增加计数值
            foreach (var f in features)
            {
                Incf(f.Key, cat);
            }

            //增加针对该分类的计数值
            Incc(cat);
        }

        public float Fprob(string f, Cat cat)
        {
            if (CatCount(cat) == 0) return 0;
            // 特征在分类中出现的总次数，除以分类中包含的内容项总数
            return Fcount(f, cat) / (float)CatCount(cat);
        }

        public float WeightedProb(string f, Cat cat, Func<string, Cat, float> prf, float weight = 1.0f, float ap = 0.5f)
        {
            // 计算当前概率
            var basicprob = prf(f, cat);

            // 特征（即单词）在所有分类中出现的次数
            var totals = Categories().Select(c => Fcount(f, c)).Sum();

            // 计算加权平均
            var bp = (weight * ap + totals * basicprob) / (weight + totals);
            return bp;
        }
    }
}
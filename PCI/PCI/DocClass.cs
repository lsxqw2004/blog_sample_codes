using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;

namespace ConsoleApplication1
{
    public class DocClass
    {
        public Dictionary<string, int> GetWords(string doc)
        {
            // 根据非字母字符进行单词拆分
            var words = Regex.Split(doc, "\\W")
                    .Where(s => s.Length > 2 && s.Length < 20)
                    .Select(s => s.ToLower());
            // 只返回一组不重复的单词
            return words.Distinct().ToDictionary(w => w, w => 1);
        }

        public void SampleTrain(IClassifier cl)
        {
            cl.Train("Nobody owns the water.", Cat.Good);
            cl.Train("the quick rabbit jumps fences", Cat.Good);
            cl.Train("buy pharmaceuticals now", Cat.Bad);
            cl.Train("make quick money at the online casino", Cat.Bad);
            cl.Train("the quick brown fox jumps", Cat.Good);
        }

    }


    public enum Cat
    {
        Good, Bad,
        None
    }

    public class Classifier : IClassifier
    {
        // 统计特征/分类组合的数量
        private Dictionary<string, Dictionary<Cat,int>> _fc = new Dictionary<string, Dictionary<Cat, int>>();

        // 统计每个分类中的文档数量
        public Dictionary<Cat, int> _cc = new Dictionary<Cat, int>();

        protected Func<string, Dictionary<string, int>> _getFeatures;

        public Classifier(Func<string, Dictionary<string,int>> getFeatures, string filename = null)
        {
            _getFeatures = getFeatures;
        }

        // 增加对特征/分类组合的计数
        public void Incf(string f, Cat cat)
        {
            if(!_fc.ContainsKey(f))
                _fc.Add(f,new Dictionary<Cat, int>());
            if(!_fc[f].ContainsKey(cat))
                _fc[f].Add(cat, 0);
            _fc[f][cat] += 1;
        }

        // 增加对某一分类的计数值
        public void Incc(Cat cat)
        {
            if(!_cc.ContainsKey(cat))
                _cc.Add(cat,0);
            _cc[cat] += 1;
        }

        // 某一特征出现于某一分类中的次数
        public int Fcount(string f, Cat cat)
        {
            if (_fc.ContainsKey(f) && _fc[f].ContainsKey(cat))
                return _fc[f][cat];
            return 0;
        }

        // 属于某一分类的内容项数量
        public int CatCount(Cat cat)
        {
            if (_cc.ContainsKey(cat))
                return _cc[cat];
            return 0;
        }

        // 所有内容项的数量
        public int TotalCount()
        {
            return _cc.Values.Sum();
        }

        // 所有分类的列表
        public List<Cat> Categories()
        {
            return _cc.Keys.ToList();
        }


        public void Train(string item, Cat cat)
        {
            var features = _getFeatures(item);
            //针对该分类为每个特征增加计数值
            foreach (var f in features)
            {
                Incf(f.Key,cat);
            }

            //增加针对该分类的计数值
            Incc(cat);
        }

        public float Fprob(string f, Cat cat)
        {
            if (CatCount(cat) == 0) return 0;
            // 特征在分类中出现的总次数，除以分类中包含的内容项总数
            return Fcount(f, cat)/(float)CatCount(cat);
        }

        public float WeightedProb(string f, Cat cat, Func<string,Cat,float> prf, float weight = 1.0f, float ap = 0.5f)
        {
            // 计算当前概率
            var basicprob = prf(f, cat);

            // 特征（即单词）在所有分类中出现的次数
            var totals = Categories().Select(c=>Fcount(f,c)).Sum();

            // 计算加权平均
            var bp = (weight*ap + totals*basicprob)/(weight + totals);
            return bp;
        }
    }

    public class NaiveBayes:SqliteClassifier
    {
        public NaiveBayes(Func<string, Dictionary<string, int>> getFeatures, string filename = null) 
            :base(getFeatures,filename)
        {
        }

        public float DocProb(string item, Cat cat)
        {
            var features = _getFeatures(item);
            //将所有特征的概率相乘
            return features.Select(f => f.Key).Aggregate(1f, (tp, f) => tp*WeightedProb(f, cat, Fprob));
        }

        public float Prob(string item, Cat cat)
        {
            var catProb = CatCount(cat) / (float)TotalCount();
            var docProb = DocProb(item, cat);
            return docProb * catProb;
        }

        public Dictionary<Cat, float> Thresholds { get; } = new Dictionary<Cat, float>()
        {
            [Cat.Bad] = 1f,
            [Cat.Good] = 1f
        };

        public Cat Classify(string item, Cat defaultc = Cat.None)
        {
            var probs = new Dictionary<Cat, float>();
            //寻找概率最大的分类
            var max = 0f;
            var best = defaultc;
            foreach (var cat in Categories())
            {
                probs.Add(cat, Prob(item, cat));
                if (probs[cat] > max)
                {
                    max = probs[cat];
                    best = cat;
                }
            }
            //确保概率值超出阈值*次大概率值
            foreach (var cat in probs.Keys)
            {
                if (cat == best) continue;
                if (probs[cat] * Thresholds[best] > probs[best]) return defaultc;
            }
            return best;
        }
    }

    public class FisherClassifier : SqliteClassifier
    {
        public FisherClassifier(Func<string, Dictionary<string, int>> getFeatures, string filename = null) 
            : base(getFeatures, filename)
        {
        }

        public float Cprob(string f, Cat cat)
        {
            //特征在该分类中出现的频率
            var clf = Fprob(f, cat);
            if (clf == 0) return 0;

            //特征在所有分类中出现的频率
            var freqsum = Categories().Select(c => Fprob(f, c)).Sum();

            //概率等于特征在该分类中出现的频率除以总体频率
            var p = clf / freqsum;
            return p;
        }

        public float FisherProb(string item, Cat cat)
        {
            //将所有概率值相乘
            var features = _getFeatures(item).Keys;
            var p = features.Aggregate(1f, (current, f) => current * WeightedProb(f, cat, Cprob));

            //取自然对数，并乘以-2
            var fscore = (float)(-2 * Math.Log(p));

            //利用倒置对数卡方函数
            return Invchi2(fscore, features.Count * 2);
        }

        public float Invchi2(float chi, int df)
        {
            var m = chi / 2;
            float sum, term;
            sum = term = (float)Math.Exp(-m);
            for (int i = 1; i < df/2; i++)
            {
                term *= m / i;
                sum += term;
            }
            return Math.Min(sum, 1f);
        }

        public Dictionary<Cat, float> Minimum { get; } = new Dictionary<Cat, float>()
        {
            [Cat.Bad] = 0.6f,
            [Cat.Good] = 0.2f
        };

        public Cat Classify(string item, Cat defaultc = Cat.None)
        {
            // 循环遍历并寻找最佳结果
            var best = defaultc;
            var max = 0f;
            foreach (var c in Categories())
            {
                var p = FisherProb(item, c);
                // 确保其超过下限值
                if (p > Minimum[c] && p > max)
                {
                    best = c;
                    max = p;
                }
            }
            return best;
        }
    }
}

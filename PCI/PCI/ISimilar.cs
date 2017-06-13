using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    interface ISimilar
    {
        double Calc(Dictionary<string, Dictionary<string, float>> prefs, string person1, string person2);
    }

    public class SimilarDistance : ISimilar
    {
        public double Calc(Dictionary<string, Dictionary<string, float>> prefs, string person1, string person2)
        {
            //得到双方都评价过的电影列表
            var si = prefs[person1].Keys.Intersect(prefs[person2].Keys).ToList();
            if (!si.Any()) return 0;
            var sumSquares = si.Sum(s => Math.Pow(prefs[person1][s] - prefs[person2][s], 2));
            return 1 / (1 + Math.Sqrt(sumSquares));
        }
    }

    public class SimilarPerson : ISimilar
    {
        public double Calc(Dictionary<string, Dictionary<string, float>> prefs, string person1, string person2)
        {
            //得到双方都评价过的电影列表
            var si = prefs[person1].Keys.Intersect(prefs[person2].Keys).ToList();
            if (!si.Any()) return -1;//没有共同之处，返回-1 (博主注，原文是返回1，感觉是个bug)
            //各种打分求和
            var sum1 = si.Sum(s => prefs[person1][s]);
            var sum2 = si.Sum(s => prefs[person2][s]);
            //打分的平方和
            var sum1Sq = si.Sum(s => Math.Pow(prefs[person1][s], 2));
            var sum2Sq = si.Sum(s => Math.Pow(prefs[person2][s], 2));
            //打分乘积之和
            var pSum = si.Sum(s => prefs[person1][s] * prefs[person2][s]);
            //计算皮尔逊评价值
            var num = pSum - (sum1 * sum2 / si.Count);
            var den = Math.Sqrt((sum1Sq - Math.Pow(sum1, 2) / si.Count) * (sum2Sq - Math.Pow(sum2, 2) / si.Count));
            if (den == 0) return 0;
            return num / den;
        }
    }
}

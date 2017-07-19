using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using MatplotlibCS;
using MatplotlibCS.PlotItems;
using Newtonsoft.Json;

namespace ConsoleApplication1
{
    public class NumPredict
    {
        public double WinePrice(double rating, double age)
        {
            var peakAge = rating - 50;
            //根据等级计算价格
            var price = rating / 2;
            if (age > peakAge)
            {
                //经过“峰值年”，后继5年里其品质将会变差
                price = price * (5 - (age - peakAge) / 2); //原书配书源码有/2，印刷版中没有是个错误，会导致为0的商品过多
            }
            else
            {
                //价格在接近“峰值年”时会增加到原值的5倍
                price = price * (5 * (age + 1) / peakAge);
            }
            if (price < 0)
                price = 0;
            return price;
        }

        public class PriceStructure
        {
            public double[] Input { get; set; }
            public double Result { get; set; }
        }

        public List<PriceStructure> WineSet1()
        {
            var rows = new List<PriceStructure>(300);
            var rnd = new Random();
            for (int i = 0; i < 300; i++)
            {
                //随机生成年代和等级
                var rating = rnd.NextDouble() * 50 + 50;
                var age = rnd.NextDouble() * 50;
                //得到参考价格
                var price = WinePrice(rating, age);
                //增加“噪声”
                price *= rnd.NextDouble() * 0.9 + 0.2; //配书代码的实现
                //加入数据集
                rows.Add(new PriceStructure()
                {
                    Input = new[] { rating, age },
                    Result = price
                });
            }
            return rows;
        }

        public double Euclidean(double[] v1, double[] v2)
        {
            var d = v1.Select((t, i) => (double)Math.Pow(t - v2[i], 2)).Sum();
            return (double)Math.Sqrt(d);
        }

        private SortedDictionary<double, int> GetDistances(List<PriceStructure> data, double[] vec1)
        {
            var distancelist = new SortedDictionary<double, int>(new RankComparer());
            for (int i = 0; i < data.Count; i++)
            {
                var vec2 = data[i].Input;
                distancelist.Add(Euclidean(vec1, vec2), i);
            }
            return distancelist;
        }

        class RankComparer : IComparer<double>
        {
            public int Compare(double x, double y)
            {
                if (x == y) //这样可以让SortedDictionary保存重复的key
                    return 1;
                return x.CompareTo(y); //从小到大排序
            }
        }

        public double KnnEstimate(List<PriceStructure> data, double[] vec1, int k = 5)
        {
            //得到经过排序的距离值
            var dlist = GetDistances(data, vec1);
            return dlist.Values.Take(k).Average(dv => data[dv].Result);
        }

        public double Weightedknn(List<PriceStructure> data, double[] vec1, int k = 5,
            Func<double, double, double> weightf = null)
        {
            if (weightf == null) weightf = Gaussian;
            //得到经过排序的距离值
            var dlist = GetDistances(data, vec1);
            var avg = 0d;
            var totalweight = 0d;

            //var minDist = dlist.First().Key;
            //得到加权平均
            foreach (var kvp in dlist.Take(k))
            {

                //Console.WriteLine(JsonConvert.SerializeObject(kvp));
                var dist = kvp.Key;
                var idx = kvp.Value;
                var weight = weightf(dist, 10);

                avg += weight * data[idx].Result;
                totalweight += weight;
            }
            if (totalweight == 0) return 0;
            avg /= totalweight;
            return avg;
        }

        public double InverseWeight(double dist, double num = 1, double @const = 0.1f)
        {
            return num / (dist + @const);
        }

        public double SubtractWeight(double dist, double @const = 1)
        {
            if (dist > @const) return 0;
            return @const - dist;
        }

        public double Gaussian(double dist, double sigma = 10)
        {
            var exp = (double)Math.Pow(Math.E, -dist * dist / (2 * sigma * sigma));
            return exp;
        }

        public Tuple<List<PriceStructure>, List<PriceStructure>> DivideData(List<PriceStructure> data,
            double test = 0.05f)
        {
            var trainSet = new List<PriceStructure>();
            var testSet = new List<PriceStructure>();
            var rnd = new Random();
            foreach (var row in data)
            {
                if (rnd.NextDouble() < test)
                    testSet.Add(row);
                else
                    trainSet.Add(row);
            }
            return Tuple.Create(trainSet, testSet);
        }

        public double TestAlgorithm(Func<List<PriceStructure>, double[], double> algf,
            List<PriceStructure> trainSet, List<PriceStructure> testSet)
        {
            var error = 0d;
            foreach (var row in testSet)
            {
                var guess = algf(trainSet, row.Input);
                error += Math.Pow(row.Result - guess, 2);
            }
            return error / testSet.Count;
        }

        public double CrossValidate(Func<List<PriceStructure>, double[], double> algf,
            List<PriceStructure> data, int trials = 100, double test = 0.05f)
        {
            var error = 0d;
            for (int i = 0; i < trials; i++)
            {
                var setDiv = DivideData(data, test);
                error += TestAlgorithm(algf, setDiv.Item1, setDiv.Item2);
            }
            return error / trials;
        }

        public List<PriceStructure> WineSet2()
        {
            var rows = new List<PriceStructure>(300);
            var rnd = new Random();
            for (int i = 0; i < 300; i++)
            {
                //随机生成年代和等级
                var rating = rnd.NextDouble() * 50 + 50;
                var age = rnd.NextDouble() * 50;
                var aisle = (double)rnd.Next(1, 20);
                var sizeArr = new[] { 375d, 750d, 1500d, 3000d };
                var bottleSize = sizeArr[rnd.Next(0, 3)];
                //得到参考价格
                var price = WinePrice(rating, age);
                price *= (bottleSize / 750);
                //增加“噪声”
                price *= (rnd.NextDouble() * 0.9d + 0.2d); //配书代码的实现
                //加入数据集
                rows.Add(new PriceStructure()
                {
                    Input = new[] { rating, age, aisle, bottleSize },
                    Result = price
                });
            }
            return rows;
        }

        public List<PriceStructure> ReScale(List<PriceStructure> data, double[] scale)
        {
            return (from row in data
                    let scaled = scale.Select((s, i) => s * row.Input[i]).ToArray()
                    select new PriceStructure()
                    {
                        Input = scaled,
                        Result = row.Result
                    }).ToList();
        }

        public Func<double[], double> CreateCostFunction(Func<List<PriceStructure>, double[], double> algf,
            List<PriceStructure> data)
        {
            Func<double[], double> Costf = scale =>
            {
                var sdata = ReScale(data, scale);
                return CrossValidate(algf, sdata, 10);
            };
            return Costf;
        }

        public List<Tuple<int, int>> GetWeightDomain(int count)
        {
            var domains = new List<Tuple<int, int>>(count);
            for (var i = 0; i < 4; i++)
            {
                domains.Add(Tuple.Create(0, 20));
            }
            return domains;
        }

        public List<PriceStructure> WineSet3()
        {
            var rows = WineSet1();
            var rnd = new Random();
            foreach (var row in rows)
            {
                if (rnd.NextDouble() < 0.5)
                    // 模拟从折扣店购得的葡萄酒
                    row.Result *= 0.5;
            }
            return rows;
        }

        public double ProbGuess(List<PriceStructure> data, double[] vec1, double low,
            double high, int k = 5, Func<double, double, double> weightf = null)
        {
            if (weightf == null) weightf = Gaussian;
            var dlist = GetDistances(data, vec1);
            var nweight = 0d;
            var tweight = 0d;
            for (int i = 0; i < k; i++)
            {
                var dlistCurr = dlist.Skip(i).First();
                var dist = dlistCurr.Key;
                var idx = dlistCurr.Value;
                var weight = weightf(dist, 10);
                var v = data[idx].Result;
                // 当前数据点在指定范围吗？
                if (v >= low && v <= high)
                    nweight += weight;
                tweight += weight;
            }
            if (tweight == 0) return 0;
            //概率等于位于指定范围内的权重值除以所有权重值
            return nweight / tweight;
        }

        public List<Axes> BuildAxes()
        {
            return new List<Axes>()
            {
                new Axes(1, "X", "Y")
                {
                    Title = "MatplotlibCS Test",
                    PlotItems =
                    {
                        new Line2D("Line 1")
                        {
                            X = new List<object>() {1,2,3,4},
                            Y = new List<double>() {2,3,4,1}
                        }
                    }
                }
            };
        }

        public void CumulativeGraph(List<PriceStructure> data, double[] vec1,
            double high, int k = 5, Func<double, double, double> weightf = null)
        {
            if (weightf == null) weightf = Gaussian;
            var t1 = new List<object>();
            for (var i = 0d; i < high; i += 0.1)
                t1.Add(i);
            var cprob = t1.Select(v => ProbGuess(data, vec1, 0, (double)v, k, weightf)).ToList();

            var axes = new Axes(1, "Price", "Cumulative Probility")
            {
                Title = "Price Cumulative Probility",
                PlotItems =
                {
                    new Line2D("")
                    {
                        X = t1,
                        Y = cprob
                    }
                }
            };
            Draw(new List<Axes>() { axes });
        }

        public void Draw(List<Axes> plots)
        {
            // 由于我们在外部启动Python服务，这两个参数传空字符串就可以了
            var matplotlibCs = new MatplotlibCS.MatplotlibCS("", "");

            var figure = new Figure(1, 1)
            {
                FileName = $"/mnt/e/Temp/result{DateTime.Now:ddHHmmss}.png",
                OnlySaveImage = true,
                DPI = 150,
                Subplots = plots
            };
            var t = matplotlibCs.BuildFigure(figure);
            t.Wait();
        }

        public void ProbabilityGraph(List<PriceStructure> data, double[] vec1,
            double high, int k = 5, Func<double, double, double> weightf = null,double ss=5)
        {
            if (weightf == null) weightf = Gaussian;
            // 价格值域范围
            var t1 = new List<object>();
            for (var i = 0d; i < high; i += 0.1)
                t1.Add(i);
            // 整个值域范围的所有概率
            var probs = t1.Cast<double>()
                .Select(v => ProbGuess(data, vec1, v, v+0.1, k, weightf)).ToList();
            // 通过加上近邻概率的高斯计算结果，对概率值做平滑处理
            var smoothed = new List<double>();
            for (int i = 0; i < probs.Count; i++)
            {
                var sv = 0d;
                for (int j = 0; j < probs.Count; j++)
                {
                    var dist = Math.Abs(i - j)*0.1;
                    var weight = Gaussian(dist, sigma: ss);
                    sv += weight*probs[j];
                }
                smoothed.Add(sv);
            }
            var axes = new Axes(1, "Price", "Probility")
            {
                Title = "Price Probility",
                PlotItems =
                {
                    new Line2D("")
                    {
                        X = t1,
                        Y = smoothed
                    }
                }
            };
            Draw(new List<Axes>() { axes });
        }

    }


}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    public class TravelForNumPredict
    {
        public List<Tuple<string, string>> People { get; } = new List<Tuple<string, string>>()
        {
            Tuple.Create("Seymour", "BOS"),
            Tuple.Create("Franny", "DAL"),
            Tuple.Create("Zooey", "CAK"),
            Tuple.Create("Walt", "MIA"),
            Tuple.Create("Buddy", "ORD"),
            Tuple.Create("Les", "OMA")
        };

        // New York的LaGuardia机场 
        private string _destination = "LGA";

        private Dictionary<string, List<Tuple<string, string, int>>> _flights
            = new Dictionary<string, List<Tuple<string, string, int>>>();

        public void PopulateFlights(string filename)
        {
            using (var fs = new FileStream(filename, FileMode.Open))
            {
                var sr = new StreamReader(fs);
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        var vals = line.Split(',');
                        var key = $"{vals[0]}-{vals[1]}";
                        if (!_flights.ContainsKey(key))
                            _flights.Add(key, new List<Tuple<string, string, int>>());
                        // 将航班详情添加到航班列表
                        _flights[key].Add(Tuple.Create(vals[2], vals[3], int.Parse(vals[4])));
                    }
                }
            }
        }

        private int GetMinutes(string t)
        {
            var x = t.Split(':');
            return int.Parse(x[0]) * 60 + int.Parse(x[1]);
        }

        public void PrintSchedule(List<int> r)
        {
            for (int d = 0; d < r.Count / 2; d++)
            {
                var name = People[d].Item1;
                var origin = People[d].Item2;
                var key = $"{origin}-{_destination}";
                var keyRet = $"{_destination}-{origin}";
                var @out = _flights[key][r[2 * d]];
                var ret = _flights[keyRet][r[2 * d + 1]];
                Console.Write($"{name.PadLeft(10)}{origin.PadLeft(10)} ");
                Console.Write($"{@out.Item1.PadLeft(5)}-{@out.Item2.PadLeft(5)} ${@out.Item3.ToString().PadLeft(3)} ");
                Console.WriteLine($"{ret.Item1.PadLeft(5)}-{ret.Item2.PadLeft(5)} ${ret.Item3.ToString().PadLeft(3)}");
            }
        }

        public float ScheduleCost(List<int> sol)
        {
            var totalprice = 0f;
            int latestArrival = 0;
            int earliestDep = 24 * 60;

            for (int d = 0; d < sol.Count / 2; d++)
            {
                //得到去程航班和返程航班
                var origin = People[d].Item2;
                var outbound = _flights[$"{origin}-{_destination}"][sol[2 * d]];
                var returnf = _flights[$"{_destination}-{origin}"][sol[2 * d + 1]];

                //总价等于所有人往返航班价格之和
                totalprice += outbound.Item3;
                totalprice += returnf.Item3;

                //记录最晚到达时间和最早离开时间
                if (latestArrival < GetMinutes(outbound.Item2))
                    latestArrival = GetMinutes(outbound.Item2);
                if (earliestDep > GetMinutes(returnf.Item1))
                    earliestDep = GetMinutes(returnf.Item1);
            }
            //每个人必须在机场等待直到最后一个人到到达为止
            //他们也必须在相同时间到达，并等候他们的返程航班
            var totalwait = 0f;
            for (int d = 0; d < sol.Count / 2; d++)
            {
                var origin = People[d].Item2;
                var outbound = _flights[$"{origin}-{_destination}"][sol[2 * d]];
                var returnf = _flights[$"{_destination}-{origin}"][sol[2 * d + 1]];
                totalwait += latestArrival - GetMinutes(outbound.Item2);
                totalwait += GetMinutes(returnf.Item1) - earliestDep;
            }

            //如果需要多支付一天的汽车租赁费用，则总价加50
            if (latestArrival < earliestDep) totalprice += 50;

            return totalprice + totalwait;
        }

        public List<int> RandomOptimize(List<Tuple<int, int>> domain, Func<List<int>, float> costf)
        {
            var best = 999999999f;
            List<int> bestr = null;
            var random = new Random();
            for (int i = 0; i < 1000; i++)
            {
                //创建一个随机解
                var r = domain.Select(t => random.Next(t.Item1, t.Item2)).ToList();
                //得到成本
                var cost = costf(r);
                //与目前为止最优解进行比较
                if (cost < best)
                {
                    best = cost;
                    bestr = r;
                }
            }
            return bestr;
        }

        public List<double> AnnealingOptimize(List<Tuple<int, int>> domain, Func<double[], double> costf,
            float T = 10000.0f, float cool = 0.95f, int step = 1)
        {
            //随机初始化值
            var random = new Random();
            var vec = domain.Select(t => (double)random.Next(t.Item1, t.Item2)).ToArray();

            while (T > 0.1)
            {
                //选择一个索引值
                var i = random.Next(0, domain.Count - 1);
                //选择一个改变索引值的方向
                var dir = random.Next(-step, step);
                //创建一个代表题解的新列表，改变其中一个值
                var vecb = vec.ToArray();
                vecb[i] += dir;
                if (vecb[i] < domain[i].Item1) vecb[i] = domain[i].Item1;
                else if (vecb[i] > domain[i].Item2) vecb[i] = domain[i].Item2;
                //计算当前成本和新成本
                var ea = costf(vec);
                var eb = costf(vecb);
                //是更好的解？或是退火过程中可能的波动的临界值上限？
                if (eb < ea || random.NextDouble() < Math.Pow(Math.E, -(eb - ea) / T))
                    vec = vecb;
                //降低温度
                T *= cool;
            }
            return vec.ToList();
        }

        public List<int> HillClimb(List<Tuple<int, int>> domain, Func<List<int>, float> costf)
        {
            //创建一个随机解
            var random = new Random();
            var sol = domain.Select(t => random.Next(t.Item1, t.Item2)).ToList();

            //主循环
            while (true)
            {
                //创建相邻解的列表
                var neighbors = new List<List<int>>();
                for (int j = 0; j < domain.Count; j++)
                {
                    //在每个方向上相对于原值偏离一点
                    if (sol[j] > domain[j].Item1)
                        neighbors.Add(sol.Take(j).Concat(new[] { sol[j] - 1 }).Concat(sol.Skip(j + 1)).ToList());
                    if (sol[j] < domain[j].Item2)
                        neighbors.Add(sol.Take(j).Concat(new[] { sol[j] + 1 }).Concat(sol.Skip(j + 1)).ToList());
                }
                //在相邻解中寻找
                var current = costf(sol);
                var best = current;
                foreach (var n in neighbors)
                {
                    var cost = costf(n);
                    if (cost < best)
                    {
                        best = cost;
                        sol = n;
                    }
                }
                if (best == current)
                    break;
            }
            return sol;
        }

        class RankComparer : IComparer<float>
        {
            public int Compare(float x, float y)
            {
                if (x == y)//这样可以让SortedList保存重复的key
                    return 1;
                return x.CompareTo(y);//从小到大排序
            }
        }



        public List<double> GeneticOptimize(List<Tuple<int, int>> domain, Func<double[], double> costf,
            int popsize = 50, int step = 1, float mutprob = 0.2f, float elite = 0.2f, int maxiter = 100)
        {
            var random = new Random();
            //变异操作
            Func<double[], double[]> mutate = vec =>
            {
                var i = random.Next(0, domain.Count - 1);
                if (random.NextDouble() < 0.5 && vec[i] > domain[i].Item1)
                    return vec.Take(i).Concat(new[] { vec[i] - step }).Concat(vec.Skip(i + 1)).ToArray();
                else if (vec[i] < domain[i].Item2)
                    return vec.Take(i).Concat(new[] { vec[i] + step }).Concat(vec.Skip(i + 1)).ToArray();
                return vec;
            };
            //配对操作
            Func<double[], double[], double[]> crossover = (r1, r2) =>
            {
                var i = random.Next(1, domain.Count - 2);
                return r1.Take(i).Concat(r2.Skip(i)).ToArray();
            };
            //构造初始种群
            var pop = new List<double[]>();
            for (int i = 0; i < popsize; i++)
            {
                var vec = domain.Select(t => (double)random.Next(t.Item1, t.Item2)).ToArray();
                pop.Add(vec);
            }
            //每一代中有多少胜出者？
            var topelite = (int) (elite*popsize);
            Func<double, double, int> cf = (x, y) => x == y ? 1 : x.CompareTo(y);
            var scores = new SortedList<double, double[]>(cf.AsComparer());
            //主循环
            for (int i = 0; i < maxiter; i++)
            {
                foreach (var v in pop)
                   scores.Add(costf(v),v);
                var ranked = scores.Values;
                //从胜出者开始
                pop = ranked.Take(topelite).ToList();

                //添加变异和配对后的胜出者
                while (pop.Count<popsize)
                {
                    if (random.NextDouble() < mutprob)
                    {
                        //变异
                        var c = random.Next(0, topelite);
                        pop.Add(mutate(ranked[c]));
                    }
                    else
                    {
                        //配对
                        var c1 = random.Next(0, topelite);
                        var c2 = random.Next(0, topelite);
                        pop.Add(crossover(ranked[c1],ranked[c2]));
                    }
                }

                //打印当前最优值
                //Console.WriteLine(scores.First().Key);
            }
            return scores.First().Value.ToList();
        }
    }

    public static class ComparisonEx
    {
        public static IComparer<T> AsComparer<T>(this Comparison<T> @this)
        {
            if (@this == null)
                throw new System.ArgumentNullException("Comparison<T> @this");
            return new ComparisonComparer<T>(@this);
        }

        public static IComparer<T> AsComparer<T>(this Func<T, T, int> @this)
        {
            if (@this == null)
                throw new System.ArgumentNullException("Func<T, T, int> @this");
            return new ComparisonComparer<T>((x, y) => @this(x, y));
        }

        private class ComparisonComparer<T> : IComparer<T>
        {
            public ComparisonComparer(Comparison<T> comparison)
            {
                if (comparison == null)
                    throw new System.ArgumentNullException("comparison");
                this.Comparison = comparison;
            }

            public int Compare(T x, T y)
            {
                return this.Comparison(x, y);
            }

            public Comparison<T> Comparison { get; private set; }
        }
    }
}
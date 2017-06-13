using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MatplotlibCS;
using MatplotlibCS.PlotItems;

namespace ConsoleApplication1
{
    public class AdvancedClassify
    {

        public List<MatchRow> LoadMatch(string file, bool allnum = false)
        {
            var rows = new List<MatchRow>();
            var fs = File.OpenRead(file);
            var sr = new StreamReader(fs);
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (string.IsNullOrEmpty(line)) continue;
                rows.Add(new MatchRow(line.Split(','), allnum));
            }
            return rows;
        }

        public class MatchRow
        {
            public readonly string[] Data;
            public readonly double[] NumData;
            public readonly int Match;

            public MatchRow(string[] row, bool allnum = false)
            {
                if (allnum)
                {
                    NumData = new double[row.Length - 1];
                    for (int i = 0; i < row.Length - 1; i++)
                        NumData[i] = double.Parse(row[i]);
                }
                else
                {
                    Data = new string[row.Length - 1];
                    for (int i = 0; i < row.Length - 1; i++)
                        Data[i] = row[i];
                }
                Match = int.Parse(row.Last());
            }

            public MatchRow(double[] row)
            {
                NumData = row.Take(row.Length - 1).ToArray();
                Match = (int)row.Last();
            }
        }

        public void PlotageMatches(List<MatchRow> rows)
        {
            var xdm = rows.Where(r => r.Match == 1).Select(r => r.NumData[0]).ToList();
            var ydm = rows.Where(r => r.Match == 1).Select(r => r.NumData[1]).ToList();
            var xdn = rows.Where(r => r.Match == 0).Select(r => r.NumData[0]).ToList();
            var ydn = rows.Where(r => r.Match == 0).Select(r => r.NumData[1]).ToList();

            var axes = new Axes(1, "", "")
            {
                Title = "Age Distribution",
                ShowLegend = false
            };

            for (int i = 0; i < xdm.Count; i++)
                axes.PlotItems.Add(new Point2D("go", xdm[i], ydm[i]) { Marker = Marker.Point });
            for (int i = 0; i < xdn.Count; i++)
                axes.PlotItems.Add(new Point2D("ro", xdn[i], ydn[i]) { Marker = Marker.Plus });

            Draw(new List<Axes> { axes });
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

        public Dictionary<int, double[]> LinearTrain(List<MatchRow> rows)
        {
            var averages = new Dictionary<int, double[]>();
            var counts = new Dictionary<int, int>();
            foreach (var row in rows)
            {
                //得到该坐标点所属分类
                var cl = row.Match;

                if (!averages.ContainsKey(cl))
                    averages.Add(cl, new double[row.NumData.Length]);
                if (!counts.ContainsKey(cl))
                    counts.Add(cl, 0);

                //将该坐标点加入averages中
                for (int i = 0; i < row.NumData.Length; i++)
                {
                    averages[cl][i] += row.NumData[i];
                }
                //记录每个分类中有多少坐标点
                counts[cl] += 1;
            }
            // 将总和除以计数值以求得平均值
            foreach (var kvp in averages)
            {
                var cl = kvp.Key;
                var avg = kvp.Value;
                for (var i = 0; i < avg.Length; i++)
                {
                    avg[i] /= counts[cl];
                }
            }
            return averages;
        }

        public double DotProduct(double[] v1, double[] v2)
        {
            return v1.Select((v, i) => v * v2[i]).Sum();
        }

        public int DpClassify(double[] point, Dictionary<int, double[]> avgs)
        {
            var b = (DotProduct(avgs[1], avgs[1]) - DotProduct(avgs[0], avgs[0])) / 2;
            var y = DotProduct(point, avgs[0]) - DotProduct(point, avgs[1]) + b;
            if (y > 0) return 0;
            return 1;
        }

        public int YesNo(string v)
        {
            if (v == "yes") return 1;
            if (v == "no") return -1;
            return 0;
        }

        public int MatchCount(string interest1, string interest2)
        {
            var l1 = interest1.Split(':');
            var l2 = interest2.Split(':');
            return l1.Intersect(l2).Count();
        }

        public int MilesDistance(string a1, string a2)
        {
            return 0;
        }

        public List<MatchRow> LoadNumerical()
        {
            var oldrows = LoadMatch(@"TestData\matchmaker.csv");
            var newrows = new List<MatchRow>();
            foreach (var row in oldrows)
            {
                var d = row.Data;
                var data = new[]
                {
                    double.Parse(d[0]),
                    YesNo(d[1]),
                    YesNo(d[2]),
                    double.Parse(d[5]),
                    YesNo(d[6]),
                    YesNo(d[7]),
                    MatchCount(d[3], d[8]),
                    MilesDistance(d[4], d[9]),
                    row.Match
                };
                newrows.Add(new MatchRow(data));
            }
            return newrows;
        }

        public Tuple<List<MatchRow>, Func<double[], double[]>> ScaleData(List<MatchRow> rows)
        {
            var low = ArrayList.Repeat(999999999.0, rows[0].NumData.Length).Cast<double>().ToList();
            var high = ArrayList.Repeat(-999999999.0, rows[0].NumData.Length).Cast<double>().ToList();
            // 寻找最大值和最小值
            foreach (var row in rows)
            {
                var d = row.NumData;
                for (int i = 0; i < row.NumData.Length; i++)
                {
                    if (d[i] < low[i]) low[i] = d[i];
                    if (d[i] > high[i]) high[i] = d[i];
                }
            }
            //对数据进行缩放处理的函数
            // 注意：原书印刷代码有问题，配书代码是正确的，还要自己做一下“除0”的处理
            Func<double[], double[]> scaleInput = d =>
                    low.Select((l, i) =>
                        {
                            if (high[i] == low[i]) return 0;
                            return (d[i] - low[i]) / (high[i] - low[i]);
                        }).ToArray();
            //对所有数据进行缩放处理
            var newrows = rows.Select(r =>
            {
                var newRow = scaleInput(r.NumData).ToList();
                newRow.Add(r.Match);
                return new MatchRow(newRow.ToArray());
            }).ToList();

            return Tuple.Create(newrows, scaleInput);
        }

        public double Rbf(double[] v1, double[] v2, int gamma = 20)
        {
            var dv = v1.Select((v, i) => v - v2[i]).ToArray();
            var l = VecLength(dv);
            return Math.Pow(Math.E, -gamma * l);
        }

        public double VecLength(double[] v)
        {
            return v.Sum(p => p * p);
        }

        public int NlClassify(double[] point, List<MatchRow> rows, double offset, int gamma = 10)
        {
            double sum0 = 0;
            double sum1 = 0;
            var count0 = 0;
            var count1 = 0;

            foreach (var row in rows)
            {
                if (row.Match == 0)
                {
                    sum0 += Rbf(point, row.NumData, gamma);
                    ++count0;
                }
                else
                {
                    sum1 += Rbf(point, row.NumData, gamma);
                    ++count1;
                }
            }
            var y = sum0 / count0 - sum1 / count1 + offset;
            if (y > 0) return 0;
            return 1;
        }

        public double GetOffset(List<MatchRow> rows, int gamma = 10)
        {
            var l0 = new List<double[]>();
            var l1 = new List<double[]>();
            foreach (var row in rows)
            {
                if (row.Match == 0)
                    l0.Add(row.NumData);
                else
                    l1.Add(row.NumData);
            }
            var sum0 = (from v2 in l0 from v1 in l0 select Rbf(v1, v2, gamma)).Sum();
            var sum1 = (from v2 in l1 from v1 in l1 select Rbf(v1, v2, gamma)).Sum();
            return sum1 / (l1.Count * l1.Count) - sum0 / (l0.Count * l0.Count);
        }
    }
}

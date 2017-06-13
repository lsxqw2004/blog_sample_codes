using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ConsoleApplication1
{
    public class TreePredict
    {
        public static List<object[]> MyData = new List<object[]>()
        {
            new object[]{"slashdot","USA","yes",18,"None"},
            new object[]{"google","France","yes",23,"Premium"},
            new object[]{"digg","USA","yes",24,"Basic"},
            new object[]{"kiwitobes","France","yes",23,"Basic"},
            new object[]{"google","UK","no",21,"Premium"},
            new object[]{"(direct)","New Zealand","no",12,"None"},
            new object[]{"(direct)","UK","no",21,"Basic"},
            new object[]{"google","USA","no",24,"Premium"},
            new object[]{"slashdot","France","yes",19,"None"},
            new object[]{"digg","USA","no",18,"None"},
            new object[]{"google","UK","no",18,"None"},
            new object[]{"kiwitobes","UK","no",19,"None"},
            new object[]{"digg","New Zealand","yes",12,"Basic"},
            new object[]{"slashdot","UK","no",21,"None"},
            new object[]{"google","UK","yes",18,"Basic"},
            new object[]{"kiwitobes","France","yes",19,"Basic"}
        };

        // 在某一列上对数据集合进行拆分，能处理数值型数据或名词性数据(字符串)
        public Tuple<List<object[]>, List<object[]>> DivideSet(List<object[]> rows, int column, object value)
        {
            // 定义一个lambda用于判断记录应该归为第一组还是第二组（即匹配参考值还是不匹配）
            Func<object[], bool> splitFunc = null;
            if (value is int)
                splitFunc = r => Convert.ToInt32(r[column]) >= Convert.ToInt32(value);
            else if (value is float)
                splitFunc = r => Convert.ToSingle(r[column]) >= Convert.ToSingle(value);
            else
                splitFunc = r => r[column].ToString() == value.ToString();

            // 将数据集拆分成两个集合并返回
            var set1 = rows.Where(r => splitFunc(r)).ToList();
            var set2 = rows.Where(r => !splitFunc(r)).ToList();
            return Tuple.Create(set1, set2);
        }

        // 对结果列（最后一列）进行计数
        public Dictionary<string, int> UniqueCounts(List<object[]> rows)
        {
            var results = new Dictionary<string, int>();
            foreach (var row in rows)
            {
                // 计数结果在最后一列
                var r = row.Last().ToString();
                if (!results.ContainsKey(r))
                    results.Add(r, 0);
                results[r] += 1;
            }
            return results;
        }

        // 随机放置的数据项出现于错误分类中的概率
        public float GiniImpurity(List<object[]> rows)
        {
            var total = rows.Count;
            var counts = UniqueCounts(rows);
            var imp = 0f;
            foreach (var k1 in counts.Keys)
            {
                var p1 = counts[k1] / (float)total;
                foreach (var k2 in counts.Keys)
                {
                    if (k1 == k2)
                        continue;
                    var p2 = counts[k2] / (float)total;
                    imp += p1 * p2;
                }
            }
            return imp;
        }

        // 熵是遍历所有可能结果之后所得到的p(x)log(p(x))之和
        public float Entropy(List<object[]> rows)
        {
            Func<float, float> log2 = x => (float)(Math.Log(x) / Math.Log(2));
            var results = UniqueCounts(rows);
            // 开始计算熵值
            var ent = 0f;
            foreach (var r in results.Keys)
            {
                var p = results[r] / (float)rows.Count;
                ent -= p * log2(p);
            }
            return ent;
        }

        public DecisionNode BuildTree(List<object[]> rows, Func<List<object[]>, float> scoref = null)
        {
            if (scoref == null)
                scoref = Entropy;
            var rowsCount = rows.Count;
            if (rowsCount == 0) return new DecisionNode();
            var currentScore = scoref(rows);

            //定义一些变量记录最佳拆分条见
            var bestGain = 0f;
            Tuple<int, object> bestCriteria = null;
            Tuple<List<object[]>, List<object[]>> bestSets = null;

            var columnCount = rows[0].Length - 1;
            for (int i = 0; i < columnCount; i++)
            {
                // 在当前列中生成一个由不同值构成的序列
                var columnValues = new List<object>();
                if (rows[0][i] is int)
                    columnValues = rows.Select(r => r[i]).Cast<int>().Distinct().Cast<object>().ToList();
                else if (rows[0][i] is float)
                    columnValues = rows.Select(r => r[i]).Cast<float>().Distinct().Cast<object>().ToList();
                else
                    columnValues = rows.Select(r => r[i].ToString()).Distinct().Cast<object>().ToList();
                // 根据这一列中的每个值，尝试对数据集进行拆分
                foreach (var value in columnValues)
                {
                    var setTuple = DivideSet(rows, i, value);
                    var set1 = setTuple.Item1;
                    var set2 = setTuple.Item2;
                    //信息增益
                    var p = set1.Count / (float)rowsCount;
                    var gain = currentScore - p * scoref(set1) - (1 - p) * scoref(set2);
                    if (gain > bestGain && set1.Count > 0 && set2.Count > 0)
                    {
                        bestGain = gain;
                        bestCriteria = Tuple.Create(i, value);
                        bestSets = setTuple;
                    }
                }
            }
            // 创建子分支
            if (bestGain > 0)
            {
                var trueBranch = BuildTree(bestSets.Item1);
                var falseBranch = BuildTree(bestSets.Item2);
                return new DecisionNode(
                    col: bestCriteria.Item1,
                    value: bestCriteria.Item2,
                    tb: trueBranch,
                    fb: falseBranch
                );
            }
            else
            {
                return new DecisionNode(UniqueCounts(rows));
            }
        }


        public void PrintTree(DecisionNode tree, string indent = "")
        {
            //是叶节点吗？
            if (tree.Results != null)
                Console.WriteLine(JsonConvert.SerializeObject(tree.Results));
            else
            {
                //打印判断条件
                Console.WriteLine($"{tree.Col}:{tree.Value}? ");

                //打印分支
                Console.Write($"{indent}T->");
                PrintTree(tree.Tb, indent + "  ");
                Console.Write($"{indent}F->");
                PrintTree(tree.Fb, indent + "  ");
            }
        }

        public Dictionary<string, int> Classify(object[] observation, DecisionNode tree)
        {
            if (tree.Results != null)
                return tree.Results;
            var v = observation[tree.Col];
            DecisionNode branch;
            if (v is int || v is float)
            {
                var val = v is int ? Convert.ToInt32(v) : Convert.ToSingle(v);
                var treeVal = tree.Value is int ? Convert.ToInt32(tree.Value) : Convert.ToSingle(tree.Value);
                branch = val >= treeVal ? tree.Tb : tree.Fb;
            }
            else
            {
                branch = v.ToString() == tree.Value.ToString() ? tree.Tb : tree.Fb;
            }
            return Classify(observation, branch);
        }

        public void Prune(DecisionNode tree, float mingain)
        {
            //如果分支不是叶节点，则进行剪枝操作
            if (tree.Tb.Results == null)
                Prune(tree.Tb, mingain);
            if (tree.Fb.Results == null)
                Prune(tree.Fb, mingain);

            //如果两个子分支都是叶节点，则判断是否需要合并
            if (tree.Tb.Results != null && tree.Fb.Results != null)
            {
                //构造合并后的数据集
                IEnumerable<object[]> tb = new List<object[]>();
                IEnumerable<object[]> fb = new List<object[]>();
                tb = tree.Tb.Results.Aggregate(tb, (current, tbKvPair)
                    => current.Union(ArrayList.Repeat(new object[] { tbKvPair.Key }, (int)tbKvPair.Value).Cast<object[]>()));
                fb = tree.Fb.Results.Aggregate(fb, (current, tbKvPair)
                    => current.Union(ArrayList.Repeat(new object[] { tbKvPair.Key }, (int)tbKvPair.Value).Cast<object[]>()));

                //检查熵增加情况
                var mergeNode = tb.Union(fb).ToList();
                var delta = Entropy(mergeNode) - (Entropy(tb.ToList()) + Entropy(fb.ToList()) / 2);
                Debug.WriteLine(delta);
                if (delta < mingain)
                {
                    //合并分支
                    tree.Tb = null;
                    tree.Fb = null;
                    tree.Results = UniqueCounts(mergeNode);
                }
            }


        }

        public Dictionary<string, float> MdClassify(object[] observation, DecisionNode tree)
        {
            if (tree.Results != null)
                return tree.Results.ToDictionary(r=>r.Key,r=>(float)r.Value);
            var v = observation[tree.Col];
            if (v == null)
            {
                var tr = MdClassify(observation, tree.Tb);
                var fr = MdClassify(observation, tree.Fb);
                var tcount = tr.Values.Count;
                var fcount = fr.Values.Count;
                var tw = tcount / (float)(tcount + fcount);
                var fw = fcount / (float)(tcount + fcount);
                var result = tr.ToDictionary(trKvp => trKvp.Key, trKvp => trKvp.Value*tw);
                foreach (var frKvp in fr)
                {
                    if (!result.ContainsKey(frKvp.Key))
                        result.Add(frKvp.Key, 0);
                    result[frKvp.Key] += frKvp.Value * fw;
                }
                return result;
            }
            else
            {
                DecisionNode branch;
                if (v is int || v is float)
                {
                    var val = v is int ? Convert.ToInt32(v) : Convert.ToSingle(v);
                    var treeVal = tree.Value is int ? Convert.ToInt32(tree.Value) : Convert.ToSingle(tree.Value);
                    branch = val >= treeVal ? tree.Tb : tree.Fb;
                }
                else
                {
                    branch = v.ToString() == tree.Value.ToString() ? tree.Tb : tree.Fb;
                }
                return MdClassify(observation, branch);
            }
        }


        public float Variance(List<object[]> rows)
        {
            if (rows.Count == 0) return 0;
            var data = rows.Select(r => Convert.ToSingle(r.Last())).ToList();
            var mean = data.Average();
            var variance = data.Select(d => (float) Math.Pow(d - mean, 2)).Average();
            return variance;
        }
    }

    public class DecisionNode
    {
        public DecisionNode()
        {
        }

        public DecisionNode(int col, object value, DecisionNode tb, DecisionNode fb)
        {
            Col = col;
            Value = value;

            Tb = tb;
            Fb = fb;
        }

        public DecisionNode(Dictionary<string, int> results)
        {
            Results = results;
        }

        public int Col { get; set; }
        public object Value { get; set; }
        public Dictionary<string, int> Results { get; set; }
        public DecisionNode Tb { get; set; }
        public DecisionNode Fb { get; set; }
    }


}

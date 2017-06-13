using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    public class Dorm
    {
        // 宿舍，每个宿舍有两个隔间
        private List<string> _dorms = new List<string>()
        {
            "Zeus","Athena","Hercules","Bacchus","Pluto"
        };

        // 学生，以及首选和次选
        private List<Tuple<string, Tuple<string, string>>> _prefs =
            new List<Tuple<string, Tuple<string, string>>>()
            {
                Tuple.Create("Toby",Tuple.Create("Bacchus", "Hercules")),
                Tuple.Create("Steve",Tuple.Create("Zeus", "Pluto")),
                Tuple.Create("Andrea",Tuple.Create("Athena", "Zeus")),
                Tuple.Create("Sarah",Tuple.Create("Zeus", "Pluto")),
                Tuple.Create("Dave",Tuple.Create("Athena", "Bacchus")),
                Tuple.Create("Jeff",Tuple.Create("Hercules", "Pluto")),
                Tuple.Create("Fred",Tuple.Create("Pluto", "Athena")),
                Tuple.Create("Suzie",Tuple.Create("Bacchus", "Hercules")),
                Tuple.Create("Laura",Tuple.Create("Bacchus", "Hercules")),
                Tuple.Create("Neil",Tuple.Create("Hercules", "Athena"))
            };

        //题解范围
        public List<Tuple<int, int>> Domain =>
            Enumerable.Repeat(0, _dorms.Count*2)
                .Select((v, i) => Tuple.Create(0, _dorms.Count*2 - i - 1))
                .ToList();

        public void PrintSolution(List<int> vec)
        {
            var slots = new List<int>();
            // 为每个宿舍见两个槽
            for (int i = 0; i < _dorms.Count; i++)
                slots.AddRange(new []{i,i});
            // 遍历每一个名学生的安置情况
            for (int i = 0; i < vec.Count; i++)
            {
                var x = vec[i];
                //从剩余槽中选择
                var dorm = _dorms[slots[x]];
                //输出学生及其被分配的宿舍
                Console.WriteLine($"{_prefs[i].Item1} {dorm}");
                //删除该槽
                slots.RemoveAt(x);
            }
        }

        public float DormCost(List<int> vec)
        {
            var cost = 0;
            // 建立槽序列
            var slots=new List<int>() {0,0,1,1,2,2,3,3,4,4};
            // 遍历每一名学生
            for (int i = 0; i < vec.Count; i++)
            {
                var x = vec[i];
                var dorm = _dorms[slots[x]];
                var pref = _prefs[i].Item2;
                //首选成本为0，次选成本值为1
                //不在选择之列，成本加3
                if (pref.Item1 == dorm) cost += 0;
                else if (pref.Item2 == dorm) cost += 1;
                else cost += 3;
                //删除该槽
                slots.RemoveAt(x);
            }
            return cost;
        }

        }
}

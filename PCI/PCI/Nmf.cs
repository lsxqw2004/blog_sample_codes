using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace PCI
{
    public class Nmf
    {
        private readonly Action<object> _outputWriter;

        public Nmf(Action<object> outputAction)
        {
            _outputWriter = outputAction;
        }

        private double DifCost(DenseMatrix a, Matrix<double> b)
        {
            var dif = 0d;
            // 遍历矩阵中的每一行和每一列
            for (int i = 0; i < a.RowCount; i++)
            {
                for (int j = 0; j < a.ColumnCount; j++)
                {
                    //将差值相加
                    dif += Math.Pow(a[i, j] - b[i, j], 2);
                }
            }
            return dif;
        }


        public (Matrix<double>, Matrix<double>) Factorize(DenseMatrix v, int pc = 10, int iter = 50)
        {
            var ic = v.RowCount;
            var fc = v.ColumnCount;

            //以随机值初始化权重矩阵和特征矩阵
            var mb = Matrix<double>.Build;
            var rndGen = new Random(DateTime.Now.Second);
            var w = mb.Dense(ic,pc, (i, j) => rndGen.NextDouble());
            var h = mb.Dense(pc,fc, (i, j)=> rndGen.NextDouble());

            // 最多执行iter次操作
            for (int i = 0; i < iter; i++)
            {
                var wh = w * h;
                // 计算当前查值
                var cost = DifCost(v, wh);
                if (i % 10 == 0)
                    _outputWriter(cost);
                //如果矩阵已分解彻底，终止循环
                if (cost == 0)
                    break;
                //更新特征矩阵
                var hn = w.Transpose() * v;
                var hd = w.Transpose() * w * h;
                h = DenseMatrix.OfColumnMajor(pc, fc,
                    h.AsColumnMajorArray().Multiple(hn.AsColumnMajorArray()).Divide(hd.AsColumnMajorArray()));

                //更新权重矩阵
                var wn = v * h.Transpose();
                var wd = w * h * h.Transpose();
                w = DenseMatrix.OfColumnMajor(ic, pc,
                    w.AsColumnMajorArray().Multiple(wn.AsColumnMajorArray()).Divide(wd.AsColumnMajorArray()));
            }
            return (w, h);
        }

    }

    public static class DoubleArrayExt
    {
        public static double[] Multiple(this double[] left, double[] right)
        {
            return left.Select((l, i) => l * right[i]).ToArray();
        }

        public static double[] Divide(this double[] left, double[] right)
        {
            return left.Select((l, i) => l / (right[i]==0?double.MinValue:right[i])).ToArray();
        }
    }
}

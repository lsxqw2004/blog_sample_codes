using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleApplication1;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Newtonsoft.Json;
using PCI.pythonnet;
using Python.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace PCI
{
    public class NewsFeaturesTest
    {
        private readonly ITestOutputHelper _output;

        public NewsFeaturesTest(ITestOutputHelper output)
        {
            _output = output;
        }

        private void TestOutput(object obj)
        {
            _output.WriteLine(obj.ToString());
        }

        [Fact]
        public void TestMakeMatrix()
        {
            var newsFeatures = new NewsFeatures();
            (var allW,var artW,var artT) = newsFeatures.GetArticleWords();
            (var wordMatrix,var wordVec) = newsFeatures.MakeMatrix(allW, artW);
            // 单词向量的前10个词
            TestOutput(JsonConvert.SerializeObject(wordVec.Take(10)));
            // 第二篇文章的标题
            TestOutput(artT[1]);
            // 第二篇文章的在单词矩阵中对应的数据行的前10个值
            TestOutput(JsonConvert.SerializeObject(wordMatrix[1].Take(10)));
        }

        [Fact]
        public void TestMathNet()
        {
            // 测试矩阵乘法
            var m1 = DenseMatrix.OfArray(new[,] { { 1d, 2d, 3d }, { 4d, 5d, 6d } });
            TestOutput(m1);
            var m2 = DenseMatrix.OfArray(new[,] { { 1d, 2d }, { 3d, 4d }, { 5d, 6d } });
            TestOutput(m2);
            TestOutput((m1 * m2).ToString());

            //测试生成随机数组
            var mb = Matrix<double>.Build;
            var rnd = new Random();
            var mr = mb.Dense(2, 3, (i, j) => rnd.NextDouble());
            TestOutput(mr);

            //测试矩阵转一维数组并由数组重构矩阵
            var arr = mr.AsColumnMajorArray();
            TestOutput(JsonConvert.SerializeObject(arr));

            var mback = mb.DenseOfColumnMajor(2, 3, arr);
            TestOutput(mback);

            //测试由行列表生成矩阵，并转换
            var rowList = new List<List<double>>()
            {
                new List<double>(){1, 2,3,4 },
                new List<double>(){5,6,7,8 },
                new List<double>(){9,10,11,12}
            };
            var matrixFromRow = mb.DenseOfRows(rowList);
            var matrixFromRowToColumnArr = matrixFromRow.AsColumnMajorArray();
            TestOutput(JsonConvert.SerializeObject(matrixFromRowToColumnArr));
        }

        [Fact]
        public void TestFactorize()
        {
            var m1 = DenseMatrix.OfArray(new[,] { { 1d, 2d, 3d }, { 4d, 5d, 6d } });
            var m2 = DenseMatrix.OfArray(new[,] { { 1d, 2d }, { 3d, 4d }, { 5d, 6d } });
            var m12 = m1 * m2;
            TestOutput(m12);

            var nmf = new Nmf(o => TestOutput(o.ToString()));
            (var w, var h) = nmf.Factorize(m12, 3, 100);
            TestOutput(w);
            TestOutput(h);
            TestOutput(w * h);
        }

        [Fact]
        public void TestArticleFactorize()
        {
            var newsFeatures = new NewsFeatures();
            (var allW, var artW, var artT) = newsFeatures.GetArticleWords();
            (var wordMatrix, var wordVec) = newsFeatures.MakeMatrix(allW, artW);
            var nmf = new Nmf(o => TestOutput(o.ToString()));
            var matrix = DenseMatrix.OfRows(wordMatrix);
            (var weights, var feat) = nmf.Factorize(matrix, 20, 50);
            TestOutput(weights);
            TestOutput(feat);
        }

        [Fact]
        public void TestShowFeatures()
        {
            var newsFeatures = new NewsFeatures();
            (var allW, var artW, var artT) = newsFeatures.GetArticleWords();
            (var wordMatrix, var wordVec) = newsFeatures.MakeMatrix(allW, artW);
            var nmf = new Nmf(o => TestOutput(o.ToString()));
            var matrix = DenseMatrix.OfRows(wordMatrix);
            (var weights, var feat) = nmf.Factorize(matrix, 20, 50);
            newsFeatures.ShowFeatures(weights, feat, artT, wordVec);
        }

        [Fact]
        public void TestShowArticles()
        {
            var newsFeatures = new NewsFeatures();
            (var allW, var artW, var artT) = newsFeatures.GetArticleWords();
            (var wordMatrix, var wordVec) = newsFeatures.MakeMatrix(allW, artW);
            var nmf = new Nmf(o => TestOutput(o.ToString()));
            var matrix = DenseMatrix.OfRows(wordMatrix);
            (var weights, var feat) = nmf.Factorize(matrix, 20, 50);
            (var topPatterns, var patternNames) = newsFeatures.ShowFeatures(weights, feat, artT, wordVec);
            newsFeatures.ShowArticles(artT, topPatterns, patternNames);
        }
    }
}

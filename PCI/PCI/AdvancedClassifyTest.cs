using System;
using System.Linq;
using LibSVMsharp;
using LibSVMsharp.Extensions;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace ConsoleApplication1
{

    public class AdvancedClassifyTest
    {
        private readonly ITestOutputHelper _output;

        public AdvancedClassifyTest(ITestOutputHelper output)
        {
            _output = output;
        }

        private void TestOutput(object obj)
        {
            _output.WriteLine(obj.ToString());
        }

        [Fact]
        public void TestLoad()
        {
            var advancedClassify = new AdvancedClassify();
            var agesOnly = advancedClassify.LoadMatch(@"TestData\agesonly.csv", true);
            _output.WriteLine(JsonConvert.SerializeObject(agesOnly));
            var matchmaker = advancedClassify.LoadMatch(@"TestData\matchmaker.csv");
            _output.WriteLine(JsonConvert.SerializeObject(matchmaker));

        }

        [Fact]
        public void TestPlot()
        {
            var advancedClassify = new AdvancedClassify();
            var agesOnly = advancedClassify.LoadMatch(@"TestData\agesonly.csv", true);
            advancedClassify.PlotageMatches(agesOnly);
        }

        [Fact]
        public void TestLinearTrain()
        {
            var advancedClassify = new AdvancedClassify();
            var agesOnly = advancedClassify.LoadMatch(@"TestData\agesonly.csv", true);
            var avgs = advancedClassify.LinearTrain(agesOnly);
            _output.WriteLine(JsonConvert.SerializeObject(avgs));
        }

        [Fact]
        public void TestDpClassify()
        {
            var advancedClassify = new AdvancedClassify();
            var agesOnly = advancedClassify.LoadMatch(@"TestData\agesonly.csv", true);
            var avgs = advancedClassify.LinearTrain(agesOnly);
            var classify = advancedClassify.DpClassify(new double[] { 30, 30 }, avgs);
            _output.WriteLine(classify.ToString());
            classify = advancedClassify.DpClassify(new double[] { 30, 25 }, avgs);
            _output.WriteLine(classify.ToString());
            classify = advancedClassify.DpClassify(new double[] { 25, 40 }, avgs);
            _output.WriteLine(classify.ToString());
            classify = advancedClassify.DpClassify(new double[] { 48, 20 }, avgs);
            _output.WriteLine(classify.ToString());
        }

        [Fact]
        public void TestLoadNumerical()
        {
            var advancedClassify = new AdvancedClassify();
            var numericalset = advancedClassify.LoadNumerical();
            var dataRow = numericalset[0].Data;
            _output.WriteLine(JsonConvert.SerializeObject(dataRow));
        }

        [Fact]
        public void TestScaledLinearTrain()
        {
            var advancedClassify = new AdvancedClassify();
            var numericalset = advancedClassify.LoadNumerical();
            var result = advancedClassify.ScaleData(numericalset);
            var scaledSet = result.Item1;
            var scalef = result.Item2;
            var avgs = advancedClassify.LinearTrain(scaledSet);
            _output.WriteLine(JsonConvert.SerializeObject(numericalset[0].NumData));
            _output.WriteLine(numericalset[0].Match.ToString());
            _output.WriteLine(advancedClassify.DpClassify(scalef(numericalset[0].NumData), avgs).ToString());
            _output.WriteLine(numericalset[11].Match.ToString());
            _output.WriteLine(advancedClassify.DpClassify(scalef(numericalset[11].NumData), avgs).ToString());
        }

        [Fact]
        public void TestNlClassify()
        {
            var advancedClassify = new AdvancedClassify();
            var agesOnly = advancedClassify.LoadMatch(@"TestData\agesonly.csv", true);
            var offset = advancedClassify.GetOffset(agesOnly);
            TestOutput(advancedClassify.NlClassify(new[] { 30.0, 30 }, agesOnly, offset));
            TestOutput(advancedClassify.NlClassify(new[] { 30.0, 25 }, agesOnly, offset));
            TestOutput(advancedClassify.NlClassify(new[] { 25.0, 40 }, agesOnly, offset));
            TestOutput(advancedClassify.NlClassify(new[] { 48.0, 20 }, agesOnly, offset));
        }

        [Fact]
        public void TestNlClassifyMore()
        {
            var advancedClassify = new AdvancedClassify();
            var numericalset = advancedClassify.LoadNumerical();
            var result = advancedClassify.ScaleData(numericalset);
            var scaledSet = result.Item1;
            var scalef = result.Item2;
            var ssoffset = advancedClassify.GetOffset(scaledSet);
            TestOutput(numericalset[0].Match);
            TestOutput(advancedClassify.NlClassify(scalef(numericalset[0].NumData), scaledSet, ssoffset));
            TestOutput(numericalset[1].Match);
            TestOutput(advancedClassify.NlClassify(scalef(numericalset[1].NumData), scaledSet, ssoffset));
            TestOutput(numericalset[2].Match);
            TestOutput(advancedClassify.NlClassify(scalef(numericalset[2].NumData), scaledSet, ssoffset));
            var newrow = new[] { 28, -1, -1, 26, -1, 1, 2, 0.8 };//男士不想要小孩，而女士想要
            TestOutput(advancedClassify.NlClassify(scalef(newrow), scaledSet, ssoffset));
            newrow = new[] { 28, -1, 1, 26, -1, 1, 2, 0.8 };//双方都想要小孩
            TestOutput(advancedClassify.NlClassify(scalef(newrow), scaledSet, ssoffset));
        }

        [Fact]
        public void LibsvmFirstLook()
        {
            var prob = new SVMProblem();
            prob.Add(new[] { new SVMNode(1, 1), new SVMNode(2, 0), new SVMNode(3, 1) }, 1);
            prob.Add(new[] { new SVMNode(1, -1), new SVMNode(2, 0), new SVMNode(3, -1) }, -1);
            var param = new SVMParameter();
            param.Kernel = SVMKernelType.LINEAR;
            param.C = 10;
            var m = prob.Train(param);
            TestOutput(m.Predict(new []{new SVMNode(1,1), new SVMNode(2, 1), new SVMNode(3, 1) }));
            m.SaveModel("trainModel");
            var ml = SVM.LoadModel("trainModel");
            TestOutput(ml.Predict(new[] { new SVMNode(1, 1), new SVMNode(2, 1), new SVMNode(3, 1) }));
        }

        [Fact]
        public void TestLibsvmClassify()
        {
            var advancedClassify = new AdvancedClassify();
            var numericalset = advancedClassify.LoadNumerical();
            var result = advancedClassify.ScaleData(numericalset);
            var scaledSet = result.Item1;
            var scalef = result.Item2;
            var prob = new SVMProblem();
            foreach (var matchRow in scaledSet)
            {
                prob.Add(matchRow.NumData.Select((v,i)=>new SVMNode(i+1,v)).ToArray(),matchRow.Match);
            }
            var param = new SVMParameter() {Kernel = SVMKernelType.RBF};
            var m = prob.Train(param);
            m.SaveModel("trainModel");
            Func<double[], SVMNode[]> makeInput = ma => scalef(ma).Select((v, i) => new SVMNode(i + 1, v)).ToArray();
            var newrow = new[] { 28, -1, -1, 26, -1, 1, 2, 0.8 };//男士不想要小孩，而女士想要
            TestOutput(m.Predict(makeInput(newrow)));
            newrow = new[] { 28, -1, 1, 26, -1, 1, 2, 0.8 };//双方都想要小孩
            TestOutput(m.Predict(makeInput(newrow)));
        }
    }
}

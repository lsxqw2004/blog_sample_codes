using System.Collections.Generic;
using System.Linq;
using ConsoleApplication1;
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
            var rssResult = newsFeatures.GetArticleWords();
            var matrixTuple = newsFeatures.MakeMatrix(rssResult.Item1, rssResult.Item2);
            TestOutput(JsonConvert.SerializeObject(matrixTuple.Item2.Take(10)));
            TestOutput(rssResult.Item3[1]);
            TestOutput(JsonConvert.SerializeObject(matrixTuple.Item1[1].Take(10)));
        }

        [Fact]
        public void TestNumpy()
        {
            using (Py.GIL())
            {
                Numpy.Initialize();
                var scope = Py.CreateScope();
                var np = scope.Import("numpy", "np");


                dynamic m1 = np.matrix(new List<List<float>> { new List<float>() { 1, 2, 3 }, new List<float>() { 4, 5, 6 } });
                TestOutput(m1);
                dynamic m2 = np.matrix(new List<List<float>> { new List<float>() { 1, 2,  }, new List<float>() { 3,4},new List<float>() { 5, 6 } });
                TestOutput(m2);

                TestOutput((m1*m2).ToString());
            }
        }
    }
}

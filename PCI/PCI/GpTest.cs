using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace PCI
{
    public class GpTest
    {
        private readonly ITestOutputHelper _output;

        public GpTest(ITestOutputHelper output)
        {
            _output = output;
        }

        private void TestOutput(object obj)
        {
            _output.WriteLine(obj.ToString());
        }


        [Fact]
        public void TestBuildHiddenset()
        {
            var gp = new Gp();
            var hiddenset = gp.BuildHiddenSet();
            TestOutput(JsonConvert.SerializeObject(hiddenset));
        }

        [Fact]
        public void TestExpressionTree()
        {
            var xParamExp = Expression.Parameter(typeof(int), "x");
            var yParamExp = Expression.Parameter(typeof(int), "y");

            var returnLabel = Expression.Label(typeof(int));
            var blockExp = Expression.Block(
                Expression.IfThenElse(
                    //if判断
                    Expression.GreaterThan(xParamExp, Expression.Constant(3)),
                    //if为true执行
                    Expression.Return(returnLabel, Expression.Add(yParamExp, Expression.Constant(5))),
                    //else执行
                    Expression.Return(returnLabel, Expression.Subtract(yParamExp, Expression.Constant(2)))
                    ),
                Expression.Label(returnLabel, Expression.Constant(0))
            );

            var lambdaExp = Expression.Lambda<Func<int, int, int>>(blockExp, xParamExp, yParamExp);
            var lambda = lambdaExp.Compile();
            TestOutput(lambda(5, 5));
            TestOutput(lambda(0, 0));
        }

        [Fact]
        public void TestExpressionTreeDisplay()
        {
            var xParamExp = Expression.Parameter(typeof(int), "x");
            var yParamExp = Expression.Parameter(typeof(int), "y");

            var returnLabel = Expression.Label(typeof(int));
            var blockExp = Expression.Block(
                Expression.IfThenElse(
                    //if判断
                    Expression.GreaterThan(xParamExp, Expression.Constant(3)),
                    //if为true执行
                    Expression.Return(returnLabel, Expression.Add(yParamExp, Expression.Constant(5))),
                    //else执行
                    Expression.Return(returnLabel, Expression.Subtract(yParamExp, Expression.Constant(2)))
                ),
                Expression.Label(returnLabel, Expression.Constant(0))
            );

            var printer = new GpPrinter();
            TestOutput(printer.Display(blockExp));
        }

        [Fact]
        public void TestMakeRandomTree()
        {
            
            var input1ParamExp = Expression.Parameter(typeof(int), "input1");
            var input2ParamExp = Expression.Parameter(typeof(int), "input2");
            var paramArr = new[] { input1ParamExp, input2ParamExp };
            var random1 = Gp.MakeRandomTree(paramArr);

            var printer = new GpPrinter();
            TestOutput(printer.Display(random1));
            
            var func = Expression.Lambda<Func<int, int, int>>(random1, paramArr).Compile();
            TestOutput(func(7, 1));
            TestOutput(func(2, 4));
            
            var random2 = Gp.MakeRandomTree(paramArr);
            TestOutput(printer.Display(random2));
            
            var func2 = Expression.Lambda<Func<int, int, int>>(random2, paramArr).Compile();
            TestOutput(func2(5, 3));
            TestOutput(func2(5, 20));
        }

        [Fact]
        public void TestScoreFunction()
        {
            var gp = new Gp();
            var input1ParamExp = Expression.Parameter(typeof(int), "input1");
            var input2ParamExp = Expression.Parameter(typeof(int), "input2");
            var paramArr = new[] { input1ParamExp, input2ParamExp };
            var random1 = Gp.MakeRandomTree(paramArr);
            var func = Expression.Lambda<Func<int, int, int>>(random1, paramArr).Compile();
            var hiddenset = gp.BuildHiddenSet();
            var diff = gp.ScoreFunction(func, hiddenset);
            TestOutput(diff);
            var random2 = Gp.MakeRandomTree(paramArr);
            var func2 = Expression.Lambda<Func<int, int, int>>(random2, paramArr).Compile();
            diff = gp.ScoreFunction(func2, hiddenset);
            TestOutput(diff);
        }

        [Fact]
        public void TestExpressionTreeMutate()
        {
            var xParamExp = Expression.Parameter(typeof(int), "x");
            var yParamExp = Expression.Parameter(typeof(int), "y");

            var returnLabel = Expression.Label(typeof(int));
            var blockExp = Expression.Block(
                Expression.IfThenElse(
                    //if判断
                    Expression.GreaterThan(xParamExp, Expression.Constant(3)),
                    //if为true执行
                    Expression.Return(returnLabel, Expression.Add(yParamExp, Expression.Constant(5))),
                    //else执行
                    Expression.Return(returnLabel, Expression.Subtract(yParamExp, Expression.Constant(2)))
                ),
                Expression.Label(returnLabel, Expression.Constant(0))
            );

            var printer = new GpPrinter();
            TestOutput(printer.Display(blockExp));

            var mutater = new ExpTestMutate();
            var newExp = mutater.Mutate(blockExp);
            TestOutput(printer.Display(newExp));
        }


        [Fact]
        public void TestMutate()
        {
            var gp = new Gp();
            var input1ParamExp = Expression.Parameter(typeof(int), "input1");
            var input2ParamExp = Expression.Parameter(typeof(int), "input2");
            var paramArr = new[] { input1ParamExp, input2ParamExp };
            var random1 = Gp.MakeRandomTree(paramArr);

            var printer = new GpPrinter();
            TestOutput(printer.Display(random1));

            TestOutput("-----------我是分隔线-------------");

            var newExp = gp.Mutate(random1, paramArr);
            TestOutput(printer.Display(newExp));
        }


        [Fact]
        public void TestMutateResult()
        {
            var gp = new Gp();
            var input1ParamExp = Expression.Parameter(typeof(int), "input1");
            var input2ParamExp = Expression.Parameter(typeof(int), "input2");
            var paramArr = new[] { input1ParamExp, input2ParamExp };
            var random1 = Gp.MakeRandomTree(paramArr);

            var hiddenset = gp.BuildHiddenSet();
            var func1 = random1.Compile<Func<int, int, int>>(paramArr);
            TestOutput(gp.ScoreFunction(func1,hiddenset));

            TestOutput("-----------我是分隔线-------------");

            var newExp = gp.Mutate(random1, paramArr);
            var funcMutate = newExp.Compile<Func<int, int, int>>(paramArr);
            TestOutput(gp.ScoreFunction(funcMutate, hiddenset));
        }

        [Fact]
        public void TestCrossOver()
        {
            var gp = new Gp();
            var printer = new GpPrinter();
            var input1ParamExp = Expression.Parameter(typeof(int), "input1");
            var input2ParamExp = Expression.Parameter(typeof(int), "input2");
            var paramArr = new[] { input1ParamExp, input2ParamExp };
            var random1 = Gp.MakeRandomTree(paramArr);
            TestOutput(printer.Display(random1));

            TestOutput("-----------我是分隔线-------------");

            var random2 = Gp.MakeRandomTree(paramArr);
            TestOutput(printer.Display(random2));

            TestOutput("-----------我是分隔线-------------");

            var crossed = gp.CrossOver(random1,random2);
            TestOutput(printer.Display(crossed));
        }

        [Fact]
        public void TestEvolve()
        {
            var gp = new Gp(TestOutput);
            var rf = gp.GetRankFunction(gp.BuildHiddenSet());
            var input1ParamExp = Expression.Parameter(typeof(int), "input1");
            var input2ParamExp = Expression.Parameter(typeof(int), "input2");
            var paramArr = new[] { input1ParamExp, input2ParamExp };
            gp.Evolve(paramArr,500,rf,mutationrate: 0.2,breedingreate: 0.1,pexp: 0.7,pnew: 0.1);
        }
    }
}
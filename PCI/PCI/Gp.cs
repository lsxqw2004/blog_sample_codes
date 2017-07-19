using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LibSVMsharp.Core;
using MathNet.Numerics.Random;

namespace PCI
{
    public class Gp
    {
        private readonly Action<object> _outputWriter;

        public Gp()
        {
            
        }

        public Gp(Action<object> outputAction)
        {
            _outputWriter = outputAction;
        }

        private int HiddenFunction(int x, int y)
        {
            return x * x + 2 * y + 3 * x + 5;
        }

        public List<ValueTuple<int, int, int>> BuildHiddenSet()
        {
            var rows = new List<ValueTuple<int, int, int>>();
            for (int i = 0; i < 200; i++)
            {
                var x = RndGenerator.RndInt32(0, 40);
                var y = RndGenerator.RndInt32(0, 40);
                rows.Add((x, y, HiddenFunction(x, y)));
            }
            return rows;
        }

        public static Expression MakeRandomTree(ParameterExpression[] paramExps, int maxTreeDepth = 4,
            double fpr = 0.5, double ppr = 0.6)
        {
            if (RndGenerator.RndDouble() < fpr && maxTreeDepth > 0)
            {
                (var funcExp, var pc) = ExpFactory.Choice();
                var children = new Expression[pc];
                for (int i = 0; i < pc; i++)
                {
                    children[i] = MakeRandomTree(paramExps, maxTreeDepth - 1, fpr, ppr);
                }
                return funcExp(children);
            }

            else if (RndGenerator.RndDouble() < ppr)
            {
                return paramExps[RndGenerator.RndInt32(0, paramExps.Length)];
            }
            else
            {
                return Expression.Constant(RndGenerator.RndInt32(0, 10));
            }
        }

        public long ScoreFunction(Func<int, int, int> func, List<ValueTuple<int, int, int>> s)
        {
            var dif = 0L;
            foreach ((int x, int y, int r) data in s)
            {
                var v = func(data.x, data.y);
                dif += Math.Abs(v - data.r);
            }
            return dif;
        }

        public Expression Mutate(Expression t, ParameterExpression[] paramExps, double probchange = 0.1)
        {
            var expMutate = new ExpMutate(paramExps, probchange);
            return expMutate.Mutate(t);
        }

        public Expression CrossOver(Expression t1, Expression t2, double probswap = 0.7, bool top = true)
        {
            if (RndGenerator.RndDouble() < probswap && !top)
                return t2;
            var result = t1;
            var childrenExpsT1 = GetChildren(t1);
            var childrenExpsT2 = GetChildren(t2);
            if (childrenExpsT1 == null || childrenExpsT2 == null)
            {
                return result;
            }
            var newChildren = new List<Expression>();
            foreach (var expression in childrenExpsT1)
            {
                newChildren.Add(CrossOver(expression, childrenExpsT2[RndGenerator.RndInt32(0, childrenExpsT2.Count)], probswap, false));
            }
            return UpdateChildren(result, newChildren);
        }

        private List<Expression> GetChildren(Expression exp)
        {
            if (exp is BinaryExpression)
            {
                var binExp = (BinaryExpression)exp;
                return new List<Expression>()
                {
                    binExp.Left,
                    binExp.Right
                };
            }
            if (exp is BlockExpression)
            {
                var ifelseExp = ((BlockExpression)exp).Expressions[0] as ConditionalExpression;
                if (ifelseExp != null)
                {
                    return new List<Expression>()
                    {
                        ((GotoExpression)ifelseExp.IfTrue).Value,
                        ((GotoExpression)ifelseExp.IfFalse).Value
                    };
                }
            }

            //如果是ConstantExpression或ParameterExpression则直接返回null
            return null;
        }

        private Expression UpdateChildren(Expression origin, List<Expression> children)
        {
            if (origin is BinaryExpression)
            {
                var binExp = (BinaryExpression)origin;
                return binExp.Update(children[0], binExp.Conversion, children[1]);
            }
            if (origin is BlockExpression)
            {
                var blockExp = (BlockExpression)origin;
                var ifelseExp = blockExp.Expressions[0] as ConditionalExpression;
                if (ifelseExp != null)
                {
                    var trueExp = ifelseExp.IfTrue as GotoExpression;
                    var falseExp = ifelseExp.IfFalse as GotoExpression;
                    var newTrueExp = trueExp.Update(trueExp.Target, children[0]);
                    var newFalseExp = falseExp.Update(falseExp.Target, children[1]);
                    var newIfelseExp = ifelseExp.Update(ifelseExp.Test, newTrueExp, newFalseExp);
                    return blockExp.Update(blockExp.Variables, new[] { newIfelseExp, blockExp.Expressions[1] });
                }
                throw new Exception("无法解析的表达式");
            }
            throw new Exception("无法解析的表达式");
        }

        public Func<int,int,int> Evolve(ParameterExpression[] pc, int popsize,
            Func<List<ValueTuple<Func<int, int, int>,Expression>>, List<ValueTuple<long, Func<int, int, int>,Expression>>> rankfunction, int maxgen = 500, double mutationrate = 0.1,
            double breedingreate = 0.4, double pexp = 0.7, double pnew = 0.05)
        {
            //返回一个随机数，通常是一个较小的数
            //pexp的取值越小，我们得到的随机数就越小
            Func<int> selectIndex =()=> (int) (Math.Log(RndGenerator.RndDouble()) / Math.Log(pexp));

            // 创建一个随机的初始种群
            var population = new List<ValueTuple<Func<int,int,int>,Expression>>(popsize);
            for (int i = 0; i < popsize; i++)
            {
                var exp = MakeRandomTree(pc);
                var func= exp.Compile<Func<int, int, int>>(pc);
                population.Add((func,exp));
            }
            List<ValueTuple<long, Func<int, int, int>, Expression>> scores = null;
            for (int i = 0; i < maxgen; i++)
            {
                scores = rankfunction(population);
                _outputWriter?.Invoke(scores[0].Item1);
                if (scores[0].Item1 == 0) break;

                // 取两个最优的程序
                var newpop = new List<ValueTuple<Func<int,int,int>,Expression>>()
                {
                    (scores[0].Item2 ,scores[0].Item3),
                    (scores[1].Item2, scores[1].Item3)
                };

                //构造下一代
                while (newpop.Count<popsize)
                {
                    if(RndGenerator.RndDouble()>pnew)
                    {
                        var exp = Mutate(
                            CrossOver(scores[selectIndex()].Item3,
                                scores[selectIndex()].Item3, breedingreate), pc, mutationrate);
                        var func = exp.Compile<Func<int, int, int>>(pc);
                        newpop.Add((func,exp));
                    }
                    else
                    {
                        //加入一个随机节点，增加种群的多样性
                        var exp = MakeRandomTree(pc);
                        var func = exp.Compile<Func<int, int, int>>(pc);
                        newpop.Add((func,exp));
                    }
                }

                population = newpop;
            }
            var printer = new GpPrinter();
            _outputWriter?.Invoke(printer.Display(scores[0].Item3));
            return scores[0].Item2;
        }

        public Func<List<ValueTuple< Func<int, int, int>,Expression>>, List<ValueTuple<long, Func<int, int, int>,Expression>>> 
            GetRankFunction(List<ValueTuple<int, int, int>> dataset)
        {
            Func<List<ValueTuple<Func<int, int, int>, Expression>>,List<ValueTuple<long,Func<int,int,int>, Expression>>> rankfunction = poplation =>
            {
                var scores = poplation.Select(t => (ScoreFunction(t.Item1, dataset), t.Item1,t.Item2)).ToList();
                scores.Sort((x,y)=>x.Item1.CompareTo(y.Item1));
                return scores;
            };
            return rankfunction;
        }
    }

    public static class RndGenerator
    {
        public static double RndDouble()
        {
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            return rnd.NextDouble();
        }

        public static int RndInt32(int min, int max)
        {
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            return rnd.Next(min, max);
        }
    }

    public class ExpFactory
    {
        public static (Func<Expression[], Expression>, int) Choice()
        {
            return _supportFunc[RndGenerator.RndInt32(0, _supportFunc.Count)];
        }


        private static readonly List<ValueTuple<Func<Expression[], Expression>, int>> _supportFunc =
            new List<ValueTuple<Func<Expression[], Expression>, int>>()
            {
                (inputExp=>Expression.Add(inputExp[0],inputExp[1]),2),
                (inputExp=>Expression.Subtract(inputExp[0],inputExp[1]),2),
                (inputExp=>Expression.Multiply(inputExp[0],inputExp[1]),2),
                (inputExp=>ConstructGt0Expe(inputExp),3),
                (inputExp=>ConstructGtExpe(inputExp),2),
            };

        private static Expression ConstructGt0Expe(Expression[] inputExp)
        {
            var returnLabel = Expression.Label(typeof(int));
            var exp = Expression.Block(
                Expression.IfThenElse(
                    Expression.GreaterThan(inputExp[0], Expression.Constant(0)),
                    Expression.Return(returnLabel, inputExp[1]),
                    Expression.Return(returnLabel, inputExp[2])),
                Expression.Label(returnLabel, Expression.Constant(0))
            );
            return exp;
        }

        private static Expression ConstructGtExpe(Expression[] inputExp)
        {
            var returnLabel = Expression.Label(typeof(int));
            var exp = Expression.Block(
                Expression.IfThenElse(
                    Expression.GreaterThan(inputExp[0], inputExp[1]),
                    Expression.Return(returnLabel, Expression.Constant(0)),
                    Expression.Return(returnLabel, Expression.Constant(1))),
                Expression.Label(returnLabel, Expression.Constant(0))
            );
            return exp;
        }
    }

    public class GpPrinter : ExpressionVisitor
    {
        private readonly StringBuilder _stringBuilder;
        private const int Tab = 2;
        private int _depth;

        public GpPrinter()
        {
            _stringBuilder = new StringBuilder();
        }

        private void Indent()
        {
            _depth += Tab;
        }
        private void Dedent()
        {
            _depth -= Tab;
        }

        private void Out(string output)
        {
            if (_depth > 0)
                _stringBuilder.Append(new string(' ', _depth));
            _stringBuilder.AppendLine(output);
        }


        public string Display(Expression expression)
        {
            _stringBuilder.Clear();
            this.Visit(expression);
            return _stringBuilder.ToString();
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {

            Out("if");
            Indent();
            Visit(node.Test);
            Visit(node.IfTrue);
            Visit(node.IfFalse);
            Dedent();

            return node;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {

            Out(node.NodeType.ToString());
            Indent();
            Visit(node.Left);
            Visit(node.Right);
            Dedent();
            return node;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Out($"p_{node.Name}");

            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            Out(node.Value.ToString());
            return node;
        }
    }

    public class ExpTestMutate : ExpressionVisitor
    {

        public Expression Mutate(Expression expression)
        {
            return this.Visit(expression);
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            var newTrue = Expression.Constant(-100);
            var newFalse = Expression.Constant(-200);
            node = node.Update(node.Test, newTrue, newFalse);

            return node;
        }
    }

    public class ExpMutate : ExpressionVisitor
    {
        private readonly ParameterExpression[] _paramExps;
        private readonly double _probchange;

        public ExpMutate(ParameterExpression[] paramExps, double probchange = 0.1)
        {
            _paramExps = paramExps;
            _probchange = probchange;
        }

        public Expression Mutate(Expression expression)
        {
            return this.Visit(expression);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (RndGenerator.RndDouble() < _probchange)
                return Gp.MakeRandomTree(_paramExps);

            var newLeft = Visit(node.Left);
            var newRight = Visit(node.Right);
            node = node.Update(newLeft, node.Conversion, newRight);
            return node;
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            if (RndGenerator.RndDouble() < _probchange)
                return Gp.MakeRandomTree(_paramExps);

            // 针对我们ExpFactory构造的表达式，并非通用
            node = node.Update(node.Variables, new[] { Visit(node.Expressions[0]), node.Expressions[1] });
            return node;
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            var newTrue = Visit(node.IfTrue);
            var newFalse = Visit(node.IfFalse);
            node = node.Update(node.Test, newTrue, newFalse);
            return node;
        }

        protected override Expression VisitGoto(GotoExpression node)
        {
            var newVal = Visit(node.Value);
            return node.Update(node.Target, newVal);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (RndGenerator.RndDouble() < _probchange)
                return Gp.MakeRandomTree(_paramExps);

            return base.VisitParameter(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (RndGenerator.RndDouble() < _probchange)
                return Gp.MakeRandomTree(_paramExps);

            return base.VisitConstant(node);
        }
    }


    public static class ExpressionExtension
    {
        public static T Compile<T>(this Expression exp, ParameterExpression[] expParams)
        {
            return Expression.Lambda<T>(exp, expParams).Compile();
        }
    }
}

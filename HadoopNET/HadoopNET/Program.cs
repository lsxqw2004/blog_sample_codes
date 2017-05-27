using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Hadoop.MapReduce;

namespace HadoopNET
{
    class Program
    {
        static void Main(string[] args)
        {

            var hadoop = Hadoop.Connect();
            hadoop.MapReduceJob.ExecuteJob<FirstJob>();
        }
    }

    public class FirstMapper : MapperBase
    {
        public override void Map(string inputLine, MapperContext context)
        {
            // 输入
            int inputValue = int.Parse(inputLine);

            // 任务
            var sqrt = Math.Sqrt(inputValue);

            // 写入输出
            context.EmitKeyValue(inputValue.ToString(), sqrt.ToString());
        }
    }

        public class FirstJob : HadoopJob<FirstMapper>
        {
            public override HadoopJobConfiguration Configure(ExecutorContext context)
            {
                HadoopJobConfiguration config = new HadoopJobConfiguration();
                config.InputPath = "input/SqrtJob";
                config.OutputFolder = "output/SqrtJob";
            //config
                return config;
            }
        }
}

using System;

namespace NetCreateCluster
{
    class Program
    {
        static void Main(string[] args)
        {
            var helper = new DeploymentHelper();
            helper.Run();

            Console.ReadLine();//防止退出
        }
    }
}


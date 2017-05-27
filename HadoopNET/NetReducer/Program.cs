using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetReducer
{
    class Program
    {
        private class Wc
        {
            static void Main(string[] args)
            {
                string line;
                var count = 0;

                if (args.Length > 0)
                {
                    Console.SetIn(new StreamReader(args[0]));
                }

                while ((line = Console.ReadLine()) != null)
                {
                    count += line.Count(cr => (cr == ' ' || cr == '\n'));
                }
                Console.WriteLine(count);
            }
        }
    }
}

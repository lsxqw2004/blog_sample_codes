using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NetCoreMapper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Stream stream = new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.Read);
                Console.SetIn(new StreamReader(stream));
            }

            string line;
            while ((line = Console.ReadLine()) != null)
            {
                Console.WriteLine(line);
            }

        }
    }
}

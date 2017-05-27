using System;
using System.IO;

namespace NetMapper
{
    class Cat
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Console.SetIn(new StreamReader(args[0]));
            }

            string line;
            while ((line = Console.ReadLine()) != null)
            {
                Console.WriteLine(line);
            }
        }
    }
}

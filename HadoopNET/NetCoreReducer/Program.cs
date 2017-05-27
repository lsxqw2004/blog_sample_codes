using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NetCoreReducer
{
    public class Program
    {
        static void Main(string[] args)
        {
            ILoggerFactory loggerFactory = new LoggerFactory()
                .AddConsole()
                .AddDebug();
            ILogger logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation(
                "This is a test of the emergency broadcast system.");

            string line;
            var count = 0;

            if (args.Length > 0)
            {
                Console.SetIn(new StreamReader(new FileStream(args[0], FileMode.Open, FileAccess.Read)));
            }

            while ((line = Console.ReadLine()) != null)
            {
                count += line.Count(cr => (cr == ' ' || cr == '\n'));
            }
            Console.WriteLine(count);
        }
    }
}

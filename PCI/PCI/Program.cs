using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConsoleApplication1.Draw;
using Newtonsoft.Json;

namespace ConsoleApplication1
{


    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            
            var tx= new Task(()=> Console.WriteLine($"{DateTime.Now:mm:ss fff}"));
            tx.ContinueWith(async t =>
            {
                await Task.Delay(5000);
                Console.WriteLine($"{DateTime.Now:mm:ss fff}");
            });
            tx.Start();

            Console.Read();

        }



    }
}

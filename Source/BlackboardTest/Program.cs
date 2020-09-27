using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlackboardTest;
using BlackboardTester;

namespace Blackboard
{
    class Program
    {
        static void Main(string[] args)
        {
            var tester = new Tester();
            var receiveEvent1 = new ReceiveEvent1();
            receiveEvent1.StartTracking();

            Console.WriteLine("\npress any key to exit...");
            Console.ReadKey(true);
        }
    }
}

using System;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ACNADailyPrayer
{
    class Program
    {
        static void Main(string[] args)
        {
            ACNADailyPrayer.Service testService = new Service(Service.Office.MorningPrayer, "Monday September 2 2019");

            //Console.Write(ACNADailyPrayer.Service.GetReading("Psalm 5"));
            Console.Write(string.Join("\n", testService.serviceText.ToArray()));

            Console.Read();
            
        }

    }
}

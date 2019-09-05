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
            ACNADailyPrayer.Service testService = new Service(Service.Office.EveningPrayer, "Monday January 1 1900");

            Console.Write("Please enter a date in the format Weekday MonthName DD YYYY: ");
            string dateToTest = Console.ReadLine();

            Console.Write("Please specify Morning or Evening Prayer with M or E: ");
            string serviceTypeIn = Console.ReadLine();

            switch (serviceTypeIn)
            {
                case "M":
                    testService = new Service(Service.Office.MorningPrayer, dateToTest);
                    break;


                case "E":
                    testService = new Service(Service.Office.EveningPrayer, dateToTest);
                    break;

                default:
                    System.Console.Clear();
                    Main(null);
                    break;

            }


            //Console.Write(ACNADailyPrayer.Service.GetReading("Psalm 5"));
            Console.Write(string.Join("\n", testService.serviceText.ToArray()));

            Console.Read();
            
        }

    }
}

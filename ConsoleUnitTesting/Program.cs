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
            /* ACNADailyPrayer.Service testService = new Service(Service.Office.EveningPrayer, "Monday January 1 1900");

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
             Console.Write(string.Join("\n", testService.serviceText.ToArray()));*/

            Console.WriteLine(readBCP2019Psalms(150));

            Console.WriteLine("\n");
            Console.WriteLine("Press any key to continue...");
            Console.Read();
            
        }

        static string readBCP2019Psalms(int psalmNumber)
        {
            System.IO.StreamReader sReader = new System.IO.StreamReader("lectionary/2019bcppsalter");
            string currentLine = "";
            string psalmToReturn = "";

            while (!sReader.EndOfStream)
            {
                // Find a Psalm (any line containing the word 'Psalm' exactly)
                currentLine = sReader.ReadLine();

                // Stop and read the Psalm in if we hit the line we're looking for
                if (currentLine.Contains("Psalm" + " " + psalmNumber.ToString()))
                {
                    psalmToReturn += currentLine += "\n" + "\n";
                    currentLine = sReader.ReadLine();


                    // Read up to the next line that we hit that contains 'Psalm'
                    while (!currentLine.Contains("Psalm") && (!sReader.EndOfStream))
                    {
                        psalmToReturn += currentLine + "\n";
                        currentLine = sReader.ReadLine();
                    }

                    return psalmToReturn + "\n" + "\n";
                }
            }

            return "Unable to read Psalm";
        }

    }
}

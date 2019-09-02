using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ACNADailyPrayer
{
    class ServiceDate
    {
        int date;
        string month;
        string weekday;

        string firstReading;
        string SecondReading;
        int[] psalmsOfTheDay;
        Collect collectOfTheDay;
        string commemorations;
    }

    class Collect
    {
        string collectText;
        string collectTitle;
    }

    public class Service
    {
        
        public static string apiKey = "fc3f492dd3431c10bacd0386ed01964cb769025a";
        public static string endPointURL = "https://api.esv.org/v3/passage/text/?";

        public static string GetReading(string inputReading)
        {

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(ACNADailyPrayer.Service.endPointURL + "include-footnotes=false" + "&" + "include-verse-numbers=false" + "&" + "include-passage-references=true"
                                        + "&" + "include-headings=false" + "&" + "q=" + inputReading);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue

            ("Token", ACNADailyPrayer.Service.apiKey);

            string requestJSON = client.GetStringAsync(client.BaseAddress).Result;
            JObject parser = JObject.Parse(requestJSON);
            JToken token = parser.SelectToken("$.passages[0]");


            return token.ToString().Trim();
        }
        public static string ReadServiceElementFromFile(string filePath)
        {
            string readString = "";

            StreamReader sReader = new StreamReader(filePath);
            while (!sReader.EndOfStream) readString += sReader.ReadLine() + "\n";

                 return readString;
        }

        public enum Office
        {
            MorningPrayer, EveningPrayer, Compline
        }

        ServiceDate date;
        Office serviceType;
        public List<string> serviceText;


        public Service(Office officeTypeIn, string fullDate)
        {
            serviceType = officeTypeIn;
            serviceText = new List<String>();
            PrepareServiceText();
        }

        private string getDailyPsalms()
        {
            return "";
        }

        private void PrepareServiceText()
        {
            // Read in invitatory to begin both offices...
            serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\invitatory"));

            // Then the Venite before the psalms (or the Phos Hilaron if Evening Prayer)
            if (serviceType == Office.MorningPrayer)
                serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\venite"));
                    else if (serviceType == Office.EveningPrayer)
                        serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\phoshilaron"));


            serviceText.Add(getDailyPsalms());

            // Add the first reading

            // If Morning Prayer, add the Te Deum, or if Evening Prayer add the Magnificat

            // Add the second reading

            // If Morning Prayer, add the Benedictus, or if Evening Prayer add the Nunc Dimittis

            // Add the Apostle's Creed

            // Add the Kyrie, Our Father, Suffrages

            // Add the Collect of the Day

            // Add the two additional collects depending on Morning or Evening Prayer

            // Add the General Thanksgiving and the Prayer of St. Chrysostom

            // Add the Grace

        }
    }

}

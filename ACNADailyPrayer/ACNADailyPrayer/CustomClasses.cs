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

        public string firstReading;
        public string secondReading;
        public int[] psalmsOfTheDay;
        public Collect collectOfTheDay;
        public string commemorations;

        public ServiceDate()
        {
            firstReading = ""; secondReading = "";
            collectOfTheDay = new Collect();
            psalmsOfTheDay = new int[1]; psalmsOfTheDay[0] = 1;
        }
    }

    class Collect
    {
       public string collectText;
       public string collectTitle;

        public Collect()
        {
            collectText = ""; collectTitle = "";
        }
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
            date = new ServiceDate();

            PrepareServiceText();
        }

        private string getDailyPsalms()
        {
            string psalmsString = "";

            for(int i = 0; i < date.psalmsOfTheDay.Length; i++)
            {
               psalmsString += Service.GetReading("Psalm " + date.psalmsOfTheDay[i].ToString());
            }

            return psalmsString;
        }

        private void PrepareServiceText()
        {
            serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\invitatory"));

            if (serviceType == Office.MorningPrayer)
                serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\venite"));
                    else if (serviceType == Office.EveningPrayer)
                        serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\phoshilaron"));


            serviceText.Add(getDailyPsalms());

            if(date.firstReading != "") serviceText.Add(date.firstReading);

            if (serviceType == Office.MorningPrayer)
                serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\tedeum"));
            else if (serviceType == Office.EveningPrayer)
                serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\magnificat"));

                if(date.secondReading != "") serviceText.Add(date.secondReading);

            if (serviceType == Office.MorningPrayer)
                serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\benedictus"));
            else if (serviceType == Office.EveningPrayer)
                serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\nuncdimittis"));

            serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\apostlescreed"));

            serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\kyrieourfathersuffrages"));

            serviceText.Add(date.collectOfTheDay.collectText);

            if (serviceType == Office.MorningPrayer)
            {
                serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\collectforpeacemorning"));
                serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\collectforgrace"));


            }
            else if (serviceType == Office.EveningPrayer)
            {
                serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\collectforpeaceevening"));
                serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\collectforaidagainstperils"));


            }

            serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\prayerformission"));


            serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\generalthanksgiving"));

            serviceText.Add(ReadServiceElementFromFile(@".\servicetexts\thegrace"));

        }
    }

}

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xamarin;
using Xamarin.Forms;

namespace ACNADailyPrayer
{
    class ServiceDate
    {
        public int date;
        public string month;
        public string weekday;
        public string year;

        public string [] readings;
        public List<int> psalmsOfTheDay;
        public Collect collectOfTheDay;
        public string commemorations;

        public ServiceDate(string dateIn)
        {
            string[] dateElements = dateIn.Split(' ');
            weekday = dateElements[0];
            month = dateElements[1];
            date = int.Parse(dateElements[2]);
            year = dateElements[3];

            collectOfTheDay = new Collect();
            psalmsOfTheDay = new List<int>();
            //psalmsOfTheDay.Add(102); psalmsOfTheDay.Add(103); psalmsOfTheDay.Add(104);
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

            var assembly = typeof(App).GetTypeInfo().Assembly;
            Stream stream = assembly.GetManifestResourceStream(filePath);

            StreamReader sReader = new StreamReader(stream);
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
            serviceText = new List<string>();
            date = new ServiceDate(fullDate);

            readDailyPsalmsFromFile(@"ACNADailyPrayer.lectionary.psalmcycletabdelim");
            readDailyReadings();

            PrepareServiceText();
        }

        private void readDailyReadings()
        {
            // This function takes read the specified month file (e.g. "January_lectionary"), and then reads the Bible readings in tab-delimited format,
            // then returns a list of bible reference as a string to plug into another API for the actual reading text

            var assembly = typeof(App).GetTypeInfo().Assembly;
            Stream stream = assembly.GetManifestResourceStream(@"ACNADailyPrayer.lectionary." + date.month + "_lectionary");

            char delimiter = '\t';
            StreamReader sReader = new StreamReader(stream);

            while (!sReader.EndOfStream)
            {
                // Do processing here
                string currentLine = sReader.ReadLine();
                string[] currentLineSplit = currentLine.Split(delimiter);

                // Don't waste processing on a different date, just go to the next line
                if (currentLineSplit[0] != date.date.ToString()) continue;

                // Otherwise, parse out the readings and put them into our string List to return
                if (serviceType == Office.MorningPrayer) date.readings = currentLineSplit[1].Split(',');
                else if (serviceType == Office.EveningPrayer) date.readings = currentLineSplit[2].Split(',');
            }

            return;
        }

        private void readDailyPsalmsFromFile(string lectionaryFile)
        {
            // Check today's date against lectionary file & write psalm numbers to variable ready for pulling text from source (ESV API or BCP 2019 Psalter)

            var assembly = typeof(App).GetTypeInfo().Assembly;
            Stream stream = assembly.GetManifestResourceStream(lectionaryFile);

            // Read in tab delimited file:
            StreamReader sReader = new StreamReader(stream);
            char delimiter = '\t';

            int dateToPull = date.date;

            // 30-day psalm cycle - AVOID the 31st breaking it!
            if (dateToPull == 31)
            {
                dateToPull = 30;
            }
            
            while (!sReader.EndOfStream)
            {
                string currentLine = sReader.ReadLine();
                string [] currentLineSplit = currentLine.Split(delimiter);

                // Don't waste processing on a different date, just go to the next line
                if (currentLineSplit[0] != dateToPull.ToString()) continue;

                // Feed morning or evening psalms into service text variable
                string[] psalmsToFeedIn = new string[0];
                if (serviceType == Office.MorningPrayer) psalmsToFeedIn = currentLineSplit[1].Split(',');
                else if (serviceType == Office.EveningPrayer) psalmsToFeedIn = currentLineSplit[2].Split(',');

                foreach(string psalmNumber in psalmsToFeedIn)
                {
                    date.psalmsOfTheDay.Add(int.Parse(psalmNumber));
                }
            }
        }

        private string getDailyPsalms()
        {
            string psalmsString = "";

            foreach(int psalmNumber in date.psalmsOfTheDay)
            {
               psalmsString += "\n\n" + Service.GetReading("Psalm " + psalmNumber.ToString());
            }

            return psalmsString;
        }

        private void PrepareServiceText()
        {
            serviceText.Add(date.weekday + " " + date.month + " " + date.date + " " + date.year + "\n\n");

            if (serviceType == Office.MorningPrayer) serviceText.Add("Morning Prayer \n");
            else if (serviceType == Office.EveningPrayer) serviceText.Add("Evening Prayer \n");

            serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.invitatory"));

            if (serviceType == Office.MorningPrayer)
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.venite"));
                    else if (serviceType == Office.EveningPrayer)
                        serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.phoshilaron"));


            serviceText.Add(getDailyPsalms());

            if ((date.readings[0] != "") && (serviceType != Office.Compline))  serviceText.Add(Service.GetReading(date.readings[0]));

            if (serviceType == Office.MorningPrayer)
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.tedeum"));
            else if (serviceType == Office.EveningPrayer)
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.magnificat"));

            if((date.readings[1] != "") && (serviceType != Office.Compline)) serviceText.Add(Service.GetReading(date.readings[1]));

            if (serviceType == Office.MorningPrayer)
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.benedictus"));
            else if (serviceType == Office.EveningPrayer)
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.nuncdimittis"));

            serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.apostlescreed"));

            serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.kyrieourfathersuffrages"));

            serviceText.Add(date.collectOfTheDay.collectText);

            if (serviceType == Office.MorningPrayer)
            {
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.collectforpeacemorning"));
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.collectforgrace"));


            }
            else if (serviceType == Office.EveningPrayer)
            {
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.collectforpeaceevening"));
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.collectforaidagainstperils"));


            }

            serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.prayerformission"));


            serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.generalthanksgiving"));

            serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.thegrace"));

        }
    }

}

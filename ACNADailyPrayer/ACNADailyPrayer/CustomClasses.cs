﻿using System;
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

        public string[] readings;
        public List<string> psalmsOfTheDay;
        public string commemorations;

        public ServiceDate(string dateIn)
        {
            string[] dateElements = dateIn.Split(' ');
            weekday = dateElements[0];
            month = dateElements[1];
            date = int.Parse(dateElements[2]);
            year = dateElements[3];

            psalmsOfTheDay = new List<string>();
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


            try { return token.ToString().Trim(); } catch { return "Error obtaining reading"; }
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

        public static DateTime dateOfEaster(int year)
        {
            // Using calculation (valid up to 2199) per:
            // https://www.whyeaster.com/customs/dateofeaster.shtml

            float yearOver19 = year / 19.0f;
            int multiplier = 19 * (int)yearOver19; // Cut off the decimal value

            int subtracter = year - multiplier;
            subtracter++;

            DateTime calculatedDate = new DateTime();

            // Golden Number lookup
            switch (subtracter)
            {
                case 1:
                    calculatedDate = new DateTime(year, 3, 27);
                    break;

                case 2:
                    calculatedDate = new DateTime(year, 4, 14);
                    break;

                case 3:
                    calculatedDate = new DateTime(year, 3, 23);
                    break;

                case 4:
                    calculatedDate = new DateTime(year, 4, 11);
                    break;

                case 5:
                    calculatedDate = new DateTime(year, 3, 31);
                    break;

                case 6:
                    calculatedDate = new DateTime(year, 4, 18);
                    break;
                case 7:
                    calculatedDate = new DateTime(year, 4, 8);
                    break;

                case 8:
                    calculatedDate = new DateTime(year, 3, 28);
                    break;

                case 9:
                    calculatedDate = new DateTime(year, 4, 16);
                    break;

                case 10:
                    calculatedDate = new DateTime(year, 4, 5);
                    break;

                case 11:
                    calculatedDate = new DateTime(year, 3, 25);
                    break;

                case 12:
                    calculatedDate = new DateTime(year, 4, 13);
                    break;

                case 13:
                    calculatedDate = new DateTime(year, 4, 2);
                    break;

                case 14:
                    calculatedDate = new DateTime(year, 3, 22);
                    break;

                case 15:
                    calculatedDate = new DateTime(year, 4, 10);
                    break;

                case 16:
                    calculatedDate = new DateTime(year, 3, 30);
                    break;

                case 17:
                    calculatedDate = new DateTime(year, 4, 17);
                    break;

                case 18:
                    calculatedDate = new DateTime(year, 4, 7);
                    break;

                case 19:
                    calculatedDate = new DateTime(year, 3, 27);
                    break;

                default:
                    break;
            }


            // Sunday next after the date calculated above is Easter!
            bool sundayFound = false;
            while (!sundayFound)
            {
                if (calculatedDate.DayOfWeek != DayOfWeek.Sunday)
                    calculatedDate = calculatedDate.AddDays(1);
                else
                    sundayFound = true;
            }

            return calculatedDate;
        }

        public enum Office
        {
            MorningPrayer, EveningPrayer, Compline
        }

        ServiceDate date;
        Office serviceType;

        public List<string> serviceText;
        public List<string> collectsOfTheDay;


        public Service(Office officeTypeIn, string fullDate)
        {
            serviceType = officeTypeIn;
            serviceText = new List<string>();
            date = new ServiceDate(fullDate);
            collectsOfTheDay = new List<string>();

            readDailyPsalmsFromFile(@"ACNADailyPrayer.lectionary.psalmcycletabdelim");
            readDailyReadings();
            chooseDailyCollect();

            PrepareServiceText();
        }

        private void readDailyReadings()
        {
            // This function takes read the specified month file (e.g. "January_lectionary"), and then reads the Bible readings in tab-delimited format,
            // then returns a list of bible reference as a string to plug into another API for the actual reading text
            char delimiter = '\t';

            DateTime dateOfEaster = Service.dateOfEaster(int.Parse(this.date.year));
            DateTime dateOfChosenService = new DateTime(int.Parse(this.date.year), Convert.ToDateTime(this.date.month + " 01, 1900").Month, this.date.date);

            // Check service date alongside date of Easter, and pull from specialist lectionary if needed instead of regular monthly
            // (for Holy Week, Easter, Ascension Day and Pentecost)
            if (dateOfChosenService <= new DateTime(dateOfChosenService.Year, 6, 13)) // The latest possible date of Pentecost is June 13th
            {
                var assembly_ = typeof(App).GetTypeInfo().Assembly;
                Stream stream_ = assembly_.GetManifestResourceStream(@"ACNADailyPrayer.lectionary.HolyweekEasterAscensionPentecost_lectionary");
                StreamReader sReader_ = new StreamReader(stream_);
                while (!sReader_.EndOfStream)
                {
                    string currentLine = sReader_.ReadLine();

                    // Maundy Thursday
                    if (dateOfChosenService == dateOfEaster.AddDays(-3) && currentLine.Contains("MaundyThursday") ||

                    // Good Friday
                    (dateOfChosenService == dateOfEaster.AddDays(-2) && currentLine.Contains("GoodFriday") ||


                    // Holy Saturday
                    (dateOfChosenService == dateOfEaster.AddDays(-1) && currentLine.Contains("HolySaturday") ||


                    // Easter Day
                    (dateOfChosenService == dateOfEaster && currentLine.Contains("EasterDay") ||


                    // Ascension
                    (dateOfChosenService == dateOfEaster.AddDays(39) && currentLine.Contains("Ascension") ||


                    // Pentecost
                    (dateOfChosenService == dateOfEaster.AddDays(49) && currentLine.Contains("Pentecost")))))))

                    {
                        string[] currentLineSplit = currentLine.Split(delimiter);
                        if (this.serviceType == Office.MorningPrayer)
                        {
                            date.readings = currentLineSplit[2].Split(',');
                        }
                        else if (this.serviceType == Office.EveningPrayer)
                        {
                            date.readings = currentLineSplit[4].Split(',');
                        }

                        return;
                    }
                }

                stream_.Close();
            }


            var assembly = typeof(App).GetTypeInfo().Assembly;
            var stream = assembly.GetManifestResourceStream(@"ACNADailyPrayer.lectionary." + date.month + "_lectionary");

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
            char delimiter = '\t';

            // TODO: Pull particular Psalms for Holy Week, Easter, Ascension, and Pentecost
            DateTime dateOfEaster = Service.dateOfEaster(int.Parse(this.date.year));
            DateTime dateOfChosenService = new DateTime(int.Parse(this.date.year), Convert.ToDateTime(this.date.month + " 01, 1900").Month, this.date.date);

            // Check service date alongside date of Easter, and pull from specialist lectionary if needed instead of regular monthly
            // (for Holy Week, Easter, Ascension Day and Pentecost)
            if (dateOfChosenService <= new DateTime(dateOfChosenService.Year, 6, 13)) // The latest possible date of Pentecost is June 13th
            {
                var assembly_ = typeof(App).GetTypeInfo().Assembly;
                Stream stream_ = assembly_.GetManifestResourceStream(@"ACNADailyPrayer.lectionary.HolyweekEasterAscensionPentecost_lectionary");
                StreamReader sReader_ = new StreamReader(stream_);
                while (!sReader_.EndOfStream)
                {
                    string currentLine = sReader_.ReadLine();

                    // Maundy Thursday
                    if (dateOfChosenService == dateOfEaster.AddDays(-3) && currentLine.Contains("MaundyThursday") ||

                    // Good Friday
                    (dateOfChosenService == dateOfEaster.AddDays(-2) && currentLine.Contains("GoodFriday") ||


                    // Holy Saturday
                    (dateOfChosenService == dateOfEaster.AddDays(-1) && currentLine.Contains("HolySaturday") ||


                    // Easter Day
                    (dateOfChosenService == dateOfEaster && currentLine.Contains("EasterDay") ||


                    // Ascension
                    (dateOfChosenService == dateOfEaster.AddDays(39) && currentLine.Contains("Ascension") ||


                    // Pentecost
                    (dateOfChosenService == dateOfEaster.AddDays(49) && currentLine.Contains("Pentecost")))))))

                    {
                        string[] currentLineSplit = currentLine.Split(delimiter);
                        string[] psalmsToFeedIn = new string[0];

                        if (this.serviceType == Office.MorningPrayer)
                        {
                            psalmsToFeedIn = currentLineSplit[1].Split(',');
                        }
                        else if (this.serviceType == Office.EveningPrayer)
                        {
                            psalmsToFeedIn = currentLineSplit[3].Split(',');
                        }

                        foreach (string psalmNumber in psalmsToFeedIn)
                        {
                            date.psalmsOfTheDay.Add(psalmNumber);
                        }

                        return;
                    }
                }

                stream_.Close();
            }

            var assembly = typeof(App).GetTypeInfo().Assembly;
            Stream stream = assembly.GetManifestResourceStream(lectionaryFile);

            // Read in tab delimited file:
            StreamReader sReader = new StreamReader(stream);

            int dateToPull = date.date;

            // 30-day psalm cycle - AVOID the 31st breaking it!
            if (dateToPull == 31)
            {
                dateToPull = 30;
            }

            while (!sReader.EndOfStream)
            {
                string currentLine = sReader.ReadLine();
                string[] currentLineSplit = currentLine.Split(delimiter);

                // Don't waste processing on a different date, just go to the next line
                if (currentLineSplit[0] != dateToPull.ToString()) continue;

                // Feed morning or evening psalms into service text variable
                string[] psalmsToFeedIn = new string[0];
                if (serviceType == Office.MorningPrayer) psalmsToFeedIn = currentLineSplit[1].Split(',');
                else if (serviceType == Office.EveningPrayer) psalmsToFeedIn = currentLineSplit[2].Split(',');

                foreach (string psalmNumber in psalmsToFeedIn)
                {
                    date.psalmsOfTheDay.Add(psalmNumber);
                }
            }
        }

        private void getDailyPsalms()
        {
            var assembly = typeof(App).GetTypeInfo().Assembly;
            Stream stream = assembly.GetManifestResourceStream(@"ACNADailyPrayer.lectionary.2019bcppsalter");
            StreamReader sReader = new StreamReader(stream);
            string currentLine = "";

            //string psalmString = "";

            foreach (string psalmNumber in date.psalmsOfTheDay)
            {
                if (psalmNumber == "") break;

                // Old code for ESV Psalter
                //if (psalmNumber[0] == 'p') psalmString = Service.GetReading(psalmNumber);
                //else psalmString = Service.GetReading("Psalm " + psalmNumber);


                // New code for BCP 2019 Psalter
                string psalmToReturn = "";

                while (!sReader.EndOfStream)
                {
                    // Find a Psalm (any line containing the word 'Psalm' exactly)
                    if (!currentLine.Contains("Psalm" + " " + psalmNumber.Trim()))
                    {
                        currentLine = sReader.ReadLine();
                    }

                    // Stop and read the Psalm in if we hit the line we're looking for
                    else
                    {
                        psalmToReturn += currentLine += "\n" + "\n";
                        currentLine = sReader.ReadLine();


                        // Read up to the next line that we hit that contains 'Psalm'
                        while (!currentLine.Contains("Psalm") && (!sReader.EndOfStream))
                        {
                            psalmToReturn += currentLine + "\n";
                            currentLine = sReader.ReadLine();
                        }

                        break;
                    }
                }

                serviceText.Add(psalmToReturn + "\n");
            }
        }

        private void chooseDailyCollect()
        {
            // TOFINISH:

            // This function will ascertain which collect to look for, based on...
            // 1) Static Feast Days - DONE!
            // 
            // 2) Variable Feast/Fast Days - Holy Week, Easter, Easter Week, Ascension Day, Pentecost, or Ash Wednesday 

            // For other dates, it will roll back to the previous Sunday, and then calculate the collect from that Sunday based on:
            // 1) During Advent - DONE!
            // 2) During Epiphany
            // 3) During Lent
            // 4) During Easter
            // 5) Sunday after Ascension
            // 6) Pentecost, Trinity Sunday, and 'propers' (Sundays after Pentecost/Trinity)

            DateTime dateOfChosenService = new DateTime(int.Parse(this.date.year), Convert.ToDateTime(this.date.month + " 01, 1900").Month, this.date.date);
            DateTime previousSunday = dateOfChosenService;
            DateTime nextSunday = dateOfChosenService;

            // Get the previous Sunday to our date
            bool sundayFound = false;

            while (!sundayFound)
            {
                if (previousSunday.DayOfWeek == DayOfWeek.Sunday) sundayFound = true;
                else previousSunday = previousSunday.AddDays(-1);
            }

            // Get the next Sunday to our date
            sundayFound = false;

            while (!sundayFound)
            {
                if (nextSunday.DayOfWeek == DayOfWeek.Sunday) sundayFound = true;
                else nextSunday = nextSunday.AddDays(-1);
            }

            // Other dates we will measure by
            DateTime dateOfChristmas = new DateTime();

            if (dateOfChosenService.Month == 1)
                dateOfChristmas = new DateTime(dateOfChosenService.Year - 1, 12, 25);
            else
                dateOfChristmas = new DateTime(dateOfChosenService.Year, 12, 25);


            DateTime dateOfEaster = Service.dateOfEaster(int.Parse(this.date.year));
            DateTime dateOfAshWednesday = dateOfEaster.AddDays(-46);
            DateTime dateOfAscension = dateOfEaster.AddDays(39);
            DateTime dateOfPentecost = dateOfEaster.AddDays(49);

            // Feast days have the date in the collect line, so read based on that
            getDailyCollect(date.month + " " + date.date);

            // Let's do Advent next - every date in December up to the 25th. They all start with ADVENT 1 plus (if relevant) their additional connected Sunday
            if ((date.month == "December") && (date.date < 24))
            {
                getDailyCollect("ADVENT 1");

                if (previousSunday.AddDays(-7).Month != 12) return;

                // Check for ADVENT 2, 3, or 4
                else if ((previousSunday.AddDays(-7).Day >= 1) && (previousSunday.AddDays(-7).Day <= 7))
                {
                    getDailyCollect("ADVENT 2");
                    return;
                }
                else if ((previousSunday.AddDays(-7).Day >= 8) && (previousSunday.AddDays(-7).Day <= 14))
                {
                    getDailyCollect("ADVENT 3");
                    return;
                }
                else if ((previousSunday.AddDays(-7).Day >= 15) && (previousSunday.AddDays(-7).Day <= 21))
                {
                    getDailyCollect("ADVENT 4");
                    return;
                }
            }
            
            // Christmas 1 and 2
            else if( (date.month == "December" && date.date >= 28) || (date.month == "January" && date.date < 6) )
            {
                DateTime sundayAfterChristmas = nextSunday;

                if(dateOfChosenService < sundayAfterChristmas)
                {
                    getDailyCollect("December 25");
                    return;
                }

                // Christmas 1
                else if( (dateOfChosenService >= sundayAfterChristmas) && (dateOfChosenService < sundayAfterChristmas.AddDays(7)) )
                {
                    getDailyCollect("CHRISTMAS 1");
                    return;
                }
                // Christmas 2
                else if(dateOfChosenService >= sundayAfterChristmas.AddDays(7))
                {
                    getDailyCollect("CHRISTMAS 2");
                    return;
                }
            }
               
            // TODO: Epiphanytide, Lent, Eastertide, Ascensiontide, Ordinary Time

        }

        private void getDailyCollect(string chosenCollect)
        {

            // This function will look for the appropriate Collect title in the collects file, and read in until it hits a line containing 'Amen'.
            // For certain seasons, it will have to read in two collects (eg during Lent, the collect for Ash Wednesday AND the Sunday in Lent) and concatenate them
            string textReadIn = "";
            string currentLine = "";

            var assembly = typeof(App).GetTypeInfo().Assembly;
            Stream stream = assembly.GetManifestResourceStream(@"ACNADailyPrayer.lectionary.collects");
            StreamReader sReader = new StreamReader(stream);

            while (!sReader.EndOfStream)
            {
                // Find the start of our collect
                if (currentLine.Contains(chosenCollect))
                {
                    // For dated collects, check for an exact match
                    if(currentLine.Contains("("))
                    {
                        string [] lineDate = currentLine.Split(new char[] { '(', ')' });
                        lineDate = lineDate[lineDate.Length - 2].Split(' ');
                        string[] inputDate = chosenCollect.Split(' ');

                        if (lineDate[lineDate.Length - 1] != inputDate[1]) return;
;                    }

                    // Read in until we reach the Amen
                    while (!currentLine.Contains("Amen"))
                    {
                        textReadIn += currentLine + "\n";
                        currentLine = sReader.ReadLine();
                    }

                    textReadIn += currentLine;
                    break;
                }
                else currentLine = sReader.ReadLine();
            }

            collectsOfTheDay.Add(textReadIn + "\n");
        }


        private void PrepareServiceText()
        {
            serviceText.Add(date.weekday + " " + date.month + " " + date.date + " " + date.year + "\n\n");

            if (serviceType == Office.MorningPrayer) serviceText.Add("Morning Prayer \n");
            else if (serviceType == Office.EveningPrayer) serviceText.Add("Evening Prayer \n");

            serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.invitatory"));

            if (serviceType == Office.MorningPrayer)
            {
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.venite"));
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.doxology"));
            }
            else if (serviceType == Office.EveningPrayer)
            {
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.phoshilaron"));
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.doxology"));
            }

            getDailyPsalms();
            serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.doxology"));

            if ((date.readings[0] != "") && (serviceType != Office.Compline))
            {
                serviceText.Add(Service.GetReading(date.readings[0]) + "\n");
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.readingresponse"));
            }

            if (serviceType == Office.MorningPrayer)
            {
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.tedeum"));
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.doxology"));
            }
            else if (serviceType == Office.EveningPrayer)
            {
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.magnificat"));
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.doxology"));
            }

            if ((date.readings[1] != "") && (serviceType != Office.Compline))
            {
                serviceText.Add(Service.GetReading(date.readings[1]) + "\n");
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.readingresponse"));
            }

            if (serviceType == Office.MorningPrayer)
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.benedictus"));
            else if (serviceType == Office.EveningPrayer)
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.nuncdimittis"));

            serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.apostlescreed"));

            serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.kyrieourfathersuffrages"));

            // TODO: IMPLEMENT COLLECT READING IN FROM FILES AND DECIPHERING CORRECT ONE FOR DAY BASED ON DATE
            foreach (string collectOfTheDay in collectsOfTheDay) serviceText.Add(collectOfTheDay);

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

            serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.prayerofstchrysostom"));

            serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.thegrace"));

        }
    }
}


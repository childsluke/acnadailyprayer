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

        public static string apiKey = "";
        public static string endPointURL = "https://api.esv.org/v3/passage/text/?";

        private static string getApiKey()
        {
            var assembly_ = typeof(App).GetTypeInfo().Assembly;
            Stream stream_ = assembly_.GetManifestResourceStream(@"ACNADailyPrayer.apikey");
            StreamReader sReader_ = new StreamReader(stream_);

            string _apiKey = sReader_.ReadLine();

            return _apiKey;
        }

        private static string GetReading(string inputReading, bool subPassage = false)
        {
            string readingToParse = inputReading;
            string[] references = {""};
            bool incCopyright = true;

            try
            {
                // Multi-chapter references are separated by semicolon
                if (inputReading.Contains(";"))
                {
                    references = inputReading.Split(';');
                    readingToParse = references[0];
                    incCopyright = false;
                }


                HttpClient client = new HttpClient();
                client.Timeout = new System.TimeSpan(0, 0, 10); // 10-second timeout
                client.BaseAddress = new Uri(ACNADailyPrayer.Service.endPointURL + "include-footnotes=false" + "&" + "include-verse-numbers=false" + "&" + "include-passage-references=false"
                                                                                  + "&" + "include-short-copyright=" + incCopyright.ToString() 
                                                                                  + "&" + "include-headings=false" + "&" + "q=" + readingToParse);
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue

                ("Token", Service.getApiKey());

                string requestJSON = client.GetStringAsync(client.BaseAddress).Result;
                JObject parser = JObject.Parse(requestJSON);
                JToken token = parser.SelectToken("$.passages[0]");

                if (inputReading.Contains(";"))
                    return inputReading + "\n" + token.ToString().Trim() + GetReading(references[1], true);
                else if (subPassage)
                    return "\n" + token.ToString().Trim();
                else
                    return inputReading + "\n" + token.ToString().Trim();
            }
            catch(Exception ex)
            {
                return inputReading + "\n" + ex.Message + " - Error obtaining reading";
            }

        }
        private static string ReadServiceElementFromFile(string filePath)
        {
            string readString = "";

            var assembly = typeof(App).GetTypeInfo().Assembly;
            Stream stream = assembly.GetManifestResourceStream(filePath);

            StreamReader sReader = new StreamReader(stream);
            while (!sReader.EndOfStream) readString += sReader.ReadLine() + "\n";
            sReader.Close();
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
            MorningPrayer, MiddayPrayer, EveningPrayer, NightPrayer
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


            // Set readings for Midday and Night Prayer
            if(serviceType == Office.MiddayPrayer)
            {
                var assembly_ = typeof(App).GetTypeInfo().Assembly;
                Stream stream_ = assembly_.GetManifestResourceStream(@"ACNADailyPrayer.middayprayerreading" + new Random().Next(1,3));
                StreamReader sReader_ = new StreamReader(stream_);
                date.readings[0] = sReader_.ReadLine();
                sReader_.Close();

                return;
            }
            else if(serviceType == Office.NightPrayer)
            {
                var assembly_ = typeof(App).GetTypeInfo().Assembly;
                Stream stream_ = assembly_.GetManifestResourceStream(@"ACNADailyPrayer.nightprayerreading" + new Random().Next(1, 4));
                StreamReader sReader_ = new StreamReader(stream_);
                date.readings[0] = sReader_.ReadLine();
                sReader_.Close();

                return;
            }

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
            // Set Psalms for Midday and Night Prayer
            if(serviceType == Office.MiddayPrayer)
            {
                date.psalmsOfTheDay.Add("119:105-112");
                date.psalmsOfTheDay.Add("121");
                date.psalmsOfTheDay.Add("124");
                date.psalmsOfTheDay.Add("126");
                return;
            }
            else if(serviceType == Office.NightPrayer)
            {
                date.psalmsOfTheDay.Add("4");
                date.psalmsOfTheDay.Add("31:1-6");
                date.psalmsOfTheDay.Add("91");
                date.psalmsOfTheDay.Add("134");
                return;
            }

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

                            // Special case for Night Prayer (bit annoying)
                            if ((psalmNumber == "Psalm 31:1-6") && (currentLine.Contains("for you have redeemed me, O Lord, O God of truth")) ) break;
                        }

                        break;
                    }
                }

                serviceText.Add(psalmToReturn + "\n");
            }
        }

        private void chooseDailyCollect()
        {
            // This function will ascertain which collect to look for, based on...
            // 1) Static Feast Days
            // 
            // 2) Variable Feast/Fast Days - Holy Week, Easter, Easter Week, Ascension Day, Pentecost, or Ash Wednesday 

            // For other dates, it will roll back to the previous Sunday, and then calculate the collect from that Sunday based on:
            // 1) During Advent
            // 2) During Epiphany
            // 3) During Lent
            // 4) During Holy Week & Easter
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


            DateTime dateOfEpiphany = new DateTime(dateOfChosenService.Year, 1, 6);
            DateTime dateOfEaster = Service.dateOfEaster(dateOfChosenService.Year);
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
            else if ((date.month == "December" && date.date >= 28) || (date.month == "January" && date.date < 6))
            {
                DateTime sundayAfterChristmas = nextSunday;

                if (dateOfChosenService < sundayAfterChristmas)
                {
                    getDailyCollect("December 25");
                    return;
                }

                // Christmas 1
                else if ((dateOfChosenService >= sundayAfterChristmas) && (dateOfChosenService < sundayAfterChristmas.AddDays(7)))
                {
                    getDailyCollect("CHRISTMAS 1");
                    return;
                }
                // Christmas 2
                else if (dateOfChosenService >= sundayAfterChristmas.AddDays(7))
                {
                    getDailyCollect("CHRISTMAS 2");
                    return;
                }
            }

            // Epiphanytide
            if ((dateOfChosenService >= dateOfEpiphany) && (dateOfChosenService < dateOfAshWednesday))
            {
                // Between Epiphany itself and the first Sunday after Epiphany, we just push the Epiphany collect
                if ((dateOfChosenService > dateOfEpiphany) && (dateOfChosenService.Day < 13) && (dateOfChosenService.DayOfWeek != DayOfWeek.Sunday) && (dateOfChosenService.Month == 1) )
                {
                    getDailyCollect("January 6");
                    return;
                }
                // Sunday before Lent and second-to-last Sunday before Lent
                else if (previousSunday.AddDays(7) > dateOfAshWednesday)
                {
                    getDailyCollect("EPIPHANY 10");
                    return;
                }
                else if (previousSunday.AddDays(14) > dateOfAshWednesday)
                {
                    getDailyCollect("EPIPHANY 9");
                    return;
                }

                // Epiphany 1 through 8
                int sundaysSinceEpiphany = 1;
                while (true)
                {
                    if (previousSunday.AddDays(sundaysSinceEpiphany * -7) < dateOfEpiphany)
                    {
                        getDailyCollect("EPIPHANY " + sundaysSinceEpiphany.ToString());
                        return;
                    }
                    else sundaysSinceEpiphany++;
                    if (sundaysSinceEpiphany > 8) break;
                }

            }

            // Ash Wednesday & Lent
            if ((dateOfChosenService >= dateOfAshWednesday) && (dateOfChosenService < dateOfEaster.AddDays(-7)))
            {
                // Every day in Lent has the Ash Wednesday Collect
                getDailyCollect("ASH WEDNESDAY");
                if (dateOfChosenService < dateOfAshWednesday.AddDays(4)) return;

                // Lenten Sundays (1 through 5)
                else
                {
                    int sundaysAfterLent = 1;
                    while (true)
                    {
                        if (previousSunday.AddDays(-7 * sundaysAfterLent) < dateOfAshWednesday)
                        {
                            getDailyCollect("LENT " + sundaysAfterLent.ToString());
                            return;
                        }
                        else sundaysAfterLent++;
                        if (sundaysAfterLent > 5) break;
                    }
                }

                return;
            }


            // Holy Week
            else if(dateOfChosenService < dateOfEaster)
            {
                if(dateOfChosenService == dateOfEaster.AddDays(-7))
                {
                    getDailyCollect("PALM SUNDAY");
                    return;
                }
                else if(dateOfChosenService == dateOfEaster.AddDays(-6))
                {
                    getDailyCollect("HOLY MONDAY");
                    return;
                }
                else if (dateOfChosenService == dateOfEaster.AddDays(-5))
                {
                    getDailyCollect("HOLY TUESDAY");
                    return;
                }
                else if (dateOfChosenService == dateOfEaster.AddDays(-4))
                {
                    getDailyCollect("HOLY WEDNESDAY");
                    return;
                }
                else if (dateOfChosenService == dateOfEaster.AddDays(-3))
                {
                    getDailyCollect("MAUNDY THURSDAY");
                    return;
                }
                else if (dateOfChosenService == dateOfEaster.AddDays(-2))
                {
                    getDailyCollect("GOOD FRIDAY");
                    return;
                }
                else if ( (dateOfChosenService == dateOfEaster.AddDays(-1)) && (serviceType == Office.MorningPrayer) )
                {
                    getDailyCollect("HOLY SATURDAY");
                    return;
                }
                else if ((dateOfChosenService == dateOfEaster.AddDays(-1)) && (serviceType == Office.EveningPrayer))
                {
                    getDailyCollect("EASTER EVE");
                    return;
                }

            }

            // Easter and Easter week
            if (dateOfChosenService == dateOfEaster)
            {
                getDailyCollect("EASTER DAY");
                return;
            }
            else if (dateOfChosenService == dateOfEaster.AddDays(1))
            {
                getDailyCollect("EASTER MONDAY");
                return;
            }
            else if (dateOfChosenService == dateOfEaster.AddDays(2))
            {
                getDailyCollect("EASTER TUESDAY");
                return;
            }
            else if (dateOfChosenService == dateOfEaster.AddDays(3))
            {
                getDailyCollect("EASTER WEDNESDAY");
                return;
            }
            else if (dateOfChosenService == dateOfEaster.AddDays(4))
            {
                getDailyCollect("EASTER THURSDAY");
                return;
            }
            else if (dateOfChosenService == dateOfEaster.AddDays(5))
            {
                getDailyCollect("EASTER FRIDAY");
                return;
            }
            else if (dateOfChosenService == dateOfEaster.AddDays(6))
            {
                getDailyCollect("EASTER SATURDAY");
                return;
            }

            // Sundays of Eastertide (1 thru 5)

            if ((dateOfChosenService >= dateOfEaster.AddDays(7)) && (dateOfChosenService < dateOfAscension))
            {
                int sundaysAfterEaster = 1;
                while (true)
                {
                    if (previousSunday.AddDays(-7 * sundaysAfterEaster) < dateOfEaster)
                    {
                        getDailyCollect("EASTER " + sundaysAfterEaster.ToString());
                        return;
                    }
                    else sundaysAfterEaster++;
                    if (sundaysAfterEaster > 5) break;
                }
            }

            // Ascension, Sunday after Ascension, Pentecost, Trinity Sunday
            else if( (dateOfChosenService == dateOfAscension) || (dateOfChosenService == dateOfAscension.AddDays(1)) || (dateOfChosenService == dateOfAscension.AddDays(2)) )
            {
                getDailyCollect("ASCENSION DAY");
                return;
            }
            else if( (dateOfChosenService == dateOfAscension.AddDays(3)) || (previousSunday.AddDays(-7) < dateOfAscension) )
            {
                getDailyCollect("ASCENSION 1");
                    return;
            }
            else if (previousSunday.AddDays(-7) < dateOfPentecost)
            {
                getDailyCollect("PENTECOST");
                return;
            }
            else if(previousSunday.AddDays(-14) < dateOfPentecost)
            {
                getDailyCollect("TRINITY");
                return;
            }


            // Ordinary Time & Christ the King
            else
            {
                if(dateOfChosenService.AddDays(7).Month == 12)
                {
                    getDailyCollect("Christ the King");
                    return;
                }
                getDailyCollect("Week of " + previousSunday.ToString("MMMM") + " " + previousSunday.Day);
                return;
            }

            return;   // Should NOT ever reach here!
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

            string stringToTest = chosenCollect;
            if (chosenCollect.Contains("Week of")) stringToTest = "Week of";

            while (!sReader.EndOfStream)
            {
                // Find the start of our collect
                if (currentLine.Contains(stringToTest))
                {

                    if ((currentLine.Contains("Week of")) && (!chosenCollect.Contains("Week of"))) return;
                    else if ((currentLine.Contains("Week of")) && (chosenCollect.Contains("Week of")))
                    {
                        // Read in 'Week of' Ordinary time collects:

                        // Strip out the before and after dates to compare our input date
                        string[] splitCurrentLine = currentLine.Trim().Split(' ');
                        string[] splitChosenCollect = chosenCollect.Split(' ');

                        string currentLineMonth1 = splitCurrentLine[splitCurrentLine.Length - 5];
                        string currentLineMonth2 = splitCurrentLine[splitCurrentLine.Length - 2];
                        int currentLineDate1 = int.Parse(splitCurrentLine[splitCurrentLine.Length - 4]);
                        int currentLineDate2 = int.Parse(splitCurrentLine[splitCurrentLine.Length - 1]);
                        string chosenCollectMonth = splitChosenCollect[splitChosenCollect.Length - 2];
                        int chosenCollectDate = int.Parse(splitChosenCollect[splitChosenCollect.Length - 1]);

                        // TOFINISH: If our inputted date is between the dates we've hit, read in, otherwise Continue to the next while cycle to look for another collect
                        if( (chosenCollectMonth != currentLineMonth1) && (chosenCollectMonth != currentLineMonth2) )
                        {
                            currentLine = sReader.ReadLine();
                            continue;
                        }

                        // If all months match, just see if our read-in date is within the range of this line
                        if ((chosenCollectMonth == currentLineMonth1) && (chosenCollectMonth == currentLineMonth2))
                        {
                            if( !((chosenCollectDate >= currentLineDate1) && (chosenCollectDate <= currentLineDate2 )) )
                            {
                                currentLine = sReader.ReadLine();
                                continue;
                            }
                        }

                        // If months don't match, we have to do some arithmetic...
                        else
                        {
                            DateTime currentLineDateTime1 = DateTime.Parse(currentLineMonth1 + " " + currentLineDate1.ToString() + " " + DateTime.Today.Year.ToString() );
                            DateTime currentLineDateTime2 = DateTime.Parse(currentLineMonth2 + " " + currentLineDate2.ToString() + " " + DateTime.Today.Year.ToString());
                            DateTime chosenCollectDateTime = DateTime.Parse(chosenCollectMonth + " " + chosenCollectDate.ToString() + " " + DateTime.Today.Year.ToString());
                            
                            // If our collect date is NOT between the two line dates, continue to the next cycle
                            if (! ( (chosenCollectDateTime >= currentLineDateTime1) && (chosenCollectDateTime <= currentLineDateTime2) ) )
                            {
                                currentLine = sReader.ReadLine();
                                continue;
                            }
                        }



                    }

                    // For dated collects, check for an exact match
                    if (currentLine.Contains("("))
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
            else if (serviceType == Office.MiddayPrayer) serviceText.Add("Midday Prayer \n");
            else if (serviceType == Office.NightPrayer) serviceText.Add("Night Prayer \n");

            if ((serviceType == Office.MorningPrayer) || (serviceType == Office.EveningPrayer))
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.invitatory"));
            else if (serviceType == Office.MiddayPrayer)
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.middaynightinvitatory"));
            else if (serviceType == Office.NightPrayer)
            {
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.nightopeningversicle"));
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.nightconfession"));
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.nightinvitatory"));
            }


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

            if ( (date.readings[0] != "") && ((serviceType == Office.MorningPrayer) || (serviceType == Office.EveningPrayer)) )
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
            }

            if ((date.readings[1] != "") && ((serviceType == Office.MorningPrayer) || (serviceType == Office.EveningPrayer)))
            {
                serviceText.Add(Service.GetReading(date.readings[1]) + "\n");
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.readingresponse"));
            }

            if(serviceType == Office.MiddayPrayer)
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.middayresponse"));
            else if(serviceType == Office.NightPrayer)
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.nightresponse"));

            if (serviceType == Office.MorningPrayer)
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.benedictus"));
            else if (serviceType == Office.EveningPrayer)
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.nuncdimittis"));

            if( (serviceType == Office.MorningPrayer) || (serviceType == Office.EveningPrayer) )
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.apostlescreed"));

            serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.kyrieourfathersuffrages"));

            if ((serviceType == Office.MorningPrayer) || (serviceType == Office.EveningPrayer))
                foreach (string collectOfTheDay in collectsOfTheDay) serviceText.Add(collectOfTheDay);


            if(serviceType == Office.MiddayPrayer)
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.middaycollect" + new Random().Next(1,4)));
            if(serviceType == Office.NightPrayer)
            {
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.nightcollect" + new Random().Next(1, 4)));

                if(date.weekday == "Saturday")
                {
                    serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.nightsaturdaycollect" + new Random().Next(1, 4)));
                }
                else serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.nightsecondcollect" + new Random().Next(1, 2)));

                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.guideuswhilewaking"));
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.nuncdimittis"));
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.guideuswhilewaking"));

            }



            if (serviceType == Office.MorningPrayer)
            {
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts." + date.weekday.ToLower() + "morning"));
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.morningprayerformission"+ new Random().Next(1,3)));

            }
            else if (serviceType == Office.EveningPrayer)
            {
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts." + date.weekday.ToLower() + "evening"));
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.eveningprayerformission" + new Random().Next(1, 3)));
            }

            if ((serviceType == Office.MorningPrayer) || (serviceType == Office.EveningPrayer))
            {
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.generalthanksgiving"));
                serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.prayerofstchrysostom"));
            }

            serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.thegrace"));

            serviceText.Add(ReadServiceElementFromFile(@"ACNADailyPrayer.servicetexts.legaltext") + "\n\n");


        }
    }
}


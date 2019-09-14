using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ACNADailyPrayer
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        public DateTime todaysDate = DateTime.Today;
        public DateTime dateChoice = DateTime.Today;

        public MainPage()
        {
            InitializeComponent();
        }

        string getDate()
        {
            string weekday = dateChoice.DayOfWeek.ToString();
            string month = dateChoice.ToString("MMMM");
            string day = dateChoice.Day.ToString();
            string year = dateChoice.Year.ToString();

            return weekday + " " + month + " " + day + " " + year;
        }


        void MorningPrayer_Clicked(object sender, System.EventArgs e)
        {
            Navigation.PushAsync(new Page1( new Service(Service.Office.MorningPrayer, getDate() ) ));
        }
        void EveningPrayer_Clicked(object sender, System.EventArgs e)
        {
            Navigation.PushAsync(new Page1(new Service(Service.Office.EveningPrayer, getDate() ) ));
        }
        void DateSelected(object sender, DateChangedEventArgs e)
        {
            dateChoice = e.NewDate;
        }

    }
}

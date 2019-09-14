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
        public MainPage()
        {
            InitializeComponent();
        }


        void MorningPrayer_Clicked(object sender, System.EventArgs e)
        {
            // TODO: A working alternative to this...
            Navigation.PushAsync(new Page1( new Service(Service.Office.MorningPrayer, "Thursday September 12 2019") ) );
        }
        void EveningPrayer_Clicked(object sender, System.EventArgs e)
        {
            Navigation.PushAsync(new Page1(new Service(Service.Office.EveningPrayer, "Thursday September 12 2019")));
        }

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ACNADailyPrayer
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]


    [ContentProperty(nameof(Source))]
    public class ImageResourceExtension : IMarkupExtension
    {
        public string Source { get; set; }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Source == null)
            {
                return null;
            }

            // Do your translation lookup here, using whatever method you require
            var imageSource = ImageSource.FromResource(Source, typeof(ImageResourceExtension).GetTypeInfo().Assembly);

            return imageSource;
        }
    }

    public partial class MainPage : ContentPage
    {
        public DateTime todaysDate = DateTime.Today;
        public DateTime dateChoice = DateTime.Today;
        public string headerImage = "";

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


        async void MorningPrayer_Clicked(object sender, System.EventArgs e)
        {
            loadingIndicator.IsRunning = true;
            this.IsEnabled = false;

            try
            {
                Page1 servicePage = null;
                await Task.Run(() =>
                {
                    servicePage = new Page1(new Service(Service.Office.MorningPrayer, getDate()));
                });
                await Navigation.PushAsync(servicePage);

            }
            catch(Exception ex)
            {
               await Navigation.PopAsync();
               await DisplayAlert("ERROR", ex.Message + "\n\n" + "Unable to load service - please check your Internet Connection", "OK");
            }
            finally
            {
                this.IsEnabled = true;
                loadingIndicator.IsRunning = false;
            }
        }

        async void EveningPrayer_Clicked(object sender, System.EventArgs e)
        {
            loadingIndicator.IsRunning = true;
            this.IsEnabled = false;
            
            try
            {
                Page1 servicePage = null;
                await Task.Run(() =>
               {
                   servicePage = new Page1(new Service(Service.Office.EveningPrayer, getDate()));
               });
                await Navigation.PushAsync(servicePage);
            }
            catch (Exception ex)
            {
                await Navigation.PopAsync();

                await DisplayAlert("ERROR", ex.Message + "\n\n" + "Unable to load service - please check your Internet Connection", "OK");
            }
            finally
            {
                this.IsEnabled = true;
                loadingIndicator.IsRunning = false;
            }
        }
        void DateSelected(object sender, DateChangedEventArgs e)
        {
            dateChoice = e.NewDate;
        }

    }
}

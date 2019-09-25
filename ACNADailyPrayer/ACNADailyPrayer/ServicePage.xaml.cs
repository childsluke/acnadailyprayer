using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ACNADailyPrayer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Page1 : ContentPage
    {

        // Constructor pulls in service texts and formats appropriately to present to UI (splits title and body for example)
        public Page1(Service servicePushed)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            var swipeBack = new SwipeGestureRecognizer { Direction = SwipeDirection.Left };
            swipeBack.Swiped += OnSwiped;
            scrollView.GestureRecognizers.Add(swipeBack);


            //serviceTextLabel.FormattedText = new FormattedString();
            stackLayout.Padding = new Thickness(this.Width * 0.1, this.Height * 0.05, this.Width * 0.075, this.Height * 0.05);
            int i = 0;


            // Platform-specific font loading
            string regularFont = "";
            string boldFont = "";

            switch(Device.RuntimePlatform)
            {
                case Device.iOS:
                    regularFont = "Cormorant Garamond - Regular";
                    boldFont = "Cormorant Garamond";
                    break;

                case Device.Android:
                    regularFont = "CormorantGaramond-Regular.ttf#Cormorant Garamond - Regular";
                    boldFont = "CormorantGaramond-Bold.ttf#Cormorant Garamond";
                    break;
            }
            

            foreach(string s in servicePushed.serviceText)
            {
                Label entryLabel = new Label();

                FormattedString serviceFormattedText = new FormattedString();
                
                // Extract the title and format different to body 
                serviceFormattedText.Spans.Add(new Span { Text = s.Split('\n')[0] + "\n",/*FontAttributes = FontAttributes.Bold,*/ TextColor = Color.Black, FontFamily = boldFont, FontSize = Device.GetNamedSize(NamedSize.Title, typeof(Label)) }) ;

                // Extact the body as regular text
                serviceFormattedText.Spans.Add(new Span { Text = (s.Remove(0, serviceFormattedText.Spans[0].Text.Length - 1) + "\n"), TextColor = Color.Black, FontFamily=regularFont, FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)) });

                //serviceTextLabel.FormattedText.Spans.Add(serviceFormattedText.Spans[0]);
                //serviceTextLabel.FormattedText.Spans.Add(serviceFormattedText.Spans[1]);

                entryLabel.FormattedText = new FormattedString();
                entryLabel.FormattedText.Spans.Add(serviceFormattedText.Spans[0]);
                entryLabel.FormattedText.Spans.Add(serviceFormattedText.Spans[1]);

                
                stackLayout.Children.Add(entryLabel);

                i++;
            }
   
        }

        private void OnSwiped(object sender, SwipedEventArgs e)
        {
            throw new NotImplementedException();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height); //must be called
            stackLayout.Padding = new Thickness(this.Width * 0.1, this.Height * 0.05, this.Width * 0.075, this.Height * 0.05);
        }

    }
}
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
            //serviceTextLabel.FormattedText = new FormattedString();
            stackLayout.Padding = new Thickness(this.Width * 0.05, 0, this.Width * 0.05, 0);
            int i = 0;

            foreach(string s in servicePushed.serviceText)
            {
                Label entryLabel = new Label();

                FormattedString serviceFormattedText = new FormattedString();
                
                // Extract the title and format different to body 
                serviceFormattedText.Spans.Add(new Span { Text = s.Split('\n')[0] + "\n", FontAttributes = FontAttributes.Bold, TextColor = Color.Black, FontSize = Device.GetNamedSize(NamedSize.Title, typeof(Label)) }) ;

                // Extact the body as regular text
                serviceFormattedText.Spans.Add(new Span { Text = (s.Remove(0, serviceFormattedText.Spans[0].Text.Length - 1) + "\n"), TextColor = Color.DarkGray, FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)) });

                //serviceTextLabel.FormattedText.Spans.Add(serviceFormattedText.Spans[0]);
                //serviceTextLabel.FormattedText.Spans.Add(serviceFormattedText.Spans[1]);

                entryLabel.FormattedText = new FormattedString();
                entryLabel.FormattedText.Spans.Add(serviceFormattedText.Spans[0]);
                entryLabel.FormattedText.Spans.Add(serviceFormattedText.Spans[1]);

                
                stackLayout.Children.Add(entryLabel);

                i++;
            }
   
        }
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height); //must be called
            stackLayout.Padding = new Thickness(this.Width * 0.05, 0, this.Width * 0.05, 0);
        }

    }
}
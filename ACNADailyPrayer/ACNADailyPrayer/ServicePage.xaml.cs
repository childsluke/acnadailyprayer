﻿using System;
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

            serviceTextLabel.FormattedText = new FormattedString();
            int i = 0;

            foreach(string s in servicePushed.serviceText)
            {
                FormattedString serviceFormattedText = new FormattedString();
                
                // Extract the title and format different to body 
                serviceFormattedText.Spans.Add(new Span { Text = s.Split('\n')[0] + "\n", FontAttributes = FontAttributes.Bold, TextColor = Xamarin.Forms.Color.Red }) ;

                // Extact the body as regular text
                serviceFormattedText.Spans.Add(new Span { Text = (s.Remove(0, serviceFormattedText.Spans[0].Text.Length - 1) + "\n"), TextColor = Xamarin.Forms.Color.Blue });
                
                serviceTextLabel.FormattedText.Spans.Add(serviceFormattedText.Spans[0]);
                serviceTextLabel.FormattedText.Spans.Add(serviceFormattedText.Spans[1]);
                
                //serviceTextLabel.FormattedText ++ s;
                //serviceTextLabel.FormattedText += "\n";

                i++;
            }
        }
    }
}
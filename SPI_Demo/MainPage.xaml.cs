using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SPI_AD9834;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SPI_Demo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        SPI_AD9834.AD9834 AD9834 = new SPI_AD9834.AD9834();
        SPI_AD9834.DAC DAC = new SPI_AD9834.DAC();

        public MainPage()
        {
            this.InitializeComponent();
            AD9834.InitSPI_AD9834();
            DAC.DACinizialize();       
        }

        private void FrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFrequency();
        }

        private void AmplitudeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateAmplitude();
        }

        private void OffsetTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateOffset();
        }

        private void SetSinWave_Checked(object sender, RoutedEventArgs e)
        {
            AD9834.setOutputMode(AD9834.OutputMode.OUTPUT_MODE_SINE);
            Debug.WriteLine("Set sine wave");
        }

        private void SetTriWave_Checked(object sender, RoutedEventArgs e)
        {
            AD9834.setOutputMode(AD9834.OutputMode.OUTPUT_MODE_TRIANGLE);
            Debug.WriteLine("set triangular wave");
        }

        private void SetSqrWave_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void SetFrequencyRange_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            UpdateFrequency();
        }

        private void SetAmplitudeRange_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            UpdateAmplitude();
        }

        private void SetOffsetRange_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            UpdateOffset();
        }

        private void UpdateFrequency()
        {
            UInt32 Frequency = 0;
            switch (SetFrequencyRange.SelectedIndex)
            {
                case 0:
                    Frequency = (UInt32)FrequencyTextBox.DataContext;
                    break;

                case 1:
                    Frequency = ((UInt32)FrequencyTextBox.DataContext * 1000);
                    break;

                case 2:
                    Frequency = ((UInt32)FrequencyTextBox.DataContext * 1000000);
                    break;

                default:
                    Debug.Write("error");
                    break;
            }
            AD9834.SetFrequencyWord(0, Frequency);
            Debug.WriteLine(Frequency);
        }

        private void UpdateAmplitude()
        {
            double amplitude = 0;
            switch (SetAmplitudeRange.SelectedIndex)
            {
                case 0:
                    amplitude = ((double)AmplitudeTextBox.DataContext / 100);
                    break;

                case 1:
                    amplitude = ((double)AmplitudeTextBox.DataContext);
                    break;

                default:
                    Debug.Write("error set amplitude");
                    break;
            }
            DAC.DACwriteAmplitude(amplitude);
            Debug.WriteLine(amplitude);
        }

        private void UpdateOffset()
        {
            double offset = 0;

            switch (SetOffsetRange.SelectedIndex)
            {
                case 0:
                    offset = ((double)OffsetTextBox.DataContext / 1000);
                    break;

                case 1:
                    offset = ((double)OffsetTextBox.DataContext);
                    break;

                default:
                    Debug.Write("Error set offset");
                    break;
            }
            DAC.DACwriteOffset(offset);
            Debug.WriteLine(offset);
        }

    }
}

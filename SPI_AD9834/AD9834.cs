using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using Windows.Devices.Gpio;

namespace SPI_AD9834
{

    public class AD9834
    {
        private const ushort REG_FREQ1 = 0x8000;
        private const ushort REG_FREQ0 = 0x4000;
        private const ushort REG_PHASE0 = 0xC000;
        private const ushort REG_PHASE1 = 0xE000;
        private const ushort REG_B28 = 0x2000;
        private const ushort REG_HLB = 0x1000;
        private const ushort REG_FSEL = 0x0800;
        private const ushort REG_PSEL = 0x0400;
        private const ushort REG_PINSW = 0x0200;
        private const ushort REG_RESET = 0x0100;
        private const ushort REG_SLEEP1 = 0x0080;
        private const ushort REG_SLEEP12 = 0x0040;
        private const ushort REG_OPBITEN = 0x0020;
        private const ushort REG_SIGNPIB = 0x0010;
        private const ushort REG_DIV2 = 0x0008;
        private const ushort REG_MODE = 0x0002;
        private const ushort REG_MODE_NEG = 0xFFFD;
        private const ushort SPI_CHIP_SELECT_LINE = 0x00;
        private const int CS0 = 26;
        private const int CS1 = 22;
        private const int CS2 = 27;
        private const int FSELECT = 5;
        private const int PSELECT = 6;
        private const int RESET = 13;
        private const int SLEEP = 19;
        private UInt16 m_reg = 0;

        private const ushort SIGN_OUTPUT_MASK = (REG_OPBITEN | REG_SIGNPIB | REG_DIV2 | REG_MODE);

        public enum SignOutput
        {
            SIGN_OUTPUT_NONE = 0x0000,
            SIGN_OUTPUT_MSB = 0x0028,
            SIGN_OUTPUT_MSB_2 = 0x0020,
            SIGN_OUTPUT_COMPARATOR = 0x0038,
        };

        public enum OutputMode
        {
            OUTPUT_MODE_SINE = 0x0000,
            OUTPUT_MODE_TRIANGLE = 0x0002,
        };

        public enum EnableChip
        {
            AD9834,
            Offset,
            Amplitude,
            OffAll,
        };

        private SpiDevice SPIAD9834;
        private GpioPin pin_CS0;
        private GpioPin pin_CS1;
        private GpioPin pin_CS2;
        private GpioPin pin_FSELECT;
        private GpioPin pin_PSELECT;
        private GpioPin pin_RESET;
        //private GpioPin pin_SLEEP;

        private GpioController gpio;

        private async Task InitSPI()
        {
            try
            {
                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE);
                settings.ClockFrequency = 1000000;                              /* 5MHz is the rated speed of the ADXL345 accelerometer                     */
                settings.Mode = SpiMode.Mode2;                                  /* The accelerometer expects an idle-high clock polarity, we use Mode3    
                                                                                 * to set the clock polarity and phase to: CPOL = 1, CPHA = 1         
                                                                                 */

                string aqs = SpiDevice.GetDeviceSelector("SPI0");                     /* Get a selector string that will return all SPI controllers on the system */
                var dis = await DeviceInformation.FindAllAsync(aqs);            /* Find the SPI bus controller devices with our selector string             */
                SPIAD9834 = await SpiDevice.FromIdAsync(dis[0].Id, settings);    /* Create an SpiDevice with our bus controller and SPI settings             */
                if (SPIAD9834 == null)
                {
                    Debug.WriteLine("SPI {0} inizialized not completed. SPI is busy", dis[0].Id);
                    return;
                }

                Debug.WriteLine("SPI inizialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SPI inizialization fail:" + ex.Message);
                return;
            }
        }

        private void InitGpio()
        {
            gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                throw new Exception("GPIO do not exist");               
            }

            pin_CS0 = gpio.OpenPin(CS0);
            pin_CS0.Write(GpioPinValue.High);
            pin_CS0.SetDriveMode(GpioPinDriveMode.Output);
            pin_CS1 = gpio.OpenPin(CS1);
            pin_CS1.Write(GpioPinValue.High);
            pin_CS1.SetDriveMode(GpioPinDriveMode.Output);
            pin_CS2 = gpio.OpenPin(CS2);
            pin_CS2.Write(GpioPinValue.High);
            pin_CS2.SetDriveMode(GpioPinDriveMode.Output);
            pin_FSELECT = gpio.OpenPin(FSELECT);
            pin_FSELECT.Write(GpioPinValue.Low);
            pin_FSELECT.SetDriveMode(GpioPinDriveMode.Output);
            pin_PSELECT = gpio.OpenPin(PSELECT);
            pin_PSELECT.Write(GpioPinValue.Low);
            pin_PSELECT.SetDriveMode(GpioPinDriveMode.Output);
            pin_RESET = gpio.OpenPin(RESET);
            pin_RESET.Write(GpioPinValue.Low);
            pin_RESET.SetDriveMode(GpioPinDriveMode.Output);
            /*pin_SLEEP = gpio.OpenPin(SLEEP);
            pin_SLEEP.Write(GpioPinValue.Low);
            pin_SLEEP.SetDriveMode(GpioPinDriveMode.Output);*/


            // Show an error if the pin wasn't initialized properly
            if (pin_CS0 == null && pin_CS1 == null && pin_CS2 == null)
            {
                Debug.WriteLine("Pin not open");
                return;
            }

            Debug.WriteLine("Gpio initialize");
        }

        private void WriteReg(ushort reg)
        {
            byte[] regValue = BitConverter.GetBytes(reg);
            EnableCs(EnableChip.AD9834);
            Task.Delay(1).Wait();
            SPIAD9834.Write(regValue);
            Task.Delay(1).Wait();
            EnableCs(EnableChip.OffAll);
            Debug.WriteLine("Write SPI complete");
        }

        public async void InitSPI_AD9834()
        {
            try
            {
                await InitSPI();
                InitGpio();
                Debug.WriteLine("Inizialized AD9834 correctly");               
                WriteReg(m_reg);
                SetPhaseWord(0, 0);
                SetPhaseWord(1, 0);
                SetFrequencyWord(0, 0);
                SetFrequencyWord(1, 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fail inizialize AD9834: "+ ex.Message);
            }
        }

        public void SetFrequencyWord(byte reg, UInt32 frequency)
        {
            WriteReg((ushort)((reg == 1 ? REG_FREQ1 : REG_FREQ0) | (frequency & 0x3FFF)));
            WriteReg((ushort)((reg == 1 ? REG_FREQ1 : REG_FREQ0 | (frequency >> 14) & 0x3FFF)));
        }

        public void SetPhaseWord(byte reg, UInt32 phase)
        {
            WriteReg((ushort)((reg == 1 ? REG_PHASE1 : REG_PHASE0) | (phase & 0x0FFF)));
        }

        public void setSignOutput(SignOutput sign)
        {
            m_reg = (ushort)((m_reg & (SIGN_OUTPUT_MASK ^ 0xFFFF)) | (ushort)sign);
            WriteReg(m_reg);
        }

        public void setOutputMode(OutputMode mode)
        {
            if (mode ==  OutputMode.OUTPUT_MODE_TRIANGLE) {
                m_reg =(ushort)((m_reg & ~SIGN_OUTPUT_MASK) | (ushort)mode);
            } else {
                m_reg &= (REG_MODE ^ 0xFFFF);
            }
            WriteReg(m_reg);            
        }

        public void resetAD9834(Boolean reset)
        {
            if (reset)
            {
                m_reg |= REG_RESET;
            }
            else
            {
                m_reg &= (REG_RESET ^ 0xFFFF);
            }

            WriteReg(m_reg);
        }

        public void AD9834_begin()
        {
            resetAD9834(true);
            InitSPI_AD9834();
            resetAD9834(false);
        }

        public void EnableCs(EnableChip cs)
        {
            switch(cs)
            {
                case EnableChip.AD9834:
                    pin_CS0.Write(GpioPinValue.Low);
                    pin_CS1.Write(GpioPinValue.High);
                    pin_CS2.Write(GpioPinValue.High);
                    Debug.WriteLine("AD9834 Enable");
                    break;

                case EnableChip.Offset:
                    pin_CS0.Write(GpioPinValue.High);
                    pin_CS1.Write(GpioPinValue.Low);
                    pin_CS2.Write(GpioPinValue.High);
                    Debug.WriteLine("Offset Enable");
                    break;

                case EnableChip.Amplitude:
                    pin_CS0.Write(GpioPinValue.High);
                    pin_CS1.Write(GpioPinValue.High);
                    pin_CS2.Write(GpioPinValue.Low);
                    Debug.WriteLine("Amplitude Enable");
                    break;

                case EnableChip.OffAll:
                    pin_CS0.Write(GpioPinValue.High);
                    pin_CS1.Write(GpioPinValue.High);
                    pin_CS2.Write(GpioPinValue.High);
                    Debug.WriteLine("All CS off");
                    break;

                default:
                    Debug.Write("Error input function");
                    return;
            }
        }

        public void WriteSPI(ushort data)
        {
            byte[] word = BitConverter.GetBytes(data);
            SPIAD9834.Write(word);
        }

    }

    public class DAC
    {
        AD9834 SPI = new AD9834();

        public void DACwriteOffset(double offset)
        {
            ushort resolution = 65535;  //Resolution bit of AD5660 2^16
            ushort word = (ushort)((resolution / 10) * (offset + 5));   //formula to calculate a digital value for AD5660. Output +/-5V           
            SPI.WriteSPI(word); //write over SPI the digital data
        }

        public void DACwriteAmplitude(double amplitude)
        {
            ushort resolution = 15728;  //correspond (in digital value) to 1.20V, max analog value for AD5660 (vref AD9834)
            ushort word = (ushort)(((1.20 - (amplitude / 10)) * resolution) / 1.20);  //formula to calculate the digital value for amplitude
            SPI.WriteSPI(word); //write over SPI the data
        }

        public void DACinizialize()
        {
            DACwriteOffset(0);
            DACwriteAmplitude(1);
            Debug.Write("DAC inizialize");
        }


    }

}

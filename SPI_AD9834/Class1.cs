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
    public class Class1
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
        private const ushort SPI_CHIP_SELECT_LINE = 0x00;
        private const int CS0 = 15;
        private const int CS1 = 13;
        private const int CS2 = 13;
        private const int CS3 = 13;
        private const int FSELECT = 29;
        private const int PSELECT = 31;
        private const int RESET = 33;
        private const int SLEEP = 35;

        private const ushort SIGN_OUTPUT_MASK = (REG_OPBITEN | REG_SIGNPIB | REG_DIV2 | REG_MODE);

        private SpiDevice SPIAD9834;
        private GpioPin pin_CS0;
        private GpioPin pin_CS1;
        private GpioPin pin_CS2;
        private GpioPin pin_CS3;
        private GpioPin pin_FSELECT;
        private GpioPin pin_PSELECT;
        private GpioPin pin_RESET;
        private GpioPin pin_SLEEP;

        private async void InitSPI()
        {
            try
            {
                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE);
                settings.ClockFrequency = 1000000;                              /* 5MHz is the rated speed of the ADXL345 accelerometer                     */
                settings.Mode = SpiMode.Mode2;                                  /* The accelerometer expects an idle-high clock polarity, we use Mode3    
                                                                                 * to set the clock polarity and phase to: CPOL = 1, CPHA = 1         
                                                                                 */

                string aqs = SpiDevice.GetDeviceSelector();                     /* Get a selector string that will return all SPI controllers on the system */
                var dis = await DeviceInformation.FindAllAsync(aqs);            /* Find the SPI bus controller devices with our selector string             */
                SPIAD9834 = await SpiDevice.FromIdAsync(dis[0].Id, settings);    /* Create an SpiDevice with our bus controller and SPI settings             */
                if (SPIAD9834 == null)
                {
                    Debug.WriteLine("SPI {0} inizialized not completed. SPI is busy", dis[0].Id);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SPI inizialization fail:" + ex.Message);
                return;
            }
        }

        private void InitGpio()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                pin_CS0 = null;
                pin_CS1 = null;
                pin_CS2 = null;
                pin_CS3 = null;
                pin_FSELECT = null;
                pin_PSELECT = null;
                pin_RESET = null;
                pin_SLEEP = null;
                Debug.WriteLine("GPIO inizialization fail.");
                return;
            }

            pin_CS0 = gpio.OpenPin(CS0);
            pin_CS1 = gpio.OpenPin(CS1);
            pin_CS2 = gpio.OpenPin(CS2);
            pin_CS3 = gpio.OpenPin(CS3);
            pin_FSELECT = gpio.OpenPin(FSELECT);
            pin_PSELECT = gpio.OpenPin(PSELECT);
            pin_RESET = gpio.OpenPin(RESET);
            pin_SLEEP = gpio.OpenPin(SLEEP);

            // Show an error if the pin wasn't initialized properly
            if (pin_CS0 == null && pin_CS1 == null)
            {
                Debug.WriteLine("Pin not open");
                return;
            }

            pin_CS0.Write(GpioPinValue.High);
            pin_CS1.Write(GpioPinValue.High);
            pin_CS2.Write(GpioPinValue.High);
            pin_CS3.Write(GpioPinValue.High);
            pin_FSELECT.Write(GpioPinValue.Low);
            pin_PSELECT.Write(GpioPinValue.Low);
            pin_RESET.Write(GpioPinValue.High);
            pin_SLEEP.Write(GpioPinValue.High);
            pin_CS0.SetDriveMode(GpioPinDriveMode.Output);
            pin_CS1.SetDriveMode(GpioPinDriveMode.Output);
            pin_CS2.SetDriveMode(GpioPinDriveMode.Output);
            pin_CS3.SetDriveMode(GpioPinDriveMode.Output);
            pin_FSELECT.SetDriveMode(GpioPinDriveMode.Output);
            pin_PSELECT.SetDriveMode(GpioPinDriveMode.Output);
            pin_RESET.SetDriveMode(GpioPinDriveMode.Output);
            pin_SLEEP.SetDriveMode(GpioPinDriveMode.Output);

            Debug.WriteLine("Gpio initialize");
        }

        private void WriteReg(ushort reg)
        {
            byte[] regValue = BitConverter.GetBytes(reg);
            pin_CS0.Write(GpioPinValue.Low);
            Task.Delay(TimeSpan.FromMilliseconds(0.01)).Wait();
            SPIAD9834.Write(regValue);
            Task.Delay(TimeSpan.FromMilliseconds(0.01)).Wait();
            pin_CS0.Write(GpioPinValue.High);
        }

        public void InitSPI_AD9834()
        {
            try
            {
                InitSPI();
                InitGpio();
                Debug.WriteLine("Inizialize AD9834");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fail inizialize AD9834:"+ ex.Message);
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
    }

    
}

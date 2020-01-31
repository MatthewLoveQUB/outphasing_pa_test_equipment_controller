using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QubVisa
    {
    public class RS_SMR20
        {
        public VisaDevice Device;
        public RS_SMR20(string deviceAddress)
            {
            this.Device = new VisaDevice(deviceAddress);
            }

        // IEEE Common Commands
        public string GetId()
            {
            return this.Device.ReadString("*IDN?");
            }

        public void ResetDevice()
            {
            this.Device.Write("*RST");
            }


        // OUTP Commands
        public void SetRfOutputState(bool on)
            {
            var state = on ? "ON" : "OFF";
            this.Device.Write($":OUTP:PON {state}");
            }

        // Source commands
        public void SetFrequencyContinuousWaveMode()
            {
            this.Device.Write("FREQ:MODE CW");
            }

        public void SetSourceFrequency(double freq)
            {
            this.Device.Write($":SOUR:FREQ:CW {freq}");
            }

        public void SetPowerContinuousWaveMode()
            {
            this.Device.connection.RawIO.Write(":SOUR:POW:MODE CW");
            }

        public void SetPowerLevel(double power, double offset = 0)
            {
            var pIn = power + offset;
            this.Device.connection.RawIO.Write($":POW {pIn}");
            }
        }
    }

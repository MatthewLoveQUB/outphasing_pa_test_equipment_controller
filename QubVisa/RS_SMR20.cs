using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QubVisa
    {
    class RS_SMR20
        {
        public VisaDevice Device;
        public RS_SMR20(string deviceAddress)
            {
            this.Device = new VisaDevice(deviceAddress);
            }

        // OUTP Commands
        void SetRfOutputState(bool on)
            {
            var state = on ? "ON" : "OFF";
            this.Device.Write($":OUTP:PON {state}");
            }

        // Source commands
        public void SetFrequencyContinuousWaveMode()
            {
            this.Device.Write("FREQ:MODE CW");
            }

        public void SetCwFreq(double freq)
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

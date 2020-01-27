using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QubVisa
{
    public class KeysightE8257D
        {
        public VisaDevice Device;
        public KeysightE8257D(string deviceAddress)
            {
            Device = new VisaDevice(deviceAddress);
            }

        public KeysightE8257D(VisaDevice device)
            {
            Device = device;
            }
        // IEEE Common Commands
        public string GetId()
            {
            return Device.ReadString("*IDN?");
            }

        // Calibration Commands
        public void RunDcfmCalibration()
            {
            Device.connection.RawIO.Write(":CAL:DCFM");
            }
        
        // Display Commands
        public void SetDisplayAmplitudeUnitsToDbm()
            {
            Device.connection.RawIO.Write(":DISP:ANN:AMPL:UNIT DBM");
            }

        // Output Commands
        public bool GetSettledState()
            {
            var query = Device.ReadString(":OUTP:SETT?");
            return Convert.ToBoolean(Convert.ToInt64(query));
            }

        public void SetRfOutputState(bool on)
            {
            var command = string.Format(":OUTP {0}", on ? "ON" : "OFF");
            Device.connection.RawIO.Write(command);
            }

        public bool GetRfOutputState()
            {
            var query = Device.ReadString(":OUTP?");
            return Convert.ToBoolean(Convert.ToInt64(query));
            }

        // Source Commands
        public void SetPowerContinuousWaveMode()
            {
            Device.connection.RawIO.Write(":POW:MODE FIX");
            }

        public void SetAmplitudeLimitLock(bool locked)
            {
            var command = string.Format(":POW:LIM:ADJ {0}", locked ? "OFF" : "ON");
            Device.connection.RawIO.Write(command);
            }

        public void SetPowerLevel(double power, double offset=0)
            {
            var inputPower = power + offset;
            var command = string.Format(":POW:LEV:IMM:AMPL {0}", inputPower);
            Device.connection.RawIO.Write(command);
            }

        public void SetSourceFrequency(double frequency)
            {
            var command = string.Format(":FREQ:FIX {0}", frequency);
            Device.connection.RawIO.Write(command);
            }
        }
}

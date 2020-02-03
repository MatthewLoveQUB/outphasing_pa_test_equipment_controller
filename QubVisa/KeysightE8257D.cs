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
            this.Device = new VisaDevice(deviceAddress);
            }

        public KeysightE8257D(VisaDevice device)
            {
            this.Device = device;
            }
        // IEEE Common Commands
        public string GetId()
            {
            return this.Device.ReadString("*IDN?");
            }

        public void ResetDevice()
            {
            this.Device.connection.RawIO.Write("*RST");
            }

        public void RunCalibration()
            {
            this.Device.connection.RawIO.Write("*CAL?");
            }

        public bool OperationComplete()
            {
            return
                Convert.ToBoolean(
                    Convert.ToInt32(this.Device.ReadString("*OPC?")));
            }

        // Calibration Commands
        public void RunDcfmCalibration()
            {
            this.Device.connection.RawIO.Write(":CAL:DCFM");
            }
        
        // Display Commands
        public void SetDisplayAmplitudeUnitsToDbm()
            {
            this.Device.connection.RawIO.Write(":DISP:ANN:AMPL:UNIT DBM");
            }

        // Output Commands
        public bool GetSettledState()
            {
            var query = this.Device.ReadString(":OUTP:SETT?");
            return Convert.ToBoolean(Convert.ToInt64(query));
            }

        public void SetRfOutputState(bool on)
            {
            var command = string.Format(":OUTP {0}", on ? "ON" : "OFF");
            this.Device.connection.RawIO.Write(command);
            }

        public bool GetRfOutputState()
            {
            var query = this.Device.ReadString(":OUTP?");
            return Convert.ToBoolean(Convert.ToInt64(query));
            }

        // Source Commands
        public void SetPowerContinuousWaveMode()
            {
            this.Device.connection.RawIO.Write(":POW:MODE FIX");
            }

        public void SetAmplitudeLimitLock(bool locked)
            {
            var command = string.Format(":POW:LIM:ADJ {0}", locked ? "OFF" : "ON");
            this.Device.connection.RawIO.Write(command);
            }

        public void SetPowerLevel(double power, double offset=0)
            {
            var inputPower = power + offset;
            var command = string.Format(":POW:LEV:IMM:AMPL {0}", inputPower);
            this.Device.connection.RawIO.Write(command);
            }

        public void SetSourceFrequency(double frequency)
            {
            var command = string.Format(":FREQ:FIX {0}", frequency);
            this.Device.connection.RawIO.Write(command);
            }

        public void SetSourceDeltaPhase(double phase) =>
            this.Device.Write($"PHAS {phase}DEG");

        }
}

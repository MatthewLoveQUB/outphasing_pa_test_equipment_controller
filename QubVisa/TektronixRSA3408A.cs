using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QubVisa
    {
    public class TektronixRSA3408A
        {
        public VisaDevice Device;
        public string SaveDir;

        public TektronixRSA3408A(string deviceAddress, string saveDir = "")
            {
            this.SaveDir = saveDir;
            this.Device = new VisaDevice(deviceAddress);
            }

        public TektronixRSA3408A(VisaDevice device, string saveDir = "")
            {
            this.SaveDir = saveDir;
            this.Device = device;
            }

        // IEEE Common Commands
        public string GetId()
            {
            return this.Device.ReadString("*IDN?");
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

        public void ResetDevice()
            {
            this.Device.connection.RawIO.Write("*RST");
            }

        // Calculate Commands
        public double GetMarkerYValue(int markerNumber, int view)
            {
            var query = string.Format(":CALC{0}:MARK{1}:Y?", view, markerNumber);
            return Convert.ToDouble(this.Device.ReadString(query));
            }

        public double GetMarkerXValue(int markerNumber, int view)
            {
            var query = string.Format(":CALC{0}:MARK{1}:X?", view, markerNumber);
            return Convert.ToDouble(this.Device.ReadString(query));
            }

        public void SetMarkerXValue(int markerNumber, int view, double xValue)
            {
            var command = 
                string.Format(":CALC{0}:MARK{1}:X {2}", view, markerNumber, xValue);
            this.Device.connection.RawIO.Write(command);
            }

        public void SetMarkerXToPositionMode(int markerNumber, int view)
            {
            var msg = string.Format(
                ":CALC{0}:MARK{1}:MODE POS",
                view,
                markerNumber);
            this.Device.connection.RawIO.Write(msg);
            }

        public void SetMarkerState(int markerNumber, int view, bool on)
            {
            string msg = string.Format(
                ":CALC{0}:MARK{1}:STATe {2}",
                view,
                markerNumber,
                on ? "ON" : "OFF");
            this.Device.connection.RawIO.Write(msg);
            }

        // Configure Commands
        public void SetSpectrumChannelPowerMeasurementMode()
            {
            this.Device.connection.RawIO.Write(":CONF:SPEC:CHP");
            }

        // Fetch Commands
        public double GetSpectrumChannelPower()
            {
            return Convert.ToDouble(this.Device.ReadString(":FETC:SPEC:CHP?"));
            }

        // Initiate Commands
        public void SetContinuousMode(bool continuousOn)
            {
            var message = string.Format(":INIT:CONT {0}", continuousOn ? "ON" : "OFF");
            this.Device.connection.RawIO.Write(message);
            }

        public void StartSignalAcquisition()
            {
            this.Device.connection.RawIO.Write(":INIT");
            }

        public void RestartSignalAcquisition()
            {
            this.Device.connection.RawIO.Write(":INIT:REST");
            }

        // Memory Commands
        public void SaveTrace(string fileName)
            {
            var filePath = string.Format("{0}//{1}", this.SaveDir, fileName);
            var message = string.Format(":MMEM:STOR:TRAC {0}", filePath);
            this.Device.connection.RawIO.Write(message);
            }

        // Read Commands
        // Fetch commands require you to manually stop the continuous
        // measurements and start an acquisition or an exception will occur
        // Read commands will stop the continuous mode on their own
        // and take a sample
        public double ReadSpectrumChannelPower()
            {
            return Convert.ToDouble(this.Device.ReadString(":READ:SPEC:CHP?"));
            }

        // Sense Commands
        public string GetFrequencyBand()
            {
            return this.Device.ReadString(":SENSE:FREQ:BAND?");
            }

        public void SetFrequencyCenter(double frequency)
            {
            var message = string.Format(":SENS:FREQ:CENT {0}", frequency);
            this.Device.connection.RawIO.Write(message);
            }

        public double GetFrequencyCenter()
            {
            return Convert.ToDouble(this.Device.ReadString(":SENS:FREQ:CENT?"));
            }

        public double GetFrequencySpan()
            {
            return Convert.ToDouble(this.Device.ReadString(":SENS:FREQ:SPAN?"));
            }

        public void SetFrequencySpan(double frequencySpan)
            {
            var message = string.Format(":SENS:FREQ:SPAN {0}", frequencySpan);
            this.Device.connection.RawIO.Write(message);
            }

        public double GetFrequencyStart()
            {
            return Convert.ToDouble(this.Device.ReadString(":SENS:FREQ:STAR?"));
            }

        public void SetFrequencyStart(double startFrequency)
            {
            var message = string.Format(":SENS:FREQ:STAR {0}", startFrequency);
            this.Device.connection.RawIO.Write(message);
            }

        public string GetFrequencyStop()
            {
            return this.Device.ReadString(":SENSE:FREQ:STOP?");
            }

        public void SetFrequencyStop(double stopFrequency)
            {
            var message = string.Format(":SENSE:FREQ:STOP {0}", stopFrequency);
            this.Device.connection.RawIO.Write(message);
            }

        public void SetChannelBandwidth(double bandwidth)
            {
            var message = string.Format(":SENS:CHP:BAND:INT {0}", bandwidth);
            this.Device.connection.RawIO.Write(message);
            }
    }
}

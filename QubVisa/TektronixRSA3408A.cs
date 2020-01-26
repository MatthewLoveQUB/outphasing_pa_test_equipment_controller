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
            SaveDir = saveDir;
            Device = new VisaDevice(deviceAddress);
            }

        public TektronixRSA3408A(VisaDevice device, string saveDir = "")
            {
            SaveDir = saveDir;
            Device = device;
            }

        // IEEE Common Commands
        public string GetId()
            {
            return Device.ReadString("*IDN?");
            }

        public void RunCalibration()
            {
            Device.connection.RawIO.Write("*CAL?");
            }
        
        public bool OperationComplete()
            {
            return Convert.ToBoolean(Convert.ToInt32(Device.ReadString("*OPC?")));
            }
        
        // Calculate Commands
        public double GetMarkerYValue(int markerNumber)
            {
            var query = string.Format(":CALC{0}:MARK:Y?", markerNumber);
            return Convert.ToDouble(Device.ReadString(query));
            }

        public double GetMarkerXValue(int markerNumber)
            {
            var query = string.Format(":CALC{0}:MARK:X?", markerNumber);
            return Convert.ToDouble(Device.ReadString(query));
            }

        public void SetMarkerXValue(int markerNumber, double xValue)
            {
            var command = string.Format(":CALC{0}:MARK:X {1}", markerNumber, xValue);
            Device.connection.RawIO.Write(command);
            }

        // Configure Commands
        public void SetSpectrumChannelPowerMeasurementMode()
            {
            Device.connection.RawIO.Write(":CONF:SPEC:CHP");
            }

        // Fetch Commands
        public double GetSpectrumChannelPower()
            {
            return Convert.ToDouble(Device.ReadString(":FETC:SPEC:CHP?"));
            }

        // Initiate Commands
        public void SetContinuousMode(bool continuousOn)
            {
            var message = string.Format(":INIT:CONT {0}", continuousOn ? "ON" : "OFF");
            Device.connection.RawIO.Write(message);
            }

        public void StartSignalAcquisition()
            {
            Device.connection.RawIO.Write(":INIT");
            }

        public void RestartSignalAcquisition()
            {
            Device.connection.RawIO.Write(":INIT:REST");
            }

        // Memory Commands
        public void SaveTrace(string fileName)
            {
            var filePath = string.Format("{0}//{1}", SaveDir, fileName);
            var message = string.Format(":MMEM:STOR:TRAC {0}", filePath);
            Device.connection.RawIO.Write(message);
            }

        // Read Commands
        // Fetch commands require you to manually stop the continuous
        // measurements and start an acquisition or an exception will occur
        // Read commands will stop the continuous mode on their own
        // and take a sample
        public double ReadSpectrumChannelPower()
            {
            return Convert.ToDouble(Device.ReadString(":READ:SPEC:CHP?"));
            }

        // Sense Commands
        public string GetFrequencyBand()
            {
            return Device.ReadString(":SENSE:FREQ:BAND?");
            }

        public void SetFrequencyCenter(double frequency)
            {
            var message = string.Format(":SENS:FREQ:CENT {0}", frequency);
            Device.connection.RawIO.Write(message);
            }

        public double GetFrequencyCenter()
            {
            return Convert.ToDouble(Device.ReadString(":SENS:FREQ:CENT?"));
            }

        public double GetFrequencySpan()
            {
            return Convert.ToDouble(Device.ReadString(":SENS:FREQ:SPAN?"));
            }

        public void SetFrequencySpan(double frequencySpan)
            {
            var message = string.Format(":SENS:FREQ:SPAN {0}", frequencySpan);
            Device.connection.RawIO.Write(message);
            }

        public double GetFrequencyStart()
            {
            return Convert.ToDouble(Device.ReadString(":SENS:FREQ:STAR?"));
            }

        public void SetFrequencyStart(double startFrequency)
            {
            var message = string.Format(":SENS:FREQ:STAR {0}", startFrequency);
            Device.connection.RawIO.Write(message);
            }

        public string GetFrequencyStop()
            {
            return Device.ReadString(":SENSE:FREQ:STOP?");
            }

        public void SetFrequencyStop(double stopFrequency)
            {
            var message = string.Format(":SENSE:FREQ:STOP {0}", stopFrequency);
            Device.connection.RawIO.Write(message);
            }
    }
}

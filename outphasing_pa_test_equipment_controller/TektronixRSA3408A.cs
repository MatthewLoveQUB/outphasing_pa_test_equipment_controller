using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace outphasing_pa_test_equipment_controller
{
    class TektronixRSA3408A : list_visa_devices_dialogue.VisaDevice
    {
        public string SaveDir;

        public TektronixRSA3408A(string deviceAddress, string saveDir="") : base(deviceAddress, "TektronixRSA3408A")
        {
            SaveDir = saveDir;
        }

        // IEEE Common Commands
        public string GetId()
            {
            return ReadString("*IDN?");
            }

        // Configure Commands
        public void SetSpectrumChannelPowerMeasurementMode()
            {
            device.RawIO.Write(":CONF:SPEC:CHP");
            }

        // Fetch Commands
        public string GetSpectrumChannelPower()
            {
            return ReadString(":FETC:SPEC:CHP?");
            }

        // Initiate Commands
        public void SetContinuousMode(bool continuousOn)
            {
            var message = string.Format(":INIT:CONT {0}", continuousOn ? "ON" : "OFF");
            device.RawIO.Write(message);
            }

        public void StartSignalAcquisition()
            {
            device.RawIO.Write(":INIT");
            }

        public void RestartSignalAcquisition()
            {
            device.RawIO.Write(":INIT:REST");
            }

        // Memory Commands
        public void SaveTrace(string fileName)
            {
            var filePath = string.Format("{0}//{1}", SaveDir, fileName);
            var message = string.Format(":MMEM:STOR:TRAC {0}", filePath);
            device.RawIO.Write(message);
            }

        // Sense Commands
        public string GetFrequencyBand()
            {
            return ReadString(":SENSE:FREQ:BAND?");
            }
    }
}

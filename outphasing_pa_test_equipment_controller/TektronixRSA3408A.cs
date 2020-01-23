﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace outphasing_pa_test_equipment_controller
{
    public class TektronixRSA3408A
    {
        public list_visa_devices_dialogue.VisaDevice Device;
        public string SaveDir;

        public TektronixRSA3408A(string deviceAddress, string saveDir="")
            {
            SaveDir = saveDir;
            Device = new list_visa_devices_dialogue.VisaDevice(deviceAddress);
            }

        public TektronixRSA3408A(list_visa_devices_dialogue.VisaDevice device, string saveDir = "")
            {
            SaveDir = saveDir;
            Device = device;
            }

        // IEEE Common Commands
        public string GetId()
            {
            return Device.ReadString("*IDN?");
            }

        // Configure Commands
        public void SetSpectrumChannelPowerMeasurementMode()
            {
            Device.connection.RawIO.Write(":CONF:SPEC:CHP");
            }

        // Fetch Commands
        public string GetSpectrumChannelPower()
            {
            return Device.ReadString(":FETC:SPEC:CHP?");
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

        // Sense Commands
        public string GetFrequencyBand()
            {
            return Device.ReadString(":SENSE:FREQ:BAND?");
            }
    }
}

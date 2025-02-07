﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QubVisa
    {
    public class RS_SMU200A
    {
        public VisaDevice Device;
        public RS_SMU200A(string deviceAddress)
            {
            this.Device = new VisaDevice(deviceAddress);
            }

        public RS_SMU200A(VisaDevice device)
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

        // Calibration Commands
        public void RunAllCalibrations()
            {
            this.Device.connection.RawIO.Write("CAL:ALL:MEAS?");
            }

        // Sense Commands
        public void SelectUserDefinedSource()
            {
            this.Device.connection.RawIO.Write("SENSE:SOUR USER");
            }

        public void SetUserDefinedSourceFrequency(double frequency)
            {
            var command = string.Format("SENS:FREQ {0}", frequency);
            this.Device.connection.RawIO.Write(command);
            }

        public void SelectSourceA()
            {
            this.Device.connection.RawIO.Write("SENS:SOUR A");
            }

        public void Autozero()
            {
            this.Device.connection.RawIO.Write("SENS:ZERO");
            }

        // Source Commands
        public void SetFrequencyContinuousWaveMode()
            {
            this.Device.connection.RawIO.Write("FREQ:MODE CW");
            }

        public void SetSourceFrequency(double frequency)
            {
            var command = string.Format("FREQ {0}", frequency);
            this.Device.connection.RawIO.Write(command);
            }

        public void SetAmplitudeLimit(double amplitude)
            {
            var command = string.Format("SOURce:POWer:LIMit:AMPLitude {0}", amplitude);
            this.Device.connection.RawIO.Write(command);
            }

        public void SetPowerContinuousWaveMode()
            {
            this.Device.connection.RawIO.Write("POW:MODE CW");
            }

        public void SetPowerLevel(double power, double offset=0)
            {
            var inputPower = power + offset;
            var command = string.Format("SOUR:POW:POW {0}", inputPower);
            this.Device.connection.RawIO.Write(command);
            }

        public void SetSourceDeltaPhase(double phase)
            {
            var command = string.Format("PHAS {0} DEG", phase);
            this.Device.connection.RawIO.Write(command);
            }

        public void SetRfOutputState(bool on)
            {
            var command = string.Format("OUTP {0}", on ? "ON" : "OFF");
            this.Device.connection.RawIO.Write(command);
            }
        }
}
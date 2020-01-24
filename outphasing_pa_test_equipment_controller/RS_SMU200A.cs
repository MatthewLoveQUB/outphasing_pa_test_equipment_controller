using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace outphasing_pa_test_equipment_controller
{
    public class RS_SMU200A
    {
        public list_visa_devices_dialogue.VisaDevice Device;
        public RS_SMU200A(string deviceAddress)
            {
            Device = new list_visa_devices_dialogue.VisaDevice(deviceAddress);
            }

        public RS_SMU200A(list_visa_devices_dialogue.VisaDevice device)
            {
            Device = device;
            }

        // IEEE Common Commands
        public string GetId()
            {
            return Device.ReadString("*IDN?");
            }

        // Calibration Commands
        public void RunAllCalibrations()
            {
            Device.connection.RawIO.Write("CAL:ALL:MEAS?");
            }

        // Sense Commands
        public void SelectUserDefinedSource()
            {
            Device.connection.RawIO.Write("SENSE:SOUR USER");
            }

        public void SetUserDefinedSourceFrequency(double frequency)
            {
            var command = string.Format("SENS:FREQ {0}", frequency);
            Device.connection.RawIO.Write(command);
            }

        public void SelectSourceA()
            {
            Device.connection.RawIO.Write("SENS:SOUR A");
            }

        public void Autozero()
            {
            Device.connection.RawIO.Write("SENS:ZERO");
            }

        // Source Commands
        public void SetFrequencyContinuousWaveMode()
            {
            Device.connection.RawIO.Write("FREQ:MODE CW");
            }

        public void SetSourceFrequency(double frequency)
            {
            var command = string.Format("FREQ {0}", frequency);
            Device.connection.RawIO.Write(command);
            }

        public void SetAmplitudeLimit(double amplitude)
            {
            var command = string.Format("SOURce:POWer:LIMit:AMPLitude {0}", amplitude);
            Device.connection.RawIO.Write(command);
            }

        public void SetPowerContinuousWaveMode()
            {
            Device.connection.RawIO.Write("POW:MODE CW");
            }

        public void SetPowerLevel(double power, double offset=0)
            {
            var inputPower = power + offset;
            var command = string.Format("SOUR:POW:POW {0}", inputPower);
            Device.connection.RawIO.Write(command);
            }

        public void SetSourceDeltaPhase(double phase)
            {
            var command = string.Format("PHAS {0} DEG", phase);
            Device.connection.RawIO.Write(command);
            }

        public void SetRfOutputState(bool on)
            {
            var command = string.Format("OUTP {0}", on ? "ON" : "OFF");
            Device.connection.RawIO.Write(command);
            }
        }
}
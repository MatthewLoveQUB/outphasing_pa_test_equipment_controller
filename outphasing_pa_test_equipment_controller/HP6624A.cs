using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace outphasing_pa_test_equipment_controller
{
    public class HP6624A : list_visa_devices_dialogue.VisaDevice
    {
        /// <summary>
        /// Convert the "000" or "001" response from the PSU to Bool.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool StringToBool(string message)
        {
            return Convert.ToBoolean(Convert.ToBoolean(message));
        }
        public HP6624A(string deviceAddress) : base(deviceAddress, "HP6624A PSU")
        {

        }
        private void SendC4Command(string header, int channel, double data)
        {
            var commandMessage = string.Format("{0} {1},{2}", header, channel, data);
            device.RawIO.Write(commandMessage);
        }

        private void SendC4Command(string header, int channel, bool data)
        {
            var state = data ? "1" : "0";
            var commandMessage = string.Format("{0} {1},{2}", header, channel, state);
            device.RawIO.Write(commandMessage);
        }

        private string ReadQ1Query(string header)
            {
            var message = string.Format("{0};", header);
            return ReadString(message);
            }

        private string ReadQ2Query(string header, int channel)
        {
            var queryMessage = string.Format("{0} {1};", header, channel);
            return ReadString(queryMessage);
        }

        public string GetId()
            {
            return ReadQ1Query("ID?");
            }
        public void SetChannelVoltage(int channel, double voltage)
        {
            SendC4Command("VSET", channel, voltage);
        }

        public double GetChannelVoltageSetting(int channel)
        {
            return Convert.ToDouble(ReadQ2Query("VSET?", channel));
        }

        public void SetChannelCurrent(int channel, double current)
        {
            SendC4Command("ISET", channel, current);
        }

        public double GetChannelCurrentSetting(int channel)
        {
            return Convert.ToDouble(ReadQ2Query("ISET?", channel));
        }

        public double GetChannelCurrentOutput(int channel)
        {
            return Convert.ToDouble(ReadQ2Query("IOUT?", channel));
        }

        public double GetChannelVoltageOutput(int channel)
        {
            return Convert.ToDouble(ReadQ2Query("VOUT?", channel));
        }

        public void SetChannelOutputState(int channel, bool on)
        {
            SendC4Command("OUT", channel, on);
        }

        public bool GetChannelOutputState(int channel)
        {
            return StringToBool(ReadQ2Query("OUT?", channel));
        }

        public void SetChannelOverVoltage(int channel, double voltage)
        {
            SendC4Command("OVSET", channel, voltage);
        }

        public double GetChannelOvervoltageSetting(int channel)
        {
            return Convert.ToDouble(ReadQ2Query("OVSET?", channel));
        }
    }
}

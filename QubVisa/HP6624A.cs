using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QubVisa
    {
    public class HP6624A
    {
        public VisaDevice Device;
        public List<bool> ChannelStates;
        public const int NumChannels = 4;
        public HP6624A(string deviceAddress, List<bool> channelStates)
            {
            Device = new VisaDevice(deviceAddress);
            ChannelStates = channelStates;
            }

        /// <summary>
        /// Convert the "000" or "001" response from the PSU to Bool.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool StringToBool(string message)
        {
            return Convert.ToBoolean(Convert.ToBoolean(message));
        }
        private void SendC4Command(string header, int channel, double data)
        {
            var commandMessage = string.Format("{0} {1},{2}", header, channel, data);
            Device.connection.RawIO.Write(commandMessage);
        }

        private void SendC4Command(string header, int channel, bool data)
        {
            var state = data ? "1" : "0";
            var commandMessage = $"{header} {channel},{state}";
            Device.connection.RawIO.Write(commandMessage);
        }

        private string ReadQ1Query(string header)
            {
            var message = string.Format("{0};", header);
            return Device.ReadString(message);
            }

        private string ReadQ2Query(string header, int channel)
        {
            var queryMessage = string.Format("{0} {1};", header, channel);
            return Device.ReadString(queryMessage);
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

        // It's hard to tell if the output state command actually works
        // so this "strong" command also zeros the 
        // current limit and voltage of the disabled channels
        public void SetChannelOutputStatesStrong()
            {
            var zeroVoltage = 0;
            var zeroCurrent = 0;
            for (int i = 0; i < NumChannels; i++)
                {
                var channelNumber = i + 1;
                bool channelState = ChannelStates[i];
                SetChannelOutputState(channelNumber, on: channelState);
                if (!channelState)
                    {
                    SetChannelVoltage(channelNumber, zeroVoltage);
                    SetChannelCurrent(channelNumber, zeroCurrent);
                    }
                }
            }

        // Set the states as listed in the config
        public void SetChannelStates()
            {
            for(int i = 0; i < NumChannels; i++)
                {
                var channelNumber = i + 1;
                SetChannelOutputState(channelNumber, ChannelStates[i]);
                }
            }

        public void SetActiveChannelsVoltages(double voltage)
            {
            for(int i = 0; i < NumChannels; i++)
                {
                int channelNumber = i + 1;
                var channelActive = ChannelStates[i];
                if (channelActive)
                    {
                    SetChannelVoltage(channelNumber, voltage);
                    }
                }
            }

        public void SetAllChannelVoltagesToZero()
            {
            for (int i = 0; i < NumChannels; i++)
                {
                var channelNumber = i + 1;
                SetChannelVoltage(channelNumber, 0.0);
                }
            }

        public void SetActiveChannelsCurrent(double current)
            {
            for (int i = 0; i < NumChannels; i++)
                {
                int channelNumber = i + 1;
                bool channelActive = ChannelStates[i];
                if (channelActive)
                    {
                    SetChannelCurrent(channelNumber, current);
                    }
                }
            }

        public double GetActiveChannelsPowerWatts()
            {
            var power = 0.0;
            for (int i = 0; i < NumChannels; i++)
                {
                int channelNumber = i + 1;
                bool channelActive = ChannelStates[i];
                if (channelActive)
                    {
                    var channelVoltage = 
                        GetChannelVoltageOutput(channelNumber);
                    var channelCurrent 
                        = GetChannelCurrentOutput(channelNumber);
                    var channelPower = channelCurrent * channelVoltage;
                    power += channelPower;
                    }
                }
            return power;
            }

        public class OutphasingDcMeasurements
            {
            public double PowerWatts;
            public List<double> Currents;

            public OutphasingDcMeasurements(
                double powerWatts, List<double> currents)
                {
                PowerWatts = powerWatts;
                Currents = currents;
                }
            }

        // 1 Assume the voltage is correct
        // 2 Return the recorded currents too
        public OutphasingDcMeasurements 
            OutphasingOptimisedMeasurement(double voltage)
            {
            var power = 0.0;
            var currents = new List<double>();
            for (int i = 0; i < NumChannels; i++)
                {
                var channelNumber = i + 1;
                if (ChannelStates[i])
                    {
                    var currentMeasurement =
                        GetChannelCurrentOutput(channelNumber);
                    currents.Add(currentMeasurement);
                    power += voltage * currentMeasurement;
                    }
                else
                    {
                    currents.Add(0);
                    }
                }
            return new OutphasingDcMeasurements(power, currents);
            }
    }
}

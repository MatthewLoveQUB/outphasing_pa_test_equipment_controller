﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace QubVisa
    {
    public class HP6624A
    {
        public VisaDevice Device;
        public List<bool> ChannelStates;
        public const int NumChannels = 4;
        public HP6624A(string deviceAddress, List<bool> channelStates)
            {
            this.Device = new VisaDevice(deviceAddress);
            this.ChannelStates = channelStates;
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
            var commandMessage = 
                string.Format("{0} {1},{2}", header, channel, data);
            this.Device.connection.RawIO.Write(commandMessage);
        }

        private void SendC4Command(string header, int channel, bool data)
        {
            var state = data ? "1" : "0";
            var commandMessage = $"{header} {channel},{state}";
            this.Device.connection.RawIO.Write(commandMessage);
        }

        private string ReadQ1Query(string header)
            {
            var message = string.Format("{0};", header);
            return this.Device.ReadString(message);
            }

        private string ReadQ2Query(string header, int channel)
        {
            var queryMessage = string.Format("{0} {1};", header, channel);
            return this.Device.ReadString(queryMessage);
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

        // Set the states as listed in the config
        public void SetChannelStates()
            {
            for(int i = 0; i < NumChannels; i++)
                {
                var channelNumber = i + 1;
                SetChannelOutputState(channelNumber, this.ChannelStates[i]);
                }
            }

        public void SetActiveChannelsVoltages(double voltage)
            {
            for(int i = 0; i < NumChannels; i++)
                {
                int channelNumber = i + 1;
                var channelActive = this.ChannelStates[i];
                if (channelActive)
                    {
                    SetChannelVoltage(channelNumber, voltage);
                    }
                }
            }

        public void SetAllChannelVoltagesToZero()
            {
            for (int i = 1; i <= NumChannels; i++)
                {
                SetChannelVoltage(i, 0.0);
                }
            }

        public void SetAllChannelCurrentsToZero()
            {
            for (int i = 1; i <= NumChannels; i++)
                {
                SetChannelCurrent(i, 0.0);
                }
            }

        public void ZeroAllChannels()
            {
            this.SetAllChannelVoltagesToZero();
            this.SetAllChannelCurrentsToZero();
            }

        public void SetActiveChannelsCurrent(double current)
            {
            for (int i = 0; i < NumChannels; i++)
                {
                int channelNumber = i + 1;
                bool channelActive = this.ChannelStates[i];
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
                bool channelActive = this.ChannelStates[i];
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

        // Ramp the voltage slowly
        // Assumes that all channels are at the same voltage
        public void SetPsuVoltageStepped(
            double newVoltage,
            double rampVoltageStep,
            int rampUpStepDelayMilliseconds)
            {
            // Will change sign depending on ramp up/down later
            var step = Math.Abs(rampVoltageStep);

            // Read the current voltage
            double currentVoltage = 0;
            for (int i = 0; i < HP6624A.NumChannels; i++)
                {
                bool channelEnabled = this.ChannelStates[i];
                if (channelEnabled)
                    {
                    var channel = i + 1;
                    currentVoltage =
                        this.GetChannelVoltageSetting(channel);
                    break;
                    }
                }

            if (newVoltage == currentVoltage) { return; }
            // Invert the step if we're decreasing the channel voltage
            if (newVoltage < currentVoltage)
                {
                step *= -1;
                }

            int numSteps =
                (int)(Math.Abs(currentVoltage - newVoltage) / step);
            var intermediateVoltage = currentVoltage;

            for (int currentStep = 0; currentStep < numSteps; currentStep++)
                {
                intermediateVoltage += step;
                this.SetActiveChannelsVoltages(intermediateVoltage);
                Thread.Sleep(rampUpStepDelayMilliseconds);
                }

            // Set to V_stop in case of overshoot
            this.SetActiveChannelsVoltages(newVoltage);
            }

        // 1 Assume the voltage is correct
        // 2 Return the recorded currents too
        public OutphasingDcMeasurements 
            OutphasingOptimisedMeasurement(double voltage)
            {
            var currents = new List<double>();
            for (int i = 0; i < NumChannels; i++)
                {
                var channelNumber = i + 1;
                if (this.ChannelStates[i])
                    {
                    currents.Add(
                        Math.Abs(GetChannelCurrentOutput(channelNumber)));
                    }
                }
            return new OutphasingDcMeasurements(voltage, currents);
            }
    }
}

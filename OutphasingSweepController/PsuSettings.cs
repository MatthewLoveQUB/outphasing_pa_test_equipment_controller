using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    public class PsuSettings
        {
        public bool Nominal { get; set; }
        public bool Plus10 { get; set; }
        public bool Minus10 { get; set; }

        public bool[] ChannelStates = 
            new bool[4] {false, false, false, false};
        public bool Channel1On
            {
            get
                {
                return this.ChannelStates[0];
                }
            set
                {
                this.ChannelStates[0] = value;
                }
            }
        public bool Channel2On
            {
            get
                {
                return this.ChannelStates[1];
                }
            set
                {
                this.ChannelStates[1] = value;
                }
            }
        public bool Channel3On
            {
            get
                {
                return this.ChannelStates[2];
                }
            set
                {
                this.ChannelStates[2] = value;
                }
            }
        public bool Channel4On
            {
            get
                {
                return this.ChannelStates[3];
                }
            set
                {
                this.ChannelStates[3] = value;
                }
            }

        public double NominalVoltage { get; set; }
        public double CurrentLimit { get; set; }
        public int RampUpStepTimeMilliseconds { get; set; }
        public double RampVoltageStep { get; set; }

    public PsuSettings(
            bool nominal, 
            bool plus10, 
            bool minus10, 
            bool[] channelStates, 
            double nominalVoltage, 
            double currentLimit, 
            int rampUpTimeMilliseconds, 
            double rampVoltageStep)
        {
            this.Nominal = nominal;
            this.Plus10 = plus10;
            this.Minus10 = minus10;
            this.ChannelStates = channelStates;
            this.NominalVoltage = nominalVoltage;
            this.CurrentLimit = currentLimit;
            this.RampUpStepTimeMilliseconds = rampUpTimeMilliseconds;
            this.RampVoltageStep = rampVoltageStep;
        }
        }
    }

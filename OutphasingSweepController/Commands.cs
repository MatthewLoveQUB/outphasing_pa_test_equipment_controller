using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    public class DeviceCommands
        {
        public delegate void SetInputPowerDelegate(
            double inputPower, double offset1, double offset2);
        public delegate void SetRfOutputStateDelegate(bool on);
        public delegate void SetFrequecyDelegate(double frequency);
        public delegate void SetPhaseDelegate(double phase);

        public SetInputPowerDelegate SetInputPower;
        public SetRfOutputStateDelegate SetRfOutputState;
        public SetFrequecyDelegate SetFrequency;
        public SetPhaseDelegate SetPhase;

        public DeviceCommands(
            SetInputPowerDelegate setInputPower,
            SetRfOutputStateDelegate setRfOutputState,
            SetFrequecyDelegate setFrequency,
            SetPhaseDelegate setPhase)
            {
            this.SetInputPower = setInputPower;
            this.SetRfOutputState = setRfOutputState;
            this.SetFrequency = setFrequency;
            this.SetPhase = setPhase;
            }
        }
    }

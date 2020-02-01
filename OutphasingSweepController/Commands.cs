using System;
using System.Collections.Generic;
using QubVisa;

namespace OutphasingSweepController
    {
    public class DeviceCommands
        {
        public delegate void SetInputPowerDelegate(
            double inputPower, double offset1, double offset2);
        public delegate void SetRfOutputStateDelegate(bool on);
        public delegate void SetFrequecyDelegate(double frequency);
        public delegate void SetPhaseDelegate(double phase);
        public delegate List<bool> GetPsuChannelStatesDelegate();
        public delegate void PreMeasurementSetupDelegate(
            MeasurementConfig sweepConf);
        public delegate void SetDcVoltageSteppedDelegate(
            double newVoltage, 
            double rampVoltageStep, 
            int rampUpStepDelayMilliseconds);
        public delegate OutphasingDcMeasurements
            OutphasingOptimisedMeasurementDelegate(double voltage);
        public delegate double GetSpectrumPowerDelegate();

        public SetInputPowerDelegate SetInputPower;
        public SetRfOutputStateDelegate SetRfOutputState;
        public SetFrequecyDelegate SetFrequency;
        public SetPhaseDelegate SetPhase;
        public GetPsuChannelStatesDelegate GetPsuChannelStates;
        public Action ResetDevices;
        public PreMeasurementSetupDelegate PreMeasurementSetup;
        public SetDcVoltageSteppedDelegate SetDcVoltageStepped;
        public OutphasingOptimisedMeasurementDelegate
            OutphasingOptimisedMeasurement;
        public GetSpectrumPowerDelegate GetSpectrumPower;

        public DeviceCommands(
            SetInputPowerDelegate setInputPower,
            SetRfOutputStateDelegate setRfOutputState,
            SetFrequecyDelegate setFrequency,
            SetPhaseDelegate setPhase,
            GetPsuChannelStatesDelegate getPsuChannelStates,
            Action resetDevices,
            PreMeasurementSetupDelegate preMeasurementSetup,
            SetDcVoltageSteppedDelegate setDcVoltageStepped,
            OutphasingOptimisedMeasurementDelegate outphasingOptimisedMeasurement,
            GetSpectrumPowerDelegate getSpectrumPower)
            {
            this.SetInputPower = setInputPower;
            this.SetRfOutputState = setRfOutputState;
            this.SetFrequency = setFrequency;
            this.SetPhase = setPhase;
            this.GetPsuChannelStates = getPsuChannelStates;
            this.ResetDevices = resetDevices;
            this.PreMeasurementSetup = preMeasurementSetup;
            this.SetDcVoltageStepped = setDcVoltageStepped;
            this.OutphasingOptimisedMeasurement = outphasingOptimisedMeasurement;
            this.GetSpectrumPower = getSpectrumPower;
            }
        }
    }

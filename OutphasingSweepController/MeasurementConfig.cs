﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QubVisa;

namespace OutphasingSweepController
    {
    public class MeasurementConfig
        {
        public List<Double> Frequencies;
        public List<Double> InputPowers;
        public List<Double> Phases;
        public double Temperature;
        public string Corner;
        public List<Double> Voltages;
        public string OutputFilePath;
        public DeviceOffsets GenOffsets1;
        public DeviceOffsets GenOffsets2;
        public DeviceOffsets SpectrumAnalyzerOffsets;
        public int MeasurementPoints
            {
            get
                {
                return this.Frequencies.Count
                    * this.InputPowers.Count
                    * this.Phases.Count
                    * this.Voltages.Count;
                }
            }
        public double MeasurementChannelBandwidth;
        public double MeasurementFrequencySpan;
        public PhaseSearchConfig PhaseSearchSettings;
        public DeviceCommands Commands;
        public double RampVoltageStep;
        public int PsuRampUpStepTimeMilliseconds;

        public MeasurementConfig(
            List<Double> frequencySettings,
            List<Double> powerSettings,
            List<Double> phaseSettings,
            double temperature,
            string corner,
            List<Double> voltages,
            double measurementBandwidth,
            double measurementSpan,
            string outputFilePath,
            string genOffsetsFilePath1,
            string genOffsetsFilePath2,
            string spectrumAnalyzerOffsetsFilePath,
            PhaseSearchConfig phasePeakTroughSearchSettings,
            DeviceCommands commands,
            double rampVoltageStep,
            int psuRampUpStepTimeMilliseconds)
            {
            this.Frequencies = frequencySettings;
            this.InputPowers = powerSettings;
            this.Phases = phaseSettings;
            this.Temperature = temperature;
            this.Corner = corner;
            this.Voltages = voltages;
            this.MeasurementChannelBandwidth = measurementBandwidth;
            this.MeasurementFrequencySpan = measurementSpan;
            this.OutputFilePath = outputFilePath;
            this.GenOffsets1 = new DeviceOffsets(genOffsetsFilePath1);
            this.GenOffsets2 = new DeviceOffsets(genOffsetsFilePath2);
            this.SpectrumAnalyzerOffsets = 
                new DeviceOffsets(spectrumAnalyzerOffsetsFilePath);
            this.PhaseSearchSettings = phasePeakTroughSearchSettings;
            this.Commands = commands;
            this.RampVoltageStep = rampVoltageStep;
            this.PsuRampUpStepTimeMilliseconds = psuRampUpStepTimeMilliseconds;
            }
        }
    }

using System;
using System.Collections.Generic;
using QubVisa;

namespace OutphasingSweepController
    {
    public class MeasurementConfig
        {
        public List<Double> Frequencies;
        public List<Double> InputPowers;
        public List<Double> Phases;
        public PsuSettings PsuConfig;
        public double Temperature;
        public string Corner;
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
                    * this.PsuConfig.Voltages.Count;
                }
            }
        public double MeasurementChannelBandwidth;
        public double MeasurementFrequencySpan;
        public PhaseSearchConfig PhaseSearchSettings;
        public DeviceCommands Commands;

        public MeasurementConfig(
            List<Double> frequencySettings,
            List<Double> powerSettings,
            List<Double> phaseSettings,
            PsuSettings psuConfig,
            double temperature,
            string corner,
            double measurementBandwidth,
            double measurementSpan,
            string outputFilePath,
            string genOffsetsFilePath1,
            string genOffsetsFilePath2,
            string spectrumAnalyzerOffsetsFilePath,
            PhaseSearchConfig phasePeakTroughSearchSettings,
            DeviceCommands commands)
            {
            this.Frequencies = frequencySettings;
            this.InputPowers = powerSettings;
            this.Phases = phaseSettings;
            this.PsuConfig = psuConfig;
            this.Temperature = temperature;
            this.Corner = corner;
            this.MeasurementChannelBandwidth = measurementBandwidth;
            this.MeasurementFrequencySpan = measurementSpan;
            this.OutputFilePath = outputFilePath;
            this.GenOffsets1 = new DeviceOffsets(genOffsetsFilePath1);
            this.GenOffsets2 = new DeviceOffsets(genOffsetsFilePath2);
            this.SpectrumAnalyzerOffsets = 
                new DeviceOffsets(spectrumAnalyzerOffsetsFilePath);
            this.PhaseSearchSettings = phasePeakTroughSearchSettings;
            this.Commands = commands;
            }
        }
    }

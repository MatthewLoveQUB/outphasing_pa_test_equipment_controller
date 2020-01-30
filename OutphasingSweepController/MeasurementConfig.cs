using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QubVisa;

namespace OutphasingSweepController
    {
    public class MeasurementConfig
        {
        public Equipment Devices;
        public List<Double> Frequencies;
        public List<Double> InputPowers;
        public List<Double> Phases;
        public double Temperature;
        public string Corner;
        public List<Double> Voltages;
        public string OutputFilePath;
        public DeviceOffsets Smu200aOffsets;
        public DeviceOffsets E8257dOffsets;
        public DeviceOffsets Rsa3408aOffsets;
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
        public bool PeakTroughPhaseSearch;
        public PhaseSearchConfig PhasePeakTroughSearchSettings;
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
            string Smu200aOffsetFilePath,
            string E8257dOffsetFilePath,
            string Rsa3408aOffsetsFilePath,
            bool peakTroughPhaseSearch,
            PhaseSearchConfig phasePeakTroughSearchSettings,
            Equipment devices)
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
            this.Smu200aOffsets = new DeviceOffsets(Smu200aOffsetFilePath);
            this.E8257dOffsets = new DeviceOffsets(E8257dOffsetFilePath);
            this.Rsa3408aOffsets = new DeviceOffsets(Rsa3408aOffsetsFilePath);
            this.PeakTroughPhaseSearch = peakTroughPhaseSearch;
            this.PhasePeakTroughSearchSettings = phasePeakTroughSearchSettings;
            this.Devices = devices;
            }
        }
    }

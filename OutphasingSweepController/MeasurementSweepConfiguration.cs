using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QubVisa;

namespace OutphasingSweepController
    {
    public class MeasurementSweepConfiguration
        {
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
                return Frequencies.Count
                    * InputPowers.Count
                    * Phases.Count
                    * Voltages.Count;
                }
            }
        public double MeasurementChannelBandwidth;
        public double MeasurementFrequencySpan;
        public bool PeakTroughPhaseSearch;
        public MeasurementSweepConfiguration(
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
            bool peakTroughPhaseSearch)
            {
            Frequencies = frequencySettings;
            InputPowers = powerSettings;
            Phases = phaseSettings;
            Temperature = temperature;
            Corner = corner;
            Voltages = voltages;
            MeasurementChannelBandwidth = measurementBandwidth;
            MeasurementFrequencySpan = measurementSpan;
            OutputFilePath = outputFilePath;
            Smu200aOffsets = new DeviceOffsets(Smu200aOffsetFilePath);
            E8257dOffsets = new DeviceOffsets(E8257dOffsetFilePath);
            Rsa3408aOffsets = new DeviceOffsets(Rsa3408aOffsetsFilePath);
            PeakTroughPhaseSearch = peakTroughPhaseSearch;
            }
        }
    }

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QubVisa;

namespace OutphasingSweepController
    {
    class MeasurementSweepConfiguration
        {
        public List<Double> Frequencies;
        public List<Double> InputPowers;
        public List<Double> Phases;
        public double Temperature;
        public string Corner;
        public List<Double> Voltages;
        public DeviceOffsets Smu200aOffsets;
        public DeviceOffsets E8257dOffsets;
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
        public MeasurementSweepConfiguration(
            List<Double> frequencySettings,
            List<Double> powerSettings,
            List<Double> phaseSettings,
            double temperature,
            string corner,
            List<Double> voltages,
            double measurementBandwidth,
            string Smu200aOffsetFilePath,
            string E8257dOffsetFilePath)
            {
            Frequencies = frequencySettings;
            InputPowers = powerSettings;
            Phases = phaseSettings;
            Temperature = temperature;
            Corner = corner;
            Voltages = voltages;
            MeasurementChannelBandwidth = measurementBandwidth;
            Smu200aOffsets = new DeviceOffsets(Smu200aOffsetFilePath);
            E8257dOffsets = new DeviceOffsets(E8257dOffsetFilePath);
            }
        }
    }

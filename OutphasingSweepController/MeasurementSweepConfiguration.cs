using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    class MeasurementSweepConfiguration
        {
        public List<Double> Frequencies;
        public List<Double> Powers;
        public List<Double> Phases;
        public double Temperature;
        public string Corner;
        public List<Double> Voltages;
        public int MeasurementPoints
            {
            get
                {
                return Frequencies.Count
                    * Powers.Count
                    * Phases.Count
                    * Voltages.Count;
                }
            }
        public MeasurementSweepConfiguration(
            List<Double> frequencySettings,
            List<Double> powerSettings,
            List<Double> phaseSettings,
            double temperature,
            string corner,
            List<Double> voltages)
            {
            Frequencies = frequencySettings;
            Powers = powerSettings;
            Phases = phaseSettings;
            Temperature = temperature;
            Corner = corner;
            Voltages = voltages;
            }
        }
    }

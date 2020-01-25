using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    class MeasurementSweepConfiguration
        {
        public SweepSettings FrequencySettings;
        public SweepSettings PowerSettings;
        public SweepSettings PhaseSettings;
        public double Temperature;
        public string Corner;
        public List<Double> Voltages;
        public MeasurementSweepConfiguration(
            SweepSettings frequencySettings,
            SweepSettings powerSettings,
            SweepSettings phaseSettings,
            double temperature,
            string corner,
            List<Double> voltages)
            {
            FrequencySettings = frequencySettings;
            PowerSettings = powerSettings;
            PhaseSettings = phaseSettings;
            Temperature = temperature;
            Corner = corner;
            Voltages = voltages;
            }
        }
    }

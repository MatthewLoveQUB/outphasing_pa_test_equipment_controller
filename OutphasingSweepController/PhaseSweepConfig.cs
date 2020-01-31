using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    public class PhaseSweepConfig
        {
        public MeasurementConfig MeasurementConfig;
        public CurrentOffset Offset;
        public double SupplyVoltage;
        public double Frequency;
        public double InputPower;

        public PhaseSweepConfig(
            MeasurementConfig measurementConfig,
            CurrentOffset offset,
            double supplyVoltage,
            double frequency,
            double inputPower)
            {
            this.MeasurementConfig = measurementConfig;
            this.Offset = offset;
            this.SupplyVoltage = supplyVoltage;
            this.Frequency = frequency;
            this.InputPower = inputPower;
            }
        }
    }

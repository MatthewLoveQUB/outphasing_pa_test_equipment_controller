using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    public class CurrentOffset
        {
        public double Smu200a;
        public double E8257d;
        public double Rsa3408a;

        public CurrentOffset(
            double offsetSmu200a, 
            double offsetE8257d, 
            double offsetRsa)
            {
            Smu200a = offsetSmu200a;
            E8257d = offsetE8257d;
            Rsa3408a = offsetRsa;
            }
        }

    public class MeasurementSampleConfiguration
        {
        public MeasurementSweepConfiguration Conf;
        public double SupplyVoltage;
        public double Frequency;
        public double InputPower;
        public double Phase;
        public CurrentOffset Offset;

        public MeasurementSampleConfiguration(
            MeasurementSweepConfiguration conf,
            double supplyVoltage,
            double frequency,
            double inputPower,
            double phase,
            double offsetSmu200a,
            double offsetE8257d,
            double offsetRsa)
            {
            Conf = conf;
            SupplyVoltage = supplyVoltage;
            Frequency = frequency;
            InputPower = inputPower;
            Phase = phase;
            Offset = new CurrentOffset(offsetSmu200a, offsetE8257d, offsetRsa);
            }
        }
    }

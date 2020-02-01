using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    // The offset values in dB for the latest frequency
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
            this.Smu200a = offsetSmu200a;
            this.E8257d = offsetE8257d;
            this.Rsa3408a = offsetRsa;
            }
        }

    public class SampleConfig : PhaseSweepConfig
        {
        public double Phase;

        public SampleConfig(PhaseSweepConfig phaseSweepConfig, double phase) 
            : base(phaseSweepConfig)
            {
            this.Phase = phase;
            }
        }
    }

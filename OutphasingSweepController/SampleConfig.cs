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
        public double SignalGenerator1;
        public double SignalGenerator2;
        public double SpectrumAnalyzer;

        public CurrentOffset(
            double signalGenerator1, 
            double signalGenerator2, 
            double spectrumAnalyzer)
            {
            this.SignalGenerator1 = signalGenerator1;
            this.SignalGenerator2 = signalGenerator2;
            this.SpectrumAnalyzer = spectrumAnalyzer;
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

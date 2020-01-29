using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QubVisa;

namespace OutphasingSweepController
    {
    class MeasurementSample
        {
        public readonly MeasurementSampleConfiguration Conf;
        public double InputPowerdBm { get
                {
                return Conf.InputPower;
                }
            }
        public double InputPowerWatts
            {
            get
                {
                return PowerConversion.dBmToWatts(InputPowerdBm);
                }
            }
        // Measurement values
        public double MeasuredPowerDcWatts;
        public double MeasuredPowerDcdBm
            {
            get
                {
                return PowerConversion.WattsTodBm(MeasuredPowerDcWatts);
                }
            }
        public double MeasuredOutputPowerdBm;
        public double CalibratedOutputPowerdBm { get
                {
                return MeasuredOutputPowerdBm + Conf.Offset.Rsa3408a;
                }
            }
        public double CalibratedOutputPowerWatts {  get
                {
                return PowerConversion.dBmToWatts(CalibratedOutputPowerdBm);
                }
            }
        public double CalibratedDrainEfficiency { get
                {
                return 100.0 * 
                    (CalibratedOutputPowerWatts / MeasuredPowerDcWatts);
                }
            }
        public double CalibratedPowerAddedEfficiency {  get
                {
                return 100.0 * 
                    ((CalibratedOutputPowerWatts - InputPowerWatts) 
                    / MeasuredPowerDcWatts);
                }
            }
        public double MeasuredChannelPowerdBm;
        public double CalibratedGaindB { get
                {
                return CalibratedOutputPowerdBm - (InputPowerdBm + 3);
                }
            }
        public List<double> DcCurrent;
        
        public MeasurementSample(
            MeasurementSampleConfiguration conf,
            double measuredPowerDcWatts,
            double measuredPoutdBm,
            double channelPowerdBm,
            List<double> dcCurrent)
            {
            Conf = conf;
            MeasuredPowerDcWatts = measuredPowerDcWatts;
            MeasuredOutputPowerdBm = measuredPoutdBm;
            MeasuredChannelPowerdBm = channelPowerdBm;
            DcCurrent = dcCurrent;
            }
        }
    }

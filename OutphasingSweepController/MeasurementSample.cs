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
                return this.Conf.InputPower;
                }
            }
        public double InputPowerWatts
            {
            get
                {
                return PowerConversion.dBmToWatts(this.InputPowerdBm);
                }
            }
        // Measurement values
        public double MeasuredPowerDcWatts;
        public double MeasuredPowerDcdBm
            {
            get
                {
                return PowerConversion.WattsTodBm(this.MeasuredPowerDcWatts);
                }
            }
        public double MeasuredOutputPowerdBm;
        public double CalibratedOutputPowerdBm { get
                {
                return this.MeasuredOutputPowerdBm + this.Conf.Offset.Rsa3408a;
                }
            }
        public double CalibratedOutputPowerWatts {  get
                {
                return PowerConversion.dBmToWatts(this.CalibratedOutputPowerdBm);
                }
            }
        public double CalibratedDrainEfficiency { get
                {
                return 100.0 * 
                    (this.CalibratedOutputPowerWatts / this.MeasuredPowerDcWatts);
                }
            }
        public double CalibratedPowerAddedEfficiency {  get
                {
                return 100.0 * 
                    ((this.CalibratedOutputPowerWatts - this.InputPowerWatts) 
                    / this.MeasuredPowerDcWatts);
                }
            }
        public double MeasuredChannelPowerdBm;
        public double CalibratedGaindB { get
                {
                return this.CalibratedOutputPowerdBm - (this.InputPowerdBm + 3);
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
            this.Conf = conf;
            this.MeasuredPowerDcWatts = measuredPowerDcWatts;
            this.MeasuredOutputPowerdBm = measuredPoutdBm;
            this.MeasuredChannelPowerdBm = channelPowerdBm;
            this.DcCurrent = dcCurrent;
            }
        }
    }

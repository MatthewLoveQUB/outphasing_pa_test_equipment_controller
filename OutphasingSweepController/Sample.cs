﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QubVisa;

namespace OutphasingSweepController
    {
    public class Sample
        {
        public readonly SampleConfig Conf;
        public double InputPowerdBm
            {
            get
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
        public double MeasuredOutputPowerWatts
            {
            get
                {
                return PowerConversion.dBmToWatts(this.MeasuredOutputPowerdBm);
                }
            }
        public double CalibratedOutputPowerdBm
            {
            get
                {
                return this.MeasuredOutputPowerdBm + this.Conf.Offset.SpectrumAnalyzer;
                }
            }
        public double CalibratedOutputPowerWatts
            {
            get
                {
                return PowerConversion.dBmToWatts(this.CalibratedOutputPowerdBm);
                }
            }
        public double CalibratedDrainEfficiency
            {
            get
                {
                return 100.0 * 
                    (this.CalibratedOutputPowerWatts / this.MeasuredPowerDcWatts);
                }
            }
        public double CalibratedPowerAddedEfficiency
            {
            get
                {
                return 100.0 * 
                    ((this.CalibratedOutputPowerWatts - this.InputPowerWatts) 
                    / this.MeasuredPowerDcWatts);
                }
            }
        public double MeasuredChannelPowerdBm;
        public double MeasuredChannelPowerWatts
            {
            get
                {
                return PowerConversion.dBmToWatts(
                    this.MeasuredChannelPowerdBm);
                }
            }
        public double CalibratedChannelPowerdBm
            {
            get
                {
                return this.MeasuredChannelPowerdBm 
                    + this.Conf.Offset.SpectrumAnalyzer;
                }
            }

        public double CalibratedChannelPowerWatts
            {
            get
                {
                return PowerConversion.dBmToWatts(
                    this.CalibratedChannelPowerdBm);
                }
            }

        public double CalibratedChannelPowerAddedEfficiency
            {
            get
                {
                var pout = this.CalibratedChannelPowerWatts;
                var pin = this.InputPowerWatts;
                var diffp = pout - pin;
                return 100.0 * (diffp / this.MeasuredPowerDcWatts);
                }
            }

        public double CalibratedChannelDrainEfficiency
            {
            get
                {
                var pout = this.CalibratedChannelPowerWatts;
                return 100.0 * (pout / this.MeasuredPowerDcWatts);
                }
            }

        public double CalibratedGaindB
            {
            get
                {
                return this.CalibratedOutputPowerdBm - (this.InputPowerdBm + 3);
                }
            }

        public double CalibratedChannelGaindB
            {
            get
                {
                return this.CalibratedChannelPowerdBm 
                    - (this.InputPowerdBm + 3);
                }
            }

        public List<double> DcCurrents;
        
        public Sample(
            SampleConfig conf,
            double measuredPowerDcWatts,
            double measuredPoutdBm,
            double channelPowerdBm,
            List<double> dcCurrent)
            {
            this.Conf = conf;
            this.MeasuredPowerDcWatts = measuredPowerDcWatts;
            this.MeasuredOutputPowerdBm = measuredPoutdBm;
            this.MeasuredChannelPowerdBm = channelPowerdBm;
            this.DcCurrents = dcCurrent;
            }
        }
    }

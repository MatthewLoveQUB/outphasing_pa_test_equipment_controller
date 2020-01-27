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
        public double Frequency;
        // Input power without offsets
        public double InputPowerdBm;
        public double InputPowerWatts
            {
            get
                {
                return PowerConversion.dBmToWatts(InputPowerdBm);
                }
            }
        public double PhaseDeg;
        public double Temperature;
        public string Corner;
        public double SupplyVoltage;
        // Measurement values
        public double MeasuredPowerDcWatts;
        public double MeasuredPowerDcdBm
            {
            get
                {
                return PowerConversion.WattsTodBm(MeasuredPowerDcWatts);
                }
            }
        public double OffsetSmu200adB;
        public double OffsetE8557ddB;
        public double OffsetRsa3408AdB;
        public double MeasuredOutputPowerdBm;
        public double CalibratedOutputPowerdBm { get
                {
                return MeasuredOutputPowerdBm + OffsetRsa3408AdB;
                }
            }
        public double CalibratedOutputPowerWatts {  get
                {
                return PowerConversion.dBmToWatts(CalibratedOutputPowerdBm);
                }
            }
        public double CalibratedDrainEfficiency { get
                {
                return 100.0 * (CalibratedOutputPowerWatts / MeasuredPowerDcWatts);
                }
            }
        public double CalibratedPowerAddedEfficiency {  get
                {
                return 100.0 * ((CalibratedOutputPowerWatts - InputPowerWatts) / MeasuredPowerDcWatts);
                }
            }
        public double MeasuredChannelPowerdBm;
        public double RsaFrequencySpan;
        public double RsaChannelBandwidth;
        public double CalibratedGaindB { get
                {
                return CalibratedOutputPowerdBm - (InputPowerdBm + 3);
                }
            }
        public double DcCurrent1;
        public double DcCurrent2;

        public MeasurementSample(
            double frequency,
            double inputPowerdBm,
            double phaseDeg,
            double temperature,
            string corner,
            double supplyVoltage,
            double measuredPowerDcWatts,
            double offsetSmu,
            double offsetE8257d,
            double offsetRsa,
            double measuredPoutdBm,
            double measurementFrequencySpan,
            double measurementChannelBandwidth,
            double channelPowerdBm,
            double dcCurrent1,
            double dcCurrent2)
            {
            Frequency = frequency;
            InputPowerdBm = inputPowerdBm;
            PhaseDeg = phaseDeg;
            Temperature = temperature;
            Corner = corner;
            SupplyVoltage = supplyVoltage;
            MeasuredPowerDcWatts = measuredPowerDcWatts;
            OffsetSmu200adB = offsetSmu;
            OffsetE8557ddB = offsetE8257d;
            OffsetRsa3408AdB = offsetRsa;
            MeasuredOutputPowerdBm = measuredPoutdBm;
            RsaFrequencySpan = measurementFrequencySpan;
            RsaChannelBandwidth = measurementChannelBandwidth;
            MeasuredChannelPowerdBm = channelPowerdBm;
            DcCurrent1 = dcCurrent1;
            DcCurrent2 = dcCurrent2;
            }
        }
    }

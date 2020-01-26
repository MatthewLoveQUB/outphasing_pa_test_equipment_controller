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
        public double MeasuredOutputPowerdBm;
        public double MeasuredOutputPowerWatts {  get
                {
                return PowerConversion.dBmToWatts(MeasuredOutputPowerdBm);
                }
            }
        public double DrainEfficiency { get
                {
                return 100.0 * (MeasuredOutputPowerWatts / MeasuredPowerDcWatts);
                }
            }
        public double PowerAddedEfficiency {  get
                {
                return 100.0 * ((MeasuredOutputPowerWatts - InputPowerWatts) / MeasuredPowerDcWatts);
                }
            }
        public double MeasuredChannelPowerdBm;
        public double RsaMeasurementBandwidth;

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
            double measuredPoutdBm,
            double measurementBw,
            double channelPowerdBm)
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
            MeasuredOutputPowerdBm = measuredPoutdBm;
            RsaMeasurementBandwidth = measurementBw;
            MeasuredChannelPowerdBm = channelPowerdBm;
            }
        }
    }

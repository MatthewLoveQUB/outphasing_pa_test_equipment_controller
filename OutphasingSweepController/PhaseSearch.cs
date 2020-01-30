using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    public static class PhaseSearch
        {
        public enum Mode
            {
            Peak,
            Trough
            }

        public enum NewSampleResult
            {
            Better,
            Worse
            }

        public enum LoopStatus
            {
            Continue,
            Stop
            }

        public enum Gradient
            {
            Positive,
            Negative
            }

        public static Gradient GetGradient(
            Sample sampleRef,
            Sample sampleNew)
            {
            return (sampleNew.MeasuredChannelPowerdBm
                > sampleRef.MeasuredChannelPowerdBm)
                ? Gradient.Positive
                : Gradient.Negative;
            }

        public static NewSampleResult PeakTroughComparison(
            Mode mode,
            Sample bestSample,
            Sample newSample)
            {
            var newPower = newSample.MeasuredChannelPowerdBm;
            var bestPower = bestSample.MeasuredChannelPowerdBm;
            var newGreater = newPower > bestPower;
            var peakBetter = (mode == Mode.Peak) && newGreater;
            var troughBetter = (mode == Mode.Trough) && !newGreater;
            var better = peakBetter || troughBetter;
            return better ? NewSampleResult.Better : NewSampleResult.Worse;
            }

        public static LoopStatus EvaluateNewSample(
            Sample bestSample,
            Sample newSample,
            Mode searchMode,
            double threshold)
            {
            var newPwr = newSample.MeasuredChannelPowerdBm;
            var bestPwr = bestSample.MeasuredChannelPowerdBm;
            var peakContinue = (searchMode == Mode.Peak)
                && ((bestPwr - newPwr) < threshold);
            var troughContinue = (searchMode == Mode.Trough)
                && ((newPwr - bestPwr) < threshold);
            var continueMeasure = peakContinue || troughContinue;
            return continueMeasure ?
                LoopStatus.Continue : LoopStatus.Stop;
            }

        public static double FindSearchDirection(
            PhaseSearch.Mode searchMode,
            List<Sample> samples,
            Sample bestSample,
            MeasurementConfig measConfig,
            CurrentOffset offset,
            double supplyVoltage,
            double frequency,
            double inputPower,
            double phaseStep)
            {
            SampleConfig makeSampleConfig(double phase)
                {
                return new SampleConfig(
                    measConfig, 
                    supplyVoltage,
                    frequency,
                    inputPower,
                    phase,
                    offset);
                }
            var corePhase = bestSample.Conf.Phase;
            Action<double> setPhase = 
                measConfig.Devices.Smu200a.SetSourceDeltaPhase;
            Sample makeSample(double stepScale)
                {
                var newPhase = corePhase + (stepScale * phaseStep);
                var sampleConfig = makeSampleConfig(newPhase);
                setPhase(newPhase);
                var newSample = Measurement.TakeSample(sampleConfig);
                samples.Add(newSample);
                return newSample;
                }

            double scalar = 0.0;
            while (true)
                {
                scalar += 1.0;
                var samplePos = makeSample(scalar);
                var gradientPos = GetGradient(bestSample, samplePos);
                var sampleNeg = makeSample(scalar * -1);
                var gradientNeg = GetGradient(bestSample, sampleNeg);

                // If both adjacent points move in the same direction
                // then there's no clear direction to move in
                if (gradientNeg == gradientPos)
                    {
                    continue;
                    }

                if (searchMode == PhaseSearch.Mode.Peak)
                    {
                    return (gradientPos == Gradient.Positive) ? 1.0 : -1.0;
                    }
                else
                    {
                    return (gradientPos == Gradient.Negative) ? 1.0 : -1.0;
                    }
                }
            }
        }
    }

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

        public static List<Sample> MeasurementPhaseSweep(
            MeasurementConfig sweepConfig,
            CurrentOffset offset,
            double supplyVoltage,
            double frequency,
            double inputPower)
            {
            var samples = new List<Sample>();
            BasicPhaseSweep(
                samples, 
                sweepConfig,
                supplyVoltage,
                frequency,
                inputPower,
                offset);

            if (!sweepConfig.PeakTroughPhaseSearch) { return samples; }

            void DoSearch(
                List<PhaseSearchSingleConfig> searchSettings,
                Mode mode)
                {
                foreach (var searchSetting in searchSettings)
                    {
                    var orderedSamples =
                        samples.OrderByDescending(
                            s => s.MeasuredChannelPowerdBm).ToList();
                    var bestSample = (mode == Mode.Peak)
                        ? orderedSamples.First()
                        : orderedSamples.Last();
                    FindPeakOrTrough(
                        mode,
                        samples,
                        bestSample,
                        sweepConfig,
                        offset,
                        supplyVoltage,
                        frequency,
                        inputPower,
                        searchSetting.ThresholddB,
                        searchSetting.StepDeg);
                    }

                }

            DoSearch(sweepConfig.PhaseSearchSettings.Peak, Mode.Peak);
            DoSearch(sweepConfig.PhaseSearchSettings.Trough, Mode.Trough);
            return samples;
            }

        public static void BasicPhaseSweep(
            List<Sample> samples,
            MeasurementConfig measConf,
            double supplyVoltage,
            double frequency,
            double inputPower,
            CurrentOffset offset)
            {
            foreach (var phase in measConf.Phases)
                {
                var sampleConfig = new SampleConfig(
                    measConf,
                    supplyVoltage,
                    frequency,
                    inputPower,
                    phase,
                    offset);
                TakeSample(sampleConfig, samples);
                }
            }

        private static Sample TakeSample(
            SampleConfig sampleConfig,
            List<Sample> samples)
            {
            sampleConfig.MeasConfig.Devices.Smu200a.SetSourceDeltaPhase(
                sampleConfig.Phase);
            var newSample = Measurement.TakeSample(sampleConfig);
            samples.Add(newSample);
            return newSample;
            }

        public static void FindPeakOrTrough(
            Mode searchMode,
            List<Sample> samples,
            Sample startBestSample,
            MeasurementConfig measConfig,
            CurrentOffset offset,
            double supplyVoltage,
            double frequency,
            double inputPower,
            double exitThreshold,
            double phaseStep)
            {
            var bestSample = startBestSample;
            Sample newSample;
            
            Func<double, SampleConfig> makeSampleConfig = (double phase) =>
            {
                return new SampleConfig(
                    measConfig,
                    supplyVoltage,
                    frequency,
                    inputPower,
                    phase,
                    offset);
            };

            phaseStep = FindSearchDirection(
                searchMode,
                samples,
                bestSample,
                measConfig,
                offset,
                supplyVoltage,
                frequency,
                inputPower,
                phaseStep,
                makeSampleConfig);

            // Loop until we've moved exitThreshold dB
            // away from the best found value
            var currentPhase = bestSample.Conf.Phase;
            newSample = bestSample;
            while (EvaluateNewSample(
                bestSample,
                newSample,
                searchMode,
                exitThreshold)
                == LoopStatus.Continue)
                {
                currentPhase += phaseStep;
                var sampleConfig = makeSampleConfig(currentPhase);
                newSample = TakeSample(sampleConfig, samples);
                var newRes = PeakTroughComparison(
                    searchMode, bestSample, newSample);
                if (newRes == NewSampleResult.Better)
                    {
                    bestSample = newSample;
                    }
                }
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
            double phaseStep,
            Func<double, SampleConfig> makeSampleConfig)
            {
            var corePhase = bestSample.Conf.Phase;

            Sample makeSample(double stepScale)
                {
                var newPhase = corePhase + (stepScale * phaseStep);
                var newConfig = makeSampleConfig(newPhase);
                return TakeSample(newConfig, samples);
                }

            double scalar = 0.0;
            while (true)
                {
                scalar += 1.0;
                var samplePos = makeSample(scalar);
                var sampleNeg = makeSample(scalar * -1);
                var gradientPos = GetGradient(bestSample, samplePos);
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
        }
    }

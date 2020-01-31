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

        public enum Direction
            {
            Positive,
            Negative,
            Stop
            }

        public static List<Sample> MeasurementPhaseSweep(
            PhaseSweepConfig phaseSweepConfig)
            {
            var samples = new List<Sample>();
            BasicPhaseSweep(samples, phaseSweepConfig);

            if (!phaseSweepConfig.MeasurementConfig.PeakTroughPhaseSearch)
                {
                return samples;
                }

            void DoSearch(
                List<PhaseSearchPointConfig> searchSettings,
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
                    FindPeakOrTrough1(
                        mode,
                        samples,
                        bestSample,
                        phaseSweepConfig,
                        searchSetting);
                    }

                }

            DoSearch(
                phaseSweepConfig.MeasurementConfig.PhaseSearchSettings.Peak, 
                Mode.Peak);
            DoSearch(
                phaseSweepConfig.MeasurementConfig.PhaseSearchSettings.Trough, 
                Mode.Trough);
            return samples;
            }

        public static void BasicPhaseSweep(
            List<Sample> samples,
            PhaseSweepConfig phaseSweepConfig)
            {
            foreach (var phase in phaseSweepConfig.MeasurementConfig.Phases)
                {
                var sampleConfig = new SampleConfig(
                    phaseSweepConfig,
                    phase);
                TakeSample(sampleConfig, samples);
                }
            }

        private static Sample TakeSample(
            SampleConfig sampleConfig,
            List<Sample> samples)
            {
            sampleConfig
                .PhaseSweepConfig
                .MeasurementConfig
                .Devices
                .E8257d
                .SetSourceDeltaPhase(sampleConfig.Phase);
            var newSample = Measurement.TakeSample(sampleConfig);
            samples.Add(newSample);
            return newSample;
            }

        public static void FindPeakOrTrough1(
            Mode searchMode,
            List<Sample> samples,
            Sample startBestSample,
            PhaseSweepConfig phaseSweepConfig,
            PhaseSearchPointConfig searchPtConfig)
            {
            SampleConfig makeSampleConfig(double phase)
            {
                return new SampleConfig(phaseSweepConfig, phase);
            };
            bool continueSearch(Sample best, Sample current)
                {
                var evaluation = EvaluateNewSample(
                    best, current, searchMode, searchPtConfig);
                return evaluation == LoopStatus.Continue;
                }


            var bestSample = startBestSample;
            var phaseStep = FindSearchDirection(
                searchMode,
                samples,
                bestSample,
                phaseSweepConfig,
                searchPtConfig,
                makeSampleConfig);

            // Loop until we've moved exitThreshold dB
            // away from the best found value
            var currentPhase = bestSample.Conf.Phase;
            var newSample = bestSample;
            while (continueSearch(bestSample, newSample))
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
            Mode searchMode,
            List<Sample> samples,
            Sample bestSample,
            PhaseSweepConfig phaseSweepConfig,
            PhaseSearchPointConfig phasePtConfig,
            Func<double, SampleConfig> makeSampleConfig)
            {
            var corePhase = bestSample.Conf.Phase;

            Gradient getNewSampleGradient(double stepScale)
                {
                var newPhase = corePhase + (stepScale * phasePtConfig.StepDeg);
                var newConfig = makeSampleConfig(newPhase);
                var newSample = TakeSample(newConfig, samples);
                return GetGradient(bestSample, newSample);
                }

            double scalar = 0.0;
            while (true)
                {
                scalar += 1.0;
                var gradientPos = getNewSampleGradient(scalar);
                var gradientNeg = getNewSampleGradient(scalar * -1);

                // If both adjacent points move in the same direction
                // then there's no clear direction to move in
                if (gradientNeg == gradientPos)
                    {
                    continue;
                    }

                if (searchMode == Mode.Peak)
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
            PhaseSearchPointConfig searchPointConfig)
            {
            var newPwr = newSample.MeasuredChannelPowerdBm;
            var bestPwr = bestSample.MeasuredChannelPowerdBm;
            var peakContinue = (searchMode == Mode.Peak)
                && ((bestPwr - newPwr) < searchPointConfig.ThresholddB);
            var troughContinue = (searchMode == Mode.Trough)
                && ((newPwr - bestPwr) < searchPointConfig.ThresholddB);
            var continueMeasure = peakContinue || troughContinue;
            return continueMeasure ?
                LoopStatus.Continue : LoopStatus.Stop;
            }

        // Version 2
        private class SamplePair
            {
            public Sample Sample1;
            public Sample Sample2;
            public double Gradient;
            public SamplePair(Sample s1, Sample s2)
                {
                this.Sample1 = s1;
                this.Sample2 = s2;
                this.Gradient = 
                    s2.MeasuredChannelPowerdBm / s1.MeasuredChannelPowerdBm;
                }
            }

        private static Direction GetDirection(SamplePair pair)
            {
            var sign = Math.Sign(pair.Gradient);
            switch (sign)
                {
                case 1:
                    return Direction.Negative;
                case -1:
                    return Direction.Positive;
                default:
                    // Unlikely to happen
                    return Direction.Stop;
                }
            }

        // Do coarse sweep
        // Find a pair with the highest gradient whose adjacent pairs
        // have the same graient
        // Sweep in the direction of the gradient to find the null
        // until it inverts.
        // When it does, sweep around the lowest point
        // Take the lowest point and shift the phase by 180 degrees
        // Sweep around that point and we should find a max value
        public static void FindPeakAndTrough(
            List<Sample> samples,
            PhaseSweepConfig phaseSweepConfig)
            {
            var samplePairs = new List<SamplePair>();
            for(int i = 0; i < samples.Count; i++)
                {
                if (i+1 == samples.Count)
                    {
                    break;
                    }
                samplePairs.Add(
                    new SamplePair(samples[i], samples[i + 1]));
                }

            var validPairs = new List<SamplePair>();
            Func<SamplePair, int> getSign = x => Math.Sign(x.Gradient);
            for (int i = 1; i < samplePairs.Count-1; i++)
                {
                var pairs = 
                    (samplePairs[i - 1], samplePairs[i], samplePairs[i + 1]);
                var sameGradientSign =
                    (getSign(pairs.Item1) == getSign(pairs.Item2))
                     && (getSign(pairs.Item2) == getSign(pairs.Item3));
                if (sameGradientSign) 
                    {
                    validPairs.Add(pairs.Item2);
                    }
                }
            var sortedPairs = 
                validPairs.OrderByDescending(p => p.Gradient).ToList();
            var bestPair = sortedPairs.First();
            var startingGradientSign = getSign(bestPair);
            var direction = GetDirection(bestPair);

            if (direction == Direction.Stop)
                {
                return;
                }
            var directionPos = (direction == Direction.Positive);
            var stepSign = directionPos ? 1.0 : -1.0;
            var coarseStep = stepSign * 1.0;
            double currentPhase;
            Sample newSample;
            if (directionPos)
                {
                newSample = bestPair.Sample2;
                currentPhase = newSample.Conf.Phase;
                }
            else
                {
                newSample = bestPair.Sample1;
                currentPhase = newSample.Conf.Phase;
                }

            // Sweep until the gradient inverts
            while (true)
                {
                var oldSample = newSample;
                currentPhase += coarseStep;
                var sampleConfig = new SampleConfig(
                    phaseSweepConfig, currentPhase);
                newSample = TakeSample(sampleConfig, samples);
                var currentPair = new SamplePair(oldSample, newSample);
                if(getSign(currentPair) != startingGradientSign)
                    {
                    break;
                    }
                }

            // Sweep around the best point
            var lowestPowerSample = samples.OrderByDescending(
                s => s.MeasuredChannelPowerdBm).ToList().Last();
            const double fineStep = 0.1;
            var startingPhase = lowestPowerSample.Conf.Phase - 5.0;
            var numSamples = 10.0 / fineStep;
            for (int i = 0; i < numSamples; i++)
                {
                currentPhase = ((double)i * fineStep) + startingPhase;
                var sampleConfig = new SampleConfig(
                    phaseSweepConfig, currentPhase);
                newSample = TakeSample(sampleConfig, samples);
                }

            // Take the new lowest and sweep 180 away to get the highest power
            lowestPowerSample = samples.OrderByDescending(
                s => s.MeasuredChannelPowerdBm).ToList().Last();
            const double powStep = 1;
            var highPowerPhaseStart = 
                lowestPowerSample.Conf.Phase + 180.0 - (5*powStep);
            for (int i = 0; i < 10; i++)
                {
                currentPhase = ((double)i * powStep) + highPowerPhaseStart;
                var sampleConfig = new SampleConfig(
                    phaseSweepConfig, currentPhase);
                newSample = TakeSample(sampleConfig, samples);
                }
            }
        }
    }

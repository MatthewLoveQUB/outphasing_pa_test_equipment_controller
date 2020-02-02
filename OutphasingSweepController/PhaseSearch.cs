using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    public static class PhaseSearch
        {
        public enum SearchType
            {
            None,
            LowestValue,
            HighestGradient
            }

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
            Negative,
            None
            }

        public enum Direction
            {
            Positive,
            Negative,
            CentreSweep
            }

        public static Sample GetBestSample(List<Sample> samples, Mode mode)
            {
            var orderedSamples =
                        samples.OrderByDescending(
                            s => s.MeasuredChannelPowerdBm).ToList();
            return (mode == Mode.Peak)
                ? orderedSamples.First()
                : orderedSamples.Last();
            }

        public static List<Sample> MeasurementPhaseSweep(
            PhaseSweepConfig phaseSweepConfig)
            {
            var samples = new List<Sample>();
            BasicPhaseSweep(samples, phaseSweepConfig);

            var searchType = phaseSweepConfig
                                .MeasurementConfig
                                .PhaseSearchSettings
                                .PhaseSearchType;

            if (searchType == SearchType.None)
                {
                return samples;
                }
            if (searchType == SearchType.HighestGradient)
                {
                FindPeakAndTrough(samples, phaseSweepConfig);
                }
            if (searchType == SearchType.LowestValue)
                {
                void DoSearch(
                        List<PhaseSearchPointConfig> searchSettings,
                        Mode mode)
                    {
                    foreach (var searchSetting in searchSettings)
                        {
                        var bestSample = GetBestSample(samples, mode);
                        FindPeakOrTrough(
                            mode,
                            samples,
                            bestSample,
                            phaseSweepConfig,
                            searchSetting);
                        }
                    }

                DoSearch(
                    phaseSweepConfig
                        .MeasurementConfig
                        .PhaseSearchSettings
                        .LowerValue
                        .Peak,
                    Mode.Peak);
                DoSearch(
                    phaseSweepConfig
                        .MeasurementConfig
                        .PhaseSearchSettings
                        .LowerValue
                        .Trough,
                    Mode.Trough);
                }

            return samples.OrderByDescending(s => s.Conf.Phase).ToList();
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
                .MeasurementConfig
                .Commands
                .SetPhase(sampleConfig.Phase);

            var newSample = Measurement.TakeSample(sampleConfig);
            samples.Add(newSample);
            return newSample;
            }

        public static void FindPeakOrTrough(
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

            var bestSample = startBestSample;
            var searchDirection = FindSearchDirection(
                searchMode,
                samples,
                bestSample,
                phaseSweepConfig,
                searchPtConfig,
                makeSampleConfig);

            // If no definite direction is found then
            // assume we're very close to the peak or trough
            if (searchDirection == Direction.CentreSweep)
                {
                double numSteps = phaseSweepConfig
                    .MeasurementConfig
                    .PhaseSearchSettings
                    .LowerValue
                    .PhaseSearchNumCenterSamples;
                var centerPhase = 
                    GetBestSample(samples, searchMode).Conf.Phase;
                var centreSweepPhaseStep = searchPtConfig.StepDeg;
                var startPhase = 
                    centerPhase - (numSteps/2.0 * centreSweepPhaseStep);
                for (int i = 0; i < numSteps; i++)
                    {
                    var curPhase = startPhase + (i * centreSweepPhaseStep);
                    var newConfig = makeSampleConfig(curPhase);
                    TakeSample(newConfig, samples);
                    }
                return;
                }

            // If the direction is negative then invert the phase step
            var phaseStep = searchPtConfig.StepDeg;
            if (searchDirection == Direction.Negative)
                {
                phaseStep *= -1;
                }            

            // Loop until we've moved exitThreshold dB
            // away from the best found value
            var currentPhase = bestSample.Conf.Phase;
            var newSample = bestSample;
            var searchSampleLimit = phaseSweepConfig
                                        .MeasurementConfig
                                        .PhaseSearchSettings
                                        .LowerValue
                                        .DirectionSearchIterationLimit;

            bool continueSearch(Sample best, Sample current)
                {
                var evaluation = EvaluateNewSample(
                    best, current, searchMode, searchPtConfig);
                return evaluation == LoopStatus.Continue;
                }

            var numSamples = 0;
            while (continueSearch(bestSample, newSample))
                {
                numSamples++;
                if(numSamples > searchSampleLimit)
                    {
                    break;
                    }
                currentPhase += phaseStep;
                var sampleConfig = makeSampleConfig(currentPhase);
                newSample = TakeSample(sampleConfig, samples);
                var newResult = PeakTroughComparison(
                    searchMode, bestSample, newSample);
                if (newResult == NewSampleResult.Better)
                    {
                    bestSample = newSample;
                    }
                }
            }
        
        public static Direction FindSearchDirection(
            Mode searchMode,
            List<Sample> samples,
            Sample bestSample,
            PhaseSweepConfig phaseSweepConfig,
            PhaseSearchPointConfig phasePtConfig,
            Func<double, SampleConfig> makeSampleConfig)
            {
            var corePhase = bestSample.Conf.Phase;

            Gradient getNewSampleGradient(double phaseStepScalar)
                {
                var phaseStep = phaseStepScalar * phasePtConfig.StepDeg;
                var newPhase = corePhase + phaseStep;
                var newConfig = makeSampleConfig(newPhase);
                var newSample = TakeSample(newConfig, samples);
                return GetGradient(bestSample, newSample);
                }

            // Find two points adjacent to the best position
            // If we're on a line they should have the same gradient
            // which should tell us where to search to find the 
            // minima or maxima
            // Due to measurement noise, the search is done multiple times
            // If it never finds a definite gradient then assume we're
            // close to the minima/maxima
            var searchIterationLimit = phaseSweepConfig
                                                .MeasurementConfig
                                                .PhaseSearchSettings
                                                .LowerValue
                                                .DirectionSearchIterationLimit;
            int iterations = 0;
            double scalar = 0.0;
            while (true)
                {
                iterations++;
                scalar += 1.0;
                var gradientRightSample = getNewSampleGradient(scalar);
                var gradientLeftSample = getNewSampleGradient(scalar * -1);
                
                if (gradientLeftSample == gradientRightSample)
                    {
                    if(iterations > searchIterationLimit)
                        {
                        return Direction.CentreSweep;
                        }
                    continue;
                    }
                if (searchMode == Mode.Peak)
                    {
                    return (gradientRightSample == Gradient.Positive) 
                        ? Direction.Positive 
                        : Direction.Negative;
                    }
                else
                    {
                    return (gradientRightSample == Gradient.Negative)
                        ? Direction.Positive
                        : Direction.Negative;
                    }
                }
            }

        public static Gradient GetGradient(
            Sample sampleRef,
            Sample sampleNew)
            {
            return new SamplePair(sampleRef, sampleNew).GradientDirection; 
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
        public class SamplePair
            {
            public Sample Sample1;
            public Sample Sample2;
            public double PowerGradient;
            public Gradient GradientDirection
                {
                get
                    {
                    var gradientSign = Math.Sign(this.PowerGradient);
                    if (gradientSign == 1)
                        {
                        return Gradient.Positive;
                        }
                    if (gradientSign == -1)
                        {
                        return Gradient.Negative;
                        }
                    return Gradient.None;
                    }
                }


            public SamplePair(Sample s1, Sample s2)
                {
                this.Sample1 = s1;
                this.Sample2 = s2;
                var pow2 = s2.MeasuredChannelPowerdBm;
                var pow1 = s1.MeasuredChannelPowerdBm;
                var phase2 = s2.Conf.Phase;
                var phase1 = s1.Conf.Phase;
                this.PowerGradient = (pow2 - pow1) / (phase2 - phase1);
                }
            }

        private static Direction GetDirection(SamplePair pair)
            {
            var sign = Math.Sign(pair.PowerGradient);
            switch (sign)
                {
                case 1:
                    return Direction.Negative;
                case -1:
                    return Direction.Positive;
                default:
                    // Unlikely to happen
                    return Direction.CentreSweep;
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
                if (i+1 == samples.Count) { break; }
                samplePairs.Add(new SamplePair(samples[i], samples[i + 1]));
                }

            var validPairs = new List<SamplePair>();
            Func<SamplePair, int> getSign = x => Math.Sign(x.PowerGradient);
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
                validPairs.OrderByDescending(p => Math.Abs(p.PowerGradient)).ToList();
            var bestPair = sortedPairs.First();
            var startingGradientSign = getSign(bestPair);
            var direction = GetDirection(bestPair);

            if (direction == Direction.CentreSweep)
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
                var currentPair = directionPos
                    ? new SamplePair(oldSample, newSample)
                    : new SamplePair(newSample, oldSample);
                if (getSign(currentPair) != startingGradientSign)
                    {
                    break;
                    }
                }

            // Sweep around the best point
            var lowestPowerSample = samples.OrderByDescending(
                s => s.MeasuredChannelPowerdBm).ToList().Last();
            const double fineStep = 0.1;
            var startingPhase = lowestPowerSample.Conf.Phase - 2.0;
            var numSamples = 4 / fineStep;
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

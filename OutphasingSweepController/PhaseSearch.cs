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

        public static void FindPeakOrTrough(
            Mode searchMode,
            List<Sample> samples,
            Sample startBestSample,
            PhaseSweepConfig phaseSweepConfig,
            PhaseSearchPointConfig searchPtConfig,
            ref SweepProgress sweepProgress)
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
                makeSampleConfig,
                ref sweepProgress);

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
                    PhaseSweep.GetBestSample(samples, searchMode).Conf.Phase;
                var centreSweepPhaseStep = searchPtConfig.StepDeg;
                var startPhase = 
                    centerPhase - (numSteps/2.0 * centreSweepPhaseStep);
                for (int i = 0; i < numSteps; i++)
                    {
                    var curPhase = startPhase + (i * centreSweepPhaseStep);
                    var newConfig = makeSampleConfig(curPhase);
                    PhaseSweep.TakeSample(
                        newConfig, samples, ref sweepProgress);
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
                newSample = PhaseSweep.TakeSample(
                    sampleConfig, samples, ref sweepProgress);
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
            Func<double, SampleConfig> makeSampleConfig,
            ref SweepProgress sweepProgress)
            {
            var corePhase = bestSample.Conf.Phase;

            Gradient getNewSampleGradient(
                    double phaseStepScalar, 
                    bool leftPoint, 
                    ref SweepProgress sp)
                {
                var sign = leftPoint ? -1.0 : 1.0;
                var phaseStep = 
                    phaseStepScalar * phasePtConfig.StepDeg * sign;
                var newPhase = corePhase + phaseStep;
                var newConfig = makeSampleConfig(newPhase);
                var newSample = PhaseSweep.TakeSample(
                    newConfig, samples, ref sp);
                return leftPoint
                    ? GetGradient(from: newSample, to:bestSample)
                    : GetGradient(from: bestSample, to:newSample);
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
                var gradientToRightSample = 
                    getNewSampleGradient(
                        scalar, 
                        leftPoint: false, 
                        sp: ref sweepProgress);
                var gradientFromLeftSample = 
                    getNewSampleGradient(
                        scalar, 
                        leftPoint: true, 
                        sp: ref sweepProgress);
                
                if (gradientFromLeftSample != gradientToRightSample)
                    {
                    if(iterations > searchIterationLimit)
                        {
                        return Direction.CentreSweep;
                        }
                    continue;
                    }
                if (searchMode == Mode.Peak)
                    {
                    return (gradientToRightSample == Gradient.Positive) 
                        ? Direction.Positive 
                        : Direction.Negative;
                    }
                else
                    {
                    return (gradientToRightSample == Gradient.Negative)
                        ? Direction.Positive
                        : Direction.Negative;
                    }
                }
            }

        public static Gradient GetGradient(
            Sample from,
            Sample to)
            {
            return new SamplePair(from, to).GradientDirection; 
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
            var sign = pair.GradientDirection;
            switch (sign)
                {
                case Gradient.Positive:
                    return Direction.Negative;
                case Gradient.Negative:
                    return Direction.Positive;
                case Gradient.None:
                default:
                    // Unlikely to happen
                    return Direction.CentreSweep;
                }
            }

        // Do coarse sweep
        // Find a pair with the highest gradient whose adjacent pairs
        // have the same gradient
        // Sweep in the direction of the gradient to find the null
        // until it inverts.
        // When it does, sweep around the lowest point
        // Take the lowest point and shift the phase by 180 degrees
        // Sweep around that point and we should find a max value
        public static void FindPeakAndTrough(
            List<Sample> samples,
            PhaseSweepConfig phaseSweepConfig,
            ref SweepProgress sweepProgress)
            {

            var config = 
                phaseSweepConfig
                    .MeasurementConfig
                    .PhaseSearchSettings
                    .GradientSearch;

            // Put the samples into pairs
            var samplePairs = new List<SamplePair>();
            for(int i = 0; i < (samples.Count-1); i++)
                {
                samplePairs.Add(new SamplePair(samples[i], samples[i + 1]));
                }

            // Find the pairs of adjacent points that share the same gradient
            // as the adjacent points
            // e.g. for pair (x1,x2), also check (x0,x1) and (x2,x3)
            var validPairs = new List<SamplePair>();
            for (int i = 1; i < (samplePairs.Count - 1); i++)
                {
                var curPair = samplePairs[i];
                var prevPair = samplePairs[i - 1];
                var nextPair = samplePairs[i + 1];

                var sameGradientSign =
                    (curPair.GradientDirection == prevPair.GradientDirection)
                     && (curPair.GradientDirection 
                        == nextPair.GradientDirection);
                if (sameGradientSign) 
                    {
                    validPairs.Add(curPair);
                    }
                }

            // Find the pair with the steepest gradient
            // This should correspond to the pair closest to the power null
            // If no pairs have the same gradient then just give up
            var sortedPairs = 
                validPairs
                    .OrderByDescending(p => Math.Abs(p.PowerGradient))
                    .ToList();
            if(sortedPairs == null)
                {
                return;
                }
            if(sortedPairs.Count < 1)
                {
                return;
                }


            var bestPair = sortedPairs.First();
            var startingGradientSign = bestPair.GradientDirection;
            var direction = GetDirection(bestPair);

            var coarseStepCore = config.MinimaCoarseStep;
            var directionPos = (direction == Direction.Positive);
            var coarseStep = directionPos ? coarseStepCore : -coarseStepCore;

            Sample newSample =
                directionPos ? bestPair.Sample2 : bestPair.Sample1;
            double currentPhase = newSample.Conf.Phase;

            var searchSampleLimit = phaseSweepConfig
                                        .MeasurementConfig
                                        .PhaseSearchSettings
                                        .LowerValue
                                        .DirectionSearchIterationLimit;
            int nIterations = 0;
            // Sweep until the gradient inverts
            while (true)
                {
                nIterations++;
                var oldSample = newSample;
                currentPhase += coarseStep;
                var sampleConfig = new SampleConfig(
                    phaseSweepConfig, currentPhase);
                newSample = PhaseSweep.TakeSample(
                    sampleConfig, samples, ref sweepProgress);
                var currentPair = directionPos
                    ? new SamplePair(oldSample, newSample)
                    : new SamplePair(newSample, oldSample);
                if (currentPair.GradientDirection != startingGradientSign)
                    {
                    break;
                    }

                if(nIterations > searchSampleLimit)
                    {
                    return;
                    }
                }

            // Sweep around the best point
            var lowestPowerSample = 
                samples
                    .OrderByDescending(s => s.MeasuredChannelPowerdBm)
                    .ToList()
                    .Last();
            double fineStep = config.MinimaFineStep;
            int numSamples = config.MinimaNumFineSteps;
            var startDelta = fineStep * (numSamples / 2);
            var startingPhase = lowestPowerSample.Conf.Phase - startDelta;
            for (int i = 0; i < numSamples; i++)
                {
                currentPhase = startingPhase + ((double)i * fineStep);
                var sampleConfig = new SampleConfig(
                    phaseSweepConfig, currentPhase);
                newSample = PhaseSweep.TakeSample(
                    sampleConfig, samples, ref sweepProgress);
                }

            // Take the new lowest and sweep 180 away to get the highest power
            lowestPowerSample = 
                samples
                    .OrderByDescending(s => s.MeasuredChannelPowerdBm)
                    .ToList()
                    .Last();
            double maximaSweepStep = config.MaximaCoarseStep;
            int maximaNumSteps = config.MaximaNumCoarseSteps;
            var maximaStartDelta = (maximaNumSteps / 2) * maximaSweepStep;
            var maximaPhaseStart = lowestPowerSample.Conf.Phase - maximaStartDelta;

            if (lowestPowerSample.Conf.Phase > 180)
                {
                maximaPhaseStart -= 180.0;
                }
            else
                {
                maximaPhaseStart += 180.0;
                }

            for (int i = 0; i < maximaNumSteps; i++)
                {
                currentPhase = 
                    maximaPhaseStart + ((double)i * maximaSweepStep);
                var sampleConfig = new SampleConfig(
                    phaseSweepConfig, currentPhase);
                newSample = PhaseSweep.TakeSample(
                    sampleConfig, samples, ref sweepProgress);
                }
            }
        }
    }

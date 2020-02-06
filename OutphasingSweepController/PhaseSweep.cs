using System;
using System.Collections.Generic;
using System.Linq;

namespace OutphasingSweepController
    {
    public static class PhaseSweep
        {
        public static List<Sample> MeasurementPhaseSweep(
            PhaseSweepConfig phaseSweepConfig,
            ref SweepProgress sweepProgress)
            {
            var samples = BasicPhaseSweep(
                phaseSweepConfig, ref sweepProgress);

            var searchType = phaseSweepConfig
                                .MeasurementConfig
                                .PhaseSearchSettings
                                .PhaseSearchType;

            if (searchType == PhaseSearch.SearchType.None)
                {
                return samples;
                }
            else if (searchType == PhaseSearch.SearchType.HighestGradient)
                {
                PhaseSearch.FindPeakAndTrough(
                    samples, phaseSweepConfig, ref sweepProgress);
                }
            else if (searchType == PhaseSearch.SearchType.LowestValue)
                {
                void DoSearch(
                        List<PhaseSearchPointConfig> searchSettings,
                        PhaseSearch.Mode mode,
                        ref SweepProgress sp)
                    {
                    foreach (var searchSetting in searchSettings)
                        {
                        var bestSample = GetBestSample(samples, mode);
                        PhaseSearch.FindPeakOrTrough(
                            mode,
                            samples,
                            bestSample,
                            phaseSweepConfig,
                            searchSetting,
                            ref sp);
                        }
                    }

                DoSearch(
                    phaseSweepConfig
                        .MeasurementConfig
                        .PhaseSearchSettings
                        .LowerValue
                        .Peak,
                    PhaseSearch.Mode.Peak,
                    ref sweepProgress);
                DoSearch(
                    phaseSweepConfig
                        .MeasurementConfig
                        .PhaseSearchSettings
                        .LowerValue
                        .Trough,
                    PhaseSearch.Mode.Trough,
                    ref sweepProgress);
                }
            else
                {
                throw new Exception();
                }

            return samples.OrderByDescending(s => s.Conf.Phase).ToList();
            }

        public static List<Sample> BasicPhaseSweep(
            PhaseSweepConfig phaseSweepConfig,
            ref SweepProgress sweepProgress)
            {
            var samples = new List<Sample>();
            foreach (var phase in phaseSweepConfig.MeasurementConfig.Phases)
                {
                var sampleConfig = new SampleConfig(
                    phaseSweepConfig,
                    phase);
                TakeSample(sampleConfig, samples, ref sweepProgress);
                }
            return samples;
            }

        public static Sample TakeSample(
            SampleConfig sampleConfig,
            List<Sample> samples,
            ref SweepProgress sweepProgress)
            {
            sampleConfig
                .MeasurementConfig
                .Commands
                .SetPhase(sampleConfig.Phase);

            var newSample = Measurement.TakeSample(sampleConfig);
            samples.Add(newSample);
            sweepProgress.CurrentPoint++;
            return newSample;
            }

        public static Sample GetBestSample(
                List<Sample> samples, 
                PhaseSearch.Mode mode)
            {
            var orderedSamples =
                        samples.OrderByDescending(
                            s => s.MeasuredChannelPowerdBm).ToList();
            return (mode == PhaseSearch.Mode.Peak)
                ? orderedSamples.First()
                : orderedSamples.Last();
            }
        }
    }

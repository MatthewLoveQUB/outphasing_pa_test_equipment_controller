using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    public class LowerValueConfig
        {
        public List<PhaseSearchPointConfig> Peak;
        public List<PhaseSearchPointConfig> Trough;
        public int DirectionSearchIterationLimit;
        public int SearchIterationLimit;
        public int PhaseSearchNumCenterSamples;

        public LowerValueConfig(
                string peakSettings,
                string troughSettings,
                int directionSearchIterationLimit,
                int searchIterationLimit,
                int phaseSearchNumCenterSamples)
            {
            this.Peak = this.ParseInput(peakSettings);
            this.Trough = this.ParseInput(troughSettings);
            this.DirectionSearchIterationLimit = 
                directionSearchIterationLimit;
            this.SearchIterationLimit = searchIterationLimit;
            this.PhaseSearchNumCenterSamples =
                phaseSearchNumCenterSamples;
            }

        private List<PhaseSearchPointConfig> ParseInput(string settingsInput)
            {
            var settings = new List<PhaseSearchPointConfig>();
            var splitInputs = settingsInput
                .Split(';')
                .Where(s => s != "")
                .ToList();
            foreach (var input in splitInputs)
                {
                var splitSetting = input
                    .Split(',')
                    .Select(x => Convert.ToDouble(x))
                    .ToList();
                var newSetting = new PhaseSearchPointConfig(
                    splitSetting[0], splitSetting[1]);
                settings.Add(newSetting);
                }
            return settings;
            }
        }
    
    public class GradientSearchConfig
        {
        public double MinimaCoarseStep { get; set; } = 1;
        public double MinimaFineStep { get; set; } = 0.1;
        public int MinimaNumFineSteps { get; set; } = 10;
        public double MaximaCoarseStep { get; set; } = 2;
        public int MaximaNumCoarseSteps { get; set; } = 10;
        
        public GradientSearchConfig(
                double minimaCoarseStep,
                double minimaFineStep,
                int minimaNumFineSteps,
                double maximaCoarseStep,
                int maximaNumCoarseSteps)
            {
            this.MinimaCoarseStep = minimaCoarseStep;
            this.MinimaFineStep = minimaFineStep;
            this.MinimaNumFineSteps = minimaNumFineSteps;
            this.MaximaCoarseStep = maximaCoarseStep;
            this.MaximaNumCoarseSteps = maximaNumCoarseSteps;
            }
        }

    public class PhaseSearchConfig
        {
        public PhaseSearch.SearchType PhaseSearchType;
        public LowerValueConfig LowerValue;
        public GradientSearchConfig GradientSearch;

        public PhaseSearchConfig(
                PhaseSearch.SearchType phaseSearchType,
                string peakSettings,
                string troughSettings,
                int directionSearchIterationLimit,
                int searchIterationLimit,
                int phaseSearchNumCenterSamples,
                double minimaCoarseStep,
                double minimaFineStep,
                int minimaNumFineSteps,
                double maximaCoarseStep,
                int maximaNumCoarseSteps)
            {
            this.PhaseSearchType = phaseSearchType;
            this.LowerValue = new LowerValueConfig(
                peakSettings,
                troughSettings,
                directionSearchIterationLimit,
                searchIterationLimit,
                phaseSearchNumCenterSamples);
            this.GradientSearch = new GradientSearchConfig(
                minimaCoarseStep,
                minimaFineStep,
                minimaNumFineSteps,
                maximaCoarseStep,
                maximaNumCoarseSteps);
            }
        }     
    }

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    public class GradientSearchSettings
        {
        public double MinimaCoarseStep { get; set; }
        public double MinimaFineStep { get; set; }
        public int MinimaNumFineSteps { get; set; }
        public double MaximaCoarseStep { get; set; }
        public int MaximaNumCoarseSteps { get; set; }

        public GradientSearchSettings(
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
    }

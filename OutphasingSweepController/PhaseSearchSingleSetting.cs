using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    class PhaseSearchSingleSetting
        {
        public double StepDeg;
        public double ThresholddB;
        public PhaseSearchSingleSetting(double stepDeg, double thresholddB)
            {
            this.StepDeg = stepDeg;
            this.ThresholddB = thresholddB;
            }
        }
    }

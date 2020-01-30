using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    public class PhaseSearchPointConfig
        {
        public double StepDeg;
        public double ThresholddB;
        public PhaseSearchPointConfig(double stepDeg, double thresholddB)
            {
            this.StepDeg = stepDeg;
            this.ThresholddB = thresholddB;
            }
        }
    }

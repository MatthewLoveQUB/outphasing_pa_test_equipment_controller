using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    class SweepSettings
        {
        public double Start;
        public double Step;
        public double Stop;
        public SweepSettings(double start, double step, double stop)
            {
            Start = start;
            Step = step;
            Stop = stop;
            }
        }
    }

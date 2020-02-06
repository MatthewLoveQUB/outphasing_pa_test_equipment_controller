using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    public class SweepProgress
        {
        public bool Running;
        public int CurrentPoint;
        public SweepProgress(bool running, int currentPoint)
            {
            this.Running = running;
            this.CurrentPoint = currentPoint;
            }
        }
    }

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    class SweepProgress
        {
        public bool Running;
        public int CurrentPoint;
        public int NumberOfPoints;
        public SweepProgress(bool running, int currentPoint, int numberOfPoints)
            {
            Running = running;
            CurrentPoint = currentPoint;
            NumberOfPoints = numberOfPoints;
            }
        }
    }

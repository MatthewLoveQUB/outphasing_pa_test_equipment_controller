using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QubVisa
    {
    public class AmplitudeOffset
        {
        public double Frequency;
        public double OffsetdB;

        public AmplitudeOffset(double frequency, double offset)
            {
            Frequency = frequency;
            OffsetdB = offset;
            }
        }
    }

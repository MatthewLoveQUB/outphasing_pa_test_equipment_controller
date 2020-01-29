namespace QubVisa
    {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class AmplitudeOffset
        {
        public double Frequency;
        public double OffsetdB;

        public AmplitudeOffset(double frequency, double offset)
            {
            this.Frequency = frequency;
            this.OffsetdB = offset;
            }
        }
    }

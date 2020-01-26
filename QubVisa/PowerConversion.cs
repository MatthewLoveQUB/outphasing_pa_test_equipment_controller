using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QubVisa
    {
    public static class PowerConversion
        {
        public static double dBmToWatts(double powerDbm)
            {
            return (1e-3) * Math.Pow(10, powerDbm / 10);
            }

        public static double WattsTodBm(double powerWatts)
            {
            return 10.0 * Math.Log10(1000 * powerWatts);
            }
        }
    }

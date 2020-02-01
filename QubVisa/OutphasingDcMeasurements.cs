using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QubVisa
    {
    public class OutphasingDcMeasurements
        {
        public double PowerWatts
            {
            get
                {
                return this.Currents.Select(c => c * this.Voltage).Sum();
                }
            }
        public List<double> Currents;
        public double Voltage;

        public OutphasingDcMeasurements(double voltage, List<double> currents)
            {
            this.Voltage = voltage;
            this.Currents = currents;
            }
        }
    }

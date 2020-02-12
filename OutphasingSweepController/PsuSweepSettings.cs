using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    public class PsuSweepSettings
        {
        public bool Nominal { get; set; }
        public bool Plus10 { get; set; }
        public bool Minus10 { get; set; }

        public PsuSweepSettings(bool nom, bool plus, bool minus)
            {
            this.Nominal = nom;
            this.Plus10 = plus;
            this.Minus10 = minus;
            }
        }
    }
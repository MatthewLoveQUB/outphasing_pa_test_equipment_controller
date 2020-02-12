using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    public class PsuChannelSettings
        {
        public bool PsuChannel1On { get; set; }
        public bool PsuChannel2On { get; set; }
        public bool PsuChannel3On { get; set; }
        public bool PsuChannel4On { get; set; }
        public List<bool> All
            { get
                {
                return new List<bool>()
                    {
                    this.PsuChannel1On,
                    this.PsuChannel2On,
                    this.PsuChannel3On,
                    this.PsuChannel4On
                    };
                }
            }

        public PsuChannelSettings(
                bool ch1, bool ch2, bool ch3, bool ch4)
            {
            this.PsuChannel1On = ch1;
            this.PsuChannel2On = ch2;
            this.PsuChannel3On = ch3;
            this.PsuChannel4On = ch4;
            }
        }
    }

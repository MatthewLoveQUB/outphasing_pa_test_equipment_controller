using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    static class Constants
        {
        public const string LastSampleTemplate = @"Pout: {0} dBm
Frequency: {1} Hz
Pin: {2} dBm
Gain: {3} dB
PAE: {4} %
Drain Efficiency: {5} %
DC Power: {8} W";

        public const string LastSamplePsuChannelTemplate = @"PSU Channel {0}: {1} V, {2} A, {3} W";
        }
    }

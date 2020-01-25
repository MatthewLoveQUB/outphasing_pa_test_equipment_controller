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
DC Voltage: {6} V
DC Current: {7} A
DC Power: {8} W";

        public const string ConnectionStatusTemplate = @"HP6624: {0}
TektronixRSA3408A: {1}
R&S SMU200A: {2}
Keysight E8257D: {3}";
        }

        public const string LastSamplePsuChannelTemplate = @"PSU Channel {0}: {1} V, {2} A, {3} W";
    }

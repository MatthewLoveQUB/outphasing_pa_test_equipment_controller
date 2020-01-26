using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QubVisa
    {
    public class DeviceOffsets
        {
        public List<AmplitudeOffset> Offsets;
        // The offsets need to be sorted
        public DeviceOffsets(string offsetFilePath)
            {
            Offsets = new List<AmplitudeOffset>();
            var offsetFile = System.IO.File.OpenText(offsetFilePath);

            string line;
            while((line = offsetFile.ReadLine()) != null)
                {
                var values = line.Split('=');
                var frequency = Convert.ToDouble(values[0]);
                var offset = Convert.ToDouble(values[1]);
                var offsetValue = new AmplitudeOffset(frequency, offset);
                Offsets.Add(offsetValue);
                }
            }

        public double GetOffset(double frequency)
            {
            AmplitudeOffset previousOffset = null;
            AmplitudeOffset currentOffset = null;

            foreach(var offset in Offsets)
                {
                previousOffset = currentOffset;
                currentOffset = offset;

                if (previousOffset == null)
                    {
                    continue;
                    }
                if ((frequency > previousOffset.Frequency) && (frequency < currentOffset.Frequency))
                    {
                    var progressAlong = frequency - previousOffset.Frequency;
                    var totalDistance = currentOffset.Frequency - previousOffset.Frequency;
                    var offsetDifference = currentOffset.OffsetdB - previousOffset.OffsetdB;
                    var deltaOffset = (progressAlong / totalDistance) * offsetDifference;
                    return previousOffset.OffsetdB + deltaOffset;
                    }
                }
            return 0;
            }
        }
    }

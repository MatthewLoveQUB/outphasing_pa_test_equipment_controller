using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    public class PhaseSearchConfig
        {
        public List<PhaseSearchPointConfig> Peak;
        public List<PhaseSearchPointConfig> Trough;

        public PhaseSearchConfig(string peakSettings, string troughSettings)
            {
            this.Peak = this.ParseInput(peakSettings);
            this.Trough = this.ParseInput(troughSettings);
            }

        private List<PhaseSearchPointConfig> ParseInput(string settingsInput)
            {
            var settings = new List<PhaseSearchPointConfig>();
            var splitInputs = settingsInput
                .Split(';')
                .Where(s => s != "")
                .ToList();
            foreach (var input in splitInputs)
                {
                var splitSetting = input
                    .Split(',')
                    .Select(x => Convert.ToDouble(x))
                    .ToList();
                var newSetting = new PhaseSearchPointConfig(
                    splitSetting[0], splitSetting[1]);
                settings.Add(newSetting);
                }
            return settings;
            }
        }
    }

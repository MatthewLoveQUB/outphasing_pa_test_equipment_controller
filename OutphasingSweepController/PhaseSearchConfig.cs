using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    public class PhaseSearchConfig
        {
        public List<PhaseSearchSingleConfig> PeakSettings;
        public List<PhaseSearchSingleConfig> TroughSettings;

        public PhaseSearchConfig(string peakSettings, string troughSettings)
            {
            this.PeakSettings = this.ParseInput(peakSettings);
            this.TroughSettings = this.ParseInput(troughSettings);
            }

        private List<PhaseSearchSingleConfig> ParseInput(string settingsInput)
            {
            var settings = new List<PhaseSearchSingleConfig>();
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
                var newSetting = new PhaseSearchSingleConfig(
                    splitSetting[0], splitSetting[1]);
                settings.Add(newSetting);
                }
            return settings;
            }
        }
    }

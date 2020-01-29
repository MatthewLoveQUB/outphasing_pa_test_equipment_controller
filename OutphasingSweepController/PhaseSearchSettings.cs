using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutphasingSweepController
    {
    class PhaseSearchSettings
        {
        List<PhaseSearchSingleSetting> Settings;

        public PhaseSearchSettings(string settingsInput)
            {
            this.Settings = new List<PhaseSearchSingleSetting>();
            var splitInputs = settingsInput
                .Split(';')
                .Where(s => s != "")
                .ToList();
            foreach(var input in splitInputs)
                {
                var splitSetting = input
                    .Split(',')
                    .Select(x => Convert.ToDouble(x))
                    .ToList();
                var newSetting = new PhaseSearchSingleSetting(
                    splitSetting[0], splitSetting[1]);
                this.Settings.Add(newSetting);
                }
            }
        }
    }

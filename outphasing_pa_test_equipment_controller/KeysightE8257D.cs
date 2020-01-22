using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace outphasing_pa_test_equipment_controller
{
    class KeysightE8257D : list_visa_devices_dialogue.VisaDevice
        {
        public KeysightE8257D(string deviceAddress) : base(deviceAddress, "KeysightE8257D")
            {

            }
        // IEEE Common Commands
        public string GetId()
            {
            return ReadString("*IDN?");
            }
        }

}

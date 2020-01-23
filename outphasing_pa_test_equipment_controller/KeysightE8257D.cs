using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace outphasing_pa_test_equipment_controller
{
    public class KeysightE8257D
        {
        public list_visa_devices_dialogue.VisaDevice Device;
        public KeysightE8257D(string deviceAddress)
            {
            Device = new list_visa_devices_dialogue.VisaDevice(deviceAddress);
            }

        public KeysightE8257D(list_visa_devices_dialogue.VisaDevice device)
            {
            Device = device;
            }
        // IEEE Common Commands
        public string GetId()
            {
            return Device.ReadString("*IDN?");
            }
        }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace outphasing_pa_test_equipment_controller
{
    public class RS_SMU200A
    {
        public list_visa_devices_dialogue.VisaDevice Device;
        public RS_SMU200A(string deviceAddress)
            {
            Device = new list_visa_devices_dialogue.VisaDevice(deviceAddress);
            }

        public RS_SMU200A(list_visa_devices_dialogue.VisaDevice device)
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
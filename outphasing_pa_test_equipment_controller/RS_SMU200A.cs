using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace outphasing_pa_test_equipment_controller
{
    class RS_SMU200A : list_visa_devices_dialogue.VisaDevice
    {
        public RS_SMU200A(string deviceAddress) : base(deviceAddress, "R&S SMU200A")
            {

            }

        // IEEE Common Commands
        public string GetId()
            {
            return ReadString("*IDN?");
            }
        }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace outphasing_pa_test_equipment_controller
{
    class TektronixRSA3408A : list_visa_devices_dialogue.VisaDevice
    {
        public TektronixRSA3408A(string deviceAddress) : base(deviceAddress, "TektronixRSA3408A")
        {

        }
    }
}

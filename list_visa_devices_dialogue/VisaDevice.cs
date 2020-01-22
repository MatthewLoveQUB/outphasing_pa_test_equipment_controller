using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments.Visa;
using Ivi.Visa;

namespace list_visa_devices_dialogue
{
    public class VisaManager
    {
        ResourceManager rm;
        public VisaManager()
        {
            rm = new ResourceManager();
        }

        public List<string> GetAvailableDevices()
        {
            return rm.Find("?*").ToList();
        }
    }

    public class VisaDevice
    {
        public MessageBasedSession device;
        public readonly string name;

        public VisaDevice(string deviceAddress, string deviceName)
        {
            var rm = new ResourceManager();
            device = (MessageBasedSession)rm.Open(deviceAddress);
            name = deviceName;
        }

        /// <summary>
        /// Send a querying command and read the reply.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public string ReadString(string command, bool suppressError=true)
        {
            device.RawIO.Write(command);
            try
            {
                return device.RawIO.ReadString();
            }
            catch (Ivi.Visa.IOTimeoutException e)
            {
                if (suppressError)
                    {
                    return string.Format("Timeout Error: {0}", e.Message);
                    }
                else
                    {
                    throw;
                    }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments.Visa;
using Ivi.Visa;

namespace QubVisa
{
    public class VisaManager
    {
        ResourceManager rm;
        public VisaManager()
        {
            this.rm = new ResourceManager();
        }

        public List<string> GetAvailableDevices()
        {
            return this.rm.Find("?*").ToList();
        }
    }

    public class VisaDevice
    {
        public MessageBasedSession connection;
        public Action<string> Write;

        public VisaDevice(string deviceAddress)
        {
            var rm = new ResourceManager();
            this.connection = (MessageBasedSession)rm.Open(deviceAddress);
            this.Write = this.connection.RawIO.Write;
        }

        

        /// <summary>
        /// Send a querying command and read the reply.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public string ReadString(string command, bool suppressError=true)
        {
            this.connection.RawIO.Write(command);
            try
            {
                return this.connection.RawIO.ReadString();
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

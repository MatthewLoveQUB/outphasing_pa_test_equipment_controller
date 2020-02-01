using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QubVisa;

namespace OutphasingSweepController
    {
    public static class VisaSetup
        {
        private static string GetVisaAddress(string deviceName)
            {
            var visaWindow =
                new list_visa_devices_dialogue.MainWindow(deviceName);
            visaWindow.ShowDialog();
            return visaWindow.SelectedAddress;
            }

        public static Equipment SetUpConnections(List<bool> psuChannelStates)
            {
            string psuAddress;
            string spectrumAnalyzerAddress;
            string signalGen1Address;
            string signalGen2Address;
            // Hard-coded addresses for me to save time
            if (true)
                {
                // HP6624A
                psuAddress = "GPIB1::14::INSTR";
                // Tek RSA3408A
                spectrumAnalyzerAddress = "GPIB1::1::INSTR";
                //  R&S SMR20
                signalGen1Address = "GPIB0::28::INSTR";
                // Keysight E8257D
                signalGen2Address = "TCPIP0::192.168.1.3::inst1::INSTR";
                }
            else
                {
                psuAddress = GetVisaAddress("PSU");
                spectrumAnalyzerAddress = GetVisaAddress("Spectrum Analyzer");
                signalGen1Address = GetVisaAddress("Signal Generator 1");
                signalGen2Address = GetVisaAddress("Signal Generator 2");
                }

            var hp6624a = new HP6624A(psuAddress, psuChannelStates);
            var rsa3408a = new TektronixRSA3408A(spectrumAnalyzerAddress);
            var smr20 = new RS_SMR20(signalGen1Address);
            var e8257d = new KeysightE8257D(signalGen2Address);

            return new Equipment(hp6624a, rsa3408a, smr20, e8257d);
            }
        }
    }

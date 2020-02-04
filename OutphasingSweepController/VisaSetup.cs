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

        // This is the dirty method that is edited when
        // the equipment used in the program is changed
        public static DeviceCommands SetUpVisaDevices(
            List<bool> psuChannelStates,
            double currentLimit)
            {
            var devices = SetUpConnections(psuChannelStates);

            void setPow(double inputPower, double offset1, double offset2)
                {
                Task SetPowerLevel(
                    Action<double, double> setPower, double offset)
                    {
                    return Task.Factory.StartNew(
                        () => setPower(inputPower, offset));
                    }
                Task.WaitAll(new Task[]
                    {
                        SetPowerLevel(devices.Smr20.SetPowerLevel, offset1),
                        SetPowerLevel(devices.E8257d.SetPowerLevel, offset2)
                    });
                }

            void setRfOutputState(bool on)
                {
                devices.Smr20.SetRfOutputState(on);
                devices.E8257d.SetRfOutputState(on);
                }

            void setFrequency(double frequency)
                {
                Task SetFrequency(Action<double> setDeviceFrequency)
                    {
                    return Task.Factory.StartNew(
                        () => setDeviceFrequency(frequency));
                    }
                Task.WaitAll(new Task[]
                    {
                            SetFrequency(devices.Rsa3408a.SetFrequencyCenter),
                            SetFrequency(devices.Smr20.SetSourceFrequency),
                            SetFrequency(devices.E8257d.SetSourceFrequency)
                    });
                }

            List<bool> getChannelStates()
                {
                return devices.Hp6624a.ChannelStates;
                }

            void resetDevices()
                {
                devices.Rsa3408a.ResetDevice();
                devices.Smr20.ResetDevice();
                devices.E8257d.ResetDevice();
                
                while(!devices.Rsa3408a.OperationComplete())
                    { }
                devices.Rsa3408a.SetSpectrumChannelPowerMeasurementMode();
                devices.Rsa3408a.SetContinuousMode(continuousOn: false);
                devices.Rsa3408a.StartSignalAcquisition();
                }

            void preMeasurementSetup(MeasurementConfig sweepConf)
                {
                // DC supply
                var psu = devices.Hp6624a;
                psu.SetAllChannelVoltagesToZero();
                psu.SetChannelOutputStatesStrong();
                psu.SetActiveChannelsCurrent(currentLimit);

                // Spectrum Analyser
                var rsa = devices.Rsa3408a;
                
                rsa.SetFrequencyCenter(sweepConf.Frequencies[0]);
                rsa.SetFrequencySpan(sweepConf.MeasurementFrequencySpan);
                rsa.SetChannelBandwidth(sweepConf.MeasurementChannelBandwidth);

                rsa.SetMarkerState(markerNumber: 1, view: 1, on: true);
                rsa.SetMarkerXToPositionMode(1,1);
                // When the frequency changes, the marker should automatially
                // track to the new centre frequency
                rsa.SetMarkerXValue(
                    markerNumber: 1, 
                    view: 1, 
                    xValue: sweepConf.Frequencies[0]);
                // Set the power sources to an extremely low
                // power before starting the sweep
                // in case they default to some massive value
                sweepConf.Commands.SetRfOutputState(on: false);
                sweepConf.Commands.SetInputPower(-60, offset1: 0, offset2: 0);
                sweepConf.Commands.SetRfOutputState(on: true);
                }

            bool operationsComplete()
                {
                return devices.Rsa3408a.OperationComplete()
                    && devices.E8257d.OperationComplete();
                }

            double GetMarkerPower(int marker)
                {
                return devices
                    .Rsa3408a
                    .GetMarkerYValue(markerNumber: marker, view: 1);
                }

            return new DeviceCommands(
                setPow,
                setRfOutputState,
                setFrequency,
                devices.E8257d.SetSourceDeltaPhase,
                getChannelStates,
                resetDevices,
                preMeasurementSetup,
                devices.Hp6624a.SetPsuVoltageStepped,
                devices.Hp6624a.OutphasingOptimisedMeasurement,
                devices.Rsa3408a.ReadSpectrumChannelPower,
                operationsComplete,
                GetMarkerPower);
            }
        }
    }

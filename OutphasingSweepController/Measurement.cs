using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using QubVisa;

namespace OutphasingSweepController
    {
    // Write a sample to a file stream
    static class Measurement
        {
        static public Sample TakeSample(SampleConfig conf)
            {
            double channelPowerdBm = -1;
            double measuredPoutdBm = -1;
            OutphasingDcMeasurements dcResults = null;
            var commands = conf.MeasurementConfig.Commands;

            // Make sure all commands are settled first
            while (!commands.OperationsComplete())
                {

                }

            Task.WaitAll(new Task[]
                {
                Task.Factory.StartNew(() =>
                {
                    channelPowerdBm = commands.GetSpectrumPower();
                    measuredPoutdBm = commands.GetMarkerPower(marker: 1);
                    //measuredPoutdBm = channelPowerdBm;
                }),
                Task.Factory.StartNew(() =>
                {
                    dcResults =
                        commands.OutphasingOptimisedMeasurement(
                            conf.SupplyVoltage);
                })});

            return new Sample(
                conf,
                dcResults.PowerWatts,
                measuredPoutdBm,
                channelPowerdBm,
                dcResults.Currents);
            }

        public static void SaveSample(StreamWriter outputFile, Sample sample)
            {
            var outputLine =
                $"{sample.Conf.Frequency}"
                + $", {sample.InputPowerdBm}"
                + $", {sample.InputPowerWatts}"
                + $", {sample.Conf.Phase}"
                + $", {sample.Conf.MeasurementConfig.Temperature}"
                + $", {sample.Conf.MeasurementConfig.Corner}"
                + $", {sample.Conf.SupplyVoltage}"
                + $", {sample.MeasuredPowerDcWatts}"
                + $", {sample.MeasuredChannelPowerdBm}"
                + $", {sample.MeasuredChannelPowerWatts}"
                + $", {sample.CalibratedChannelPowerdBm}"
                + $", {sample.CalibratedChannelPowerWatts}"
                + $", {sample.MeasuredOutputPowerdBm}"
                + $", {sample.MeasuredOutputPowerWatts}"
                + $", {sample.CalibratedOutputPowerdBm}"
                + $", {sample.CalibratedOutputPowerWatts}"
                + $", {sample.Conf.Offset.SignalGenerator1}"
                + $", {sample.Conf.Offset.SignalGenerator2}"
                + $", {sample.Conf.Offset.SpectrumAnalyzer}"
                + $", {sample.CalibratedDrainEfficiency}"
                + $", {sample.CalibratedPowerAddedEfficiency}"
                + $", {sample.CalibratedChannelDrainEfficiency}"
                + $", {sample.CalibratedChannelPowerAddedEfficiency}"
                + $", {sample.Conf.MeasurementConfig.MeasurementFrequencySpan}"
                + $", {sample.Conf.MeasurementConfig.MeasurementChannelBandwidth}"
                + $", {sample.CalibratedGaindB}"
                + $", {sample.CalibratedChannelGaindB}";

            foreach (var current in sample.DcCurrents)
                {
                    outputLine += $", {current}";
                }

            outputFile.WriteLine(outputLine);
            }

        public static void RunSweep(
                MeasurementConfig sweepConf,
                ref SweepProgress sweepProgress)
            {
            var outFile = new StreamWriter(sweepConf.OutputFilePath);

            void cleanup()
                {
                outFile.Flush();
                outFile.Close();
                outFile.Dispose();
                }

            var headerLine =
                "Frequency (Hz)"
                + ", Input Power (dBm)"
                + ", Input Power (W)"
                + ", Phase (deg)"
                + ", Temperature (Celcius)"
                + ", Corner"
                + ", Supply Voltage (V)"
                + ", Measured DC Power (W)"
                + ", Measured Channel Power (dBm)"
                + ", Measured Channel Power (W)"
                + ", Calibrated Channel Power (dBm)"
                + ", Calibrated Channel Power (W)"
                + ", Measured Output Power (dBm)"
                + ", Measured Output Power (W)"
                + ", Calibrated Output Power (dBm)"
                + ", Calibrated Output Power (W)"
                + ", Signal Generator 1 Input Power Offset (dB)"
                + ", Signal Generator 2 Input Power Offset (dB)"
                + ", Spectrum Analyzer Measurement Offset (dB)"
                + ", Calibrated Drain Efficiency (%)"
                + ", Calibrated Power Added Efficiency (%)"
                + ", Calibrated Channel Drain Efficiency (%)"
                + ", Calibrated Channel Power Added Efficiency (%)"
                + ", Measurement Frequency Span (Hz)"
                + ", Channel Measurement Bandwidth (Hz)"
                + ", Calibrated Gain (dB)"
                + ", Calibrated Channel Gain (dB)";

            var numActiveChannels =
                sweepConf.Commands.GetPsuChannelStates().Count(c => c);
            for (int i = 0; i < numActiveChannels; i++)
                {
                int channel = i + 1;
                headerLine += $", DC Current Channel {channel} (A)";
                }

            outFile.WriteLine(headerLine);
            var numberOfPoints = sweepConf.MeasurementPoints;
            sweepProgress.CurrentPoint = 1;
            sweepProgress.Running = true;
            
            // Pre-setup
            sweepConf.Commands.PreMeasurementSetup(sweepConf);

            // All of the sweeps are <= as we want to include the stop
            // value in the sweep
            foreach (var voltage in sweepConf.Voltages)
                {
                sweepConf.Commands.SetDcVoltageStepped(
                    voltage,
                    sweepConf.RampVoltageStep,
                    sweepConf.PsuRampUpStepTimeMilliseconds);

                foreach (var frequency in sweepConf.Frequencies)
                    {
                    var offsets = new CurrentOffset(
                        sweepConf.GenOffsets1.GetOffset(frequency),
                        sweepConf.GenOffsets2.GetOffset(frequency),
                        sweepConf.SpectrumAnalyzerOffsets.GetOffset(frequency));
                    sweepConf.Commands.SetFrequency(frequency);
                    foreach (var inputPower in sweepConf.InputPowers)
                        {
                        outFile.Flush();
                        if (!sweepProgress.Running)
                            {
                            outFile.Flush();
                            outFile.Close();
                            sweepConf.Commands.SetRfOutputState(on: false);
                            return;
                            }
                        sweepConf.Commands.SetInputPower(
                            inputPower,
                            offsets.SignalGenerator1,
                            offsets.SignalGenerator2);

                        var phaseSweepConfig = new PhaseSweepConfig(
                            sweepConf, 
                            offsets, 
                            voltage, 
                            frequency, 
                            inputPower);
                        var samples = PhaseSweep.MeasurementPhaseSweep(
                            phaseSweepConfig,
                            ref sweepProgress);

                        foreach (var sample in samples)
                            {
                            SaveSample(outFile, sample);
                            }

                        if(sweepProgress.Running == false)
                            {
                            cleanup();
                            return;
                            }
                        }
                    }
                }
            sweepProgress.Running = false;
            cleanup();
            sweepConf.Commands.SetRfOutputState(on: false);
            sweepConf.Commands.SetDcVoltageStepped(0, 0.1, 1000);
            }
        }
    }

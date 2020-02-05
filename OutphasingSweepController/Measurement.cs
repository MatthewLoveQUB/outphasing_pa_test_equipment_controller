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
        }
    }

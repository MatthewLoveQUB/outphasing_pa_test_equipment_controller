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

            // Make sure all commands are settled first
            while (!conf.MeasurementConfig.Commands.OperationsComplete())
                {

                }

            Task.WaitAll(new Task[]
                {
                Task.Factory.StartNew(() =>
                {
                    channelPowerdBm = 
                        conf.MeasurementConfig.Commands.GetSpectrumPower();
                    measuredPoutdBm
                        = conf.MeasurementConfig.Commands.GetMarkerPower(marker: 1);
                    //measuredPoutdBm = channelPowerdBm;
                }),
                Task.Factory.StartNew(() =>
                {
                    dcResults =
                    conf
                    .MeasurementConfig
                    .Commands
                    .OutphasingOptimisedMeasurement(conf.SupplyVoltage);
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
                $"{sample.Conf.Frequency}" // 1
                + $", {sample.InputPowerdBm}"
                + $", {sample.Conf.Phase}"
                + $", {sample.Conf.MeasurementConfig.Temperature}"
                + $", {sample.Conf.MeasurementConfig.Corner}"
                + $", {sample.Conf.SupplyVoltage}"
                + $", {sample.MeasuredPowerDcWatts}"
                + $", {sample.MeasuredChannelPowerdBm}"
                + $", {sample.MeasuredChannelPowerWatts}"
                + $", {sample.MeasuredOutputPowerdBm}"
                + $", {sample.MeasuredOutputPowerWatts}"
                + $", {sample.CalibratedOutputPowerdBm}"
                + $", {sample.CalibratedOutputPowerWatts}"
                + $", {sample.Conf.Offset.SignalGenerator1}"
                + $", {sample.Conf.Offset.SignalGenerator2}"
                + $", {sample.Conf.Offset.SpectrumAnalyzer}"
                + $", {sample.CalibratedDrainEfficiency}"
                + $", {sample.CalibratedPowerAddedEfficiency}"
                + $", {sample.Conf.MeasurementConfig.MeasurementFrequencySpan}"
                + $", {sample.Conf.MeasurementConfig.MeasurementChannelBandwidth}"
                + $", {sample.CalibratedGaindB}";

            foreach (var current in sample.DcCurrents)
                {
                    outputLine += $", {current}";
                }

            outputFile.WriteLine(outputLine);
            }
        }
    }

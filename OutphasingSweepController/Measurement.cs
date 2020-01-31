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
            HP6624A.OutphasingDcMeasurements dcResults = null;
            var rsa3408 = conf.PhaseSweepConfig.MeasurementConfig.Devices.Rsa3408a;
            var hp6624a = conf.PhaseSweepConfig.MeasurementConfig.Devices.Hp6624a;

            Task.WaitAll(new Task[]
                {
                Task.Factory.StartNew(() =>
                {
                    channelPowerdBm = rsa3408.ReadSpectrumChannelPower();
                    //measuredPoutdBm 
                    //    = rsa3408a.GetMarkerYValue(markerNumber: 1, view: 1);
                    measuredPoutdBm = channelPowerdBm;
                }),
                Task.Factory.StartNew(() =>
                {
                    dcResults = hp6624a.OutphasingOptimisedMeasurement(
                        conf.PhaseSweepConfig.SupplyVoltage);
                })});

            return new Sample(
                conf,
                dcResults.PowerWatts,
                measuredPoutdBm,
                channelPowerdBm,
                dcResults.Currents);
            }

        public static void SaveSample(
            StreamWriter outputFile,
            Sample sample,
            QubVisa.HP6624A hp6624a)
            {
            var outputLine =
                $"{sample.Conf.PhaseSweepConfig.Frequency}" // 1
                + $", {sample.InputPowerdBm}"
                + $", {sample.Conf.Phase}"
                + $", {sample.Conf.PhaseSweepConfig.MeasurementConfig.Temperature}"
                + $", {sample.Conf.PhaseSweepConfig.MeasurementConfig.Corner}"
                + $", {sample.Conf.PhaseSweepConfig.SupplyVoltage}"
                + $", {sample.MeasuredPowerDcWatts}"
                + $", {sample.MeasuredChannelPowerdBm}"
                + $", {sample.MeasuredChannelPowerWatts}"
                + $", {sample.MeasuredOutputPowerdBm}"
                + $", {sample.CalibratedOutputPowerdBm}"
                + $", {sample.Conf.PhaseSweepConfig.Offset.Smu200a}"
                + $", {sample.Conf.PhaseSweepConfig.Offset.E8257d}"
                + $", {sample.Conf.PhaseSweepConfig.Offset.Rsa3408a}"
                + $", {sample.CalibratedDrainEfficiency}"
                + $", {sample.CalibratedPowerAddedEfficiency}"
                + $", {sample.MeasuredChannelPowerdBm}"
                + $", {sample.Conf.PhaseSweepConfig.MeasurementConfig.MeasurementFrequencySpan}"
                + $", {sample.Conf.PhaseSweepConfig.MeasurementConfig.MeasurementChannelBandwidth}"
                + $", {sample.CalibratedGaindB}";

            for (int i = 0; i < QubVisa.HP6624A.NumChannels; i++)
                {
                var channelNumber = i + 1;
                if (hp6624a.ChannelStates[i])
                    {
                    outputLine += $", {sample.DcCurrent[i]}";
                    }
                }

            outputFile.WriteLine(outputLine);
            }
        }
    }

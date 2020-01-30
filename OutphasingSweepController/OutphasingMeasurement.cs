using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace OutphasingSweepController
    {
    // Write a sample to a file stream
    static class OutphasingMeasurement
        {
        public static void SaveSample(
            StreamWriter outputFile,
            Sample sample,
            QubVisa.HP6624A hp6624a)
            {
            var outputLine =
                $"{sample.Conf.Frequency}" // 1
                + $", {sample.InputPowerdBm}"
                + $", {sample.Conf.Phase}"
                + $", {sample.Conf.Conf.Temperature}"
                + $", {sample.Conf.Conf.Corner}" // 5
                + $", {sample.Conf.SupplyVoltage}"
                + $", {sample.MeasuredPowerDcWatts}"
                + $", {sample.MeasuredOutputPowerdBm}"
                + $", {sample.CalibratedOutputPowerdBm}"
                + $", {sample.Conf.Offset.Smu200a}" // 10
                + $", {sample.Conf.Offset.E8257d}"
                + $", {sample.Conf.Offset.Rsa3408a}"
                + $", {sample.CalibratedDrainEfficiency}"
                + $", {sample.CalibratedPowerAddedEfficiency}"
                + $", {sample.MeasuredChannelPowerdBm}" // 15
                + $", {sample.Conf.Conf.MeasurementFrequencySpan}"
                + $", {sample.Conf.Conf.MeasurementChannelBandwidth}"
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

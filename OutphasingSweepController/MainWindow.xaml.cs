using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using QubVisa;

namespace OutphasingSweepController
    {
    enum SearchMode
        {
        Peak,
        Trough
        }

    enum NewSampleResult
        {
        Better,
        Worse
        }

    enum PhaseSweepLoopStatus
        {
        Continue,
        Stop
        }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
        {
        // Misc
        public string ChipCorner { get; set; } = "TT";
        public double ChipTemperature { get; set; } = 25.0;
        // PSU
        HP6624A hp6624a;
        public double PsuNominalVoltage { get; set; } = 2.2;
        public double PsuCurrentLimit { get; set; } = 0.3;
        public int PsuRampUpStepTimeMilliseconds { get; set; } = 100;
        public double RampVoltageStep { get; set; } = 0.1;
        public bool PsuPlus10Percent { get; set; } = true;
        public bool PsuMinus10Percent { get; set; } = true;

        // Spectrum Analyser
        TektronixRSA3408A rsa3408a;
        public double Rsa3408ChannelBandwidth { get; set; } = 25e3;
        public double Rsa3408FrequencySpan { get; set; } = 1e6;
        // UI
        public Queue<String> LogQueue = new Queue<string>();
        List<CheckBox> PsuChannelEnableCheckboxes;
        System.Windows.Threading.DispatcherTimer dispatcherTimer;
        // File IO
        public string ResultsSavePath { get; set; } = 
            "C:\\Users\\matth\\Downloads\\x.csv";
        public string Smu200aOffsetsPath { get; set; } = 
            "C:\\Users\\matth\\Downloads\\Cable_5_offset_file.cor";
        public string E8257dOffsetsPath { get; set; } = 
            "C:\\Users\\matth\\Downloads\\Cable_2_offset_file.cor";
        public string Rsa3408aOffsetsPath { get; set; } = 
            "C:\\Users\\matth\\Downloads\\Cable_7_offset_file.cor";
        // Signal Generators
        RS_SMU200A smu200a;
        KeysightE8257D e8257d;
        // Measurement
        public bool PeakTroughSearch { get; set; } = true;
        SweepProgress CurrentSweepProgress = new SweepProgress(false, 0, 0);
        MeasurementSample CurrentSample;
        public double EstimatedTimePerSample { get; set; } = 0.32;
        public System.Diagnostics.Stopwatch MeasurementStopWatch = 
            new System.Diagnostics.Stopwatch();
        public Task Measurement;

        public MainWindow()
            {
            InitializeComponent();
            this.DataContext = this;
            PopulatePsuCheckboxList();
            //SetUpVisaConnections();
            SetUpDispatcherTimer();
            UpdateEstimatedMeasurementTime();

            this.ResultsSavePathTextBlock.Text = this.ResultsSavePath;
            this.Smu200aOffsetsFilePathTextBlock.Text = this.Smu200aOffsetsPath;
            this.E8257dOffsetsFilePathTextBlock.Text = this.E8257dOffsetsPath;
            this.Rsa3408aOffsetsFilePathTextBlock.Text = this.Rsa3408aOffsetsPath;
            }

        private void PopulatePsuCheckboxList()
            {
            this.PsuChannelEnableCheckboxes =
                new List<CheckBox>()
                    {
                    this.PsuChannel1Enable,
                    this.PsuChannel2Enable,
                    this.PsuChannel3Enable,
                    this.PsuChannel4Enable
                    };
            }

        private void SetUpDispatcherTimer()
            {
            this.dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            this.dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            this.dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 2);
            this.dispatcherTimer.Start();
            }

        private void SetUpVisaConnections()
            {
            var psuChannelStates = new List<bool>()
                {
                this.PsuChannel1Enable.IsChecked == true,
                this.PsuChannel2Enable.IsChecked == true,
                this.PsuChannel3Enable.IsChecked == true,
                this.PsuChannel4Enable.IsChecked == true
                };
            string hpAddress;
            string rsaAddress;
            string smaAddress;
            string e82Address;
            // Hard-coded addresses for me to save time
            // Set it to false if someone who isn't
            // me is somehow using this code
            // Sorry if this causes you bother!
            if (true)
                {
                hpAddress = "GPIB0::14::INSTR";
                rsaAddress = "GPIB1::1::INSTR";
                smaAddress = "TCPIP0::192.168.1.101::inst0::INSTR";
                e82Address = "TCPIP0::192.168.1.3::inst1::INSTR";
                }
            else
                {
                hpAddress = this.GetVisaAddress("HP6624A");
                rsaAddress = this.GetVisaAddress("Tektronix RSA3408");
                smaAddress = this.GetVisaAddress("R&S SMU200A");
                e82Address = this.GetVisaAddress("Keysight E8257D");
                }

            this.hp6624a = new HP6624A(hpAddress, psuChannelStates);
            this.rsa3408a = new TektronixRSA3408A(rsaAddress);
            this.smu200a = new RS_SMU200A(smaAddress);
            this.e8257d = new KeysightE8257D(e82Address);
            this.rsa3408a.ResetDevice();
            this.smu200a.ResetDevice();
            this.e8257d.ResetDevice();
            }

        private string GetVisaAddress(string deviceName)
            {
            var visaWindow = 
                new list_visa_devices_dialogue.MainWindow(deviceName);
            visaWindow.ShowDialog();
            return visaWindow.SelectedAddress;
            }

        private void AddNewLogLine(string line)                                
            {
            this.SweepLogTextBox.Text = $"{this.SweepLogTextBox.Text}{line}\n";
            }

        private MeasurementSweepConfiguration ParseMeasurementConfiguration()
            {
            var frequencySettings = this.FrequencySweepSettingsControl.Values;
            var powerSettings = this.PowerSweepSettingsControl.Values;
            var phaseSettings = this.PhaseSweepSettingsControl.Values;
            var voltages = new List<Double>() { this.PsuNominalVoltage };
            if (this.PsuPlus10Percent)
                {
                voltages.Add(1.1 * this.PsuNominalVoltage);
                }
            if (this.PsuMinus10Percent)
                {
                voltages.Add(0.9 * this.PsuNominalVoltage);
                }

            return new MeasurementSweepConfiguration(
                frequencySettings,
                powerSettings,
                phaseSettings,
                this.ChipTemperature,
                this.ChipCorner,
                voltages,
                this.Rsa3408ChannelBandwidth,
                this.Rsa3408FrequencySpan,
                this.ResultsSavePath,
                this.Smu200aOffsetsPath,
                this.E8257dOffsetsPath,
                this.Rsa3408aOffsetsPath,
                this.PeakTroughSearch);
            }

        private void StartSweepButton_Click(object sender, RoutedEventArgs e)
            {
            if (!MeasurementVariablesCheck()) { return; }

            var measurementSettings = ParseMeasurementConfiguration();

            this.StartSweepButton.IsEnabled = false;
            this.StopSweepButton.IsEnabled = true;
            this.ControllerGrid.IsEnabled = false;

            this.Measurement = Task.Factory.StartNew(() =>
            {
                RunSweep(measurementSettings);
            });
            }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
            {
            // To avoid memory leaks, wipe the log every tick
            this.SweepLogTextBox.Text = "";

            if (this.CurrentSweepProgress.Running)
                {
                var curPt = this.CurrentSweepProgress.CurrentPoint;
                var nPts = this.CurrentSweepProgress.NumberOfPoints;
                var timeElapsed = this.MeasurementStopWatch.Elapsed;
                var ptsRemaining = nPts - curPt;
                var timeScaler = (double)ptsRemaining / (double)curPt;
                var estimatedTime = 
                    TimeSpan.FromTicks(timeElapsed.Ticks * (long)timeScaler);
                var samplesPerSecond = curPt / (double)timeElapsed.TotalSeconds;
                var secondsPerSample = 1 / samplesPerSecond;

                if (this.CurrentSample != null)
                    {
                    this.LastSampleTextBlock.Text =
                        $"Calibrated Pout: {this.CurrentSample.CalibratedOutputPowerdBm:F2} dBm\n"
                        + $"Measured Pout: {this.CurrentSample.MeasuredOutputPowerdBm:F2} dBm\n"
                        + $"Measured Channel Power {this.CurrentSample.MeasuredChannelPowerdBm:F2} dBm\n"
                        + $"Frequency: {this.CurrentSample.Conf.Frequency:G2} Hz\n"
                        + $"Pin: {this.CurrentSample.InputPowerdBm:F2} dBm\n"
                        + $"Supply Voltage: {this.CurrentSample.Conf.SupplyVoltage:F2} V\n"
                        + $"Calibrated Gain: {this.CurrentSample.CalibratedGaindB:F2} dB\n"
                        + $"Calibrated PAE: {this.CurrentSample.CalibratedPowerAddedEfficiency:F2} %\n"
                        + $"Calibrated Drain Efficiency: {this.CurrentSample.CalibratedDrainEfficiency:F2} %\n"
                        + $"Measured DC Power: {this.CurrentSample.MeasuredPowerDcWatts:F2} W\n";
                    }

                // Print current point
                var msg = $"On task {curPt} of {nPts}";
                AddNewLogLine(msg);
                // Print elapsed time
                msg = "Elapsed Time: "
                    + $"{timeElapsed.Days} days "
                    + $"{timeElapsed.Hours} hours "
                    + $"{timeElapsed.Minutes} minutes "
                    + $"{timeElapsed.Seconds} seconds";
                AddNewLogLine(msg);
                // Print sample rate
                AddNewLogLine($"Sample Rate = {samplesPerSecond:F2} S/s");
                AddNewLogLine($"Sample Time = {secondsPerSample:F2} s");
                // Print estimated remaining time
                msg = "Est. Remaining Time: "
                    + $"{estimatedTime.Days} days "
                    + $"{estimatedTime.Hours} hours "
                    + $"{estimatedTime.Minutes} minutes "
                    + $"{estimatedTime.Seconds} seconds";
                AddNewLogLine(msg);
                }

            // Empty the log queue
            while(this.LogQueue.Count > 0)
                {
                var message = this.LogQueue.Dequeue();
                AddNewLogLine(message);
                }
            }

        // Ramp the voltage slowly
        // Assumes that all channels are at the same voltage
        private void SetPsuVoltageStepped(double newVoltage)
            {
            // Step should be positive before we alter it depending on
            // whether we're ramping up or down
            var step = Math.Abs(this.RampVoltageStep);

            // Read the current voltage
            double currentVoltage = 0;
            for (int i = 0; i < HP6624A.NumChannels; i++)
                {
                int channelNumber = i + 1;
                bool channelEnabled = this.hp6624a.ChannelStates[i];
                if (channelEnabled)
                    {
                    currentVoltage =
                        this.hp6624a.GetChannelVoltageSetting(channelNumber);
                    break;
                    }
                }

            // End the function if the voltage is already set
            // Unlikely due to the way floating point works
            if (newVoltage == currentVoltage)
                {
                return;
                }

            // Invert the step if we're decreasing the channel voltage
            if (newVoltage < currentVoltage)
                {
                step *= -1;
                }

            int numSteps = 
                (int)(Math.Abs(currentVoltage - newVoltage) / step);
            var intermediateVoltage = currentVoltage;

            for (int currentStep = 0; currentStep < numSteps; currentStep++)
                {
                intermediateVoltage += step;
                this.hp6624a.SetActiveChannelsVoltages(intermediateVoltage);
                Thread.Sleep(this.PsuRampUpStepTimeMilliseconds);
                }

            // In case we overshot the voltage then 
            // just set it to the final voltage
            this.hp6624a.SetActiveChannelsVoltages(newVoltage);
            }

        bool MeasurementVariablesCheck()
            {
            if (this.ResultsSavePath == "")
                {
                this.LogQueue.Enqueue("No save path entered.");
                return false;
                }

            if (this.Smu200aOffsetsPath == "")
                {
                this.LogQueue.Enqueue("No SMU200A amplitude file chosen.");
                return false;
                }

            if (this.E8257dOffsetsPath == "")
                {
                this.LogQueue.Enqueue("No E8257D amplitude file chosen.");
                return false;
                }

            if (this.Rsa3408aOffsetsPath == "")
                {
                this.LogQueue.Enqueue("No RSA3408A amplitude file chosen.");
                return false;
                }

            return true;
            }

        private void RunSweep(MeasurementSweepConfiguration conf)
            {
            var outputFile = new StreamWriter(conf.OutputFilePath);
            var headerLine =
                "Frequency (Hz)" // 1
                + ", Input Power (dBm)"
                + ", Phase (deg)"
                + ", Temperature (Celcius)"
                + ", Corner" // 5
                + ", Supply Voltage (V)"
                + ", Measured DC Power (W)"
                + ", Measured Output Power (dBm)"
                + ", Calibrated Output Power (dBm)"
                + ", SMU200A Input Power Offset (dB)" // 10
                + ", E8257D Input Power Offset (dB)"
                + ", RSA3408A Measurement Offset (dB)"
                + ", Calibrated Drain Efficiency (%)"
                + ", Calibrated Power Added Efficiency (%)"
                + ", Measured Channel Power (dBm)" // 15
                + ", Measurement Frequency Span (Hz)"
                + ", Channel Measurement Bandwidth (Hz)"
                + ", Calibrated Gain (dB)";
            
            for(int i = 0; i < HP6624A.NumChannels; i++)
                {
                if (this.hp6624a.ChannelStates[i])
                    {
                    headerLine +=
                        string.Format(
                            ", DC Current Channel {0} (A)",
                            i + 1);
                    }
                }

            outputFile.WriteLine(headerLine);
            var numberOfPoints = conf.MeasurementPoints;
            this.CurrentSweepProgress.CurrentPoint = 1;
            this.CurrentSweepProgress.NumberOfPoints = numberOfPoints;
            this.CurrentSweepProgress.Running = true;
            this.MeasurementStopWatch.Restart();

            // Pre-setup
            var tasksSetFrequency = new Task[3];
            var tasksSetPower = new Task[2];
            double offsetSmu200a = -1;
            double offsetE8257d = -1;
            double offsetRsa = -1;

            // DC supply
            this.hp6624a.SetAllChannelVoltagesToZero();
            this.hp6624a.SetChannelOutputStatesStrong();
            this.hp6624a.SetActiveChannelsCurrent(this.PsuCurrentLimit);

            // Spectrum Analyser
            this.rsa3408a.SetSpectrumChannelPowerMeasurementMode();
            this.rsa3408a.SetContinuousMode(continuousOn: false);

            this.rsa3408a.SetFrequencyCenter(conf.Frequencies[0]);
            this.rsa3408a.SetFrequencySpan(conf.MeasurementFrequencySpan);
            this.rsa3408a.SetChannelBandwidth(conf.MeasurementChannelBandwidth);
            this.rsa3408a.StartSignalAcquisition();

            //rsa3408a.SetMarkerState(markerNumber: 1, view: 1, on: true);
            //rsa3408a.SetMarkerXToPositionMode(1,1);
            // When the frequency changes, the marker should automatially
            // track to the new centre frequency
            //rsa3408a.SetMarkerXValue(markerNumber: 1, view: 1, xValue: conf.Frequencies[0]);
            // Set the power sources to an extremely low
            // power before starting the sweep
            // in case they default to some massive value
            this.smu200a.SetRfOutputState(on: false);
            this.e8257d.SetRfOutputState(on: false);
            this.smu200a.SetPowerLevel(-60);
            this.e8257d.SetPowerLevel(-60);
            this.smu200a.SetRfOutputState(on: true);
            this.e8257d.SetRfOutputState(on: true);

            // All of the sweeps are <= as we want to include the stop
            // value in the sweep
            foreach (var voltage in conf.Voltages)
                {
                SetPsuVoltageStepped(voltage);

                

                foreach (var frequency in conf.Frequencies)
                    {
                    tasksSetFrequency[0] = Task.Factory.StartNew(() =>
                    {
                        this.rsa3408a.SetFrequencyCenter(frequency);
                        offsetRsa = conf.Rsa3408aOffsets.GetOffset(frequency);
                    });
                    tasksSetFrequency[1] = Task.Factory.StartNew(() =>
                    {
                        this.smu200a.SetSourceFrequency(frequency);
                        offsetSmu200a = conf.Smu200aOffsets.GetOffset(frequency);
                    });
                    tasksSetFrequency[2] = Task.Factory.StartNew(() =>
                    {
                        this.e8257d.SetSourceFrequency(frequency);
                        offsetE8257d = conf.E8257dOffsets.GetOffset(frequency);
                    });
                    Task.WaitAll(tasksSetFrequency);

                    foreach (var inputPower in conf.InputPowers)
                        {
                        outputFile.Flush();
                        // Set the power
                        tasksSetPower[0] = 
                            Task.Factory.StartNew(()=>
                            {
                                this.smu200a.SetPowerLevel(inputPower, offsetSmu200a);
                            });
                        tasksSetPower[1] =
                            Task.Factory.StartNew(() =>
                            {
                                this.e8257d.SetPowerLevel(inputPower, offsetE8257d);
                            });
                        Task.WaitAll(tasksSetPower);

                        var sampleConf = new MeasurementSampleConfiguration(
                            conf,
                            voltage,
                            frequency,
                            inputPower,
                            -1e12,
                            offsetSmu200a,
                            offsetE8257d,
                            offsetRsa);

                        var samples = MeasurementPhaseSweep(sampleConf);
                        foreach (var sample in samples)
                            {
                            SaveMeasurementSample(outputFile, sample);
                            }

                        // End the measurement if signalled
                        if (!this.CurrentSweepProgress.Running)
                            {
                            outputFile.Flush();
                            outputFile.Close();
                            this.smu200a.SetRfOutputState(on: false);
                            this.e8257d.SetRfOutputState(on: false);
                            return;
                            }
                        }
                    }
                }
            outputFile.Close();
            this.MeasurementStopWatch.Stop();
            this.MeasurementStopWatch.Reset();
            }

        private void SetSignalPhase(
            ref MeasurementSampleConfiguration conf,
            double phase)
            {
            conf.Phase = phase;
            this.smu200a.SetSourceDeltaPhase(conf.Phase);
            }

        private void BasicPhaseSweep(
            List<MeasurementSample> samples,
            MeasurementSampleConfiguration conf)
            {
            // Do the coarse sweep
            foreach (var phase in conf.Conf.Phases)
                {
                SetSignalPhase(ref conf, phase);
                this.CurrentSweepProgress.CurrentPoint++;
                conf.Phase = phase;
                var sample = TakeMeasurementSample(conf);
                samples.Add(sample);
                }
            }

        private NewSampleResult PeakTroughComparison(
            SearchMode searchMode,
            ref MeasurementSample bestSample,
            MeasurementSample newSample)
            {
            var newPower = newSample.MeasuredChannelPowerdBm;
            var bestPower = bestSample.MeasuredChannelPowerdBm;
            var newGreater = newPower > bestPower;

            if (searchMode == SearchMode.Peak)
                {
                if (newGreater)
                    {
                    bestSample = newSample;
                    return NewSampleResult.Better;
                    }
                else
                    {
                    return NewSampleResult.Worse;
                    }
                }
            else
                {
                if (!newGreater)
                    {
                    bestSample = newSample;
                    return NewSampleResult.Better;
                    }
                else
                    {
                    return NewSampleResult.Worse;
                    }
                }
            }

        private PhaseSweepLoopStatus EvaluateNewSample(
            MeasurementSample bestSample,
            MeasurementSample newSample,
            SearchMode searchMode,
            double threshold)
            {
            var newPwr = newSample.MeasuredChannelPowerdBm;
            var bestPwr = bestSample.MeasuredChannelPowerdBm;
            
            if (searchMode == SearchMode.Peak)
                {
                if((bestPwr - newPwr) < threshold)
                    {
                    return PhaseSweepLoopStatus.Continue;
                    }
                else
                    {
                    return PhaseSweepLoopStatus.Stop;
                    }
                }
            else
                {
                if ((newPwr - bestPwr) < threshold)
                    {
                    return PhaseSweepLoopStatus.Continue;
                    }
                else
                    {
                    return PhaseSweepLoopStatus.Stop;
                    }
                }
            }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchMode"></param>
        /// <param name="samples"></param>
        /// <param name="orderedSamples">
        ///     Ordered in descending channel power
        /// </param>
        /// <param name="conf"></param>
        /// <param name="supplyVoltage"></param>
        /// <param name="frequency"></param>
        /// <param name="inputPower"></param>
        /// <param name="offsetSmu200a"></param>
        /// <param name="offsetE8257d"></param>
        /// <param name="offsetRsa"></param>
        private void FindPeakOrTrough(
            SearchMode searchMode,
            List<MeasurementSample> samples,
            MeasurementSample startBestSample,
            MeasurementSampleConfiguration conf,
            double exitThreshold,
            double phaseStep)
            {
            var bestSample = startBestSample;
            double currentPhase;

            // Take a new sample next to it
            currentPhase = bestSample.Conf.Phase + phaseStep;
            SetSignalPhase(ref conf, currentPhase);
            this.CurrentSample = TakeMeasurementSample(conf);
            this.CurrentSweepProgress.CurrentPoint++;
            this.CurrentSweepProgress.NumberOfPoints++;
            samples.Add(this.CurrentSample);

            // Is the new sample is better
            // If not, search in the other direction
            if (PeakTroughComparison(searchMode, ref bestSample, this.CurrentSample) 
                == NewSampleResult.Worse)
                {
                phaseStep *= -1;
                }

            currentPhase = bestSample.Conf.Phase;
            // Keep looping until we've moved exitThreshold dB
            // away from the best found value
            while (EvaluateNewSample(
                bestSample,
                this.CurrentSample, 
                searchMode, 
                exitThreshold) 
                == PhaseSweepLoopStatus.Continue)
                {
                currentPhase += phaseStep;
                SetSignalPhase(ref conf, currentPhase);
                this.CurrentSample = TakeMeasurementSample(conf);
                this.CurrentSweepProgress.CurrentPoint++;
                this.CurrentSweepProgress.NumberOfPoints++;
                samples.Add(this.CurrentSample);
                PeakTroughComparison(searchMode, ref bestSample, this.CurrentSample);
                }
            }

        private List<MeasurementSample> MeasurementPhaseSweep(
            MeasurementSampleConfiguration conf)
            {
            var samples = new List<MeasurementSample>();
            BasicPhaseSweep(samples, conf);

            if (!conf.Conf.PeakTroughPhaseSearch)
                {
                return samples;
                }

            var orderedSamples = 
                samples.OrderByDescending(
                    sample => sample.MeasuredChannelPowerdBm).ToList();

            var bestSample = orderedSamples.First();
            double exitThreshold = 0.2;
            double phaseStep = 1;
            FindPeakOrTrough(
                SearchMode.Peak,
                samples,
                bestSample,
                conf,
                exitThreshold,
                phaseStep);

            bestSample = orderedSamples.Last();
            exitThreshold = 0.2;
            phaseStep = 0.1;
            FindPeakOrTrough(
                SearchMode.Trough,
                samples,
                bestSample,
                conf,
                exitThreshold,
                phaseStep);

            return samples;
            }

        private MeasurementSample TakeMeasurementSample(
            MeasurementSampleConfiguration conf)
            {
            var readTasks = new Task[2];
            double channelPowerdBm = -1;
            double measuredPoutdBm = -1;
            HP6624A.OutphasingDcMeasurements dcResults = null;
            readTasks[0] = Task.Factory.StartNew(() =>
            {
                channelPowerdBm = this.rsa3408a.ReadSpectrumChannelPower();
                measuredPoutdBm = channelPowerdBm;// rsa3408a.GetMarkerYValue(markerNumber: 1, view: 1);
            });
            readTasks[1] = Task.Factory.StartNew(() =>
            {
                dcResults =
                    this.hp6624a.OutphasingOptimisedMeasurement(conf.SupplyVoltage);
            });
            
            Task.WaitAll(readTasks);

            return new MeasurementSample(
                conf, 
                dcResults.PowerWatts, 
                measuredPoutdBm, 
                channelPowerdBm, 
                dcResults.Currents);
            }

        private void SaveMeasurementSample(
            StreamWriter outputFile, 
            MeasurementSample sample)
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

            for(int i = 0; i < HP6624A.NumChannels; i++)
                {
                var channelNumber = i + 1;
                if (this.hp6624a.ChannelStates[i])
                    {
                    outputLine += $", {sample.DcCurrent[i]}";
                    }
                }

            outputFile.WriteLine(outputLine);
            }

        private void SweepSettingsControl_LostFocus(
            object sender, RoutedEventArgs e)
            {
            UpdateEstimatedMeasurementTime();
            }

        private void UpdateEstimatedMeasurementTime()
            {
            int voltagePoints = 1;
            if (this.PsuPlus10Percent)
                {
                voltagePoints++;
                }
            if (this.PsuMinus10Percent)
                {
                voltagePoints++;
                }
            var freqPoints = this.FrequencySweepSettingsControl.NSteps;
            var powerPoints = this.PowerSweepSettingsControl.NSteps;
            var phasePoints = this.PhaseSweepSettingsControl.NSteps;
            var nPoints = this.EstimatedTimePerSample 
                * voltagePoints 
                * freqPoints 
                * powerPoints 
                * phasePoints;
            var estimatedMeasurementTime = TimeSpan.FromSeconds(nPoints);
            this.EstimatedSimulationTimeTextBlock.Text = 
              $"Estimated Measurement Time = "
                + $"{estimatedMeasurementTime.Days} days "
                + $"{estimatedMeasurementTime.Hours} hours "
                + $"{estimatedMeasurementTime.Minutes} minutes";
            }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
            {
            var saveDialog = new Microsoft.Win32.SaveFileDialog();
            saveDialog.Filter = "CSV Files (.csv)|*.csv";
            saveDialog.DefaultExt = ".csv";
            var dialogSuccess = (saveDialog.ShowDialog() == true);

            if (dialogSuccess)
                {
                this.ResultsSavePath = saveDialog.FileName;
                this.ResultsSavePathTextBlock.Text = this.ResultsSavePath;
                }
            }

        private void LoadSmu200aOffsetsButton_Click(
            object sender, RoutedEventArgs e)
            {
            this.Smu200aOffsetsPath = GetOffsetsPath("SMU200A");
            this.Smu200aOffsetsFilePathTextBlock.Text = this.Smu200aOffsetsPath;
            }

        private void LoadE8257dOffsetsButton_Click(
            object sender, RoutedEventArgs e)
            {
            this.E8257dOffsetsPath = GetOffsetsPath("E8257D");
            this.E8257dOffsetsFilePathTextBlock.Text = this.E8257dOffsetsPath;
            }

        private void LoadRsa3408adOffsetsButton_Click(
            object sender, RoutedEventArgs e)
            {
            this.Rsa3408aOffsetsPath = GetOffsetsPath("RSA3408A");
            this.Rsa3408aOffsetsFilePathTextBlock.Text = this.Rsa3408aOffsetsPath;
            }

        private string GetOffsetsPath(string deviceName)
            {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = string.Format(
                "{0} Offset file (*.*)|*.*",
                deviceName);
            openFileDialog.Title = string.Format(
                "Open offset file for {0}",
                deviceName);
            var dialogSuccess = openFileDialog.ShowDialog() == true;
            return dialogSuccess ? openFileDialog.FileName : "";
            }

        private void StopSweepButton_Click(object sender, RoutedEventArgs e)
            {
            this.CurrentSweepProgress.Running = false;
            this.StopSweepButton.IsEnabled = false;
            this.StartSweepButton.IsEnabled = true;
            this.ControllerGrid.IsEnabled = true;
            }
        }
    }

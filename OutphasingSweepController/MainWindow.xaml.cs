using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
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
            SetUpVisaConnections();
            SetUpDispatcherTimer();
            UpdateEstimatedMeasurementTime();

            this.ResultsSavePathTextBlock.Text = 
                this.ResultsSavePath;
            this.Smu200aOffsetsFilePathTextBlock.Text = 
                this.Smu200aOffsetsPath;
            this.E8257dOffsetsFilePathTextBlock.Text = 
                this.E8257dOffsetsPath;
            this.Rsa3408aOffsetsFilePathTextBlock.Text = 
                this.Rsa3408aOffsetsPath;
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
            this.dispatcherTimer = 
                new System.Windows.Threading.DispatcherTimer();
            this.dispatcherTimer.Tick += 
                new EventHandler(dispatcherTimer_Tick);
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
                this.PeakTroughSearch,
                new PhaseSearchSettings(
                    this.PeakSearchSettingsTextBox.Text,
                    this.TroughSearchSettingsTextBox.Text));
            }

        private void ToggleGuiActive(bool on)
            {
            this.StartSweepButton.IsEnabled = on;
            this.StopSweepButton.IsEnabled = !on;
            this.ControllerGrid.IsEnabled = on;
            }

        private void StartSweepButton_Click(object sender, RoutedEventArgs e)
            {
            if (!MeasurementVariablesCheck()) { return; }

            var measurementSettings = ParseMeasurementConfiguration();
            this.ToggleGuiActive(on: false);
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
            while (this.LogQueue.Count > 0)
                {
                var message = this.LogQueue.Dequeue();
                AddNewLogLine(message);
                }
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

            for (int i = 0; i < HP6624A.NumChannels; i++)
                {
                if (this.hp6624a.ChannelStates[i])
                    {
                    int channel = i + 1;
                    headerLine += $", DC Current Channel {channel} (A)";
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
                this.hp6624a.SetPsuVoltageStepped(voltage, 
                    this.RampVoltageStep, 
                    this.PsuRampUpStepTimeMilliseconds);

                foreach (var frequency in conf.Frequencies)
                    {
                    var offsets = new CurrentOffset(
                        conf.Smu200aOffsets.GetOffset(frequency),
                        conf.E8257dOffsets.GetOffset(frequency),
                        conf.Rsa3408aOffsets.GetOffset(frequency));

                    tasksSetFrequency[0] = Task.Factory.StartNew(() =>
                    {
                        this.rsa3408a.SetFrequencyCenter(frequency);
                    });
                    tasksSetFrequency[1] = Task.Factory.StartNew(() =>
                    {
                        this.smu200a.SetSourceFrequency(frequency);
                    });
                    tasksSetFrequency[2] = Task.Factory.StartNew(() =>
                    {
                        this.e8257d.SetSourceFrequency(frequency);
                    });
                    Task.WaitAll(tasksSetFrequency);

                    foreach (var inputPower in conf.InputPowers)
                        {
                        outputFile.Flush();
                        tasksSetPower[0] =
                            Task.Factory.StartNew(() =>
                            {
                                this.smu200a.SetPowerLevel(
                                    inputPower, offsets.Smu200a);
                            });
                        tasksSetPower[1] =
                            Task.Factory.StartNew(() =>
                            {
                                this.e8257d.SetPowerLevel(
                                    inputPower, offsets.E8257d);
                            });
                        Task.WaitAll(tasksSetPower);

                        var sampleConf = new MeasurementSampleConfiguration(
                            conf,
                            voltage,
                            frequency,
                            inputPower,
                            -1e12,
                            offsets.Smu200a,
                            offsets.E8257d,
                            offsets.Rsa3408a);

                        var samples = MeasurementPhaseSweep(sampleConf);
                        foreach (var sample in samples)
                            {
                            OutphasingMeasurement.SaveSample(
                                outputFile, sample, this.hp6624a);
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
                if ((bestPwr - newPwr) < threshold)
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

            foreach (var peakSearchSetting
                in conf.Conf.PhasePeakTroughSearchSettings.PeakSettings)
                {
                var orderedSamples =
                samples.OrderByDescending(
                    sample => sample.MeasuredChannelPowerdBm).ToList();
                var bestSample = orderedSamples.First();
                FindPeakOrTrough(
                    SearchMode.Peak,
                    samples,
                    bestSample,
                    conf,
                    peakSearchSetting.ThresholddB,
                    peakSearchSetting.StepDeg);
                }

            foreach (var troughSearchSetting
                in conf.Conf.PhasePeakTroughSearchSettings.TroughSettings)
                {
                var orderedSamples =
                samples.OrderByDescending(
                    sample => sample.MeasuredChannelPowerdBm).ToList();
                var bestSample = orderedSamples.Last();
                FindPeakOrTrough(
                    SearchMode.Trough,
                    samples,
                    bestSample,
                    conf,
                    troughSearchSetting.ThresholddB,
                    troughSearchSetting.StepDeg);
                }

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

        private void SweepSettingsControl_LostFocus(
            object sender, RoutedEventArgs e)
            {
            UpdateEstimatedMeasurementTime();
            }

        private void UpdateEstimatedMeasurementTime()
            {
            var voltagePoints = 1
                + Convert.ToInt64(this.PsuPlus10Percent)
                + Convert.ToInt64(this.PsuMinus10Percent);
            var nPoints = this.EstimatedTimePerSample
                * voltagePoints
                * this.FrequencySweepSettingsControl.NSteps
                * this.PowerSweepSettingsControl.NSteps
                * this.PhaseSweepSettingsControl.NSteps;
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
            if ((saveDialog.ShowDialog() == true))
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
            this.ToggleGuiActive(on: true);
            this.CurrentSweepProgress.Running = false;
            }
        }
    }

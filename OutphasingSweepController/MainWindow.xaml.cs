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
        public double EstimatedTimePerSample { get; set; } = 0.32;
        public System.Diagnostics.Stopwatch MeasurementStopWatch =
            new System.Diagnostics.Stopwatch();

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
            Task.Factory.StartNew(() =>
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

        private void RunSweep(MeasurementSweepConfiguration sweepConf)
            {
            var outputFile = new StreamWriter(sweepConf.OutputFilePath);
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
            var numberOfPoints = sweepConf.MeasurementPoints;
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

            this.rsa3408a.SetFrequencyCenter(sweepConf.Frequencies[0]);
            this.rsa3408a.SetFrequencySpan(sweepConf.MeasurementFrequencySpan);
            this.rsa3408a.SetChannelBandwidth(sweepConf.MeasurementChannelBandwidth);
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
            foreach (var voltage in sweepConf.Voltages)
                {
                this.hp6624a.SetPsuVoltageStepped(voltage, 
                    this.RampVoltageStep, 
                    this.PsuRampUpStepTimeMilliseconds);

                foreach (var frequency in sweepConf.Frequencies)
                    {
                    var offsets = new CurrentOffset(
                        sweepConf.Smu200aOffsets.GetOffset(frequency),
                        sweepConf.E8257dOffsets.GetOffset(frequency),
                        sweepConf.Rsa3408aOffsets.GetOffset(frequency));

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

                    foreach (var inputPower in sweepConf.InputPowers)
                        {
                        outputFile.Flush();
                        if (!this.CurrentSweepProgress.Running)
                            {
                            outputFile.Flush();
                            outputFile.Close();
                            this.smu200a.SetRfOutputState(on: false);
                            this.e8257d.SetRfOutputState(on: false);
                            return;
                            }

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

                        var samples = MeasurementPhaseSweep(
                            sweepConf,
                            offsets,
                            voltage,
                            frequency,
                            inputPower);

                        foreach (var sample in samples)
                            {
                            OutphasingMeasurement.SaveSample(
                                outputFile, sample, this.hp6624a);
                            }
                        }
                    }
                }
            outputFile.Close();
            this.MeasurementStopWatch.Stop();
            this.MeasurementStopWatch.Reset();
            }

        private List<MeasurementSample> MeasurementPhaseSweep(
            MeasurementSweepConfiguration sweepConfig,
            CurrentOffset offset,
            double supplyVoltage,
            double frequency,
            double inputPower)
            {
            var samples = new List<MeasurementSample>();
            BasicPhaseSweep(
                samples, 
                sweepConfig, 
                supplyVoltage, 
                frequency, 
                inputPower, 
                offset);

            if (!sweepConfig.PeakTroughPhaseSearch) {return samples;}

            foreach (var peakSearchSetting
                in sweepConfig.PhasePeakTroughSearchSettings.PeakSettings)
                {
                var orderedSamples =
                samples.OrderByDescending(
                    sample => sample.MeasuredChannelPowerdBm).ToList();
                var bestSample = orderedSamples.First();
                FindPeakOrTrough(
                    PhaseSearch.Mode.Trough,
                    samples,
                    bestSample,
                    sweepConfig,
                    offset,
                    supplyVoltage,
                    frequency,
                    inputPower,
                    peakSearchSetting.ThresholddB,
                    peakSearchSetting.StepDeg);
                }

            foreach (var troughSearchSetting
                in sweepConfig.PhasePeakTroughSearchSettings.TroughSettings)
                {
                var orderedSamples =
                samples.OrderByDescending(
                    sample => sample.MeasuredChannelPowerdBm).ToList();
                var bestSample = orderedSamples.Last();
                FindPeakOrTrough(
                    PhaseSearch.Mode.Trough,
                    samples,
                    bestSample,
                    sweepConfig,
                    offset,
                    supplyVoltage,
                    frequency,
                    inputPower,
                    troughSearchSetting.ThresholddB,
                    troughSearchSetting.StepDeg);
                }

            return samples;
            }

        private void BasicPhaseSweep(
            List<MeasurementSample> samples,
            MeasurementSweepConfiguration sweepConf,
            double supplyVoltage,
            double frequency,
            double inputPower,
            CurrentOffset offset)
            {
            // Do the coarse sweep
            foreach (var phase in sweepConf.Phases)
                {
                this.smu200a.SetSourceDeltaPhase(phase);
                this.CurrentSweepProgress.CurrentPoint++;

                var sampleConfig = new MeasurementSampleConfiguration(
                    sweepConf,
                    supplyVoltage,
                    frequency,
                    inputPower,
                    phase,
                    offset);
                var sample = TakeMeasurementSample(sampleConfig);
                samples.Add(sample);
                }
            }

        private MeasurementSample TakeMeasurementSample(
            MeasurementSampleConfiguration conf)
            {
            double channelPowerdBm = -1;
            double measuredPoutdBm = -1;
            HP6624A.OutphasingDcMeasurements dcResults = null;
            var readTasks = new List<Task>()
                {
                Task.Factory.StartNew(() =>
                {
                    channelPowerdBm = this.rsa3408a.ReadSpectrumChannelPower();
                    //measuredPoutdBm 
                    //    = rsa3408a.GetMarkerYValue(markerNumber: 1, view: 1);
                    measuredPoutdBm = channelPowerdBm;
                }),
                Task.Factory.StartNew(() =>
                {
                    dcResults =
                    this.hp6624a.OutphasingOptimisedMeasurement(
                        conf.SupplyVoltage);
                })
                };
            Task.WaitAll(readTasks.ToArray());

            return new MeasurementSample(
                conf,
                dcResults.PowerWatts,
                measuredPoutdBm,
                channelPowerdBm,
                dcResults.Currents);
            }

        private void FindPeakOrTrough(
            PhaseSearch.Mode searchMode,
            List<MeasurementSample> samples,
            MeasurementSample startBestSample,
            MeasurementSweepConfiguration sweepConf,
            CurrentOffset offset,
            double supplyVoltage,
            double frequency,
            double inputPower,
            double exitThreshold,
            double phaseStep)
            {
            var bestSample = startBestSample;
            double currentPhase;
            MeasurementSample newSample;
            MeasurementSampleConfiguration sampleConfig;

            phaseStep = FindSearchDirection(
                searchMode, 
                samples, 
                bestSample, 
                sweepConf, 
                offset, 
                supplyVoltage, 
                frequency, 
                inputPower, 
                phaseStep);

            currentPhase = bestSample.Conf.Phase;
            newSample = bestSample;
            // Keep looping until we've moved exitThreshold dB
            // away from the best found value
            while (PhaseSearch.EvaluateNewSample(
                bestSample,
                newSample,
                searchMode,
                exitThreshold)
                == PhaseSearch.LoopStatus.Continue)
                {
                currentPhase += phaseStep;
                this.smu200a.SetSourceDeltaPhase(currentPhase);
                sampleConfig = new MeasurementSampleConfiguration(
                    sweepConf,
                    supplyVoltage,
                    frequency,
                    inputPower,
                    currentPhase,
                    offset);
                newSample = TakeMeasurementSample(sampleConfig);
                this.CurrentSweepProgress.CurrentPoint++;
                this.CurrentSweepProgress.NumberOfPoints++;
                samples.Add(newSample);
                var newRes = PhaseSearch.PeakTroughComparison(
                    searchMode, bestSample, newSample);
                if (newRes == PhaseSearch.NewSampleResult.Better)
                    {
                    bestSample = newSample;
                    }
                }
            }

        public double FindSearchDirection(
            PhaseSearch.Mode searchMode,
            List<MeasurementSample> samples,
            MeasurementSample bestSample,
            MeasurementSweepConfiguration sweepConf,
            CurrentOffset offset,
            double supplyVoltage,
            double frequency,
            double inputPower,
            double phaseStep)
            {
            var corePhase = bestSample.Conf.Phase;
            double scalar = 0.0;

            while (true)
                {
                scalar += 1.0;

                var phasePos = corePhase + (scalar * phaseStep);
                var sampleConfigPos = new MeasurementSampleConfiguration(
                    sweepConf,
                    supplyVoltage,
                    frequency,
                    inputPower,
                    phasePos,
                    offset);
                this.smu200a.SetSourceDeltaPhase(phasePos);
                var samplePos = TakeMeasurementSample(sampleConfigPos);
                var gradientPos = PhaseSearch.GetGradient(bestSample, samplePos);

                var phaseNeg = corePhase - (scalar * phaseStep);
                var sampleConfigNeg = new MeasurementSampleConfiguration(
                    sweepConf,
                    supplyVoltage,
                    frequency,
                    inputPower,
                    phasePos,
                    offset);
                this.smu200a.SetSourceDeltaPhase(phasePos);
                var sampleNeg = TakeMeasurementSample(sampleConfigPos);
                var gradientNeg = 
                    PhaseSearch.GetGradient(bestSample, sampleNeg);

                samples.Add(samplePos);
                samples.Add(sampleNeg);

                // If both adjacent points move in the same direction
                // then there's no clear direction to move in
                if (gradientNeg == gradientPos)
                    {
                    continue;
                    }

                if(searchMode == PhaseSearch.Mode.Peak)
                    {
                    return (gradientPos == PhaseSearch.Gradient.Positive) ? 1.0 : -1.0;
                    }
                else
                    {
                    return (gradientPos == PhaseSearch.Gradient.Negative) ? 1.0 : -1.0;
                    }
                }
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

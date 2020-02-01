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
        public double PsuNominalVoltage { get; set; } = 2.2;
        public double PsuCurrentLimit { get; set; } = 0.3;
        public int PsuRampUpStepTimeMilliseconds { get; set; } = 100;
        public double RampVoltageStep { get; set; } = 0.1;
        public bool PsuPlus10Percent { get; set; } = true;
        public bool PsuMinus10Percent { get; set; } = true;

        // Spectrum Analyser
        public double Rsa3408ChannelBandwidth { get; set; } = 25e3;
        public double Rsa3408FrequencySpan { get; set; } = 1e6;
        // UI
        public Queue<String> LogQueue = new Queue<string>();
        List<CheckBox> PsuChannelEnableCheckboxes;
        System.Windows.Threading.DispatcherTimer dispatcherTimer;
        // File IO
        public string ResultsSavePath { get; set; } =
            "C:\\Users\\matth\\Downloads\\x.csv";
        public string Smr20OffsetsPath { get; set; } =
            "C:\\Users\\matth\\Downloads\\Cable_5_offset_file.cor";
        public string E8257dOffsetsPath { get; set; } =
            "C:\\Users\\matth\\Downloads\\Cable_2_offset_file.cor";
        public string SpectrumAnalzyerOffsetsPath { get; set; } =
            "C:\\Users\\matth\\Downloads\\Cable_7_offset_file.cor";
        // Signal Generators
        // Measurement
        StreamWriter outFile;
        public bool PeakTroughSearch { get; set; } = true;
        SweepProgress CurrentSweepProgress = new SweepProgress(false, 0, 0);
        public double EstimatedTimePerSample { get; set; } = 0.32;
        public System.Diagnostics.Stopwatch MeasurementStopWatch =
            new System.Diagnostics.Stopwatch();

        // Interface methods
        DeviceCommands Commands;

        public MainWindow()
            {
            InitializeComponent();
            this.DataContext = this;
            PopulatePsuCheckboxList();
            SetUpDispatcherTimer();
            UpdateEstimatedMeasurementTime();

            this.ResultsSavePathTextBlock.Text = 
                this.ResultsSavePath;
            this.SignalGenerator1OffsetsFilePathTextBlock.Text = 
                this.Smr20OffsetsPath;
            this.SignalGenerator2OffsetsFilePathTextBlock.Text = 
                this.E8257dOffsetsPath;
            this.SpectrumAnalzyerOffsetsFilePathTextBlock.Text = 
                this.SpectrumAnalzyerOffsetsPath;

            this.Commands = this.SetUpVisaDevices();
            this.Commands.ResetDevices();
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

        // This is the dirty method that is edited when
        // the equipment used in the program is changed
        private DeviceCommands SetUpVisaDevices()
            {
            var psuChannelStates = new List<bool>()
                {
                this.PsuChannel1Enable.IsChecked == true,
                this.PsuChannel2Enable.IsChecked == true,
                this.PsuChannel3Enable.IsChecked == true,
                this.PsuChannel4Enable.IsChecked == true
                };

            var devices = VisaSetup.SetUpConnections(psuChannelStates);

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
                }

            void preMeasurementSetup(MeasurementConfig sweepConf)
                {
                // DC supply
                var psu = devices.Hp6624a;
                psu.SetAllChannelVoltagesToZero();
                psu.SetChannelOutputStatesStrong();
                psu.SetActiveChannelsCurrent(this.PsuCurrentLimit);

                // Spectrum Analyser
                var rsa = devices.Rsa3408a;
                rsa.SetSpectrumChannelPowerMeasurementMode();
                rsa.SetContinuousMode(continuousOn: false);
                rsa.SetFrequencyCenter(sweepConf.Frequencies[0]);
                rsa.SetFrequencySpan(sweepConf.MeasurementFrequencySpan);
                rsa.SetChannelBandwidth(sweepConf.MeasurementChannelBandwidth);
                rsa.StartSignalAcquisition();

                //rsa3408a.SetMarkerState(markerNumber: 1, view: 1, on: true);
                //rsa3408a.SetMarkerXToPositionMode(1,1);
                // When the frequency changes, the marker should automatially
                // track to the new centre frequency
                //rsa3408a.SetMarkerXValue(markerNumber: 1, view: 1, xValue: conf.Frequencies[0]);
                // Set the power sources to an extremely low
                // power before starting the sweep
                // in case they default to some massive value
                sweepConf.Commands.SetRfOutputState(on: false);
                sweepConf.Commands.SetInputPower(-60, offset1: 0, offset2: 0);
                sweepConf.Commands.SetRfOutputState(on: true);
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
                devices.Rsa3408a.ReadSpectrumChannelPower);
            }

        private void AddNewLogLine(string line)
            {
            this.SweepLogTextBox.Text = $"{this.SweepLogTextBox.Text}{line}\n";
            }

        private MeasurementConfig ParseMeasurementConfiguration()
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

            return new MeasurementConfig(
                frequencySettings,
                powerSettings,
                phaseSettings,
                this.ChipTemperature,
                this.ChipCorner,
                voltages,
                this.Rsa3408ChannelBandwidth,
                this.Rsa3408FrequencySpan,
                this.ResultsSavePath,
                this.Smr20OffsetsPath,
                this.E8257dOffsetsPath,
                this.SpectrumAnalzyerOffsetsPath,
                this.PeakTroughSearch,
                new PhaseSearchConfig(
                    this.PeakSearchSettingsTextBox.Text,
                    this.TroughSearchSettingsTextBox.Text),
                this.Commands);
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
            bool checkPath(string path, string name)
                {
                if (path == "")
                    {
                    this.LogQueue.Enqueue($"No {name} file path entered.");
                    return false;
                    }
                return true;
                }
            return checkPath(this.ResultsSavePath, "save")
                && checkPath(this.Smr20OffsetsPath, "SMU200A offset")
                && checkPath(this.E8257dOffsetsPath, "E8257D offset")
                && checkPath(this.SpectrumAnalzyerOffsetsPath, "RSA3408A offset");
            }

        private void RunSweep(MeasurementConfig sweepConf)
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
                + ", Measured Channel Power (dBm)"
                + ", Measured Channel Power (W)"
                + ", Measured Output Power (dBm)"
                + ", Calibrated Output Power (dBm)"
                + ", Signal Generator 1 Input Power Offset (dB)"
                + ", Signal Generator 2 Input Power Offset (dB)"
                + ", Spectrum Analyzer Measurement Offset (dB)"
                + ", Calibrated Drain Efficiency (%)"
                + ", Calibrated Power Added Efficiency (%)"
                + ", Measured Channel Power (dBm)"
                + ", Measurement Frequency Span (Hz)"
                + ", Channel Measurement Bandwidth (Hz)"
                + ", Calibrated Gain (dB)";

            var numActiveChannels = 
                sweepConf.Commands.GetPsuChannelStates().Count(c => c);
            for (int i = 0; i < numActiveChannels; i++)
                {
                int channel = i + 1;
                headerLine += $", DC Current Channel {channel} (A)";
                }

            outputFile.WriteLine(headerLine);
            var numberOfPoints = sweepConf.MeasurementPoints;
            this.CurrentSweepProgress.CurrentPoint = 1;
            this.CurrentSweepProgress.NumberOfPoints = numberOfPoints;
            this.CurrentSweepProgress.Running = true;
            this.MeasurementStopWatch.Restart();

            // Pre-setup
            sweepConf.Commands.PreMeasurementSetup(sweepConf);

            // All of the sweeps are <= as we want to include the stop
            // value in the sweep
            foreach (var voltage in sweepConf.Voltages)
                {
                sweepConf.Commands.SetDcVoltageStepped(
                    voltage, 
                    this.RampVoltageStep, 
                    this.PsuRampUpStepTimeMilliseconds);

                foreach (var frequency in sweepConf.Frequencies)
                    {
                    var offsets = new CurrentOffset(
                        sweepConf.GetOffsets1.GetOffset(frequency),
                        sweepConf.GenOffsets2.GetOffset(frequency),
                        sweepConf.SpectrumAnalyzerOffsets.GetOffset(frequency));
                    sweepConf.Commands.SetFrequency(frequency);
                    foreach (var inputPower in sweepConf.InputPowers)
                        {
                        outputFile.Flush();
                        if (!this.CurrentSweepProgress.Running)
                            {
                            outputFile.Flush();
                            outputFile.Close();
                            sweepConf.Commands.SetRfOutputState(on: false);
                            return;
                            }
                        sweepConf.Commands.SetInputPower(
                            inputPower, offsets.Smu200a, offsets.E8257d);

                        var phaseSweepConfig = new PhaseSweepConfig(
                            sweepConf, offsets, voltage, frequency, inputPower);
                        var samples = PhaseSearch.MeasurementPhaseSweep(
                            phaseSweepConfig);

                        foreach (var sample in samples)
                            {
                            Measurement.SaveSample(outputFile, sample);
                            }
                        }
                    }
                }
            outputFile.Close();
            this.MeasurementStopWatch.Stop();
            this.MeasurementStopWatch.Reset();
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

        private string UserLoadOffset(string name, TextBlock tb)
            {
            tb.Text = GetOffsetsPath(name);
            return tb.Text;
            }

        private void LoadSignalGenerator1OffsetsButton_Click(
            object sender, RoutedEventArgs e)
            {
            this.Smr20OffsetsPath = 
                UserLoadOffset("Signal Generator 1", 
                this.SignalGenerator1OffsetsFilePathTextBlock);
            }

        private void LoadSignalGenerator2OffsetsButton_Click(
            object sender, RoutedEventArgs e)
            {
            this.E8257dOffsetsPath = UserLoadOffset(
                "Signal Generator 2", 
                this.SignalGenerator2OffsetsFilePathTextBlock);
            }

        private void LoadSpectrumAnalzyeradOffsetsButton_Click(
            object sender, RoutedEventArgs e)
            {
            this.SpectrumAnalzyerOffsetsPath = UserLoadOffset(
                "Spectrum Analyzer", 
                this.SpectrumAnalzyerOffsetsFilePathTextBlock);
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
            {
            if(this.outFile != null)
                {
                this.outFile.Flush();
                this.outFile.Close();
                }
            }
        }
    }

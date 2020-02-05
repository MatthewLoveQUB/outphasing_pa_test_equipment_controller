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
        public bool PsuPlus10Percent { get; set; } = false;
        public bool PsuMinus10Percent { get; set; } = false;
        public bool PsuChannel1On { get; set; } = false;
        public bool PsuChannel2On { get; set; } = true;
        public bool PsuChannel3On { get; set; } = true;
        public bool PsuChannel4On { get; set; } = false;
        public List<bool> PsuChannelStates
            {
            get
                {
                return new List<bool> {
                    this.PsuChannel1On,
                    this.PsuChannel2On,
                    this.PsuChannel3On,
                    this.PsuChannel4On
                    };
                }
            }


        // Spectrum Analyser
        public double Rsa3408ChannelBandwidth { get; set; } = 25e3;
        public double Rsa3408FrequencySpan { get; set; } = 1e6;
        // UI
        public Queue<String> LogQueue = new Queue<string>();
        System.Windows.Threading.DispatcherTimer dispatcherTimer;
        // File IO
        private static string MDir =
            "C:\\Users\\matth\\OneDrive - Queen's University Belfast\\pa1_meas\\";
        public string ResultsSavePath { get; set; } = "";
        public string SignalGenerator1OffsetsPath { get; set; } =
            MDir + "Cable_8_offset_file.cor";
        public string SignalGenerator2OffsetsPath { get; set; } =
            MDir + "Cable_2_offset_file.cor";
        public string SpectrumAnalzyerOffsetsPath { get; set; } =
            MDir + "Cable_7_offset_file.cor";
        // Signal Generators
        // Measurement
        SweepProgress CurrentSweepProgress = 
            new SweepProgress(running: false, currentPoint: 0);
        public double EstimatedTimePerSample { get; set; } = 0.5;
        public System.Diagnostics.Stopwatch MeasurementStopWatch =
            new System.Diagnostics.Stopwatch();
        DeviceCommands Commands;


        // Phase Search Settings
        public int EstimatedPhaseSamples { get; set; } = 90;
        public PhaseSearch.SearchType PhaseSearchType
            {
            get
                {
                var option = this.PhaseSearchTypeComboBox.Text;
                if (option == "Lowest Value")
                    {
                    return PhaseSearch.SearchType.LowestValue;
                    }
                else if (option == "Highest Gradient")
                    {
                    return PhaseSearch.SearchType.HighestGradient;
                    }
                else if (option == "None")
                    {
                    return PhaseSearch.SearchType.None;
                    }
                else
                    {
                    throw new Exception("Could not parse Search Type");
                    }
                }
            }
        // Lowest Value
        public int DirectionSearchIterationLimit { get; set; } = 5;
        public int PhaseSearchIterationLimit { get; set; } = 500;
        public int PhaseSearchNumCenterSamples { get; set; } = 10;
        // Gradient search
        public double GradientSearchMinimaCoarseStep { get; set; } = 3;
        public double GradientSearchMinimaFineStep { get; set; } = 0.1;
        public int GradientSearchMinimaNumFineSteps { get; set; } = 20;
        public double GradientSearchMaximaCoarseStep { get; set; } = 3;
        public int GradientSearchMaximaNumCoarseSteps { get; set; } = 8;

        public MainWindow()
            {
            InitializeComponent();
            this.DataContext = this;
            SetUpDispatcherTimer();
            UpdateEstimatedMeasurementTime();

            this.ResultsSavePathTextBlock.Text = 
                this.ResultsSavePath;
            this.SignalGenerator1OffsetsFilePathTextBlock.Text = 
                this.SignalGenerator1OffsetsPath;
            this.SignalGenerator2OffsetsFilePathTextBlock.Text = 
                this.SignalGenerator2OffsetsPath;
            this.SpectrumAnalzyerOffsetsFilePathTextBlock.Text = 
                this.SpectrumAnalzyerOffsetsPath;

            this.Commands = VisaSetup.SetUpVisaDevices(
                this.PsuChannelStates, this.PsuCurrentLimit);
            this.Commands.ResetDevices();
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

        private void AddNewLogLine(string line)
            {
            this.SweepLogTextBox.Text = $"{this.SweepLogTextBox.Text}{line}\n";
            }

        public List<double> RoundVals(List<double> freqs, double minVal)
            {
            var outVals = new List<double>();

            foreach(var freq in freqs)
                {
                var x = freq / minVal;
                var y = Math.Floor(x);
                var z = y * minVal;
                outVals.Add(z);
                }
            return outVals;
            }

        private MeasurementConfig ParseMeasurementConfiguration()
            {
            var rawFrequencySettings = this.FrequencySweepSettingsControl.Values;
            var frequencySettings = this.RoundVals(rawFrequencySettings, 1e6);
            var rawPowerSettings = this.PowerSweepSettingsControl.Values;
            var powerSettings = this.RoundVals(rawPowerSettings, 0.01);
            var rawPhaseSettings = this.PhaseSweepSettingsControl.Values;
            var phaseSettings = this.RoundVals(rawPhaseSettings, 0.1);

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
                this.SignalGenerator1OffsetsPath,
                this.SignalGenerator2OffsetsPath,
                this.SpectrumAnalzyerOffsetsPath,
                new PhaseSearchConfig(
                    this.PhaseSearchType,
                    this.PeakSearchSettingsTextBox.Text,
                    this.TroughSearchSettingsTextBox.Text,
                    this.DirectionSearchIterationLimit,
                    this.PhaseSearchIterationLimit,
                    this.PhaseSearchNumCenterSamples,
                    this.GradientSearchMinimaCoarseStep,
                    this.GradientSearchMinimaFineStep,
                    this.GradientSearchMinimaNumFineSteps,
                    this.GradientSearchMaximaCoarseStep,
                    this.GradientSearchMaximaNumCoarseSteps),
                this.Commands,
                this.RampVoltageStep,
                this.PsuRampUpStepTimeMilliseconds);
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
                Measurement.RunSweep(
                    measurementSettings, 
                    ref this.CurrentSweepProgress);
            });
            this.MeasurementStopWatch.Restart();
            }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
            {
            // To avoid memory leaks, wipe the log every tick
            this.SweepLogTextBox.Text = "";

            if (!this.CurrentSweepProgress.Running)
                {
                return;
                }
            var curPt = this.CurrentSweepProgress.CurrentPoint;
            var timeElapsed = this.MeasurementStopWatch.Elapsed;
            var samplesPerSecond = curPt / (double)timeElapsed.TotalSeconds;
            var secondsPerSample = 1 / samplesPerSecond;

            // Print elapsed time
            var msg = "Elapsed Time: "
                + $"{timeElapsed.Days} days "
                + $"{timeElapsed.Hours} hours "
                + $"{timeElapsed.Minutes} minutes "
                + $"{timeElapsed.Seconds} seconds";
            AddNewLogLine(msg);

            // Print sample rate
            AddNewLogLine($"Sample Rate = {samplesPerSecond:F2} S/s");
            AddNewLogLine($"Sample Time = {secondsPerSample:F2} s");

            // Print estimated total time
            var estTime = GetEstimatedMeasurementTime();
            msg = "Estimated Total Time: "
                + $"{estTime.Days} days "
                + $"{estTime.Hours} hours "
                + $"{estTime.Minutes} minutes "
                + $"{estTime.Seconds} seconds";
            AddNewLogLine(msg);

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
                && checkPath(this.SignalGenerator1OffsetsPath, 
                    "Generator 1 offset")
                && checkPath(this.SignalGenerator2OffsetsPath, 
                    "Generator 2 offset")
                && checkPath(this.SpectrumAnalzyerOffsetsPath, 
                    "Spectrum Analzyer offset");
            }

        private void SweepSettingsControl_LostFocus(
            object sender, RoutedEventArgs e)
            {
            UpdateEstimatedMeasurementTime();
            }

        private TimeSpan GetEstimatedMeasurementTime()
            {
            var voltagePoints = 1
                + Convert.ToInt64(this.PsuPlus10Percent)
                + Convert.ToInt64(this.PsuMinus10Percent);
            var nPoints = voltagePoints
                * this.FrequencySweepSettingsControl.NSteps
                * this.PowerSweepSettingsControl.NSteps
                * this.EstimatedPhaseSamples;
            var secondsRequired = this.EstimatedTimePerSample * nPoints;
            return TimeSpan.FromSeconds(secondsRequired);
            }

        private void UpdateEstimatedMeasurementTime()
            {
            var estTime = GetEstimatedMeasurementTime();
            this.EstimatedSimulationTimeTextBlock.Text =
              $"Estimated Measurement Time = "
                + $"{estTime.Days} days "
                + $"{estTime.Hours} hours "
                + $"{estTime.Minutes} minutes";
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
            this.SignalGenerator1OffsetsPath = 
                UserLoadOffset("Signal Generator 1", 
                this.SignalGenerator1OffsetsFilePathTextBlock);
            }

        private void LoadSignalGenerator2OffsetsButton_Click(
            object sender, RoutedEventArgs e)
            {
            this.SignalGenerator2OffsetsPath = UserLoadOffset(
                "Signal Generator 2", 
                this.SignalGenerator2OffsetsFilePathTextBlock);
            }

        private void LoadSpectrumAnalzyerOffsetsButton_Click(
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
        }
    }

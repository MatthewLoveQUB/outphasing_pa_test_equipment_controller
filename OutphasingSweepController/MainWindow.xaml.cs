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
        public int PsuRampUpStepTime { get; set; } = 10;
        public double RampVoltageStep { get; set; } = 0.1;
        // Spectrum Analyser
        TektronixRSA3408A rsa3408a;
        public double Rsa3408Bandwidth { get; set; } = 10e6;
        // UI
        public Queue<String> LogQueue = new Queue<string>();
        List<CheckBox> PsuChannelEnableCheckboxes;
        System.Windows.Threading.DispatcherTimer dispatcherTimer;
        // File IO
        public string ResultsSavePath { get; set; } = "";
        public string Smu200aOffsetsPath { get; set; } = "";
        public string E8257dOffsetsPath { get; set; } = "";
        // Signal Generators
        RS_SMU200A smu200a;
        KeysightE8257D e8257d;
        // Measurement
        SweepProgress CurrentSweepProgress = new SweepProgress(false, 0, 0);
        MeasurementSample CurrentSample;
        public double EstimatedTimePerSample { get; set; } = 0.1;
        public System.Diagnostics.Stopwatch MeasurementStopWatch = new System.Diagnostics.Stopwatch();

        public MainWindow()
            {
            InitializeComponent();
            this.DataContext = this;
            PopulatePsuCheckboxList();
            SetUpDefaultLastSampleText();
            //SetUpVisaConnections();
            SetUpDispatcherTimer();
            }

        private void PopulatePsuCheckboxList()
            {
            PsuChannelEnableCheckboxes =
                new List<CheckBox>()
                    {
                    PsuChannel1Enable,
                    PsuChannel2Enable,
                    PsuChannel3Enable,
                    PsuChannel4Enable
                    };
            }

        private void SetUpDispatcherTimer()
            {
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
            }

        private void SetUpDefaultLastSampleText()
            {
            LastSampleTextBlock.Text =
                string.Format(Constants.LastSampleTemplate,
                "???",
                "???",
                "???",
                "???",
                "???",
                "???",
                "???");
            }

        private void SetUpVisaConnections()
            {
            var psuChannelStates = new List<bool>()
                {
                PsuChannel1Enable.IsChecked == true,
                PsuChannel2Enable.IsChecked == true,
                PsuChannel3Enable.IsChecked == true,
                PsuChannel4Enable.IsChecked == true
                };
            hp6624a = new HP6624A(GetVisaAddress("HP6624A"), psuChannelStates);
            rsa3408a = new TektronixRSA3408A(GetVisaAddress("Tektronix RSA3408"));
            smu200a = new RS_SMU200A(GetVisaAddress("R&S SMU200A"));
            e8257d = new KeysightE8257D(GetVisaAddress("Keysight E8257D"));
            }

        private string GetVisaAddress(string deviceName)
            {
            var visaWindow = new list_visa_devices_dialogue.MainWindow(deviceName);
            visaWindow.ShowDialog();
            return visaWindow.SelectedAddress;
            }

        private void AddNewLogLine(string line)
            {
            var oldLog = SweepLogTextBox.Text;
            var newLog = oldLog + line + "\n";
            SweepLogTextBox.Text = newLog;
            }

        private MeasurementSweepConfiguration ParseMeasurementConfiguration()
            {
            var frequencySettings = FrequencySweepSettingsControl.Values;
            var powerSettings = PowerSweepSettingsControl.Values;
            var phaseSettings = PhaseSweepSettingsControl.Values;
            var voltages = new List<Double>() {
                0.9 * PsuNominalVoltage,
                PsuNominalVoltage,
                1.1 * PsuNominalVoltage };
            return new MeasurementSweepConfiguration(
                frequencySettings,
                powerSettings,
                phaseSettings,
                ChipTemperature,
                ChipCorner,
                voltages,
                Rsa3408Bandwidth,
                ResultsSavePath,
                Smu200aOffsetsPath,
                E8257dOffsetsPath);
            }

        private void StartSweepButton_Click(object sender, RoutedEventArgs e)
            {
            if (!MeasurementVariablesCheck())
                {
                return;
                }

            var measurementSettings = ParseMeasurementConfiguration();

            Task.Factory.StartNew(() =>
            {
                RunSweep(measurementSettings);
            });
            }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
            {
            if (CurrentSweepProgress.Running)
                {
                var msg = string.Format("On task {0} of {1}", CurrentSweepProgress.CurrentPoint, CurrentSweepProgress.NumberOfPoints);
                AddNewLogLine(msg);
                var timeElapsed = MeasurementStopWatch.Elapsed;
                var pointsRemaining = (CurrentSweepProgress.NumberOfPoints - CurrentSweepProgress.CurrentPoint);
                var timeScaler = (pointsRemaining / CurrentSweepProgress.CurrentPoint);
                var estimatedTime = TimeSpan.FromTicks(timeElapsed.Ticks * timeScaler);
                msg = string.Format(
                    "Elapsed Time: {0} days {1} hours {2} minutes",
                    timeElapsed.Days,
                    timeElapsed.Hours,
                    timeElapsed.Minutes);
                AddNewLogLine(msg);
                msg = string.Format(
                    "Estimated Remaining Time: {0} days {1} hours {2} minutes",
                    estimatedTime.Days,
                    estimatedTime.Hours,
                    estimatedTime.Minutes);
                if(CurrentSample != null)
                    {
                    LastSampleTextBlock.Text =
                        string.Format(Constants.LastSampleTemplate,
                        CurrentSample.Frequency,
                        CurrentSample.InputPowerdBm,
                        CurrentSample.GaindB,
                        CurrentSample.PowerAddedEfficiency,
                        CurrentSample.DrainEfficiency,
                        CurrentSample.MeasuredPowerDcWatts);
                    }
                }

            // Empty the log queue
            while(LogQueue.Count > 0)
                {
                var message = LogQueue.Dequeue();
                AddNewLogLine(message);
                }
            }

        // Ramp the voltage slowly
        // Assumes that all channels are at the same voltage
        private void SetPsuVoltageStepped(double newVoltage)
            {
            // Step should be positive before we alter it depending on
            // whether we're ramping up or down
            var step = Math.Abs(RampVoltageStep);

            int numChannels = PsuChannelEnableCheckboxes.Count;

            // Read the current voltage
            double currentVoltage = 0;
            for (int i = 0; i < numChannels; i++)
                {
                int channelNumber = i + 1;
                bool channelEnabled = PsuChannelEnableCheckboxes[i].IsChecked == true;
                if (channelEnabled)
                    {
                    currentVoltage = hp6624a.GetChannelVoltageSetting(channelNumber);
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

            int numSteps = (int)(Math.Abs(currentVoltage - newVoltage) / step);
            var intermediateVoltage = currentVoltage;

            for (int currentStep = 0; currentStep < numSteps; currentStep++)
                {
                intermediateVoltage += step;
                hp6624a.SetActiveChannelsVoltages(intermediateVoltage);
                Thread.Sleep(PsuRampUpStepTime);
                }

            // In case we overshot the voltage then just set it to the final voltage
            hp6624a.SetActiveChannelsVoltages(newVoltage);
            }

        bool MeasurementVariablesCheck()
            {
            if (ResultsSavePath == "")
                {
                LogQueue.Enqueue("No save path entered.");
                return false;
                }

            if (Smu200aOffsetsPath == "")
                {
                LogQueue.Enqueue("No SMU200A amplitude file chosen.");
                return false;
                }

            if (E8257dOffsetsPath == "")
                {
                LogQueue.Enqueue("No E8257D amplitude file chosen.");
                return false;
                }

            return true;
            }

        private void RunSweep(MeasurementSweepConfiguration conf)
            {
            var outputFile = new StreamWriter(conf.OutputFilePath);
            outputFile.WriteLine(
                "Frequency (Hz)" // 1
                + ", Input Power (dBm)"
                + ", Phase (deg)"
                + ", Temperature (Celcius)"
                + ", Corner" // 5
                + ", Supply Voltage (V)"
                + ", DC Power (W)"
                + ", Output Power (dBm)"
                + ", SMU200A Input Power Offset (dB)"
                + ", E8257D Input Power Offset (dB)" // 10
                + ", Drain Efficiency (%)"
                + ", Power Added Efficiency (%)"
                + ", Channel Power (dBm)"
                + ", Channel Measurement Bandwidth (Hz)"
                + ", Gain (dB)"); // 15

            var numberOfPoints = conf.MeasurementPoints;
            CurrentSweepProgress.CurrentPoint = 1;
            CurrentSweepProgress.NumberOfPoints = numberOfPoints;
            CurrentSweepProgress.Running = true;
            MeasurementStopWatch.Restart();

            // Pre-setup
            // DC supply
            hp6624a.SetAllChannelVoltagesToZero();
            hp6624a.SetChannelOutputStatesStrong();
            hp6624a.SetActiveChannelsCurrent(PsuCurrentLimit);

            // Spectrum Analyser
            rsa3408a.SetFrequencyCenter(conf.Frequencies[0]);
            rsa3408a.SetContinuousMode(continuousOn: false);
            rsa3408a.SetFrequencySpan(conf.MeasurementChannelBandwidth);
            
            // Set the power sources to an extremely low
            // power before starting the sweep
            // in case they default to some massive value
            smu200a.SetRfOutputState(on: false);
            e8257d.SetRfOutputState(on: false);
            smu200a.SetPowerLevel(-60);
            e8257d.SetPowerLevel(-60);
            smu200a.SetRfOutputState(on: true);
            e8257d.SetRfOutputState(on: true);

            // All of the sweeps are <= as we want to include the stop
            // value in the sweep
            foreach (var voltage in conf.Voltages)
                {
                SetPsuVoltageStepped(voltage);
                foreach (var frequency in conf.Frequencies)
                    {
                    // Set the frequency
                    rsa3408a.SetFrequencyCenter(frequency);
                    rsa3408a.SetMarkerXValue(markerNumber: 1, xValue: frequency);
                    smu200a.SetSourceFrequency(frequency);
                    e8257d.SetSourceFrequency(frequency);

                    foreach (var inputPower in conf.InputPowers)
                        {
                        // Get the loss offset for this frequency
                        var offsetSmu200a = conf.Smu200aOffsets.GetOffset(frequency);
                        var offsetE8257d = conf.E8257dOffsets.GetOffset(frequency);

                        // Set the power
                        smu200a.SetPowerLevel(inputPower, offsetSmu200a);
                        e8257d.SetPowerLevel(inputPower, offsetE8257d);

                        foreach (var phase in conf.Phases)
                            {
                            smu200a.SetSourceDeltaPhase(phase);
                            CurrentSweepProgress.CurrentPoint += 1;

                            /// Do the measurement
                            // Sample the output signal
                            rsa3408a.StartSignalAcquisition();
                            // Wait until signal is acquired
                            while (rsa3408a.OperationComplete())
                                {
                                // Do nothing
                                }
                            // Record the sample
                            CurrentSample = TakeMeasurementSample(conf, voltage, frequency, inputPower, phase, offsetSmu200a, offsetE8257d);
                            SaveMeasurementSample(outputFile, CurrentSample);
                            }
                        }
                    }
                }
            outputFile.Close();
            MeasurementStopWatch.Stop();
            MeasurementStopWatch.Reset();
            }

        private MeasurementSample TakeMeasurementSample(
            MeasurementSweepConfiguration conf,
            double supplyVoltage,
            double frequency,
            double inputPower,
            double phase,
            double offsetSmu,
            double offsetE8257d)
            {
            var measuredDcPowerWatts = hp6624a.GetActiveChannelsPowerWatts();
            double measuredPoutdBm = rsa3408a.GetMarkerYValue(markerNumber: 1);
            double channelPowerdBm = rsa3408a.ReadSpectrumChannelPower();
            return new MeasurementSample(
                frequency,
                inputPower,
                phase,
                conf.Temperature,
                conf.Corner,
                supplyVoltage,
                measuredDcPowerWatts,
                offsetSmu,
                offsetE8257d,
                measuredPoutdBm,
                conf.MeasurementChannelBandwidth,
                channelPowerdBm);
            }

        private void SaveMeasurementSample(StreamWriter outputFile, MeasurementSample sample)
            {
            outputFile.WriteLine(
                "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14}",
                sample.Frequency,
                sample.InputPowerdBm,
                sample.PhaseDeg,
                sample.Temperature,
                sample.Corner,
                sample.SupplyVoltage,
                sample.MeasuredPowerDcWatts,
                sample.MeasuredOutputPowerdBm,
                sample.OffsetSmu200adB,
                sample.OffsetE8557ddB,
                sample.DrainEfficiency,
                sample.PowerAddedEfficiency,
                sample.MeasuredChannelPowerdBm,
                sample.RsaMeasurementBandwidth,
                sample.GaindB);
            outputFile.Flush();
            }

        private void SweepSettingsControl_LostFocus(object sender, RoutedEventArgs e)
            {
            UpdateEstimatedSimulationTime();
            }

        private void UpdateEstimatedSimulationTime()
            {
            int voltagePoints = 3;
            var freqPoints = FrequencySweepSettingsControl.NSteps;
            var powerPoints = PowerSweepSettingsControl.NSteps;
            var phasePoints = PhaseSweepSettingsControl.NSteps;
            var nPoints = EstimatedTimePerSample * voltagePoints * freqPoints * powerPoints * phasePoints;
            var estimatedSimulationTime = TimeSpan.FromSeconds(nPoints);
            EstimatedSimulationTimeTextBlock.Text = 
                string.Format(
                    "Estimated Simulation Time = {0} days, {1} hours, {2} minutes", 
                    estimatedSimulationTime.Days,
                    estimatedSimulationTime.Hours, 
                    estimatedSimulationTime.Minutes);
            }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
            {
            var saveDialog = new Microsoft.Win32.SaveFileDialog();
            saveDialog.Filter = "CSV Files (.csv)|*.csv";
            saveDialog.DefaultExt = ".csv";
            var dialogSuccess = (saveDialog.ShowDialog() == true);

            if (dialogSuccess)
                {
                ResultsSavePath = saveDialog.FileName;
                ResultsSavePathTextBlock.Text = ResultsSavePath;
                }
            }

        private void LoadSmu200aOffsetsButton_Click(object sender, RoutedEventArgs e)
            {
            Smu200aOffsetsPath = GetOffsetsPath("SMU200A");
            Smu200aOffetsFilePathTextBlock.Text = Smu200aOffsetsPath;
            }

        private void LoadE8257dOffsetsButton_Click(object sender, RoutedEventArgs e)
            {
            E8257dOffsetsPath = GetOffsetsPath("E8257D");
            E8257dOffetsFilePathTextBlock.Text = E8257dOffsetsPath;
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
        }
    }

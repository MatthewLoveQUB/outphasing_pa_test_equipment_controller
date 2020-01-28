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
        public int PsuRampUpStepTimeMilliseconds { get; set; } = 100;
        public double RampVoltageStep { get; set; } = 0.1;
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
        SweepProgress CurrentSweepProgress = new SweepProgress(false, 0, 0);
        MeasurementSample CurrentSample;
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
            UpdateEstimatedSimulationTime();
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
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 2);
            dispatcherTimer.Start();
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
                hpAddress = GetVisaAddress("HP6624A");
                rsaAddress = GetVisaAddress("Tektronix RSA3408");
                smaAddress = GetVisaAddress("R&S SMU200A");
                e82Address = GetVisaAddress("Keysight E8257D");
                }

            hp6624a = new HP6624A(hpAddress, psuChannelStates);
            rsa3408a = new TektronixRSA3408A(rsaAddress);
            smu200a = new RS_SMU200A(smaAddress);
            e8257d = new KeysightE8257D(e82Address);
            rsa3408a.ResetDevice();
            smu200a.ResetDevice();
            e8257d.ResetDevice();
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
            SweepLogTextBox.Text = $"{SweepLogTextBox.Text}{line}\n";
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
                Rsa3408ChannelBandwidth,
                Rsa3408FrequencySpan,
                ResultsSavePath,
                Smu200aOffsetsPath,
                E8257dOffsetsPath,
                Rsa3408aOffsetsPath);
            }

        private void StartSweepButton_Click(object sender, RoutedEventArgs e)
            {
            if (!MeasurementVariablesCheck()) { return; }

            var measurementSettings = ParseMeasurementConfiguration();
            Task.Factory.StartNew(() =>
            {
                RunSweep(measurementSettings);
            });
            }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
            {
            // To avoid memory leaks, wipe the log every tick
            SweepLogTextBox.Text = "";

            if (CurrentSweepProgress.Running)
                {
                var curPt = CurrentSweepProgress.CurrentPoint;
                var nPts = CurrentSweepProgress.NumberOfPoints;
                var timeElapsed = MeasurementStopWatch.Elapsed;
                var ptsRemaining = nPts - curPt;
                var timeScaler = (double)ptsRemaining / (double)curPt;
                var estimatedTime = 
                    TimeSpan.FromTicks(timeElapsed.Ticks * (long)timeScaler);
                var samplesPerSecond = curPt / (double)timeElapsed.TotalSeconds;
                var secondsPerSample = 1 / samplesPerSecond;

                if (CurrentSample != null)
                    {
                    LastSampleTextBlock.Text = 
                        $"Pout: {CurrentSample.CalibratedOutputPowerdBm} dBm\n"
                        + $"Frequency: {CurrentSample.Frequency} Hz\n"
                        + $"Pin: {CurrentSample.InputPowerdBm} dBm\n"
                        + $"Gain: {CurrentSample.CalibratedGaindB} dB\n"
                        + $"PAE: {CurrentSample.CalibratedPowerAddedEfficiency} %\n"
                        + $"Drain Efficiency: {CurrentSample.CalibratedDrainEfficiency} %\n"
                        + $"DC Power: {CurrentSample.MeasuredPowerDcWatts} W\n";
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

            // Read the current voltage
            double currentVoltage = 0;
            for (int i = 0; i < HP6624A.NumChannels; i++)
                {
                int channelNumber = i + 1;
                bool channelEnabled = hp6624a.ChannelStates[i];
                if (channelEnabled)
                    {
                    currentVoltage = 
                        hp6624a.GetChannelVoltageSetting(channelNumber);
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
                hp6624a.SetActiveChannelsVoltages(intermediateVoltage);
                Thread.Sleep(PsuRampUpStepTimeMilliseconds);
                }

            // In case we overshot the voltage then 
            // just set it to the final voltage
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

            if (Rsa3408aOffsetsPath == "")
                {
                LogQueue.Enqueue("No RSA3408A amplitude file chosen.");
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
                if (hp6624a.ChannelStates[i])
                    {
                    headerLine +=
                        string.Format(
                            ", DC Current Channel {0} (A)",
                            i + 1);
                    }
                }

            outputFile.WriteLine(headerLine);
            var numberOfPoints = conf.MeasurementPoints;
            CurrentSweepProgress.CurrentPoint = 1;
            CurrentSweepProgress.NumberOfPoints = numberOfPoints;
            CurrentSweepProgress.Running = true;
            MeasurementStopWatch.Restart();

            // Pre-setup
            var tasksSetFrequency = new Task[3];
            var tasksSetPower = new Task[2];

            // DC supply
            hp6624a.SetAllChannelVoltagesToZero();
            hp6624a.SetChannelOutputStatesStrong();
            hp6624a.SetActiveChannelsCurrent(PsuCurrentLimit);

            // Spectrum Analyser
            rsa3408a.SetSpectrumChannelPowerMeasurementMode();
            rsa3408a.SetFrequencyCenter(conf.Frequencies[0]);
            rsa3408a.SetContinuousMode(continuousOn: false);
            rsa3408a.SetFrequencySpan(conf.MeasurementFrequencySpan);
            rsa3408a.SetChannelBandwidth(conf.MeasurementChannelBandwidth);
            rsa3408a.StartSignalAcquisition();

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
                    outputFile.Flush();

                    // Set the frequency
                    tasksSetFrequency[0] = Task.Factory.StartNew(() =>
                    {
                        rsa3408a.SetFrequencyCenter(frequency);
                        rsa3408a.SetMarkerXValue(
                            markerNumber: 1, xValue: frequency);
                    });
                    tasksSetFrequency[1] = Task.Factory.StartNew(() =>
                    {
                        smu200a.SetSourceFrequency(frequency);
                    });
                    tasksSetFrequency[2] = Task.Factory.StartNew(() =>
                    {
                        e8257d.SetSourceFrequency(frequency);
                    });
                    Task.WaitAll(tasksSetFrequency);

                    foreach (var inputPower in conf.InputPowers)
                        {
                        double offsetSmu200a = -1;
                        double offsetE8257d = -1;
                        // Set the power
                        tasksSetPower[0] = 
                            Task.Factory.StartNew(()=>
                            {
                                offsetSmu200a =
                                    conf.Smu200aOffsets.GetOffset(frequency);
                                smu200a.SetPowerLevel(
                                    inputPower, offsetSmu200a);
                            });
                        tasksSetPower[1] =
                            Task.Factory.StartNew(() =>
                            {
                                offsetE8257d =
                                    conf.E8257dOffsets.GetOffset(frequency);
                                e8257d.SetPowerLevel(inputPower, offsetE8257d);
                            });
                        Task.WaitAll(tasksSetPower);                        

                        foreach (var phase in conf.Phases)
                            {
                            smu200a.SetSourceDeltaPhase(phase);
                            CurrentSweepProgress.CurrentPoint += 1;

                            /// Do the measurement
                            var offsetRsa = 
                                conf.Rsa3408aOffsets.GetOffset(frequency);
                            CurrentSample = 
                                TakeMeasurementSample(
                                    conf, 
                                    voltage, 
                                    frequency, 
                                    inputPower, 
                                    phase, 
                                    offsetSmu200a, 
                                    offsetE8257d, 
                                    offsetRsa);
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
            double offsetE8257d,
            double offsetRsa)
            {
            var readTasks = new Task[2];
            double channelPowerdBm = -1;
            double measuredPoutdBm = -1;
            HP6624A.OutphasingDcMeasurements dcResults = null;
            readTasks[0] = Task.Factory.StartNew(() =>
            {
                dcResults =
                    hp6624a.OutphasingOptimisedMeasurement(supplyVoltage);
            });
            readTasks[1] = Task.Factory.StartNew(() =>
            {
               channelPowerdBm = rsa3408a.ReadSpectrumChannelPower();
               measuredPoutdBm = rsa3408a.GetMarkerYValue(markerNumber: 1);
            });
            Task.WaitAll(readTasks);
                        
            return new MeasurementSample(
                frequency,
                inputPower,
                phase,
                conf.Temperature,
                conf.Corner,
                supplyVoltage,
                dcResults.PowerWatts,
                offsetSmu,
                offsetE8257d,
                offsetRsa,
                measuredPoutdBm,
                conf.MeasurementFrequencySpan,
                conf.MeasurementChannelBandwidth,
                channelPowerdBm,
                dcResults.Currents);
            }

        private void SaveMeasurementSample(
            StreamWriter outputFile, 
            MeasurementSample sample)
            {
            var outputLine =
                $"{sample.Frequency}" // 1
                + $", {sample.InputPowerdBm}"
                + $", {sample.PhaseDeg}"
                + $", {sample.Temperature}"
                + $", {sample.Corner}" // 5
                + $", {sample.SupplyVoltage}"
                + $", {sample.MeasuredPowerDcWatts}"
                + $", {sample.MeasuredOutputPowerdBm}"
                + $", {sample.CalibratedOutputPowerdBm}"
                + $", {sample.OffsetSmu200adB}" // 10
                + $", {sample.OffsetE8557ddB}"
                + $", {sample.OffsetRsa3408AdB}"
                + $", {sample.CalibratedDrainEfficiency}"
                + $", {sample.CalibratedPowerAddedEfficiency}"
                + $", {sample.MeasuredChannelPowerdBm}" // 15
                + $", {sample.RsaFrequencySpan}"
                + $", {sample.RsaChannelBandwidth}"
                + $", {sample.CalibratedGaindB}";

            for(int i = 0; i < HP6624A.NumChannels; i++)
                {
                var channelNumber = i + 1;
                if (hp6624a.ChannelStates[i])
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
            int voltagePoints = 3;
            var freqPoints = FrequencySweepSettingsControl.NSteps;
            var powerPoints = PowerSweepSettingsControl.NSteps;
            var phasePoints = PhaseSweepSettingsControl.NSteps;
            var nPoints = EstimatedTimePerSample 
                * voltagePoints 
                * freqPoints 
                * powerPoints 
                * phasePoints;
            var estimatedMeasurementTime = TimeSpan.FromSeconds(nPoints);
            EstimatedSimulationTimeTextBlock.Text = 
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
                ResultsSavePath = saveDialog.FileName;
                ResultsSavePathTextBlock.Text = ResultsSavePath;
                }
            }

        private void LoadSmu200aOffsetsButton_Click(
            object sender, RoutedEventArgs e)
            {
            Smu200aOffsetsPath = GetOffsetsPath("SMU200A");
            Smu200aOffsetsFilePathTextBlock.Text = Smu200aOffsetsPath;
            }

        private void LoadE8257dOffsetsButton_Click(
            object sender, RoutedEventArgs e)
            {
            E8257dOffsetsPath = GetOffsetsPath("E8257D");
            E8257dOffsetsFilePathTextBlock.Text = E8257dOffsetsPath;
            }

        private void LoadRsa3408adOffsetsButton_Click(
            object sender, RoutedEventArgs e)
            {
            Rsa3408aOffsetsPath = GetOffsetsPath("RSA3408A");
            Rsa3408aOffsetsFilePathTextBlock.Text = Rsa3408aOffsetsPath;
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

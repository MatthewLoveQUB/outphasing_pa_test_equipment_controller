using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Generic;
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
        public double EstimatedTimePerSample { get; set; } = 0.1;
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
                "???",
                "???",
                "???");
            }

        private void SetUpVisaConnections()
            {
            hp6624a = new HP6624A(GetVisaAddress("HP6624A"));
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
            var frequencySettings = new SweepSettings(
                FrequencySweepSettingsControl.Start, 
                FrequencySweepSettingsControl.Step, 
                FrequencySweepSettingsControl.Stop);
            var powerSettings = new SweepSettings(
                PowerSweepSettingsControl.Start, 
                PowerSweepSettingsControl.Step, 
                PowerSweepSettingsControl.Stop);
            var phaseSettings = new SweepSettings(
                PhaseSweepSettingsControl.Start, 
                PhaseSweepSettingsControl.Step, 
                PhaseSweepSettingsControl.Stop);
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
                voltages);
            }

        private void StartSweepButton_Click(object sender, RoutedEventArgs e)
            {
            var measurementSettings = ParseMeasurementConfiguration();

            Task.Factory.StartNew(() =>
            {
                RunSweep(measurementSettings);
            });
            }

        private int SweepPoints(SweepSettings ss)
            {
            return (int)((ss.Stop - ss.Start) / ss.Step);
            }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
            {
            if (CurrentSweepProgress.Running)
                {
                var msg = string.Format("On task {0} of {1}", CurrentSweepProgress.CurrentPoint, CurrentSweepProgress.NumberOfPoints);
                AddNewLogLine(msg);
                }

            // Empty the log queue
            while(LogQueue.Count > 0)
                {
                var message = LogQueue.Dequeue();
                AddNewLogLine(message);
                }
            }

        private void SetAllActivePsuChannels(double voltage)
            {
            int numChannels = PsuChannelEnableCheckboxes.Count;
            for (int i = 0; i < numChannels; i++)
                {
                int channelNumber = i + 1;
                bool channelEnabled = PsuChannelEnableCheckboxes[i].IsChecked == true;
                if (channelEnabled)
                    {
                    hp6624a.SetChannelVoltage(channelNumber, voltage);
                    break;
                    }
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
                SetAllActivePsuChannels(intermediateVoltage);
                Thread.Sleep(PsuRampUpStepTime);
                }

            // In case we overshot the voltage then just set it to the final voltage
            SetAllActivePsuChannels(newVoltage);
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
            if (!MeasurementVariablesCheck())
                {
                return;
                }

            int numberOfPoints = conf.Voltages.Count
                * SweepPoints(conf.FrequencySettings)
                * SweepPoints(conf.PowerSettings)
                * SweepPoints(conf.PhaseSettings);
            CurrentSweepProgress.CurrentPoint = 0;
            CurrentSweepProgress.NumberOfPoints = numberOfPoints;
            CurrentSweepProgress.Running = true;

            // Pre-setup
            // Zeroing the DC Supplies
            // It's hard to tell if the output state command actually works
            // so I'm zeroing the current limit and voltage of the disabled channels
            for(int i = 0; i < PsuChannelEnableCheckboxes.Count; i++)
                {
                var channelNumber = i + 1;
                var voltage = 0;
                var zeroCurrent = 0;
                var checkboxState = (PsuChannelEnableCheckboxes[i].IsChecked == true);
                hp6624a.SetChannelVoltage(channelNumber, voltage);
                hp6624a.SetChannelOutputState(channelNumber, checkboxState);
                if(checkboxState == false)
                    {
                    hp6624a.SetChannelCurrent(channelNumber, zeroCurrent);
                    }
                }

            // Spectrum Analyser
            rsa3408a.SetFrequencyCenter(conf.FrequencySettings.Start);
            rsa3408a.SetContinuousMode(continuousOn: false);
            rsa3408a.SetFrequencySpan(Rsa3408Bandwidth);

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
                for (var frequency = conf.FrequencySettings.Start;
                frequency <= conf.FrequencySettings.Stop;
                frequency += conf.FrequencySettings.Step)
                    {
                    // Set the frequency
                    rsa3408a.SetFrequencyCenter(frequency);
                    smu200a.SetSourceFrequency(frequency);
                    e8257d.SetSourceFrequency(frequency);

                    for (var power = conf.PowerSettings.Start;
                        power <= conf.PowerSettings.Stop;
                        power += conf.PowerSettings.Step)
                        {
                        // Set the power
                        smu200a.SetPowerLevel(power);
                        e8257d.SetPowerLevel(power);

                        for (var phase = conf.PhaseSettings.Start;
                            phase <= conf.PhaseSettings.Stop;
                            phase += conf.PhaseSettings.Step)
                            {
                            smu200a.SetSourceDeltaPhase(phase);
                            CurrentSweepProgress.CurrentPoint += 1;
                            // Do things
                            }
                        }
                    }
                }
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

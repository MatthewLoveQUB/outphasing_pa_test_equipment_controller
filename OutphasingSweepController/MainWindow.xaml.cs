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
using QubVisa;

namespace OutphasingSweepController
    {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
        {
        HP6624A hp6624a;
        TektronixRSA3408A rsa3408a;
        RS_SMU200A smu200a;
        KeysightE8257D e8257d;
        SweepProgress CurrentSweepProgress = new SweepProgress(false, 0, 0);
        System.Windows.Threading.DispatcherTimer dispatcherTimer;
        List<CheckBox> PsuChannelEnableCheckboxes;
        public MainWindow()
            {
            InitializeComponent();
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
            var hp6624aAddress = GetVisaAddress("HP6624A");
            var rsa3408Address = GetVisaAddress("Tektronix RSA3408");
            var smu200aAddress = GetVisaAddress("R&S SMU200A");
            var e8257dAddress = GetVisaAddress("Keysight E8257D");

            hp6624a = new HP6624A(hp6624aAddress);
            rsa3408a = new TektronixRSA3408A(rsa3408Address);
            smu200a = new RS_SMU200A(smu200aAddress);
            e8257d = new KeysightE8257D(e8257dAddress);

            ConnectionStatusTextBlock.Text = string.Format(
                Constants.ConnectionStatusTemplate,
                hp6624aAddress,
                rsa3408Address,
                smu200aAddress,
                e8257dAddress);
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
            var newLog = oldLog + "\n" + line;
            SweepLogTextBox.Text = newLog;
            }

        private SweepSettings ParseSweepInputBox(string input)
            {
            var rawValues = input
                .Split(',')
                .ToList()
                .Select(Convert.ToDouble)
                .ToList();
            return new SweepSettings(start: rawValues[0], step: rawValues[1], stop: rawValues[2]);
            }

        private MeasurementSweepConfiguration ParseMeasurementConfiguration()
            {
            //var frequencySettings = ParseSweepInputBox(FrequencySettingsTextBox.Text);
            var frequencySettings = new SweepSettings(FrequencySweepSettingsControl.Start, FrequencySweepSettingsControl.Step, FrequencySweepSettingsControl.Stop);
            var powerSettings = ParseSweepInputBox(PowerSettingsTextBox.Text);
            var phaseSettings = ParseSweepInputBox(PhaseSettingsTextBox.Text);
            var temperature = Convert.ToDouble(TemperatureSettingsTextBox.Text);
            var corner = CornerSettingsTextBox.Text;
            var nominalVoltage = Convert.ToDouble(VoltageSettingsTextBox.Text);
            var voltages = new List<Double>() { 0.9 * nominalVoltage, nominalVoltage, 1.1 * nominalVoltage };
            return new MeasurementSweepConfiguration(frequencySettings, powerSettings, phaseSettings, temperature, corner, voltages);
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
            }

        private void RunSweep(MeasurementSweepConfiguration conf)
            {
            int numberOfPoints = conf.Voltages.Count
                * SweepPoints(conf.FrequencySettings)
                * SweepPoints(conf.PowerSettings)
                * SweepPoints(conf.PhaseSettings);
            CurrentSweepProgress.CurrentPoint = 0;
            CurrentSweepProgress.NumberOfPoints = numberOfPoints;
            CurrentSweepProgress.Running = true;

            // Pre-setup
            // DC Supplies

            // Spectrum Analyser

            // Power Sources
            smu200a.SetRfOutputState(on: false);
            e8257d.SetRfOutputState(on: false);
            smu200a.SetPowerLevel(-110);
            e8257d.SetPowerLevel(-60);
            smu200a.SetRfOutputState(on: true);
            e8257d.SetRfOutputState(on: true);

            // All of the sweeps are <= as we want to include the stop
            // value in the sweep
            foreach (var voltage in conf.Voltages)
                {
                // Set the voltages
                hp6624a.SetChannelVoltage(1, voltage);
                hp6624a.SetChannelVoltage(2, voltage);
                hp6624a.SetChannelVoltage(3, voltage);
                hp6624a.SetChannelVoltage(4, voltage);

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
        }
    }

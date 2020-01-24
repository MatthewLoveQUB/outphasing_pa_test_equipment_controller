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
    class MeasurementSweepConfiguration
        {
        public SweepSettings FrequencySettings;
        public SweepSettings PowerSettings;
        public SweepSettings PhaseSettings;
        public double Temperature;
        public string Corner;
        public List<Double> Voltages;
        public MeasurementSweepConfiguration(
            SweepSettings frequencySettings,
            SweepSettings powerSettings,
            SweepSettings phaseSettings,
            double temperature,
            string corner,
            List<Double> voltages)
            {
            FrequencySettings = frequencySettings;
            PowerSettings = powerSettings;
            PhaseSettings = phaseSettings;
            Temperature = temperature;
            Corner = corner;
            Voltages = voltages;
            }
        }

    class SweepSettings
        {
        public double Start;
        public double Step;
        public double Stop;
        public SweepSettings(double start, double step, double stop)
            {
            Start = start;
            Step = step;
            Stop = stop;
            }
        }

    class SweepProgress
        {
        public bool Running;
        public int CurrentPoint;
        public int NumberOfPoints;
        public SweepProgress(bool running, int currentPoint, int numberOfPoints)
            {
            Running = running;
            CurrentPoint = currentPoint;
            NumberOfPoints = numberOfPoints;
            }
        }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
        {
        HP6624A hp6624a;
        TektronixRSA3408A rsa3408a;
        RS_SMU200A smu200a;
        KeysightE8257D e8257d;
        bool SweepActive = false;
        SweepProgress CurrentSweepProgress = new SweepProgress(false, 0, 0);
        System.Windows.Threading.DispatcherTimer dispatcherTimer;

        private const string LastSampleTemplate = @"Pout: {0} dBm
Frequency: {1} Hz
Pin: {2} dBm
Gain: {3} dB
PAE: {4} %
Drain Efficiency: {5} %
DC Voltage: {6} V
DC Current: {7} A
DC Power: {8} W";
        private const string LastSamplePsuChannelTemplate = @"PSU Channel {0}: {1} V, {2} A, {3} W";
        private const string ConnectionStatusTemplate = @"HP6624: {0}
TektronixRSA3408A: {1}
R&S SMU200A: {2}
Keysight E8257D: {3}";
        public MainWindow()
            {
            InitializeComponent();
            SetUpVisaConnections();
            LastSampleTextBlock.Text =
                string.Format(LastSampleTemplate, "???", "???", "???", "???", "???", "???", "???", "???", "???");
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
                ConnectionStatusTemplate,
                hp6624aAddress,
                rsa3408Address,
                smu200aAddress,
                e8257dAddress);

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
            }

        private string GetVisaAddress(string deviceName)
            {
            var visaWindow = new list_visa_devices_dialogue.MainWindow(deviceName);
            visaWindow.ShowDialog();
            return visaWindow.SelectedAddress;
            }

        public void AddNewLogLine(string line)
            {
            var oldLog = SweepLogTextBox.Text;
            var newLog = oldLog + "\n" + line;
            SweepLogTextBox.Text = newLog;
            }

        private SweepSettings ParseInputBox(string input)
            {
            var rawValues = input
                .Split(',')
                .ToList()
                .Select(Convert.ToDouble)
                .ToList();
            return new SweepSettings(start: rawValues[0], step: rawValues[1], stop: rawValues[2]);
            }

        private void StartSweepButton_Click(object sender, RoutedEventArgs e)
            {
            var frequencySettings = ParseInputBox(FrequencySettingsTextBox.Text);
            var powerSettings = ParseInputBox(PowerSettingsTextBox.Text);
            var phaseSettings = ParseInputBox(PhaseSettingsTextBox.Text);
            var temperature = Convert.ToDouble(TemperatureSettingsTextBox.Text);
            var corner = CornerSettingsTextBox.Text;
            var nominalVoltage = Convert.ToDouble(VoltageSettingsTextBox.Text);
            var voltages = new List<Double>() { 0.9 * nominalVoltage, nominalVoltage, 1.1 * nominalVoltage };
            var measurementSettings = new MeasurementSweepConfiguration(frequencySettings, powerSettings, phaseSettings, temperature, corner, voltages);

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
            
            foreach(var voltage in conf.Voltages)
                {
                for (var frequency = conf.FrequencySettings.Start;
                frequency < conf.FrequencySettings.Stop;
                frequency += conf.FrequencySettings.Step)
                    {
                    for (var power = conf.PowerSettings.Start;
                        power < conf.PowerSettings.Stop;
                        power += conf.PowerSettings.Step)
                        {
                        for (var phase = conf.PhaseSettings.Start;
                            phase < conf.PhaseSettings.Stop;
                            phase += conf.PhaseSettings.Step)
                            {
                            CurrentSweepProgress.CurrentPoint += 1;
                            // Do things
                            }
                        }
                    }
                }
            }
        }
    }

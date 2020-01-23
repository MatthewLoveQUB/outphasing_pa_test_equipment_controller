using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace outphasing_pa_test_equipment_controller
    {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
        {
        HP6624A hp6624a;
        RS_SMU200A smu200a;
        TektronixRSA3408A rsa3408a;
        KeysightE8257D e8257d;

        public MainWindow()
            {
            InitializeComponent();
            PsuChannel1.mw = this;
            PsuChannel2.mw = this;
            PsuChannel3.mw = this;
            PsuChannel4.mw = this;
            }

        private void DisplayStatusMessage(string message)
            {
            StatusTextBlock.Text = string.Format(
                "{0}: {1}",
                DateTime.Now.ToString("HH:mm:ss tt"),
                message);
            }

        public bool IsConnected(list_visa_devices_dialogue.VisaDevice device)
            {
            if (device == null)
                {
                DisplayStatusMessage("Device is not connected");
                return false;
                }
            else
                {
                return true;
                }
            }

        private string GetVisaAddress()
            {
            var visaWindow = new list_visa_devices_dialogue.MainWindow();
            visaWindow.ShowDialog();
            return visaWindow.SelectedAddress;
            }

        private void ConnectToDevice<T>(T device)
            {
            var address = GetVisaAddress();

            if (address == null)
                {
                return;
                }
            try
                {
                var args = new object[] { address };
                device = (T)Activator.CreateInstance(typeof(T), args);
                PsuConnectionStatus.Text = string.Format("Connected to {0}", address);
                }
            catch
                {
                var message = string.Format("Could not connect to: {0}", address);
                PsuConnectionStatus.Text = "";
                DisplayStatusMessage(message);
                }
            }

        private void PsuConnectionButton_Click(object sender, RoutedEventArgs e)
            {
            ConnectToDevice(hp6624a);
            }

        private void SpectrumAnalyzerConnectionButton_Click(object sender, RoutedEventArgs e)
            {
            ConnectToDevice(rsa3408a);
            }

        private void Smu200AConnectionButton_Click(object sender, RoutedEventArgs e)
            {
            ConnectToDevice(smu200a);
            }

        private void E8257DConnectionButton_Click(object sender, RoutedEventArgs e)
            {
            ConnectToDevice(e8257d);
            }

        public void SetChannelVoltage(int channel, TextBox tb)
            {
            if (!IsConnected(hp6624a)) { return; }
            try
                {
                var voltage = Convert.ToDouble(tb.Text);
                hp6624a.SetChannelVoltage(channel, voltage);
                StatusTextBlock.Text = "";
                }
            catch
                {
                var message = string.Format("Could not set channel {0} voltage.", channel);
                DisplayStatusMessage(message);
                }
            }

        public double GetChannelVoltageSetting(int channel, TextBox tb = null)
            {
            if (!IsConnected(hp6624a)) { return -1.0; }
            try
                {
                var voltage = hp6624a.GetChannelVoltageSetting(channel);
                if (!(tb == null))
                    {
                    tb.Text = string.Format("{0} V", voltage);
                    }
                return voltage;
                }
            catch
                {
                var message = string.Format("Could not read channel {0} voltage setting.", channel);
                DisplayStatusMessage(message);
                return -1;
                }
            }

        public double ReadChannelVoltageOutput(int channel, TextBox tb = null)
            {
            if (!IsConnected(hp6624a)) { return -1.0; }
            try
                {
                var voltage = hp6624a.GetChannelVoltageOutput(channel);
                if (!(tb == null))
                    {
                    tb.Text = string.Format("{0} V", voltage);
                    }
                return voltage;
                }
            catch
                {
                var message = string.Format("Could not read channel {0} voltage output.", channel);
                DisplayStatusMessage(message);
                return -1;
                }
            }

        public void SetChannelCurrent(int channel, TextBox tb)
            {
            if (!IsConnected(hp6624a)) { return; }
            try
                {
                var current = Convert.ToDouble(tb.Text);
                hp6624a.SetChannelCurrent(channel, current);
                }
            catch
                {
                var message = string.Format("Could not set the current of channel {0}.", channel);
                DisplayStatusMessage(message);
                }
            }

        public double GetChannelCurrentSetting(int channel, TextBox tb = null)
            {
            if (!IsConnected(hp6624a)) { return -1.0; }
            try
                {
                var current = hp6624a.GetChannelCurrentSetting(channel);
                if (!(tb == null))
                    {
                    tb.Text = string.Format("{0} A", current);
                    }
                return current;
                }
            catch
                {
                var message = string.Format("Could not get the current setting of channel {0}.", channel);
                DisplayStatusMessage(message);
                return -1;
                }

            }

        public double ReadChannelCurrentOutput(int channel, TextBox tb = null)
            {
            if (!IsConnected(hp6624a)) { return -1.0; }

            try
                {
                var current = hp6624a.GetChannelCurrentOutput(channel);
                if (!(tb == null))
                    {
                    tb.Text = string.Format("{0} A", current);
                    }
                return current;
                }
            catch
                {
                var message = string.Format("Could not read the output current of channel {0}.", channel);
                DisplayStatusMessage(message);
                return -1;
                }
            }

        public void SetChannelOverVoltage(int channel, TextBox tb)
            {
            if (!IsConnected(hp6624a)) { return; }
            try
                {
                var voltage = Convert.ToDouble(tb.Text);
                hp6624a.SetChannelOverVoltage(channel, voltage);
                }
            catch
                {
                var message = string.Format("Could not set channel {0} over-voltage.", channel);
                DisplayStatusMessage(message);
                }
            }

        public double GetChannelOverVoltage(int channel, TextBox tb = null)
            {
            if (!IsConnected(hp6624a)) { return -1; }
            try
                {
                var voltage = hp6624a.GetChannelOvervoltageSetting(channel);
                if (!(tb == null))
                    {
                    tb.Text = string.Format("{0} A", voltage);
                    }
                return voltage;
                }
            catch
                {
                var message = string.Format("Could not read channel {0} over-voltage setting", channel);
                DisplayStatusMessage(message);
                return -1;
                }
            }

        public bool GetChannelState(int channel, TextBlock tb = null)
            {
            if (!IsConnected(hp6624a)) { return false; }
            try
                {
                var state = hp6624a.GetChannelOutputState(channel);
                if (!(tb == null))
                    {
                    tb.Text = state ? "On" : "Off";
                    }
                return state;
                }
            catch
                {
                var message = string.Format("Could not read channel {0} state.", channel);
                DisplayStatusMessage(message);
                tb.Text = "???";
                return false;
                }
            }

        public void SwitchChannelState(int channel, TextBlock tb)
            {
            if (!IsConnected(hp6624a)) { return; }
            try
                {
                var currentState = GetChannelState(channel);
                var newState = !currentState;
                hp6624a.SetChannelOutputState(channel, newState);
                GetChannelState(channel, tb);
                }
            catch
                {
                // May never actually appear
                var message = string.Format("Could not switch channel {0}'s state", channel);
                }
            }

        private void PsuDebugWriteButton_Click(object sender, RoutedEventArgs e)
            {
            var command = PsuDebugInputTextBox.Text;
            hp6624a.device.RawIO.Write(command);
            }

        private void PsuDebugQueryButton_Click(object sender, RoutedEventArgs e)
            {
            var query = PsuDebugInputTextBox.Text;
            PsuDebugOutputTextBlock.Text = hp6624a.ReadString(query);
            }

        private void Rsa3408aDebugWriteButton_Click(object sender, RoutedEventArgs e)
            {
            var command = Rsa3408aDebugInputTextBox.Text;
            rsa3408a.device.RawIO.Write(command);
            }

        private void Rsa3408aDebugQueryButton_Click(object sender, RoutedEventArgs e)
            {
            var query = Rsa3408aDebugInputTextBox.Text;
            Rsa3408aDebugOutputTextBlock.Text = rsa3408a.ReadString(query);
            }

        private void Smu200aDebugWriteButton_Click(object sender, RoutedEventArgs e)
            {
            var command = Smu200aDebugInputTextBox.Text;
            smu200a.device.RawIO.Write(command);
            }

        private void Smu200aDebugQueryButton_Click(object sender, RoutedEventArgs e)
            {
            var query = Smu200aDebugInputTextBox.Text;
            Smu200aDebugOutputTextBlock.Text = smu200a.ReadString(query);
            }

        private void E8257DDebugWriteButton_Click(object sender, RoutedEventArgs e)
            {
            var command = E8257DDebugInputTextBox.Text;
            e8257d.device.RawIO.Write(command);
            }

        private void E8257DDebugQueryButton_Click(object sender, RoutedEventArgs e)
            {
            var query = E8257DDebugInputTextBox.Text;
            E8257DDebugOutputTextBlock.Text = e8257d.ReadString(query);
            }
        }
    }

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
        // PSU Textboxes
        // Voltage
        List<TextBox> SetChannelVoltageTextBoxes;
        List<TextBox> GetChannelVoltageTextBoxes;
        List<TextBox> ReadChannelVoltageTextBoxes;
        // Current
        List<TextBox> SetChannelCurrentTextBoxes;
        List<TextBox> GetChannelCurrentTextBoxes;
        List<TextBox> ReadChannelCurrentTextBoxes;
        // Overvoltage
        List<TextBox> SetChannelOverVoltageTextBoxes;
        List<TextBox> GetChannelOverVoltageTextBoxes;
        // Channel State
        List<TextBlock> ReadChannelStateTextBlocks;

        public MainWindow()
            {
            InitializeComponent();
            // Populate the PSU text box lists
            // Voltage
            SetChannelVoltageTextBoxes = new List<TextBox>()
                {
                SetChannel1VoltageTextBox,
                SetChannel2VoltageTextBox,
                SetChannel3VoltageTextBox,
                SetChannel4VoltageTextBox
                };
            GetChannelVoltageTextBoxes = new List<TextBox>()
                {
                GetChannel1VoltageTextBox,
                GetChannel2VoltageTextBox,
                GetChannel3VoltageTextBox,
                GetChannel4VoltageTextBox
                };
            ReadChannelVoltageTextBoxes = new List<TextBox>()
                {
                ReadChannel1VoltageTextBox,
                ReadChannel2VoltageTextBox,
                ReadChannel3VoltageTextBox,
                ReadChannel4VoltageTextBox
                };
            // Current
            SetChannelCurrentTextBoxes = new List<TextBox>()
                {
                SetChannel1CurrentTextBox,
                SetChannel2CurrentTextBox,
                SetChannel3CurrentTextBox,
                SetChannel4CurrentTextBox
                };
            GetChannelCurrentTextBoxes = new List<TextBox>()
                {
                GetChannel1CurrentTextBox,
                GetChannel2CurrentTextBox,
                GetChannel3CurrentTextBox,
                GetChannel4CurrentTextBox
                };
            ReadChannelCurrentTextBoxes = new List<TextBox>()
                {
                ReadChannel1CurrentTextBox,
                ReadChannel2CurrentTextBox,
                ReadChannel3CurrentTextBox,
                ReadChannel4CurrentTextBox
                };
            // Overvoltage
            SetChannelOverVoltageTextBoxes = new List<TextBox>()
                {
                SetChannel1OverVoltageTextBox,
                SetChannel2OverVoltageTextBox,
                SetChannel3OverVoltageTextBox,
                SetChannel4OverVoltageTextBox
                };
            GetChannelOverVoltageTextBoxes = new List<TextBox>()
                {
                GetChannel1OverVoltageTextBox,
                GetChannel2OverVoltageTextBox,
                GetChannel3OverVoltageTextBox,
                GetChannel4OverVoltageTextBox
                };
            // Channel State
            ReadChannelStateTextBlocks = new List<TextBlock>()
                {
                ReadChannel1StateTextBlock,
                ReadChannel2StateTextBlock,
                ReadChannel3StateTextBlock,
                ReadChannel4StateTextBlock
                };
            }

        private void DisplayStatusMessage(string message)
            {
            StatusTextBlock.Text = string.Format(
                "{0}: {1}",
                DateTime.Now.ToString("HH:mm:ss tt"),
                message);
            }

        private bool IsConnected(list_visa_devices_dialogue.VisaDevice device)
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

        private void PsuConnectionButton_Click(object sender, RoutedEventArgs e)
            {
            var address = GetVisaAddress();

            if (address == null)
                {
                return;
                }
            else
                {
                hp6624a = new HP6624A(address);
                PsuConnectionStatus.Text = string.Format("Connected to {0}\nID: {1}", address, hp6624a.GetId());
                }
            }

        private void SpectrumAnalyzerConnectionButton_Click(object sender, RoutedEventArgs e)
            {
            var address = GetVisaAddress();
            if (address == null)
                {
                return;
                }
            else
                {
                rsa3408a = new TektronixRSA3408A(address);
                SpectrumAnalyzerConnectionStatus.Text = string.Format("Connected to {0}\nID: {1}", address, rsa3408a.GetId());
                }
            }

        private void Smu200AConnectionButton_Click(object sender, RoutedEventArgs e)
            {
            var address = GetVisaAddress();
            if (address == null)
                {
                return;
                }
            else
                {
                smu200a = new RS_SMU200A(address);
                SMU200AConnectionStatus.Text = string.Format("Connected to {0}\nID: {1}", address, smu200a.GetId());
                }
            }

        private void E8257DConnectionButton_Click(object sender, RoutedEventArgs e)
            {
            var address = GetVisaAddress();
            if (address == null)
                {
                return;
                }
            else
                {
                e8257d = new KeysightE8257D(address);
                E8257DConnectionStatus.Text = string.Format("Connected to {0}\nID: {1}", address, e8257d.GetId());
                }
            }

        private void SetChannelVoltage(int channel)
            {
            if (!IsConnected(hp6624a)) { return; }
            var tb = SetChannelVoltageTextBoxes[channel - 1];
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

        private double GetChannelVoltageSetting(int channel, bool UpdateTextBox = false)
            {
            if (!IsConnected(hp6624a)) { return -1.0; }
            try
                {
                var voltage = hp6624a.GetChannelVoltageSetting(channel);
                if (UpdateTextBox)
                    {
                    var tb = GetChannelVoltageTextBoxes[channel - 1];
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

        private double ReadChannelVoltageOutput(int channel, bool UpdateTextBox = false)
            {
            if (!IsConnected(hp6624a)) { return -1.0; }
            try
                {
                var voltage = hp6624a.GetChannelVoltageOutput(channel);
                if (UpdateTextBox)
                    {
                    var tb = ReadChannelVoltageTextBoxes[channel - 1];
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

        private void SetChannelCurrent(int channel)
            {
            if (!IsConnected(hp6624a)) { return; }
            var tb = SetChannelCurrentTextBoxes[channel - 1];
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

        private double GetChannelCurrentSetting(int channel, bool UpdateTextBox = false)
            {
            if (!IsConnected(hp6624a)) { return -1.0; }
            try
                {
                var current = hp6624a.GetChannelCurrentSetting(channel);
                if (UpdateTextBox)
                    {
                    var tb = GetChannelCurrentTextBoxes[channel - 1];
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
        
        private double ReadChannelCurrentOutput(int channel, bool UpdateTextBox = false)
            {
            if (!IsConnected(hp6624a)) { return -1.0; }

            try
                {
                var current = hp6624a.GetChannelCurrentOutput(channel);
                if (UpdateTextBox)
                    {
                    var tb = ReadChannelCurrentTextBoxes[channel - 1];
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

        private void SetChannelOverVoltage(int channel)
            {
            if (!IsConnected(hp6624a)) { return; }
            var textBox = SetChannelOverVoltageTextBoxes[channel - 1];
            try
                {
                var voltage = Convert.ToDouble(textBox.Text);
                hp6624a.SetChannelOverVoltage(channel, voltage);
                }
            catch
                {
                var message = string.Format("Could not set channel {0} over-voltage.", channel);
                DisplayStatusMessage(message);
                }            
            }

        private double GetChannelOverVoltage(int channel, bool UpdateTextBox = false)
            {
            if (!IsConnected(hp6624a)) { return -1; }
            try
                {
                var voltage = hp6624a.GetChannelOvervoltageSetting(channel);
                if (UpdateTextBox)
                    {
                    var tb = GetChannelOverVoltageTextBoxes[channel - 1];
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

        private bool GetChannelState(int channel, bool UpdateTextBox = false)
            {
            if (!IsConnected(hp6624a)) { return false; }
            try
                {
                var state = hp6624a.GetChannelOutputState(channel);
                if (UpdateTextBox)
                    {
                    var tb = ReadChannelStateTextBlocks[channel - 1];
                    tb.Text = state ? "On" : "Off";
                    }
                return state;
                }
            catch
                {
                var message = string.Format("Could not read channel {0} state.", channel);
                DisplayStatusMessage(message);
                var tb = ReadChannelStateTextBlocks[channel - 1];
                tb.Text = "???";
                return false;
                }            
            }

        private void SwitchChannelState(int channel)
            {
            if (!IsConnected(hp6624a)) { return; }
            try
                {
                var currentState = GetChannelState(channel);
                var newState = !currentState;
                hp6624a.SetChannelOutputState(channel, newState);
                GetChannelState(channel, UpdateTextBox: true);
                }
            catch
                {
                // May never actually appear
                var message = string.Format("Could not switch channel {0}'s state", channel);
                }            
            }

        private void SetChannel1VoltageButton_Click(object sender, RoutedEventArgs e)
            {
            SetChannelVoltage(1);
            }

        private void GetChannel1VoltageButton_Click(object sender, RoutedEventArgs e)
            {
            GetChannelVoltageSetting(1, UpdateTextBox: true);
            }

        private void ReadChannel1CurrentButton_Click(object sender, RoutedEventArgs e)
            {
            ReadChannelCurrentOutput(1, UpdateTextBox: true);
            }

        private void SetChannel1CurrentButton_Click(object sender, RoutedEventArgs e)
            {
            SetChannelCurrent(1);
            }

        private void GetChannel1CurrentButton_Click(object sender, RoutedEventArgs e)
            {
            GetChannelCurrentSetting(1, UpdateTextBox: true);
            }

        private void SetChannel1OverVoltageButton_Click(object sender, RoutedEventArgs e)
            {
            SetChannelOverVoltage(1);
            }

        private void GetChannel1OverVoltageButton_Click(object sender, RoutedEventArgs e)
            {
            GetChannelOverVoltage(1, UpdateTextBox: true);
            }

        private void SetChannel1StateButton_Click(object sender, RoutedEventArgs e)
            {
            SwitchChannelState(1);
            }

        private void ReadChannel1VoltageButton_Click(object sender, RoutedEventArgs e)
            {
            ReadChannelVoltageOutput(1, UpdateTextBox: true);
            }

        private void SetChannel2VoltageButton_Click(object sender, RoutedEventArgs e)
            {
            SetChannelVoltage(2);
            }

        private void GetChannel2VoltageButton_Click(object sender, RoutedEventArgs e)
            {
            GetChannelVoltageSetting(2, UpdateTextBox: true);
            }

        private void ReadChannel2VoltageButton_Click(object sender, RoutedEventArgs e)
            {
            ReadChannelVoltageOutput(2, UpdateTextBox: true);
            }

        private void SetChannel2CurrentButton_Click(object sender, RoutedEventArgs e)
            {
            SetChannelCurrent(2);
            }

        private void GetChannel2CurrentButton_Click(object sender, RoutedEventArgs e)
            {
            GetChannelCurrentSetting(2, UpdateTextBox: true);
            }

        private void ReadChannel2CurrentButton_Click(object sender, RoutedEventArgs e)
            {
            ReadChannelCurrentOutput(2, UpdateTextBox: true);
            }

        private void SetChannel2OverVoltageButton_Click(object sender, RoutedEventArgs e)
            {
            SetChannelOverVoltage(2);
            }

        private void GetChannel2OverVoltageButton_Click(object sender, RoutedEventArgs e)
            {
            GetChannelOverVoltage(2, UpdateTextBox: true);
            }


        private void SetChannel2StateButton_Click(object sender, RoutedEventArgs e)
            {
            SwitchChannelState(2);
            }

        private void SetChannel3VoltageButton_Click(object sender, RoutedEventArgs e)
            {
            SetChannelVoltage(3);
            }

        private void GetChannel3VoltageButton_Click(object sender, RoutedEventArgs e)
            {
            GetChannelVoltageSetting(3, UpdateTextBox: true);
            }

        private void ReadChannel3VoltageButton_Click(object sender, RoutedEventArgs e)
            {
            ReadChannelVoltageOutput(3, UpdateTextBox: true);
            }

        private void SetChannel3CurrentButton_Click(object sender, RoutedEventArgs e)
            {
            SetChannelCurrent(3);
            }

        private void GetChannel3CurrentButton_Click(object sender, RoutedEventArgs e)
            {
            GetChannelCurrentSetting(3, UpdateTextBox: true);
            }

        private void ReadChannel3CurrentButton_Click(object sender, RoutedEventArgs e)
            {
            ReadChannelCurrentOutput(3, UpdateTextBox: true);
            }

        private void SetChannel4VoltageButton_Click(object sender, RoutedEventArgs e)
            {
            SetChannelVoltage(4);
            }


        private void GetChannel4VoltageButton_Click(object sender, RoutedEventArgs e)
            {
            GetChannelVoltageSetting(4, UpdateTextBox: true);
            }

        private void ReadChannel4VoltageButton_Click(object sender, RoutedEventArgs e)
            {
            ReadChannelVoltageOutput(4, UpdateTextBox: true);
            }

        private void SetChannel4CurrentButton_Click(object sender, RoutedEventArgs e)
            {
            SetChannelCurrent(4);
            }

        private void GetChannel4CurrentButton_Click(object sender, RoutedEventArgs e)
            {
            GetChannelCurrentSetting(4, UpdateTextBox: true);
            }

        private void ReadChannel4CurrentButton_Click(object sender, RoutedEventArgs e)
            {
            ReadChannelCurrentOutput(4, UpdateTextBox: true);
            }

        private void SetChannel4OverVoltageButton_Click(object sender, RoutedEventArgs e)
            {
            SetChannelOverVoltage(4);
            }

        private void GetChannel4OverVoltageButton_Click(object sender, RoutedEventArgs e)
            {
            GetChannelOverVoltage(4, UpdateTextBox: true);
            }


        private void SetChannel4StateButton_Click(object sender, RoutedEventArgs e)
            {
            SwitchChannelState(4);
            }

        private void SetChannel3StateButton_Click(object sender, RoutedEventArgs e)
            {
            SwitchChannelState(3);
            }

        private void GetChannel3OverVoltageButton_Click(object sender, RoutedEventArgs e)
            {
            GetChannelOverVoltage(3, UpdateTextBox: true);
            }

        private void SetChannel3OverVoltageButton_Click(object sender, RoutedEventArgs e)
            {
            SetChannelOverVoltage(3);
            }

        private void ReadChannel1StateButton_Click(object sender, RoutedEventArgs e)
            {
            GetChannelState(1, UpdateTextBox: true);
            }

        private void ReadChannel2StateButton_Click(object sender, RoutedEventArgs e)
            {
            GetChannelState(2, UpdateTextBox: true);
            }

        private void ReadChannel3StateButton_Click(object sender, RoutedEventArgs e)
            {
            GetChannelState(3, UpdateTextBox: true);
            }

        private void ReadChannel4StateButton_Click(object sender, RoutedEventArgs e)
            {
            GetChannelState(4, UpdateTextBox: true);
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

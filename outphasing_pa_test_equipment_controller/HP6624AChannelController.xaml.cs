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
    /// Interaction logic for HP6624AChannelController.xaml
    /// </summary>
    public partial class HP6624AChannelController : UserControl
        {
        public int Channel { get; set; }
        public MainWindow mw;

        public HP6624AChannelController()
            {
            InitializeComponent();
            this.DataContext = this;
            }

        private void SetChannelVoltageButton_Click(object sender, RoutedEventArgs e)
            {
            mw.SetChannelVoltage(Channel, SetChannelVoltageTextBox);
            }

        private void GetChannelVoltageButton_Click(object sender, RoutedEventArgs e)
            {
            mw.GetChannelVoltageSetting(Channel, GetChannelVoltageTextBox);
            }

        private void ReadChannelVoltageButton_Click(object sender, RoutedEventArgs e)
            {
            mw.ReadChannelCurrentOutput(Channel, ReadChannelVoltageTextBox);
            }

        private void SetChannelCurrentButton_Click(object sender, RoutedEventArgs e)
            {
            mw.SetChannelCurrent(Channel, SetChannelCurrentTextBox); 
            }

        private void GetChannelCurrentButton_Click(object sender, RoutedEventArgs e)
            {
            mw.GetChannelCurrentSetting(Channel, GetChannelCurrentTextBox);
            }

        private void ReadChannelCurrentButton_Click(object sender, RoutedEventArgs e)
            {
            mw.ReadChannelCurrentOutput(Channel, ReadChannelCurrentTextBox);
            }

        private void SetChannelOverVoltageButton_Click(object sender, RoutedEventArgs e)
            {
            mw.SetChannelOverVoltage(Channel, SetChannelOverVoltageTextBox);
            }

        private void GetChannelOverVoltageButton_Click(object sender, RoutedEventArgs e)
            {
            mw.GetChannelOverVoltage(Channel, GetChannelOverVoltageTextBox);
            }

        private void SetChannelStateButton_Click(object sender, RoutedEventArgs e)
            {
            mw.SwitchChannelState(Channel, ReadChannelStateTextBlock);
            }

        private void ReadChannelStateButton_Click(object sender, RoutedEventArgs e)
            {
            mw.GetChannelState(Channel, ReadChannelStateTextBlock);
            }
        }
    }

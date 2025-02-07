﻿using System;
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
using System.Collections.ObjectModel;

namespace list_visa_devices_dialogue
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> AddressList { get; set; }
        public string SelectedAddress;

        public MainWindow(string deviceName="")
        {
            InitializeComponent();
            this.DataContext = this;
            if (deviceName != "")
                {
                var newPrompt = string.Format("Choose VISA address for {0}", deviceName);
                this.AddressPromptTextBlock.Text = newPrompt;
                }
            this.AddressList = new ObservableCollection<string>();
            UpdateVisaAddresses();            
        }

        private void ChooseVisaAddressButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Search for VISA addresses and populate the listbox
        private void GetVisaAddressesButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateVisaAddresses();
        }

        private void UpdateVisaAddresses()
        {
            var vm = new QubVisa.VisaManager();
            var addresses = vm.GetAvailableDevices();
            this.AddressList.Clear();
            foreach (string address in addresses)
            {
                this.AddressList.Add(address);
            }
        }

        private void AddressListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SelectedAddress = (string)this.AddressListBox.SelectedItem;
        }
    }
}

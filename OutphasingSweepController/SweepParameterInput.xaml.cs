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

namespace OutphasingSweepController
    {
    /// <summary>
    /// Interaction logic for SweepParameterInput.xaml
    /// </summary>
    public partial class SweepParameterInput : UserControl
        {
        public string SweepDescriptionText { get; set; }
        public double Start { get; set; }
        public double Step { get; set; }
        public double Stop { get; set; }
        private int NSteps { get; set; }

        public SweepParameterInput()
            {
            InitializeComponent();
            this.DataContext = this;
            }

        private void TextBoxes_LostFocus(object sender, RoutedEventArgs e)
            {
            UpdateFields();
            }

        private void UpdateFields()
            {
            var nStepsValue = (Stop - Start) / Step;
            // Adding 1 to include the first sweep point
            NSteps = (1 + (int)nStepsValue);
            NStepsTextBox.Text = NSteps.ToString();
            }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
            {
            UpdateFields();
            }
        }
    }

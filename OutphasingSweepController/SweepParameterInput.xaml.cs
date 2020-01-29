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
        public int NSteps { get; set; }

        public List<Double> Values { get
                {
                var values = new List<Double>();
                if(this.NSteps == 1)
                    {
                    values.Add(this.Start);
                    return values;
                    }

                for (var x = this.Start; x <= this.Stop; x += this.Step)
                    {
                    values.Add(x);
                    }
                // Due to floating-point errors
                // let's explicitly set the final value to Stop
                values[values.Count - 1] = this.Stop;
                return values;
                }
            }

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
            if (this.NSteps == 1)
                {
                this.StopTextBox.IsReadOnly = true;
                this.StopTextBox.Background = Brushes.LightGray;
                }
            else
                {
                this.StopTextBox.IsReadOnly = false;
                this.StopTextBox.Background = Brushes.Transparent;
                }

            this.Step = (this.Stop - this.Start) / ((double)this.NSteps - 1.0);
            this.StepTextBox.Text = this.Step.ToString();
            }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
            {
            UpdateFields();
            }
        }
    }

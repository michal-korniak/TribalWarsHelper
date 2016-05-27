using System;
using System.Windows;

namespace TribalWarsHelper
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SaveAttackTempl : Window
    {
        public SaveAttackTempl()
        {
            InitializeComponent();
        }
        public event EventHandler<SaveAttackTemplEventArgs> Done;

        private void TxtName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(TxtName.Text))
                BtnSave.IsEnabled = false;
            else
                BtnSave.IsEnabled = true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (Done != null)
            {
                Done(this, new SaveAttackTemplEventArgs(TxtName.Text));
            }
        }
    }
    public partial class SaveAttackTemplEventArgs : EventArgs
    {
        public string FileName { get; set; }
        public SaveAttackTemplEventArgs(string filename)
        {
            FileName = filename;
        } 
    }
}

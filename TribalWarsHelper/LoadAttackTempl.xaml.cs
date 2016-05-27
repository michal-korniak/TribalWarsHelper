using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace TribalWarsHelper
{
    /// <summary>
    /// Interaction logic for LoadAttackTempl.xaml
    /// </summary>
    public partial class LoadAttackTempl : Window
    {
        string[] fullPath;
        public LoadAttackTempl(string[] templates)
        {
            InitializeComponent();
            dateTimePicker.Value = DateTime.Now;
            fullPath = templates;
            CbxTemplates.ItemsSource = from x in templates
                                       select new FileInfo(x).Name;
            CbxTemplates.SelectedIndex = 0;
        }
        public event EventHandler<LoadAttackTemplEventArgs> Done;

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (Done != null)
                Done(this, new LoadAttackTemplEventArgs(fullPath[CbxTemplates.SelectedIndex],(DateTime)dateTimePicker.Value));
        }
    }
    public partial class LoadAttackTemplEventArgs : EventArgs
    {
        public string FilePath { get; set; }
        public DateTime StartTime { get; set; }
        public LoadAttackTemplEventArgs(string filepath, DateTime startTime)
        {
            FilePath = filepath;
            StartTime = startTime;
        }
        
    }

}

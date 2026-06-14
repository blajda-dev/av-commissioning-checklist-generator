using CommissioningChecklistGenerator;
using CommissioningChecklistGenerator.UI;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CommissioningChecklistGenerator.UI
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        private const string Prefix = "[ProgressWindow]";
        public ProgressWindow(Window owner)
        {
            InitializeComponent();
            this.Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        public ProgressWindow(Window owner, ImageSource? icon, string task, string message, string title) : this(owner)
        {
            if (icon != null) { Icon = icon; }
            else { Log.Debug($"{Prefix} not setting icon"); }

            MessageMain.Text = message;
            Title = title;
            CurrentTask.Text = task;
            CurrentProgress.Value = 0;
        }

        public void UpdateProgress(int currentProgress, string currentTask)
        {
            CurrentProgress.Value = currentProgress;
            CurrentTask.Text = currentTask;
        }

        public void UpdateProgress(ProgressUpdate update)
        {
            this.UpdateProgress(update.Percent, update.Message);
        }
    }
}

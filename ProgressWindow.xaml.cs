using CommissioningChecklistGenerator;
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

namespace CommissioningChecklistGenerator
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
            CurrentTask.Text = "Beginning Checklist Export";
        }

        public ProgressWindow(string task) : this()
        {
            CurrentTask.Text = task;
        }

        public void UpdateProgress(int currentProgress, string currentTask)
        {
            CurrentProgress.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
            new DispatcherOperationCallback(delegate
            {
                CurrentProgress.Value = currentProgress;
                CurrentTask.Text = currentTask;
                return null;
            }), null);
        }
        public void IncrementProgress()
        {
            CurrentProgress.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
            new DispatcherOperationCallback(delegate
            {
                CurrentProgress.Value = CurrentProgress.Value + 10;
                return null;
            }), null);
        }
    }
}

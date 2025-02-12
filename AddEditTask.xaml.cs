using System.Windows;
using CommissioningChecklistGenerator.Tasks;
using CommissioningChecklistGenerator.AVSystem;
using System;
using System.Diagnostics;

namespace CommissioningChecklistGenerator
{
    /// <summary>
    /// Interaction logic for AddEditTask.xaml
    /// </summary>
    public partial class AddEditTask : Window
    {
        public Task? CurrentTask { get; set; }

        public AddEditTask()
        {
            InitializeComponent();
            CurrentTask = new Task();
        }

        public AddEditTask(string doneButtonText, Task? taskToEdit) : this()
        {
            CompleteButton.Content = doneButtonText;
            CurrentTask = taskToEdit;
            MediaTypes.SelectedItem = CurrentTask?.Capability;
            TaskTypes.SelectedItem = CurrentTask?.Type;
            TaskTemplateText.Text = CurrentTask?.Template;
        }

        public void OnCompleteButtonClicked(object sender, RoutedEventArgs args)
        {
            if (CurrentTask != null)
            {
                CurrentTask.Template = TaskTemplateText.Text;
                if (MediaTypes.SelectedItem != null) CurrentTask.Capability = (MediaType)MediaTypes.SelectedItem;
                if (TaskTypes.SelectedItem != null) CurrentTask.Type = (TaskType)TaskTypes.SelectedItem;
                DialogResult = true;
            }
            else { DialogResult = false; }
        }

        public void OnTaskTypeChecked(object sender, RoutedEventArgs args)
        {
            //Trace.WriteLine("Task Type Checked");
            MediaTypes.SelectedItem = sender;
        }

        public void OnTaskMediaTypeChecked(object sender, RoutedEventArgs args)
        {
            //Trace.WriteLine("Task Media Type Checked");
            MediaTypes.SelectedItem = sender;
        }
    }
}

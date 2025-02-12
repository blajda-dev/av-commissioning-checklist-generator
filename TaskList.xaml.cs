using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CommissioningChecklistGenerator.Tasks;

namespace CommissioningChecklistGenerator
{
    /// <summary>
    /// Interaction logic for TaskList.xaml
    /// </summary>
    public partial class TaskList : Window
    {
        public BindingList<Task>? CustomTasks { get; set; }
        public TaskList()
        {
            InitializeComponent();
            CustomTasks = new BindingList<Task>();
            DataContext = this;
        }
        public TaskList(BindingList<Task>? tasks) : this()
        {
            CustomTasks = tasks;
            DataContext = this;
        }
        public void OnDeleteTask(object sender, RoutedEventArgs e)
        {
            if (Tasks.SelectedItem != null)
            {
                MessageBoxResult result = MessageBox.Show("Are you sure you want to delete this task?\r\nThis cannot be undone.", "Delete Task?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                { 
                    if(CustomTasks != null) CustomTasks.Remove((Task)Tasks.SelectedItem);
                }
            }
            else
            {
                MessageBox.Show("Select A Task", "Delete Task", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
        public void OnAddTask(object sender, RoutedEventArgs e)
        {
            AddEditTask AddTaskWindow = new AddEditTask();
            bool? result = AddTaskWindow.ShowDialog();

            if(result == true)
            {
                if (AddTaskWindow.CurrentTask != null)
                {
                    CustomTasks?.Add(AddTaskWindow.CurrentTask);
                    Trace.WriteLine("Adding Task");
                }
                else { Trace.WriteLine("Task Null");  }
            }
            else { Trace.WriteLine("DialogResult not true");  }
        }
        public void OnEditTask(object sender, RoutedEventArgs e)
        {
            if (Tasks.SelectedItem != null)
            {
                AddEditTask AddTaskWindow = new AddEditTask("Save Changes?", (Task)Tasks.SelectedItem);
                AddTaskWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Select A Task", "Edit Task", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
    }
}

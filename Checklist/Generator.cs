using CommissioningChecklistGenerator.AVSystem;
using CommissioningChecklistGenerator.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CommissioningChecklistGenerator.Extensions;
using System.ComponentModel;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;

namespace CommissioningChecklistGenerator.Checklist
{
    public static class Generator
    {
        private static IXLWorksheet AddWorksheet(IXLWorkbook workbook, string name)
        {
            IXLWorksheet worksheet;
            if (workbook.GetWorksheetByName(name) == null)
            {
                worksheet = workbook.Worksheets.Add();
                worksheet.Cell(1, 1).Value = "Tasks";
                worksheet.Cell(1, 2).Value = "Status";
                worksheet.Name = name;
                return worksheet;
            }
            worksheet = workbook.Worksheet(name);
            return worksheet;
        }

        private static void FormatWorkbook(IXLWorkbook workbook)
        {
            if (workbook.Worksheets.Count > 1) workbook.GetWorksheetByName("Sheet1")?.Delete();

            foreach (IXLWorksheet sheet in workbook.Worksheets)
            {
                sheet.Columns().AdjustToContents();
                //FFFFCC background color for header cells
                //border yes
                for (int i = 1; i <= 2; i++)
                {
                    IXLCell cell = sheet.Cell(1, i);
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFFFCC");
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.LightGray;
                    
                }
            }
        }

        private static void GenerateHeaderCell(int cell, string contents, IXLWorksheet ws)
        {
            IXLCell headerCell = ws.Cell(cell, 1);
            //set cell bold
            headerCell.Style.Font.Bold = true;
            //set cell contents
            headerCell.Value = contents;
            //get the range we need
            IXLRange range = ws.Range(ws.Cell(cell, 1), ws.Cell(cell, 2));
            //merge the cells
            range.Merge();
            //align the content center
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private static void FormatDetailCell(int index, IXLWorksheet ws, string details)
        {
            IXLCell taskDetailCell = ws.Cell(index, 1);
            taskDetailCell.Value = details;
            taskDetailCell.Style.Alignment.WrapText = true;
        }

        private static void FormatTaskStatusCell(int index, IXLWorksheet ws)
        {
            var taskOptions = new List<string>() { "Incomplete", "Completed", "N/A", "In Progress" };
            var validOptions = $"\"{String.Join(",", taskOptions)}\"";

            IXLCell taskStatusCell = ws.Cell(index, 2);
            IXLDataValidation taskValidation = taskStatusCell.CreateDataValidation();

            taskValidation.List(validOptions);
            taskValidation.IgnoreBlanks = true;
            taskValidation.InCellDropdown = true;
            taskStatusCell.Value = "Incomplete";
        }

        private static void GenerateConditionalFormatting(int cells, IXLWorksheet sheet)
        {
            string conditionIncomplete = @"=$B1=""Incomplete""";
            string conditionComplete = @"=$B1=""Completed""";
            string conditionNotApplicable = @"=$B1=""N/A""";
            string conditionInProgress = @"=$B1=""In Progress""";

            sheet.RangeUsed().AddConditionalFormat().WhenIsTrue(conditionIncomplete).Fill.SetBackgroundColor(XLColor.IndianRed);
            sheet.RangeUsed().AddConditionalFormat().WhenIsTrue(conditionComplete).Fill.SetBackgroundColor(XLColor.LightGreen);
            sheet.RangeUsed().AddConditionalFormat().WhenIsTrue(conditionNotApplicable).Fill.SetBackgroundColor(XLColor.LightGray);
            sheet.RangeUsed().AddConditionalFormat().WhenIsTrue(conditionInProgress).Fill.SetBackgroundColor(XLColor.Orange);
        }

        public static XLWorkbook? GenerateChecklist(BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks)
        {
            int progress = 0;
            XLWorkbook? checklist = null;
            try
            {
                //try to create a new workbook
                checklist = new XLWorkbook();
                worker.ReportProgress(10, "Workbook Created...");
            }
            catch (Exception e) { MessageBox.Show(e.Message); }
            finally
            {
                //show busy dialog
                if (checklist != null)
                {
                    progress = GenerateSourceTaskChecklist(progress, worker, project, tasks, checklist);

                    progress = GenerateDestinationTaskChecklist(progress, worker, project, tasks, checklist);

                    progress = GenerateMatrixTaskChecklist(progress, worker, project, tasks, checklist);

                    progress = GenerateUserInterfaceTaskChecklist(progress, worker, project, tasks, checklist);

                    progress = GenerateControlledDevicesTaskChecklist(progress, worker, project, tasks, checklist);

                    progress = GenerateVideoConferencingTaskChecklist(progress, worker, project, tasks, checklist);

                    progress = GenerateAudioConferencingTaskChecklist(progress, worker, project, tasks, checklist);

                    progress = GenerateSoftConferencingTaskChecklist(progress, worker, project, tasks, checklist);

                    progress = GenerateRoomCombiningTaskChecklist(progress, worker, project, tasks, checklist);

                    FormatWorkbook(checklist);
                }
            }
            worker.ReportProgress(100, "Complete.");
            return checklist;
        }

        private static int GenerateControlledDevicesTaskChecklist(int progress, BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks, XLWorkbook? checklist)
        {
            //report the progress
            worker.ReportProgress(progress += 5, "Gathering Controlled Devices...");
            //check what devices we need to add to the controlled devices checklist that exist in other lists
            bool controlledSources = project.Sources.Where(i => i.ControlMethod != ControlType.None)?.Count() != 0;
            bool controlledDestinations = project.Destinations.Where(i => i.ControlMethod != ControlType.None)?.Count() != 0;
            bool userInterfaces = project.UserInterfaces.Where(i => i.ControlMethod != ControlType.None)?.Count() != 0;
            bool controlledDevices = project.ControlledDevices.Count != 0;
            //update progress
            worker.ReportProgress(progress += 5, "Controlled Device Tasks Generating...");
            //if any flag is set create the controlled device list
            if (controlledDevices || controlledSources || controlledDestinations || userInterfaces)
            {
                //generate the cell
                IXLWorksheet controlledDevicesWorksheet = AddWorksheet(checklist, "Controlled Devices");
                //set the starting cell
                int cell = 2;
                //create a list, starting with the controlled devices
                List<Device> controlledDeviceList = new List<Device>(project.ControlledDevices);
                //add the other devices
                controlledDeviceList.AddRange(project.Sources.Where(i => i.ControlMethod != ControlType.None));
                controlledDeviceList.AddRange(project.Destinations.Where(i => i.ControlMethod != ControlType.None));
                controlledDeviceList.AddRange(project.UserInterfaces.Where(i => i.ControlMethod != ControlType.None));
                //loop through all the controlled devices
                controlledDeviceList.ForEach(delegate (Device device)
                {
                    //generate a header cell
                    GenerateHeaderCell(cell, device.Name, controlledDevicesWorksheet);
                    //get the next cell
                    cell++;
                    //loop through all the tasks
                    tasks.FixedTasks.Where(t => t.Type == TaskType.ControlledDevice).ToList().ForEach(delegate (Tasks.Task task)
                    {
                        //generate the task details
                        task.GenerateTaskDetails(new Dictionary<string, object>() { { "Device", device }, { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } });
                        //assign to the cell & format it
                        FormatDetailCell(cell, controlledDevicesWorksheet, task.Details);
                        //format the cell
                        FormatTaskStatusCell(cell, controlledDevicesWorksheet);
                        //get the next cell
                        cell++;
                    });
                });
                //format the sheet
                GenerateConditionalFormatting(cell, controlledDevicesWorksheet);
            }
            //return progress
            return progress;
        }

        private static int GenerateVideoConferencingTaskChecklist(int progress, BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks, XLWorkbook? checklist)
        {
            //update the progress
            worker.ReportProgress(progress += 5, "Video Conferencing Tasks Generating...");
            //if video conferencing is enabled
            if (project.VideoConferencing)
            {
                //generate the sheet
                IXLWorksheet videoConferencingWorksheet = AddWorksheet(checklist, "Video Conferencing");
                //set the starting cell
                int cell = 2;
                //loop through the tasks
                tasks.FixedTasks.Where(t => t.Type == TaskType.VideoConference || t.Type == TaskType.Conference).ToList().ForEach(delegate (Tasks.Task task)
                {
                    //generate the details
                    task.GenerateTaskDetails(new Dictionary<string, object>() { { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } });
                    //assign to the cell
                    FormatDetailCell(cell, videoConferencingWorksheet, task.Details);
                    //format the cell
                    FormatTaskStatusCell(cell, videoConferencingWorksheet);
                    //get the next cell
                    cell++;
                });
                //format the sheet
                GenerateConditionalFormatting(cell, videoConferencingWorksheet);
            }
            //update progress
            return progress;
        }

        private static int GenerateAudioConferencingTaskChecklist(int progress, BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks, XLWorkbook? checklist)
        {
            //update progress
            worker.ReportProgress(progress += 5, "Audio Conferencing Tasks Generating...");
            //if audio conferencing is enabled
            if (project.AudioConferencing)
            {
                //generate the sheet
                IXLWorksheet audioConferencingWorksheet = AddWorksheet(checklist, "Audio Conferencing");
                //set the starting cell
                int cell = 2;
                //loop through fixed tasks
                tasks.FixedTasks.Where(t => t.Type == TaskType.AudioConference || t.Type == TaskType.Conference).ToList().ForEach(delegate (Tasks.Task task)
                {
                    //generate the task details
                    task.GenerateTaskDetails(new Dictionary<string, object>() { { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } });
                    //assign to the cell
                    FormatDetailCell(cell, audioConferencingWorksheet, task.Details);
                    //format the cell
                    FormatTaskStatusCell(cell, audioConferencingWorksheet);
                    //get the next cell
                    cell++;
                });
                //format the sheet
                GenerateConditionalFormatting(cell, audioConferencingWorksheet);
            }
            //return progress
            return progress;
        }

        private static int GenerateSoftConferencingTaskChecklist(int progress, BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks, XLWorkbook? checklist)
        {
            //update the progress
            worker.ReportProgress(progress += 5, "Soft Conferencing Tasks Generating...");
            //if soft conferencing is enabled
            if (project.SoftConferencing)
            {
                //generate a new sheet
                IXLWorksheet softConferencingWorksheet = AddWorksheet(checklist, "Soft Conferencing");
                //set the starting cell
                int cell = 2;
                //loop through all fixed tasks
                tasks.FixedTasks.Where(t => t.Type == TaskType.SoftConference || t.Type == TaskType.Conference).ToList().ForEach(delegate (Tasks.Task task)
                {
                    //generate the task details
                    task.GenerateTaskDetails(new Dictionary<string, object>() { { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } });
                    //assign the details
                    FormatDetailCell(cell, softConferencingWorksheet, task.Details);
                    //format the cell
                    FormatTaskStatusCell(cell, softConferencingWorksheet);
                    //get the next cell
                    cell++;
                });
                //format the worksheet
                GenerateConditionalFormatting(cell, softConferencingWorksheet);
            }
            //update progress
            return progress;
        }

        private static int GenerateRoomCombiningTaskChecklist(int progress, BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks, XLWorkbook? checklist)
        {
            //report the progress
            worker.ReportProgress(progress += 5, "Room Combining Tasks Generating...");
            //if room combining is checked
            if (project.RoomCombining)
            {
                //create the worksheet
                IXLWorksheet roomCombineWorksheet = AddWorksheet(checklist, "Room Combining");
                //set the starting cell
                int cell = 2;
                //loop through all fixed tasks
                tasks.FixedTasks.Where(t => t.Type == TaskType.RoomCombining).ToList().ForEach(delegate (Tasks.Task task)
                {
                    //store the task details for later
                    Dictionary<string, object> taskDetails = new Dictionary<string, object>() { { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } };
                    //generate the details
                    task.GenerateTaskDetails(taskDetails);
                    //assign the details to the cell
                    FormatDetailCell(cell, roomCombineWorksheet, task.Details);
                    //format the cell
                    FormatTaskStatusCell(cell, roomCombineWorksheet); ;
                    //get the next cell
                    cell++;
                });
                //format the sheet
                GenerateConditionalFormatting(cell, roomCombineWorksheet);
            }
            //update progress
            return progress;
        }

        private static int GenerateUserInterfaceTaskChecklist(int progress, BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks, XLWorkbook? checklist)
        {
            //report progress
            worker.ReportProgress(progress += 5, "User Interface Tasks Generating...");
            //check if user interfaces exist
            if (project.UserInterfaces.Count != 0)
            {
                //generate the worksheet
                IXLWorksheet userInterfaceWorksheet = AddWorksheet(checklist, "User Interfaces");
                //set the starting cell
                int cell = 2;
                //loop through all user interfaces defined
                project.UserInterfaces.ToList().ForEach(delegate (Device ui)
                {
                    //generate a header cell
                    GenerateHeaderCell(cell, ui.Name, userInterfaceWorksheet);
                    //get the next cell
                    cell++;
                    //loop through all fixed tasks
                    tasks.FixedTasks.Where(t => t.Type == TaskType.UserInterface).ToList().ForEach(delegate (Tasks.Task task)
                    {
                        //store the taskdetails for use later
                        Dictionary<string, object> taskDetails = new Dictionary<string, object>() { { "UserInterface", ui }, { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } };
                        //generate the task details
                        task.GenerateTaskDetails(taskDetails);
                        //assign the details to the cell
                        FormatDetailCell(cell, userInterfaceWorksheet, task.Details);
                        //format the cell
                        FormatTaskStatusCell(cell, userInterfaceWorksheet);
                        //get the next cell
                        cell++;

                        //check the dynamic tasks for user interfaces
                        List<Task> dynamicTasks = tasks.DynamicTasks.Where(t => t.Type == TaskType.Destination).ToList();
                        //if there are dynamic tasks for user interfaces, generate a task list
                        if (dynamicTasks.Count != 0)
                        {
                            Tuple<int, int> result = GenerateCustomTaskChecklist(progress, cell, ui.Name, taskDetails, worker, project, dynamicTasks, userInterfaceWorksheet);
                            //update the progress window
                            progress = result.Item2;
                            //return the current cell
                            cell = result.Item1;
                        }
                    });
                });
                //format the worksheet
                GenerateConditionalFormatting(cell, userInterfaceWorksheet);
            }
            //return the current progress
            return progress;
        }

        private static int GenerateMatrixTaskChecklist(int progress, BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks, XLWorkbook? checklist)
        {
            //report the progress when we start
            worker.ReportProgress(progress += 5, "Matrix Tasks Generating...");
            //check if any sources & destinations exist
            if (project.Sources.Count != 0 && project.Destinations.Count != 0)
            {
                //create the worksheet
                IXLWorksheet matrixWorksheet = AddWorksheet(checklist, "Matrix");
                //set the starting cell
                int cell = 2;
                //loop through all the sources, creating matrix tasks for each source.
                project.Sources.ToList().ForEach(delegate (Device src)
                {
                    //generate the header cell
                    GenerateHeaderCell(cell, src.Name, matrixWorksheet);
                    //increment to the next cell
                    cell++;
                    //create a list of valid destinations for this source
                    List<Device> validDestinations = new List<Device>();
                    //create a list of valid tasks for this source
                    List<Tasks.Task> validTasks = new List<Tasks.Task>();
                    //check the source's capability
                    if (src.Capability == MediaType.AudioVideo) //if its an audio/video source, get all tasks.
                    {
                        validDestinations = project.Destinations.ToList();
                        validTasks = tasks.FixedTasks.Where(t => t.Type == TaskType.Matrix).ToList();
                    }
                    else //if its a specific type audio OR video, only get destinations & tasks where they make sense
                    {
                        validDestinations = project.Destinations.Where(d => d.Capability == src.Capability || d.Capability == MediaType.AudioVideo).ToList();
                        validTasks = tasks.FixedTasks.Where(t => t.Type == TaskType.Matrix && t.Capability == src.Capability).ToList();
                    }
                    //loop through all destinations
                    validDestinations.ForEach(delegate (Device dest)
                    {
                        //loop through all tasks
                        validTasks.ForEach(delegate (Tasks.Task task)
                        {
                            //check the tasks capability against the destinations capability to make sure we only do things that make sense.
                            if (task.Capability == dest.Capability || dest.Capability == MediaType.AudioVideo)
                            {
                                //generate the task details
                                task.GenerateTaskDetails(new Dictionary<string, object>() { { "Source", src }, { "Destination", dest }, { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } });
                                //assign the task details
                                FormatDetailCell(cell, matrixWorksheet, task.Details);
                                //format the cell
                                FormatTaskStatusCell(cell, matrixWorksheet);
                                //go to the next cell
                                cell++;
                            }
                        });
                    });
                });
                //format the worksheet
                GenerateConditionalFormatting(cell, matrixWorksheet);
            }
            //update the progress
            return progress;
        }

        private static int GenerateDestinationTaskChecklist(int progress, BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks, XLWorkbook? checklist)
        {
            worker.ReportProgress(progress += 5, "Destination Tasks Generating...");
            //if destinations exist
            if (project.Destinations.Count != 0)
            {
                //create the worksheet
                IXLWorksheet destinationWorksheet = AddWorksheet(checklist, "Destinations");
                //set the starting cell
                int cell = 2;
                //loop through all the destinations
                project.Destinations.ToList().ForEach(delegate (Device destination)
                {
                    //generate the header cell for this destination
                    GenerateHeaderCell(cell, destination.Name, destinationWorksheet);
                    //increment to the next cell
                    cell++;
                    //generate the task details for use later
                    Dictionary<string, object> taskDetails = new Dictionary<string, object>() { { "Destination", destination }, { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } };
                    //go through all the fixed tasks
                    tasks.FixedTasks.Where(t => t.Type == TaskType.Destination && (t.Capability == destination.Capability || destination.Capability == MediaType.AudioVideo)).ToList().ForEach(delegate (Tasks.Task task)
                    {
                        //generate the task details
                        task.GenerateTaskDetails(taskDetails);
                        //assign the details to the pertinent cell
                        FormatDetailCell(cell, destinationWorksheet, task.Details);
                        //format the cell
                        FormatTaskStatusCell(cell, destinationWorksheet);
                        //increment to the next cell
                        cell++;
                    });

                    //check the dynamic tasks for sources
                    List<Task> dynamicTasks = tasks.DynamicTasks.Where(t => t.Type == TaskType.Destination).ToList();
                    //if there are dynamic tasks for sources, generate a task list
                    if (dynamicTasks.Count != 0)
                    {
                        Tuple<int, int> result = GenerateCustomTaskChecklist(progress, cell, destination.Name, taskDetails, worker, project, dynamicTasks, destinationWorksheet);
                        //update the progress window
                        progress = result.Item2;
                        //return the current cell
                        cell = result.Item1;
                    }
                });
                //format the worksheet
                GenerateConditionalFormatting(cell, destinationWorksheet);
            }
            //update the progress window
            return progress;
        }

        private static int GenerateSourceTaskChecklist(int progress, BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks, XLWorkbook? checklist)
        {
            worker.ReportProgress(progress += 5, "Source Tasks Generating");
            //if sources exist
            if (project.Sources.Count != 0)
            {
                //create a worksheet
                IXLWorksheet sourceWorksheet = AddWorksheet(checklist, "Sources");
                //set the starting cell
                int cell = 2;
                //loop through all sources
                project.Sources.ToList().ForEach(delegate (Device source)
                {
                    //generate the header cell for this source
                    GenerateHeaderCell(cell, source.Name, sourceWorksheet);
                    //increment to the next cell
                    cell++;
                    //get task details for use later.
                    Dictionary<string, object> taskDetails = new Dictionary<string, object>() { { "Source", source }, { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } };
                    //for each of the fixed tasks that will always be generated, do the thing
                    tasks.FixedTasks.Where(t => t.Type == TaskType.Source && (t.Capability == source.Capability || source.Capability == MediaType.AudioVideo)).ToList().ForEach(delegate (Tasks.Task task)
                    {
                        task.GenerateTaskDetails(taskDetails);
                        //assign the task's details to the pertinent cell
                        FormatDetailCell(cell, sourceWorksheet, task.Details);
                        //format the cell as needed
                        FormatTaskStatusCell(cell, sourceWorksheet);
                        //increment to the next cell
                        cell++;
                    });

                    //check the dynamic tasks for sources
                    List<Task> dynamicTasks = tasks.DynamicTasks.Where(t => t.Type == TaskType.Source).ToList();
                    //if there are dynamic tasks for sources, generate a task list
                    if (dynamicTasks.Count != 0) {
                        Tuple<int, int> result = GenerateCustomTaskChecklist(progress, cell, source.Name, taskDetails, worker, project, dynamicTasks, sourceWorksheet);
                        //update the progress window
                        progress = result.Item2;
                        //return the current cell
                        cell = result.Item1;
                    }
                });
                //format the source worksheet
                GenerateConditionalFormatting(cell, sourceWorksheet);
            }
            //update the progress
            return progress;
        }
    
        private static Tuple<int, int> GenerateCustomTaskChecklist(int progress, int cell, string title, Dictionary<string, object> details, BackgroundWorker worker, AVSystem.AVSystem project, List<Task> tasks, IXLWorksheet? sheet)
        {
            //report the progress
            worker.ReportProgress(progress += 1, "Generating Custom Tasks: " + title);
            //generate the header cell
            GenerateHeaderCell(cell, "Custom Tasks: " + title, sheet);
            //go to the next cell
            cell++;
            //loop through all tasks
            tasks.ForEach(delegate (Task task)
            {
                //generate the details for the task
                task.GenerateTaskDetails(details);
                //assign the details to the cell
                FormatDetailCell(cell, sheet, task.Details);
                //format the cell
                FormatTaskStatusCell(cell, sheet);
                //go to the next cell
                cell++;
            });
            //return the updated progress, and the latest cell
            return System.Tuple.Create(cell, progress);
        }
    }
}

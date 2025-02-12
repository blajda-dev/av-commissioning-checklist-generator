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
            worker.ReportProgress(progress += 5, "Gathering Controlled Devices...");

            //need to incorporate destinations and sources into this checklist, maybe user interfaces?? might be redundant
            bool controlledSources = project.Sources.Where(i => i.ControlMethod != ControlType.None)?.Count() != 0;
            bool controlledDestinations = project.Destinations.Where(i => i.ControlMethod != ControlType.None)?.Count() != 0;
            bool userInterfaces = project.UserInterfaces.Where(i => i.ControlMethod != ControlType.None)?.Count() != 0;
            bool controlledDevices = project.ControlledDevices.Count != 0;

            worker.ReportProgress(progress += 5, "Controlled Device Tasks Generating...");

            if (controlledDevices || controlledSources || controlledDestinations || userInterfaces)
            {
                IXLWorksheet controlledDevicesWorksheet = AddWorksheet(checklist, "Controlled Devices");
                int cell = 2;

                List<Device> controlledDeviceList = new List<Device>(project.ControlledDevices);
                controlledDeviceList.AddRange(project.Sources.Where(i => i.ControlMethod != ControlType.None));
                controlledDeviceList.AddRange(project.Destinations.Where(i => i.ControlMethod != ControlType.None));
                controlledDeviceList.AddRange(project.UserInterfaces.Where(i => i.ControlMethod != ControlType.None));

                controlledDeviceList.ForEach(delegate (Device device)
                {
                    GenerateHeaderCell(cell, device.Name, controlledDevicesWorksheet);
                    cell += 1;
                    tasks.FixedTasks.Where(t => t.Type == TaskType.ControlledDevice).ToList().ForEach(delegate (Tasks.Task task)
                    {
                        task.GenerateTaskDetails(new Dictionary<string, object>() { { "Device", device }, { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } });
                        controlledDevicesWorksheet.Cell(cell, 1).Value = task.Details;

                        FormatTaskStatusCell(cell, controlledDevicesWorksheet);

                        cell++;
                    });
                });

                GenerateConditionalFormatting(cell, controlledDevicesWorksheet);
            }

            return progress;
        }

        private static int GenerateVideoConferencingTaskChecklist(int progress, BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks, XLWorkbook? checklist)
        {
            worker.ReportProgress(progress += 5, "Video Conferencing Tasks Generating...");

            if (project.VideoConferencing)
            {
                IXLWorksheet videoConferencingWorksheet = AddWorksheet(checklist, "Video Conferencing");
                int cell = 2;
                tasks.FixedTasks.Where(t => t.Type == TaskType.VideoConference || t.Type == TaskType.Conference).ToList().ForEach(delegate (Tasks.Task task)
                {
                    task.GenerateTaskDetails(new Dictionary<string, object>() { { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } });
                    videoConferencingWorksheet.Cell(cell, 1).Value = task.Details;

                    FormatTaskStatusCell(cell, videoConferencingWorksheet);

                    cell += 1;
                });

                GenerateConditionalFormatting(cell, videoConferencingWorksheet);
            }

            return progress;
        }

        private static int GenerateAudioConferencingTaskChecklist(int progress, BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks, XLWorkbook? checklist)
        {
            worker.ReportProgress(progress += 5, "Audio Conferencing Tasks Generating...");

            if (project.AudioConferencing)
            {
                IXLWorksheet audioConferencingWorksheet = AddWorksheet(checklist, "Audio Conferencing");
                int cell = 2;
                tasks.FixedTasks.Where(t => t.Type == TaskType.AudioConference || t.Type == TaskType.Conference).ToList().ForEach(delegate (Tasks.Task task)
                {
                    task.GenerateTaskDetails(new Dictionary<string, object>() { { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } });
                    audioConferencingWorksheet.Cell(cell, 1).Value = task.Details;

                    FormatTaskStatusCell(cell, audioConferencingWorksheet);

                    cell++;
                });

                GenerateConditionalFormatting(cell, audioConferencingWorksheet);
            }

            return progress;
        }

        private static int GenerateSoftConferencingTaskChecklist(int progress, BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks, XLWorkbook? checklist)
        {
            worker.ReportProgress(progress += 5, "Soft Conferencing Tasks Generating...");

            if (project.SoftConferencing)
            {
                IXLWorksheet softConferencingWorksheet = AddWorksheet(checklist, "Soft Conferencing");
                int cell = 2;
                tasks.FixedTasks.Where(t => t.Type == TaskType.SoftConference || t.Type == TaskType.Conference).ToList().ForEach(delegate (Tasks.Task task)
                {
                    task.GenerateTaskDetails(new Dictionary<string, object>() { { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } });
                    softConferencingWorksheet.Cell(cell, 1).Value = task.Details;

                    FormatTaskStatusCell(cell, softConferencingWorksheet);

                    cell++;
                });

                GenerateConditionalFormatting(cell, softConferencingWorksheet);
            }

            return progress;
        }

        private static int GenerateRoomCombiningTaskChecklist(int progress, BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks, XLWorkbook? checklist)
        {
            worker.ReportProgress(progress += 5, "Room Combining Tasks Generating...");

            if (project.RoomCombining)
            {
                IXLWorksheet roomCombineWorksheet = AddWorksheet(checklist, "Room Combining");
                int cell = 2;
                tasks.FixedTasks.Where(t => t.Type == TaskType.RoomCombining).ToList().ForEach(delegate (Tasks.Task task)
                {
                    task.GenerateTaskDetails(new Dictionary<string, object>() { { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } });
                    roomCombineWorksheet.Cell(cell, 1).Value = task.Details;

                    FormatTaskStatusCell(cell, roomCombineWorksheet); ;

                    cell++;
                });

                GenerateConditionalFormatting(cell, roomCombineWorksheet);
            }

            return progress;
        }

        private static int GenerateUserInterfaceTaskChecklist(int progress, BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks, XLWorkbook? checklist)
        {
            worker.ReportProgress(progress += 5, "User Interface Tasks Generating...");

            if (project.UserInterfaces.Count != 0)
            {
                IXLWorksheet userInterfaceWorksheet = AddWorksheet(checklist, "User Interfaces");
                int cell = 2;
                project.UserInterfaces.ToList().ForEach(delegate (Device ui)
                {
                    GenerateHeaderCell(cell, ui.Name, userInterfaceWorksheet);
                    cell++;
                    tasks.FixedTasks.Where(t => t.Type == TaskType.UserInterface).ToList().ForEach(delegate (Tasks.Task task)
                    {
                        task.GenerateTaskDetails(new Dictionary<string, object>() { { "UserInterface", ui }, { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } });
                        userInterfaceWorksheet.Cell(cell, 1).Value = task.Details;

                        FormatTaskStatusCell(cell, userInterfaceWorksheet);

                        cell++;
                    });
                });

                GenerateConditionalFormatting(cell, userInterfaceWorksheet);
            }

            return progress;
        }

        private static int GenerateMatrixTaskChecklist(int progress, BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks, XLWorkbook? checklist)
        {
            worker.ReportProgress(progress += 5, "Matrix Tasks Generating...");

            if (project.Sources.Count != 0 && project.Destinations.Count != 0)
            {
                IXLWorksheet matrixWorksheet = AddWorksheet(checklist, "Matrix");
                int cell = 2;
                project.Sources.ToList().ForEach(delegate (Device src)
                {
                    GenerateHeaderCell(cell, src.Name, matrixWorksheet);
                    cell++;

                    List<Device> validDestinations = new List<Device>();
                    List<Tasks.Task> validTasks = new List<Tasks.Task>();

                    if (src.Capability == MediaType.AudioVideo)
                    {
                        validDestinations = project.Destinations.ToList();
                        validTasks = tasks.FixedTasks.Where(t => t.Type == TaskType.Matrix).ToList();
                    }
                    else
                    {
                        validDestinations = project.Destinations.Where(d => d.Capability == src.Capability || d.Capability == MediaType.AudioVideo).ToList();
                        validTasks = tasks.FixedTasks.Where(t => t.Type == TaskType.Matrix && t.Capability == src.Capability).ToList();
                    }

                    validDestinations.ForEach(delegate (Device dest)
                    {
                        validTasks.ForEach(delegate (Tasks.Task task)
                        {
                            if (task.Capability == dest.Capability || dest.Capability == MediaType.AudioVideo)
                            {
                                task.GenerateTaskDetails(new Dictionary<string, object>() { { "Source", src }, { "Destination", dest }, { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } });
                                matrixWorksheet.Cell(cell, 1).Value = task.Details;

                                FormatTaskStatusCell(cell, matrixWorksheet);

                                cell++;
                            }
                        });
                    });
                });

                GenerateConditionalFormatting(cell, matrixWorksheet);
            }

            return progress;
        }

        private static int GenerateDestinationTaskChecklist(int progress, BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks, XLWorkbook? checklist)
        {
            worker.ReportProgress(progress += 5, "Destination Tasks Generating...");

            if (project.Destinations.Count != 0)
            {
                IXLWorksheet destinationWorksheet = AddWorksheet(checklist, "Destinations");
                int cell = 2;
                project.Destinations.ToList().ForEach(delegate (Device destination)
                {
                    GenerateHeaderCell(cell, destination.Name, destinationWorksheet);
                    cell++;
                    tasks.FixedTasks.Where(t => t.Type == TaskType.Destination && (t.Capability == destination.Capability || destination.Capability == MediaType.AudioVideo)).ToList().ForEach(delegate (Tasks.Task task)
                    {
                        task.GenerateTaskDetails(new Dictionary<string, object>() { { "Destination", destination }, { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } });
                        destinationWorksheet.Cell(cell, 1).Value = task.Details;

                        FormatTaskStatusCell(cell, destinationWorksheet);

                        cell++;
                    });
                });

                GenerateConditionalFormatting(cell, destinationWorksheet);
            }

            return progress;
        }

        private static int GenerateSourceTaskChecklist(int progress, BackgroundWorker worker, AVSystem.AVSystem project, Tasks.Tasks tasks, XLWorkbook? checklist)
        {
            worker.ReportProgress(progress += 5, "Source Tasks Generating");

            if (project.Sources.Count != 0)
            {
                IXLWorksheet sourceWorksheet = AddWorksheet(checklist, "Sources");
                
                int cell = 2;
                project.Sources.ToList().ForEach(delegate (Device source)
                {
                    GenerateHeaderCell(cell, source.Name, sourceWorksheet);

                    cell += 1;

                    Dictionary<string, object> taskDetails = new Dictionary<string, object>() { { "Source", source }, { "UserInterfaces", string.Join(",", project.UserInterfaces.Select(u => u.Name).ToArray()) } };

                    tasks.FixedTasks.Where(t => t.Type == TaskType.Source && (t.Capability == source.Capability || source.Capability == MediaType.AudioVideo)).ToList().ForEach(delegate (Tasks.Task task)
                    {
                        task.GenerateTaskDetails(taskDetails);

                        sourceWorksheet.Cell(cell, 1).Value = task.Details;

                        FormatTaskStatusCell(cell, sourceWorksheet);

                        cell += 1;
                    });

                    Tuple<int, int> result = GenerateCustomTaskChecklist(progress, cell, source.Name, taskDetails, worker, project, tasks.DynamicTasks.Where(t => t.Type == TaskType.Source).ToList(), sourceWorksheet);
                    
                    progress = result.Item2;
                    cell = result.Item1;

                });

                GenerateConditionalFormatting(cell, sourceWorksheet);
            }

            return progress;
        }
    
        private static Tuple<int, int> GenerateCustomTaskChecklist(int progress, int cell, string title, Dictionary<string, object> details, BackgroundWorker worker, AVSystem.AVSystem project, List<Task> tasks, IXLWorksheet? sheet)
        {
            worker.ReportProgress(progress += 1, "Generating Custom Tasks: " + title);

            GenerateHeaderCell(cell, "Custom Tasks: " + title, sheet);

            cell += 1;

            tasks.ForEach(delegate (Task task)
            {
                task.GenerateTaskDetails(details);

                sheet.Cell(cell, 1).Value = task.Details;
               
                FormatTaskStatusCell(cell, sheet);
                
                cell += 1;
            });

            return System.Tuple.Create(cell, progress);
        }
    }
}

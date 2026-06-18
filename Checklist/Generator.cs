using CommissioningChecklistGenerator.ProjectModel;
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
using Serilog;
using DocumentFormat.OpenXml.Presentation;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CommissioningChecklistGenerator.UI;
using CommissioningChecklistGenerator.Database;

namespace CommissioningChecklistGenerator.Checklist
{
    public static class Generator
    {
        private static int progress;
        private const string Prefix = "[Generator]";
        private static readonly TaskCompletionSource<bool> _idle = new TaskCompletionSource<bool>(true);
        public static Task Idle => _idle.Task;

        static Generator()
        {
            //make sure generator is idle at startup
            _idle.SetResult(true);
        }

        /// <summary>
        /// adds a worksheet to the workbook
        /// </summary>
        /// <param name="workbook">the workbook to add the worksheet to</param>
        /// <param name="name">the name of the worksheet</param>
        /// <returns>returns the worksheet</returns>
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

        /// <summary>
        /// formats the provided workbook
        /// </summary>
        /// <param name="workbook">the workbook to format</param>
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

        /// <summary>
        /// generates the header cell inside a worksheet
        /// </summary>
        /// <param name="cell">the current cell that we are in within the worksheet</param>
        /// <param name="contents">the contents of the header cell</param>
        /// <param name="worksheet">the worksheet in which to create the cells</param>
        private static void GenerateHeaderCell(int cell, string contents, IXLWorksheet worksheet)
        {
            IXLCell headerCell = worksheet.Cell(cell, 1);
            //set cell bold
            headerCell.Style.Font.Bold = true;
            //set cell contents
            headerCell.Value = contents;
            //get the range we need
            IXLRange range = worksheet.Range(worksheet.Cell(cell, 1), worksheet.Cell(cell, 2));
            //merge the cells
            range.Merge();
            //align the content center
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        /// <summary>
        /// formats a details cell
        /// </summary>
        /// <param name="cell">the current cell we are working in</param>
        /// <param name="worksheet">the worksheet containing the cells</param>
        /// <param name="details">the content of the cells</param>
        private static void FormatDetailCell(int cell, IXLWorksheet worksheet, string details)
        {
            IXLCell taskDetailCell = worksheet.Cell(cell, 1);
            taskDetailCell.Value = details;
            taskDetailCell.Style.Alignment.WrapText = true;
        }

        /// <summary>
        /// formats the task status cell to 
        /// </summary>
        /// <param name="cell">the current cell we are working in</param>
        /// <param name="worksheet">the worksheet containing the cells</param>
        private static void FormatTaskStatusCell(int cell, IXLWorksheet worksheet)
        {
            var taskOptions = new List<string>() { "Incomplete", "Completed", "N/A", "In Progress" };
            var validOptions = $"\"{String.Join(",", taskOptions)}\"";

            IXLCell taskStatusCell = worksheet.Cell(cell, 2);
            IXLDataValidation taskValidation = taskStatusCell.CreateDataValidation();

            taskValidation.List(validOptions);
            taskValidation.IgnoreBlanks = true;
            taskValidation.InCellDropdown = true;
            taskStatusCell.Value = "Incomplete";
        }

        /// <summary>
        /// generates conditional formatting rules for the entire worksheet
        /// </summary>
        /// <param name="cell">the current cell</param>
        /// <param name="sheet">the worksheet to format</param>
        private static void GenerateConditionalFormatting(int cell, IXLWorksheet sheet)
        {
            string conditionIncomplete = @"=$B1=""Incomplete""";
            string conditionComplete = @"=$B1=""Completed""";
            string conditionNotApplicable = @"=$B1=""N/A""";
            string conditionInProgress = @"=$B1=""In Progress""";

            sheet.RangeUsed().AddConditionalFormat().WhenIsTrue(conditionIncomplete).Fill.SetBackgroundColor(XLColor.FromHtml("#FFC7CE"));
            sheet.RangeUsed().AddConditionalFormat().WhenIsTrue(conditionComplete).Fill.SetBackgroundColor(XLColor.FromHtml("#C6EFCE"));
            sheet.RangeUsed().AddConditionalFormat().WhenIsTrue(conditionNotApplicable).Fill.SetBackgroundColor(XLColor.PastelGray);
            sheet.RangeUsed().AddConditionalFormat().WhenIsTrue(conditionInProgress).Fill.SetBackgroundColor(XLColor.FromHtml("#FFEB9C"));
        }

        /// <summary>
        /// generates the excel checklist based on the provided projects details
        /// </summary>
        /// <param name="worker">the background worker that is handling the process</param>
        /// <param name="project">the avsystem project that we are generating the checklist for</param>
        /// <returns>an excel workbook</returns>
        public static async Task<XLWorkbook?> GenerateChecklist(IProgress<ProgressUpdate> reporter, AVSystem project)
        {
            _idle.TrySetResult(false);

            XLWorkbook? workbook = await Task.Run(async () =>
            {
                XLWorkbook? workbook = null;
                bool result = await Querier.GetDatabaseConnectionState();
                if (result)
                { 
                    try
                    {
                        //try to create a new workbook
                        workbook = new XLWorkbook();
                        progress += 5;
                        reporter.Report(new ProgressUpdate(progress, "Workbook Created..."));
                    }
                    catch (Exception e) { MessageBox.Show(App.Window, e.Message); }
                    finally
                    {
                        //show busy dialog
                        if (workbook != null)
                        {
                            //if we succeed at getting system tasks
                            if (project.GetCommissioningTasks())
                            {
                                GenerateWorksheetChecklistForDevices(reporter, project.Sources.ToList(), workbook, "Sources");

                                GenerateWorksheetChecklistForDevices(reporter, project.Destinations.ToList(), workbook, "Destinations");

                                GenerateWorksheetChecklistForDevices(reporter, project.UserInterfaces.ToList(), workbook, "User Interfaces");

                                GenerateWorksheetChecklistForDevices(reporter, project.ControlledDevices.ToList(), workbook, "Controlled Devices");
                            }

                            //system type tasks
                            project.GetSystemCommissioningTasks()?.ToList()?.ForEach(kvp =>
                            {
                                Log.Debug($"{Prefix} generate task list for {kvp.Key} -> {kvp.Value.Count} tasks");
                                GenerateTaskChecklist(reporter, kvp.Value, workbook, kvp.Key);
                            });

                            FormatWorkbook(workbook);
                        }
                    }

                    reporter.Report(new ProgressUpdate(progress, "Generated Commissioning Checklist"));
                    Log.Information($"{Prefix} generated comissioning checklist");
                }
                else { Log.Fatal($"{Prefix} cannot generate checklist because database is not available!"); }
                
                _idle.TrySetResult(true);
                
                return workbook;
            });

            return workbook;
        }

        /// <summary>
        /// generates the excel worksheet for the the devices provided
        /// </summary>
        /// <param name="worker">the background worker to report progress to</param>
        /// <param name="devices">the devices to generate the checklist for</param>
        /// <param name="workbook">the excel workbook</param>
        /// <param name="checklistName">the name of the checklist, which will become the sheet name</param>
        private static void GenerateWorksheetChecklistForDevices(IProgress<ProgressUpdate> reporter, List<Device> devices, XLWorkbook? workbook, string checklistName)
        {
            //report the progress
            reporter.Report(new ProgressUpdate(progress += 5, String.Format("Gathering {0}...", checklistName)));

            //update progress
            reporter.Report(new ProgressUpdate(progress += 5, String.Format("{0} Worksheet Generating...", checklistName)));
            //if any flag is set create the controlled device list
            if (devices.Count != 0 && workbook != null)
            {
                //generate the cell
                IXLWorksheet worksheet = AddWorksheet(workbook, checklistName);
                //set the starting cell
                int cell = 2;
                //loop through all the controlled devices
                devices.ToList().ForEach(device =>
                {
                    //generate a header cell
                    GenerateHeaderCell(cell, String.Format("{1} {2} | {0} | {3}", device.Name, device.Manufacturer, device.Model, device.Description), worksheet);
                    //get the next cell
                    cell++;
                    //loop through all the tasks
                    device.Tasks.ToList().ForEach(task =>
                    {
                        //assign to the cell & format it
                        FormatDetailCell(cell, worksheet, String.Format("{0} -> {1}", task.Name, task.Description));
                        //format the cell
                        FormatTaskStatusCell(cell, worksheet);
                        //get the next cell
                        cell++;
                    });
                });
                //format the sheet
                GenerateConditionalFormatting(cell, worksheet);
                //lastly make sure all text fits
                worksheet.Columns().AdjustToContents();
                worksheet.Rows().AdjustToContents();
            }
        }

        /// <summary>
        /// creates a sheet with a list of tasks provided
        /// </summary>
        /// <param name="worker">the background worker to report progress to</param>
        /// <param name="tasks">the tasks to fill this sheet with</param>
        /// <param name="workbook">the excel workbook</param>
        /// <param name="checklistName">the name of the checklist, which will become the sheet name</param>
        private static void GenerateTaskChecklist(IProgress<ProgressUpdate> reporter, List<CommissioningTask> tasks, XLWorkbook? workbook, string checklistName)
        {
            //update the progress
            reporter.Report(new ProgressUpdate(progress += 5, $"{checklistName} Tasks Generating..."));
            //if video conferencing is enabled
            if (tasks.Count != 0 && workbook != null)
            {
                //generate the sheet
                IXLWorksheet worksheet = AddWorksheet(workbook, checklistName);
                //set the starting cell
                int cell = 2;
                //loop through the tasks
                tasks.ForEach(task =>
                {
                    //assign to the cell
                    FormatDetailCell(cell, worksheet, $"{task.Name} -> {task.Description}");
                    //format the cell
                    FormatTaskStatusCell(cell, worksheet);
                    //get the next cell
                    cell++;
                });
                //format the sheet
                GenerateConditionalFormatting(cell, worksheet);
                //lastly make sure all text fits
                worksheet.Columns().AdjustToContents();
                worksheet.Rows().AdjustToContents();
            }
        }
    }
}

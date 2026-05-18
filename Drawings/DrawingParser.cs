using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Shell;
using CommissioningChecklistGenerator.ProjectModel;
using CommissioningChecklistGenerator.UI;
using Irony.Parsing;
using Newtonsoft.Json.Bson;
using Serilog;
using ACadSharp;
using ACadSharp.IO;
using ACadSharp.Entities;
using CSMath.Geometry;
using System.IO;

namespace CommissioningChecklistGenerator.Drawings
{
    static class DrawingParser
    {
        private const string Prefix = "[DrawingParser]";

        public delegate void DrawingParserEventHandler(DrawingParsedEventArgs args); 
        public static DrawingParserEventHandler? DrawingParsed;

        private static int progress = 0;

        public static CadDocument? OpenDxfFile(IProgress<ProgressUpdate> reporter, string path)
        {
            CadDocument? drawing = null;

            try {
                drawing = DxfReader.Read(path);
                Log.Information($"{Prefix} successfully loaded dxf drawing @ {path}");
                progress += 5;
                reporter.Report(new ProgressUpdate(progress, $"Successfully Loaded Drawing..."));
            }
            catch (Exception ex) { Log.Error(ex, $"{Prefix} unable to load dxf drawing @ {path}"); }

            return drawing;
        }

        private static CadDocument? OpenDwgFile(IProgress<ProgressUpdate> reporter, string path)
        {
            CadDocument? drawing = null;

            try
            {
                drawing = DwgReader.Read(path);
                Log.Information($"{Prefix} successfully loaded dwg drawing @ {path}");
                progress += 5;
                reporter.Report(new ProgressUpdate(progress, $"Successfully Loaded Drawing..."));
            }
            catch (Exception ex) { Log.Error(ex, $"{Prefix} unable to load dwg drawing @ {path}"); }

            return drawing;
        }

        public static async Task<DrawingParsedEventArgs> ParseSystemDrawing(IProgress<ProgressUpdate> reporter, string path)
        {
            //reset the progress value to default
            progress = 0;
            //set the initial state
            progress += 5;
            string extension = Path.GetExtension(path).ToUpper();
            reporter.Report(new ProgressUpdate(progress, $"Opening {extension} File"));

            CadDocument? drawing = null;

            switch(extension)
            {
                case DrawingConstants.DxfFileExtension:
                    drawing = OpenDxfFile(reporter, path);
                    break;
                case DrawingConstants.DwgFileExtension:
                    drawing = OpenDwgFile(reporter, path);
                    break;
            }

            if (drawing != null) {
                DrawingParsedEventArgs result = await Task.Run(() =>
                {
                    List<Device> devices = GenerateDevicesFromDrawing(reporter, drawing);
                    AVSystem system = GenerateAVSystemFromParsedDevices(reporter, devices);
                    progress = 100;
                    reporter.Report(new ProgressUpdate(progress, "Completed the drawing parsing operation"));
                    
                    if (system.Devices.Count != 0) { return new DrawingParsedEventArgs(true, $"Parsed {system.Devices.Count} devices with potential commissioning tasks!", system); }
                    else { return new DrawingParsedEventArgs(false, "No Valid Devices Found!!", system); }
                });

                return result;
            }
            else { return new DrawingParsedEventArgs(false, $"Unable To Open DXF Drawing @ {path}", null);  }
        }

        private static List<Device> GenerateDevicesFromDrawing(IProgress<ProgressUpdate> reporter, CadDocument document)
        {
            List<Device> devices = new List<Device>();
            List<Entity>? entities = document.Entities.Where(e => e.ObjectType == ObjectType.INSERT)?.ToList();
            int count = 0;

            if (entities != null)
            {
                count = entities.Count;
                int max = 40;
                int available = max - progress;
                Log.Debug($"{Prefix} calculate remaining progress available: {available} = {max} - {progress}");
                float increment = (float)available / count;
                Log.Debug($"{Prefix} calculate increment: {increment} = {available} / {count}");
                float microprogress = 0;

                List<Insert>? inserts = entities.Where(i => ((Insert)i).HasAttributes && ((Insert)i).Attributes.ToList()?.Find(a => a.Tag == DrawingConstants.BlockPrefixTag) != null)?.Select(insert => (Insert)insert).ToList();

                try
                {
                    if (inserts != null)
                    {
                        inserts.ForEach(i =>
                        {
                            microprogress += increment;
                            if (microprogress > 1)
                            {
                                //Log.Debug($"{Prefix} current progress: {progress}");
                                progress += (int)Math.Ceiling(microprogress);
                                reporter.Report(new ProgressUpdate(progress, $"Parsing Entity {inserts.IndexOf(i)} of {count}..."));
                                microprogress = 0;
                            }

                            //Log.Debug($"{Prefix} current microprogress: {microprogress}");

                            string? prefix = i.Attributes.FirstOrDefault(a => a.Tag == DrawingConstants.BlockPrefixTag)?.Value;
                            string? make = i.Attributes.FirstOrDefault(a => DrawingConstants.BlockManufacturerTags.Contains(a.Tag))?.Value;
                            string? model = i.Attributes.FirstOrDefault(a => DrawingConstants.BlockModelTags.Contains(a.Tag))?.Value;
                            string? info = i.Attributes.FirstOrDefault(a => DrawingConstants.BlockDescriptionTags.Contains(a.Tag))?.Value;

                            //make sure the insert we found has valid data
                            if (prefix != null && prefix != "")
                            {
                                //make sure that the device was not already added
                                if (devices.Find(d => d.Name == prefix) == null)
                                {
                                    if (Regex.Match(prefix, DrawingConstants.ValidDevicePattern).Success)
                                    {
                                        Log.Debug($"{Prefix} attempting to create device | prefix: {prefix} // manufacturer: {make} // model: {model} // description: {info}");
                                        devices.Add(new Device(prefix, make, model, info));
                                    }
                                    else { Log.Debug($"{Prefix} not creating device | prefix: {prefix} -> is not a match to the prefix pattern"); }
                                }
                            }
                        });
                    }
                    else
                    {
                        Log.Warning($"{Prefix} failed to retrieve any insert entities from the dxf document @ {document.SummaryInfo}");
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, $"{Prefix} converting entities into devices from the dxf document @ {document.SummaryInfo}");
                }
            }
            else { Log.Warning($"{Prefix} no insert entities found in drawing"); }

            return devices;
        }

        private static AVSystem GenerateAVSystemFromParsedDevices(IProgress<ProgressUpdate> reporter, List<Device> devices)
        {
            progress += 5;
            reporter.Report(new ProgressUpdate(progress, "Generating AV System..."));

            AVSystem parsed = new AVSystem();
            int max = 70;
            int available = max - progress;
            Log.Debug($"{Prefix} calculate remaining progress: {available} = {max} - {progress}");
            float increment = (float)available / devices.Count;
            Log.Debug($"{Prefix} calculate increment: {increment} = {available} / {devices.Count}");
            float microprogress = 0;

            devices.ForEach(d =>
            {
                parsed.Devices.Add(d);
                Log.Debug($"{Prefix} attempting to add device: {d.Name} to new av system object");
                microprogress += increment;

                if (microprogress > 1)
                {
                    //Log.Debug($"{Prefix} progress: {progress}");
                    progress += (int)Math.Ceiling(microprogress);
                    reporter.Report(new ProgressUpdate(progress, $"Added Device {devices.IndexOf(d)} of {devices.Count} to Av System..."));
                    microprogress = 0;
                }

                //Log.Debug($"{Prefix} microprogress: {microprogress}");
            });
            
            progress += 5;
            reporter.Report(new ProgressUpdate(progress, $"{Prefix} Successfully {devices.Count} to New AV System..."));
            return parsed;
        }
    }
}

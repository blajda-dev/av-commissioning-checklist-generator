using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommissioningChecklistGenerator.Settings
{
    class Constants
    {
        internal const string ApplicationName = "CommissioningChecklistGenerator";
        internal const string ConfigurationFileName = "config.json";
        internal static readonly string ApplicationDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Settings.Constants.ApplicationName);
    }
}

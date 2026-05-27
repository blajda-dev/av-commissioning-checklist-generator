using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommissioningChecklistGenerator.Database
{
    internal static class DatabaseConstants
    {


        //databases
        internal const string LatestDatabaseFileName = "latest.db";
        internal const string EmbeddedDatabaseOutputFilename = "embed.db";
        internal const string DesignTimeDatabaseFilename = "local.db";

        //views
        internal const string DeviceModelTaskView = "device_model_task";
        internal const string TaskRequiredCapabilityView = "task_required_capabilities";
        internal const string DeviceTypeTaskView = "device_type_task";

        //colummns
        internal const string DeviceModelIDColumnName = "device_model_id";
        internal const string CapabilityIDColumnName = "capability_id";
        internal const string TaskNameColumnName = "task_name";
        internal const string TaskDescriptionColumnName = "task_description";
        //file locations
        internal static readonly string EmbeddedDatabaseFilePath = Path.Combine(Settings.Constants.ApplicationDataDirectory, DatabaseConstants.EmbeddedDatabaseOutputFilename);
        internal static readonly string LatestDatabaseFilePath = Path.Combine(Settings.Constants.ApplicationDataDirectory, DatabaseConstants.LatestDatabaseFileName);
        //updaters
        internal const string ServerDatabaseFilePath = "/db/latest/database.db";
        internal const int ServerUpdateInterval = 60 * 1000 * 60;
    }
}

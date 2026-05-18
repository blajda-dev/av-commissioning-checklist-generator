using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommissioningChecklistGenerator.ProjectModel;

namespace CommissioningChecklistGenerator.Drawings
{
    internal class DrawingParsedEventArgs
    {
        public bool Success { get; private set; }
        public string Reason { get; private set; }
        public AVSystem? System { get; private set; }

        public DrawingParsedEventArgs(bool success, string reason, AVSystem? parsed)
        {
            this.Reason = reason;
            this.Success = success;
            this.System = parsed;
        }
    }
}

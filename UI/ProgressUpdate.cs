using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommissioningChecklistGenerator.UI
{
    public class ProgressUpdate
    {
        public int Percent { get; private set; }
        public string Message { get; private set; }
        public ProgressUpdate(int progress, string message) { 
            this.Percent = progress;
            this.Message = message;
        }
    }
}

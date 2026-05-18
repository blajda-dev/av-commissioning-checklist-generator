using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommissioningChecklistGenerator.Checklist
{
    public class CommissioningTask
    {
        public string Name { get; private set; } = "Unnamed Task";
        public string Description { get; private set; } = "No Description Provided";
        public CommissioningTask(string name, string description)
        {
            this.Name = name;
            this.Description = description;
        }
    }
}

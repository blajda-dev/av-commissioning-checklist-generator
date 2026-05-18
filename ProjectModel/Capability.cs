using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommissioningChecklistGenerator.ProjectModel
{
    public enum Capability : Int64
    {
        None = 0,
        Input = 1,
        Output = 2,
        Audio = 3,
        Video = 4,
        Controllable = 5,
        UserInterface = 6,
        Power = 7, 
        Combine = 8,
        Conference = 9,
        Camera = 10,
        USB = 11, 
        DTMF = 12, 
        Speech = 13,
        Endpoint = 14,
        Switching = 15
    }
}

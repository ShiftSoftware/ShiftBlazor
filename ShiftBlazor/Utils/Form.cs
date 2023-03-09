using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Utils
{
    public class Form
    {

        public enum Modes
        {
            View = 0,
            Edit = 10,
            Create = 20,
        };

        public enum Tasks
        {
            None,
            Print,
            Delete,
            Save,
            Fetch,
            Custom,
        }
    }
}

using ShiftSoftware.ShiftEntity.Core.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Tests
{
    public class Sample : ShiftEntityDTOBase
    {
        public string Name { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public string City { get; set; }
        public int Revisions { get; set; }
    }
}

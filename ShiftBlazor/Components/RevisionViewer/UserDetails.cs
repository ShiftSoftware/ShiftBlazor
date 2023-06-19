using ShiftSoftware.ShiftEntity.Model.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public class UserDetails : ShiftEntityListDTO
    {
        [ShiftEntity.Model.HashId.UserHashIdConverter]
        public override string? ID { get; set; }

        public string Name { get; set; }
    }
}

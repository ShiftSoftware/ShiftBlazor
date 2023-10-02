using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftBlazor.Tests.Shared.DTOs
{
    public class RequiredPropertyDTO<T>
    {
        [Required]
        public T? Property { get; set; }
    }
}

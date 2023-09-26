using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftBlazor.Tests.Viewer.Models
{
    public class User : ShiftEntityDTO
    {
        public override string? ID { get; set; }
        [Required]
        public string Name { get; set; }
        public string Email { get; set; }
    }
}

using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Services;

namespace ShiftSoftware.ShiftBlazor.Interfaces
{
    public interface IODataComponent : IComponent
    {
        public string EntitySet { get; set; }
        public string? BaseUrl { get; set; }
        public string? BaseUrlKey { get; set; }
    }
}

using Theoremone.SmartAc.Domain.Models;

namespace Theoremone.SmartAc.Application.DTOs
{
    public class DeviceAlertDto
    {
        public int DeviceAlertId { get; set; }
        public DeviceAlertType Type { get; set; }
        public DateTimeOffset DateCreated { get; set; }
        public DateTimeOffset DateRecorded { get; set; }
        public DateTimeOffset DateLastRecorded { get; set; }
        public string Message { get; set; } = string.Empty;
        public ResolutionState State { get; set; }
        public string DeviceSerialNumber { get; set; } = string.Empty;
        public string? ReadingNonNumeric { get; set; }
        public decimal? Reading { get; set; }
        public decimal HighestReading { get; set; }
        public decimal LowestReading { get; set; }
    }
}

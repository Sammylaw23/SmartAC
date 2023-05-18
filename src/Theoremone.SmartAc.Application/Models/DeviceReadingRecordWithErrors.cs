using Theoremone.SmartAc.Domain.Models;

namespace Theoremone.SmartAc.Application.Models
{
    public class DeviceReadingRecordWithErrors
    {
        public DateTimeOffset RecordedDateTime { get; set; }
        public decimal Temperature { get; set; }
        public decimal Humidity { get; set; }
        public decimal CarbonMonoxide { get; set; }
        public string DeviceSerialNumber { get; set; }
        public DeviceHealth Health { get; set; }
        public Error Error { get; set; } = new();
    }

    public class Error
    {
        public string? Temperature { get; set; }
        public string? CarbonMonoxide { get; set; }
        public string? Humidity { get; set; }
        public string? UnsafeCO { get; set; }
        public string? Unhealthy { get; set; }
    }
}

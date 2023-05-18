using System.ComponentModel.DataAnnotations.Schema;
using Theoremone.SmartAc.Domain.Models;

namespace Theoremone.SmartAc.Domain.Entities;

public class DeviceReading
{
    public int DeviceReadingId { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal Temperature { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal Humidity { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal CarbonMonoxide { get; set; }

    public DeviceHealth Health { get; set; }
    public DateTimeOffset RecordedDateTime { get; set; }
    public DateTimeOffset ReceivedDateTime { get; set; } = DateTime.UtcNow;

    public string DeviceSerialNumber { get; set; } = string.Empty;
    public Device Device { get; set; } = null!;
}
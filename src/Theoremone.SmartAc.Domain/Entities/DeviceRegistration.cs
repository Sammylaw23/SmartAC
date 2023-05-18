namespace Theoremone.SmartAc.Domain.Entities;

public class DeviceRegistration
{
    public int DeviceRegistrationId { get; set; }

    public Device Device { get; set; } = null!;
    public string DeviceSerialNumber { get; set; } = string.Empty;

    public DateTimeOffset RegistrationDate { get; set; } = DateTimeOffset.Now;
    public string TokenId { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
}
using Theoremone.SmartAc.Application.Models;

namespace Theoremone.SmartAc.Application.Interfaces
{
    public interface IAlertService
    {
        Task ProduceAlerts(string serialNumber, IEnumerable<DeviceReadingRecordWithErrors> readings);
    }
}

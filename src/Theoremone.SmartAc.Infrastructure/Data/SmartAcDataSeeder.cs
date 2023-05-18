using Theoremone.SmartAc.Domain.Entities;
using Theoremone.SmartAc.Infrastructure.DbContexts;

namespace Theoremone.SmartAc.Infrastructure.Data;

public class SmartAcDataSeeder
{
    public static void Seed(SmartAcContext smartAcContext)
    {
        if (!smartAcContext.Devices.Any())
        {
            var testData = new List<Device>()
            {
                new Device()
                {
                    SerialNumber = "test-ABC-123-XYZ-001",
                    SharedSecret = "secret-ABC-123-XYZ-001"
                },
                new Device()
                {
                    SerialNumber = "test-ABC-123-XYZ-002",
                    SharedSecret = "secret-ABC-123-XYZ-002"
                },
                new Device()
                {
                    SerialNumber = "test-ABC-123-XYZ-003",
                    SharedSecret = "secret-ABC-123-XYZ-003"
                },
                new Device()
                {
                    SerialNumber = "test-ABC-123-XYZ-004",
                    SharedSecret = "secret-ABC-123-XYZ-004"
                },
                new Device()
                {
                    SerialNumber = "test-ABC-123-XYZ-005",
                    SharedSecret = "secret-ABC-123-XYZ-005"
                }
            };

            smartAcContext.Devices.AddRange(testData);
            smartAcContext.SaveChanges();
        }
    }
}
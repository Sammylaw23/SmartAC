using Microsoft.EntityFrameworkCore;
using Theoremone.SmartAc.Domain.Entities;

namespace Theoremone.SmartAc.Application.Data.Extensions;
public static class DeviceRegistrationExtensions
{
    public static async Task<IQueryable<DeviceRegistration>> DeactivateRegistrations(
        this IQueryable<DeviceRegistration> deviceRegistrations, string deviceSerialNumber)
    {
        var registrations = deviceRegistrations
            .Where(registration => registration.DeviceSerialNumber == deviceSerialNumber && registration.Active);

        await foreach (var registration in registrations.AsAsyncEnumerable())
        {
            registration.Active = false;
        }

        return deviceRegistrations;
    }
}
using Theoremone.SmartAc.Domain.Models;

namespace Theoremone.SmartAc.Application.Models
{
    public class DeviceParameters : QueryStringParameters
    {
        public ResolutionState State { get; set; } = ResolutionState.New;
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Theoremone.SmartAc.Domain.Entities;
using Theoremone.SmartAc.Domain.Models;

namespace Theoremone.SmartAc.Domain.Entities
{
    public class DeviceAlert
    {
        public int DeviceAlertId { get; set; }
        public DeviceAlertType Type { get; set; }
        public DateTimeOffset DateCreated { get; set; } = DateTime.UtcNow;
        public DateTimeOffset DateRecorded { get; set; }
        public DateTimeOffset DateLastRecorded { get; set; }
        public string Message { get; set; } = string.Empty;
        public ResolutionState State { get; set; }
        public string DeviceSerialNumber { get; set; } = string.Empty;
        public decimal Data { get; set; }
        public string? DataNonNumeric { get; set; }

        public void SetState(ResolutionState newState,DateTimeOffset dateTime)
        {
            State = newState;
            DateLastRecorded = dateTime;
        }
    }


}

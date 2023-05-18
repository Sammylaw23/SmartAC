using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Theoremone.SmartAc.Application.Settings
{
    public class GeneralSettingsOptions
    {
        public const string GeneralSettings = "GeneralSettings";

        public int TimeIntervalToMergeAlerts { get; set; }
        public int BufferTimeInMinutes { get; set; }
    }
}
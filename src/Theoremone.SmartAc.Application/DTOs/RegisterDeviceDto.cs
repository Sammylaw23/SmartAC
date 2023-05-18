using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Theoremone.SmartAc.Application.DTOs;
public record RegisterDeviceDto(string serialNumber, string sharedSecret, string firmwareVersion);

using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetAuthServer
{
    public class KeyRotationOptions
    {
        public int KeyRotationInDays { get; set; }
        public int KeyRotationInHours { get; set; }
        public int KeyRotationInMinutes { get; set; }
        public int KeyRotationInSeconds { get; set; }
    }
}

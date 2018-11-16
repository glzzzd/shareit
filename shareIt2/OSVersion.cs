using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;

namespace shareIt2
{
    class OSVersion
    {
        public static string getOSInfo()
        {
            var name = (from x in new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem").Get().OfType<ManagementObject>()
                        select x.GetPropertyValue("Caption")).FirstOrDefault();
            return name != null ? name.ToString() : "Unknown";
        }
    }
}

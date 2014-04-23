using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetInfoCollect
{

    public class HostResponse
    {
        public long Timeout { get; set; }
        public string DNSName { get; set; }
        public string IP { get; set; }
        public string StatusMessage { get; set; }
        public int Status { get; set; }
        public int HOST_ID { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool isDMZ { get; set; }
    }

    public class ResourceResponce : HostResponse
    {
        public int PhysicalMemoryTotal { get; set; }
        public int PhysicalMemoryFree { get; set; }
        public int DiskSpaceTotal { get; set; }
        public int DiskSpaceFree { get; set; }
        public string ERROR_MESSAGE { get; set; }
        public string OperatingSystemName { get; set; }
    }

    public class ParallelConnect
    {
        public int RULE_ID { get; set; }
        public int HOST_ID { get; set; }
        public int S_STATUS { get; set; }
        public int PORT { get; set; }
        public string S_STATUS_MESSAGE { get; set; }
        public string DNS_NAME { get; set; }
        public string IP { get; set; }
        public long S_TIMEOUT { get; set; }
        public DateTime LASTCHECKED { get; set; }
    }

    public class ParallelDriveInfo : ResourceResponce
    {

        
        public DateTime LASTCHECKED { get; set; }
    }

    public class ParallelInfo : ResourceResponce
    {
        public DateTime LASTCHECKED { get; set; }
    }

    public class ParallelOperatingSystemInfo : ResourceResponce
    {
        
        //public string OperatingSystemName { get; set; }
    }
    
    public class ParallelPing
    {

        public int HOST_ID { get; set; }
        public int STATUS { get; set; }
        public string STATUS_MESSAGE { get; set; }
        public string DNS_NAME { get; set; }
        public string IP { get; set; }
        public long TIMEOUT { get; set; }
        public DateTime LASTCHECKED { get; set; }
    }

    class LpcEqualityComprer : IEqualityComparer<ParallelConnect>
    {
        public bool Equals(ParallelConnect x, ParallelConnect y)
        {
            if (Object.ReferenceEquals(x, y)) return true;
            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;
            return x.IP == y.IP
                   && x.PORT == y.PORT;
        }

        public int GetHashCode(ParallelConnect pc)
        {
            if (Object.ReferenceEquals(pc, null)) return 0;
            int hashIP = pc.IP == null ? 0 : pc.IP.GetHashCode();
            int hashPORT = pc.PORT == null ? 0 : pc.PORT.GetHashCode();
            return hashPORT ^ hashIP;
        }
    }
}

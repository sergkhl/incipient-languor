using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetInfoCollect
{
    

    abstract class HostInfo
    {        
        private int? _value_int;
        public virtual int? value_int { get { return _value_int; } set { _value_int = value; } }
        private int? _value_int_total;
        public virtual int? value_int_total { get { return _value_int_total; } set { _value_int_total = value; } }
        private int? _timeout;
        public virtual int? timeout { get { return _timeout; } set { _timeout = value; } }
        private int? _status;
        public virtual int? status { get { return _status; } set { _status = value; } }
        public virtual bool isdmz { get { return false; } }
        private string _value_text;
        public virtual string value_text { get { return _value_text; } set { _value_text = value; } }
        private string _status_message;
        public virtual string status_message { get { return _status_message; } set { _status_message = value; } }
        public virtual string DNS_NAME { get { return null; } }
        private string _ip;
        public virtual string IP { get { return _ip; } set { _ip = value; } }
        public virtual int? HOST_ID { get { return null; } }
        public virtual int? NODE_ID { get { return null; } }
        public virtual int? RULE_ID { get { return null; } }
        public virtual int? SERVICE_ID { get { return null; } }
        public virtual string SERVICE_NAME { get { return null; } }
        public virtual int PORT { get { return -1; } }
        public abstract void GetInfo(HostInfo hi);
    }

    abstract class HostInfoCreator
    {        
        
        public abstract List<HostInfo> GetListHostInfo();
        public abstract HostInfo CreateHostInfo(string TYPE);        
        public abstract int TYPE_ID { get; }
        public abstract string TYPE { get; }
        
    }

    class ParallelHostInfo : HostInfo
    {
        string _ip;
        string _dns_name;
        bool _isdmz;
        int? _host_id;
        public override void GetInfo(HostInfo hi)
        {

        }
        public ParallelHostInfo(string IP, string DNS_NAME, int? HOST_ID)
        {
            _ip = IP;
            _dns_name = DNS_NAME;
            _isdmz = _dns_name.ToLower().Contains("-dmz-");
            _host_id = HOST_ID;
        }
        public override string IP { get { return _ip; } }
        public override string DNS_NAME { get { return _dns_name; } }
        public override bool isdmz { get { return _isdmz; } }
        public override int? HOST_ID { get { return _host_id; } }
    }

    class WMIHostInfo : HostInfoCreator
    {
        string _type;
        int _type_id;

        public WMIHostInfo(string args)
        {
            TSDBEntities db = new TSDBEntities();
            _type = args;
            _type_id = db.MONITORING_TYPES.Where(mt => mt.NAME == args).FirstOrDefault().TYPE_ID;
        }

        public override int TYPE_ID { get { return _type_id; } }
        public override string TYPE { get { return _type; } }

        public override List<HostInfo> GetListHostInfo()
        {            
            HostInfo chi = CreateHostInfo(TYPE);
            UpdateMonitoring();
            List<HostInfo> lhi = GetLhi(TYPE_ID);
            Parallel.ForEach(lhi, hi =>
            {
                chi.GetInfo(hi);
            });

            TSDBEntities db = new TSDBEntities();            
            foreach (HostInfo hi in lhi)
            {    
                var mon = db.MONITORING.Where(m => m.HOST_ID == hi.HOST_ID & m.TYPE_ID == TYPE_ID).FirstOrDefault();
                if (mon == null)
                {
                    mon = new MONITORING();
                    db.MONITORING.Add(mon);
                    db.SaveChanges();                    
                    mon.HOST_ID = hi.HOST_ID;
                    mon.TYPE_ID = TYPE_ID;
                    mon.USER_ID = 0;
                    mon.VALUE_INT_ALERT = 2;
                    mon.ENABLED = true;
                }
                mon.STATUS = hi.status;
                mon.VALUE_TEXT = hi.value_text;
                mon.VALUE_INT_TOTAL = hi.value_int_total;
                mon.VALUE_INT = hi.value_int;
                mon.LASTCHECKED = DateTime.Now;
                mon.STATUS_MESSAGE = hi.status_message;
                mon.TIMEOUT = hi.timeout;
            }
            db.SaveChanges();

            return lhi;
        }

        public List<HostInfo> GetLhi(int TYPE_ID, string TYPE = "N", int STATUS = 2)
        {
            TSDBEntities db = new TSDBEntities();
            List<HOSTS> hosts = new List<HOSTS>();
            var mon = db.MONITORING.Where(m => m.HOSTS1.TYPE == TYPE & m.HOSTS1.MONITORING.STATUS < STATUS & m.TYPE_ID == TYPE_ID & m.ENABLED == true);
            foreach (var m in mon)
            {
                hosts.Add(m.HOSTS1);
            }
            hosts = hosts.Distinct().ToList();
            List<HostInfo> lpi = new List<HostInfo>();
            foreach (HOSTS host in hosts)
            {                
                ParallelHostInfo pi = new ParallelHostInfo(host.IP, host.DNS_NAME, host.HOST_ID);
                lpi.Add(pi);
            }
            return lpi;
        }
        public void UpdateMonitoring()
        {
            TSDBEntities db = new TSDBEntities();
            var allhosts = db.HOSTS.Where(h => h.MONITORING.STATUS < 2 & h.TYPE == "N" & h.MONITORING.ENABLED == true);

            foreach (var h in allhosts)
            {
                foreach (var t in db.MONITORING_TYPES.Where(mt => mt.DATA_TYPE_ID == 1 | mt.DATA_TYPE_ID == 2))
                {
                    var monh = db.MONITORING.Where(m => m.HOST_ID == h.HOST_ID & m.TYPE_ID == t.TYPE_ID).FirstOrDefault();
                    if (monh == null)
                    {
                        monh = new MONITORING
                        {
                            TYPE_ID = t.TYPE_ID,
                            HOST_ID = h.HOST_ID,
                            ENABLED = true,
                        };
                        db.MONITORING.Add(monh);
                    }
                }
            }
            db.SaveChanges();
        }
        public override HostInfo CreateHostInfo(string TYPE)
        {
            HostInfo hi = null;
            if (TYPE == "OS")
            {
                hi = new WMIOSInfo();
            }
            else if (TYPE == "DriveSpace")
            {
                hi = new WMIDriveSpaceInfo();
            }
            return hi;
        }
    }

    class WMIOSInfo : HostInfo
    {   
        public override void GetInfo(HostInfo hi)
        {            
            try     
            {
                ConnectionOptions oConn = new ConnectionOptions();
                if (hi.isdmz)
                {
                    oConn.Username = "****";
                    oConn.Password = "****";
                }
                string strNameSpace = @"\\";                
                strNameSpace += hi.IP;                
                strNameSpace += @"\root\cimv2";
                ManagementScope oMs = new ManagementScope(strNameSpace, oConn);
                ObjectQuery oQuery = new ObjectQuery("SELECT Caption,Version FROM Win32_OperatingSystem");
                ManagementObjectSearcher oSearcher = new ManagementObjectSearcher(oMs, oQuery);
                ManagementObjectCollection oReturnCollection = oSearcher.Get();
                var queryCollection = from ManagementObject x in oReturnCollection select x;
                var oReturn = queryCollection.FirstOrDefault();
                hi.value_text = oReturn["Caption"].ToString() + " " + oReturn["Version"].ToString();
            }
            catch
            {
                hi.status_message = string.Format("{0} Failed to obtain Server Information. The node you are trying to scan can be a Filer or a node which you don't have administrative priviledges. Please use the UNC convention to scan the shared folder in the server", hi.IP);                
            }             
            //return hi;
        }                           
    }    

    class WMIDriveSpaceInfo : HostInfo
    {
        public override void GetInfo(HostInfo hi)
        {            
            try
            {
                ConnectionOptions oConn = new ConnectionOptions();
                if (hi.isdmz)
                {
                    oConn.Username = "****";
                    oConn.Password = "****";
                }
                string strNameSpace = @"\\";                
                    strNameSpace += hi.IP;               
                strNameSpace += @"\root\cimv2";
                ManagementScope oMs = new ManagementScope(strNameSpace, oConn);
                //get Fixed disk state
                ObjectQuery oQuery = new ObjectQuery("select FreeSpace,Size,Name from Win32_LogicalDisk where DriveType=3");
                //Execute the query
                ManagementObjectSearcher oSearcher = new ManagementObjectSearcher(oMs, oQuery);
                //Get the results
                ManagementObjectCollection oReturnCollection = oSearcher.Get();
                //loop through found drives and write out info
                double D_Freespace = 0;
                double D_Totalspace = 0;
                //var oReturnq = oReturnCollection.GetEnumerator().MoveNext();
                var queryCollection = from ManagementObject x in oReturnCollection select x;
                var oReturn = queryCollection.FirstOrDefault();
                string strFreespace = oReturn["FreeSpace"].ToString();
                D_Freespace = D_Freespace + System.Convert.ToDouble(strFreespace) / 1024 / 1024;
                // Size
                string strTotalspace = oReturn["Size"].ToString();
                D_Totalspace = D_Totalspace + System.Convert.ToDouble(strTotalspace) / 1024 / 1024;
                hi.value_int = Convert.ToInt32(D_Freespace);
                hi.value_int_total = Convert.ToInt32(D_Totalspace);
                hi.status = 0;
                //Console.WriteLine("{0} {1} {2}/{3}", IP, oReturn["Name"].ToString(), Convert.ToInt32(D_Freespace), Convert.ToInt32(D_Totalspace));                
            }
            catch
            {
                hi.status_message = string.Format("{0} Failed to obtain Server Information. The node you are trying to scan can be a Filer or a node which you don't have administrative priviledges. Please use the UNC convention to scan the shared folder in the server", hi.IP);
                hi.status = 4;
                //Console.WriteLine("{0} Failed to obtain Server Information. The node you are trying to scan can be a Filer or a node which you don't have administrative priviledges. Please use the UNC convention to scan the shared folder in the server", IP);
            }
            //return hi;
        }
    }

    class ServicesInfoParallel : HostInfo
    {
        string _ip;
        string _dns_name;
        bool _isdmz;
        int? _host_id;
        int? _node_id;
        int? _service_id;
        string _service_name;
        public override void GetInfo(HostInfo hi)
        {

        }
        public ServicesInfoParallel(string IP, string DNS_NAME, int? HOST_ID, int? NODE_ID, int? SERVICE_ID, string SERVICE_NAME)
        {
            _ip = IP;
            _dns_name = DNS_NAME;
            _isdmz = _dns_name.ToLower().Contains("-dmz-");
            _host_id = HOST_ID;
            _node_id = NODE_ID;
            _service_id = SERVICE_ID;
            _service_name = SERVICE_NAME;
        }
        public override string IP { get { return _ip; } }
        public override string DNS_NAME { get { return _dns_name; } }
        public override bool isdmz { get { return _isdmz; } }
        public override int? HOST_ID { get { return _host_id; } }
        public override int? NODE_ID { get { return _node_id; } }
        public override int? SERVICE_ID { get { return _service_id; } }
        public override string SERVICE_NAME { get { return _service_name; } }
    }

    class ServicesHostInfo : HostInfoCreator
    {
        string _type;
        int _type_id;

        public ServicesHostInfo(string args)
        {
            TSDBEntities db = new TSDBEntities();
            _type = args;
            _type_id = db.MONITORING_TYPES.Where(mt => mt.NAME == args).FirstOrDefault().TYPE_ID;
        }

        public override int TYPE_ID { get { return _type_id; } }
        public override string TYPE { get { return _type; } }

        public override List<HostInfo> GetListHostInfo()
        {
            HostInfo chi = CreateHostInfo(TYPE);
            List<HostInfo> lhi = GetLhi(TYPE_ID);
            Parallel.ForEach(lhi, hi =>
            {
                chi.GetInfo(hi);
            });

            TSDBEntities db = new TSDBEntities();            
            foreach (HostInfo hi in lhi)
            {                
                MONITORING mon = db.MONITORING.Where(m => m.HOST_ID == hi.HOST_ID & m.TYPE_ID == TYPE_ID & m.SERVICE_ID == hi.SERVICE_ID).FirstOrDefault();
                if (mon == null)
                {
                    mon = new MONITORING
                    {
                        SERVICE_ID = hi.SERVICE_ID,
                        HOST_ID = hi.HOST_ID,
                        TYPE_ID = TYPE_ID,
                        USER_ID = 0,
                        VALUE_INT_ALERT = 2,
                        ENABLED = true
                    };
                    db.MONITORING.Add(mon);
                    db.SaveChanges();
                    if (hi.NODE_ID == hi.HOST_ID)
                    {
                        SERVICES service = db.SERVICES.Find(hi.SERVICE_ID);
                        service.MONITORING_ID = mon.MONITORING_ID;
                    }
                }
                mon.VALUE_TEXT = hi.value_text;
                mon.STATUS = hi.status;
                mon.STATUS_MESSAGE = hi.status_message;
                mon.TIMEOUT = hi.timeout;
                mon.LASTCHECKED = DateTime.Now;
            }
            try
            {
                db.SaveChanges();
            }
            catch (Exception ex) { Console.WriteLine(ex.InnerException); }
            return lhi;
        }

        public List<HostInfo> GetLhi(int TYPE_ID)
        {
            TSDBEntities db = new TSDBEntities();

            var services = db.SERVICES.Where(r => r.MONITORING == null | r.MONITORING.ENABLED == true);

            List<HostInfo> lhi = new List<HostInfo>();
            foreach (SERVICES service in services)
            {
                var Hosts = db.HOSTS.Where(h => (h.NODE_ID == service.HOSTS.HOST_ID & h.TYPE == "N") & (h.MONITORING.STATUS < 2 & h.MONITORING.ENABLED == true));
                foreach (HOSTS h in Hosts)
                {
                    ServicesInfoParallel sip = new ServicesInfoParallel(h.IP, h.DNS_NAME.ToString(), h.HOST_ID, h.NODE_ID, service.SERVICE_ID, service.SERVICE_NAME);
                    lhi.Add(sip);
                }
            }
            lhi = lhi.Distinct(new LhiEqualityComprer()).ToList();
            return lhi;
        }

        public override HostInfo CreateHostInfo(string TYPE)
        {
            HostInfo hi = null;
            if (TYPE == "Services")
            {
                hi = new ServicesInfo();
            }
            return hi;
        }
    }    

    class ServicesInfo : HostInfo
    {
        public override void GetInfo(HostInfo hi)
        {
            //try
            //{
                ConnectionOptions oConn = new ConnectionOptions();
                if (hi.isdmz)
                {
                    oConn.Username = "****";
                    oConn.Password = "****";
                }                
                string strNameSpace = @"\\";
                strNameSpace += hi.IP;
                strNameSpace += @"\root\cimv2";
                ManagementScope oMs = new ManagementScope(strNameSpace, oConn);
                ObjectQuery oQuery = new ObjectQuery("select * from Win32_Service Where Name ='" + hi.SERVICE_NAME + "'");                
                try
                {
                    ManagementObjectCollection oReturnCollection;
                    ManagementObjectSearcher oSearcher = new ManagementObjectSearcher(oMs, oQuery);
                    oReturnCollection = oSearcher.Get();
                    var queryCollection = from ManagementObject x in oReturnCollection select x;
                    var oReturn = queryCollection.FirstOrDefault();
                    hi.value_text = oReturn["State"].ToString();
                }
                catch (Exception ex)
                {
                    //System.Windows.Forms.MessageBox.Show(ex.Message);
                    hi.status_message = ex.Message;
                }
           // }
           // catch
           // {
            //    hi.status_message = string.Format("{0} Failed to obtain Server Information. The node you are trying to scan can be a Filer or a node which you don't have administrative priviledges. Please use the UNC convention to scan the shared folder in the server", hi.IP);
           // }
        }        
    }

    class RulesInfoParallel : HostInfo
    {
        string _ip;
        string _dns_name;
        bool _isdmz;
        int? _host_id;
        int? _node_id;
        int? _rule_id;
        int _port;
        public override void GetInfo(HostInfo hi)
        {

        }
        public RulesInfoParallel(string IP, string DNS_NAME, int? HOST_ID, int? NODE_ID, int? RULE_ID, int PORT)
        {
            _ip = IP;
            _dns_name = DNS_NAME;
            _isdmz = _dns_name.ToLower().Contains("-dmz-");
            _host_id = HOST_ID;
            _node_id = NODE_ID;
            _rule_id = RULE_ID;
            _port = PORT;
        }
        public override string IP { get { return _ip; } }
        public override string DNS_NAME { get { return _dns_name; } }
        public override bool isdmz { get { return _isdmz; } }
        public override int? HOST_ID { get { return _host_id; } }
        public override int? NODE_ID { get { return _node_id; } }
        public override int? RULE_ID { get { return _rule_id; } }
        public override int PORT { get { return _port; } }
    }

    class RulesHostInfo : HostInfoCreator
    {
        string _type;
        int _type_id;

        public RulesHostInfo(string args)
        {
            TSDBEntities db = new TSDBEntities();
            _type = args;
            _type_id = db.MONITORING_TYPES.Where(mt => mt.NAME == args).FirstOrDefault().TYPE_ID;
        }

        public override int TYPE_ID { get { return _type_id; } }
        public override string TYPE { get { return _type; } }

        public override List<HostInfo> GetListHostInfo()
        {
            HostInfo chi = CreateHostInfo(TYPE);
            List<HostInfo> lhi = GetLhi(TYPE_ID);
            Parallel.ForEach(lhi, hi =>
            {
                chi.GetInfo(hi);
            });

            TSDBEntities db = new TSDBEntities();
            //lhi = lhi.OrderBy(l => l.HOST_ID).ToList();
            foreach (HostInfo hi in lhi)
            {                
                //MONITORING mon = rule.MONITORING;
                MONITORING mon = db.MONITORING.Where(m => m.HOST_ID == hi.HOST_ID & m.TYPE_ID == TYPE_ID & m.RULE_ID == hi.RULE_ID).FirstOrDefault();
                if (mon == null)
                {
                    mon = new MONITORING
                        {
                            RULE_ID = hi.RULE_ID,
                            HOST_ID = hi.HOST_ID,
                            TYPE_ID = TYPE_ID,
                            USER_ID = 0,
                            VALUE_INT_ALERT = 2,
                            ENABLED = true
                        };
                    db.MONITORING.Add(mon);
                    db.SaveChanges();
                    if (hi.NODE_ID == hi.HOST_ID)
                    {
                        RULES rule = db.RULES.Find(hi.RULE_ID);
                        rule.MONITORING_ID = mon.MONITORING_ID;
                    }
                }
                mon.STATUS = hi.status;
                mon.STATUS_MESSAGE = hi.status_message;
                mon.TIMEOUT = hi.timeout;
                mon.LASTCHECKED = DateTime.Now;
            }            
            try
            {
                db.SaveChanges();
            }
            catch (Exception ex) { Console.WriteLine(ex.InnerException); }
            return lhi;
        }

        public List<HostInfo> GetLhi(int TYPE_ID)
        {
            TSDBEntities db = new TSDBEntities();

            var rules = db.RULES.Where(r => r.HOSTS.TYPE == "C" && (r.MONITORING == null | r.MONITORING.ENABLED == true));

            List<HostInfo> lhi = new List<HostInfo>();
            foreach (RULES rule in rules)
            {
                var Hosts = db.HOSTS.Where(h => ((h.NODE_ID == rule.HOSTS.HOST_ID & h.TYPE == "N") || h.HOST_ID == rule.HOSTS.HOST_ID) & (h.MONITORING.STATUS < 2 & h.MONITORING.ENABLED == true));
                foreach (HOSTS h in Hosts)
                {
                    RulesInfoParallel rip = new RulesInfoParallel(h.IP, h.DNS_NAME.ToString(), h.HOST_ID, h.NODE_ID, rule.RULE_ID, rule.PORT.Value);
                    lhi.Add(rip);
                }
            }            
            lhi = lhi.Distinct(new LhiEqualityComprer()).ToList();
            return lhi;
        }

        public override HostInfo CreateHostInfo(string TYPE)
        {
            HostInfo hi = null;
            if (TYPE == "SocketAvailability")
            {
                hi = new RulesInfo();
            }
            
            return hi;
        }
    }    

    class RulesInfo : HostInfo
    {
        public override void GetInfo(HostInfo hi)
        {            
            hi.status = 0; //Successfully connected       
            int timeout = 500; //Socket.BeginConnect timeout

            IPAddress[] IPs = null;
            if (hi.IP != null & hi.IP != "")
            {
                IPs = new IPAddress[1];
                IPs[0] = IPAddress.Parse(hi.IP);
            }

            if (IPs != null)
            {
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //allDone.Reset();
                //Console.WriteLine("{1}: Establishing Connection to {0}:{2}", IP, DateTime.Now, port);
                Stopwatch timer = new Stopwatch();
                timer.Start();
                hi.IP = IPs.First().ToString();
                //s.BeginConnect(IPs, port, new AsyncCallback(ConnectCallback1), s);
                //allDone.WaitOne(1500);
                IAsyncResult result = s.BeginConnect(IPs, hi.PORT, null, null);
                result.AsyncWaitHandle.WaitOne(timeout, true);
                timer.Stop();
                hi.timeout = Convert.ToInt32(timer.ElapsedMilliseconds);
                //hi.timeout = int.Parse(timer.ElapsedMilliseconds.ToString());
                if (!s.Connected)
                {
                    hi.status += 2;
                    s.Close();
                    hi.status_message += string.Format("Socket unavailable {0}:{1}.", hi.IP, hi.PORT);                    
                }
                else
                {
                    hi.status_message = string.Format("Connection established {0}:{1}", hi.IP, hi.PORT);                    
                    //s.Shutdown(SocketShutdown.Both);
                    s.EndConnect(result);
                }
            }
            else
            {
                hi.status_message += string.Format("No IP in database. Unable to connect {0}:{1}.", hi.IP, hi.PORT);
                hi.status = 4;
            }                        
        }
    }
        
    class TasksInfo : HostInfo
    {
        public override void GetInfo(HostInfo hi)
        {
            try
            {
                ConnectionOptions oConn = new ConnectionOptions();
                if (hi.isdmz)
                {
                    oConn.Username = "****";
                    oConn.Password = "****";
                }
                string strNameSpace = @"\\";
                strNameSpace += hi.IP;
                strNameSpace += @"\root\cimv2";
                ManagementScope oMs = new ManagementScope(strNameSpace, oConn);
                ObjectQuery oQuery = new ObjectQuery("SELECT Caption,Version FROM Win32_OperatingSystem");
                ManagementObjectSearcher oSearcher = new ManagementObjectSearcher(oMs, oQuery);
                ManagementObjectCollection oReturnCollection = oSearcher.Get();
                var queryCollection = from ManagementObject x in oReturnCollection select x;
                var oReturn = queryCollection.FirstOrDefault();
                hi.value_text = oReturn["Caption"].ToString() + " " + oReturn["Version"].ToString();
            }
            catch
            {
                hi.status_message = string.Format("{0} Failed to obtain Server Information. The node you are trying to scan can be a Filer or a node which you don't have administrative priviledges. Please use the UNC convention to scan the shared folder in the server", hi.IP);
            }
            //return hi;
        }
    }

    class HostInfoAssembler
    {
        public void AssembleHostInfo(HostInfoCreator hic)
        {            
            List<HostInfo> lhi = hic.GetListHostInfo();
            //int _dtype_id = db.MONITORING_TYPES.Where(mt => mt.NAME == hic.TYPE).FirstOrDefault().DATA_TYPE_ID;
            
            Console.WriteLine("assembled a {0}", hic.GetType().FullName);
        }
        
    }

    class LhiEqualityComprer : IEqualityComparer<HostInfo>
    {
        public bool Equals(HostInfo x, HostInfo y)
        {
            if (Object.ReferenceEquals(x, y)) return true;
            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;
            return x.IP == y.IP
                   && x.PORT == y.PORT 
                   && x.SERVICE_NAME == y.SERVICE_NAME;
        }

        public int GetHashCode(HostInfo hi)
        {
            if (Object.ReferenceEquals(hi, null)) return 0;
            int hashIP = hi.IP == null ? 0 : hi.IP.GetHashCode();
            //int hashPORT = hi.PORT == null ? 0 : hi.PORT.GetHashCode();
            int hashPORT = hi.PORT.GetHashCode();
            int hashSN = hi.SERVICE_NAME == null ? 0 : hi.SERVICE_NAME.GetHashCode();
            return hashPORT ^ hashIP ^ hashSN;
        }
    }
}

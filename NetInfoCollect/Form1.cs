using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace NetInfoCollect
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            int i = 0;
            while (true)
            {
                i++;
                Console.WriteLine("{0} START ITERATION {1}", DateTime.Now, i);
                //Ping & scan ports
                //PingAndScan();
                //GET RESOURCES INFO
                //GetResourceInfo();
                //GetOperatingSystemInfo();
                //SEND EMAIL
                //SendEmail();
                GetRes("Services");
                //GetRes("SocketAvailability");
                //GetRes("OS");
                //GetRes("DriveSpace");

                Console.WriteLine("{0} ITERATION {1} ENDED. Going to sleep", DateTime.Now, i);
                Thread.Sleep(900000);
            }
        }

        public static HostResponse BeginConnect2(int port, string IP, int timeout = 1000)
        { 
            HostResponse HR = new HostResponse();
            HR.Status = 0; //Successfully connected           

            IPAddress[] IPs = null;            
            if (IP != null & IP != "")
            {
                IPs = new IPAddress[1];
                IPs[0] = IPAddress.Parse(IP);
            }
            
            if (IPs != null)
            {
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //allDone.Reset();
                //Console.WriteLine("{1}: Establishing Connection to {0}:{2}", IP, DateTime.Now, port);
                Stopwatch timer = new Stopwatch();
                timer.Start();
                HR.IP = IPs.First().ToString();
                //s.BeginConnect(IPs, port, new AsyncCallback(ConnectCallback1), s);
                //allDone.WaitOne(1500);
                IAsyncResult result = s.BeginConnect(IPs, port, null, null);
                result.AsyncWaitHandle.WaitOne(timeout, true);
                timer.Stop();
                HR.Timeout = timer.ElapsedMilliseconds;
                if (!s.Connected)
                {
                    HR.Status += 2;
                    s.Close();
                    HR.StatusMessage += string.Format("Socket unavailable {0}:{1}.", IP, port);
                    //Console.WriteLine("{0}: Socket unavailable {1}:{2}", DateTime.Now, IP, port);
                }
                else
                {
                    HR.StatusMessage = string.Format("Connection established {0}:{1}", IP, port);
                    //Console.WriteLine("{0}: Connection established {1}:{2}", DateTime.Now, IP, port);
                    //s.Shutdown(SocketShutdown.Both);
                    s.EndConnect(result);
                }
            }
            else
            {
                HR.StatusMessage += string.Format("No IP in database. Unable to connect {0}:{1}.", IP, port);
                HR.Status = 4;
            }
            return HR;
        }       

        public static HostResponse BeginPing(string host, string IP)
        {
            HostResponse HR = new HostResponse();
            HR.Status = 0; //Successfully pinged           

            IPAddress[] IPs = null;

            try
            {
                IPs = Dns.GetHostAddresses(host);
            }
            catch (Exception ex)
            {
                HR.Status = 1; //Failed to resolve IP
                HR.StatusMessage = string.Format("{0}\r\n", ex.Message);
                //Console.WriteLine("{1}: {2} - {0}", ex.Message, DateTime.Now, host);
                if (IP != null & IP != "")
                {
                    IPs = new IPAddress[1];
                    IPs[0] = IPAddress.Parse(IP);
                }
            }
            if (IPs != null)
            {
                //Console.WriteLine("{1}: Trying to ping {0}", host, DateTime.Now);
                HR.IP = IPs.First().ToString();
                Ping pingSender = new Ping();
                //pingSender.PingCompleted += new PingCompletedEventHandler(AsyncPingCompleted);
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 200;

                // Set options for transmission: 
                // The data can go through 64 gateways or routers 
                // before it is destroyed, and the data packet 
                // cannot be fragmented.
                PingOptions options = new PingOptions(64, true);

                // Send the ping asynchronously. 
                // Use the waiter as the user token. 
                // When the callback completes, it can wake up this thread.
                var reply = pingSender.Send(IPs[0], timeout, buffer, options);                
               
                // Prevent this example application from ending. 
                // A real application should do something useful 
                // when possible.
                //waiter.WaitOne();
                if (reply.Status != IPStatus.Success)
                {
                    HR.Status += 2;
                    HR.StatusMessage += string.Format("Host unavailable {0}.", host);
                    //Console.WriteLine("{0}: Host unavailable {1}", DateTime.Now, host);
                }
                else
                {                    
                    HR.StatusMessage = string.Format("Ping successfull {0}", host);
                    //Console.WriteLine("{0}: Ping successfull {1}", DateTime.Now, host);                    
                }
                HR.Timeout = reply.RoundtripTime;
            }
            else
            {
                HR.StatusMessage += string.Format("No IP in database. Unable to ping {0}.", host);
                HR.Status = 4;
            }

            return HR;
        }

        public static ManualResetEvent allDone = new ManualResetEvent(false);

        // handles the completion of the prior asynchronous 
        // connect call. the socket is passed via the objectState 
        // paramater of BeginConnect().
        public static void ConnectCallback1(IAsyncResult ar)
        {
            allDone.Set();
            try
            {
                Socket s = (Socket)ar.AsyncState;
                s.EndConnect(ar);
            }
            catch (Exception ex) { Console.WriteLine("Connection FAILED. Exception: {0}",ex.Message); }
            
        }

        private void PingAndScan()
        {
            TSDBEntities db = new TSDBEntities();            
            var hosts = db.HOSTS;
            List<ParallelPing> lpp = new List<ParallelPing>();
            foreach (HOSTS host in hosts.Where(h => (h.MONITORING == null) || (h.MONITORING.ENABLED == true)))
            {
                ParallelPing p_ping = new ParallelPing
                {
                    HOST_ID = host.HOST_ID,
                    DNS_NAME = host.DNS_NAME.ToString(),
                    IP = host.IP,
                    LASTCHECKED = DateTime.Now
                };
                lpp.Add(p_ping);
            }

            Parallel.ForEach(lpp, pp =>
            {
                var hr = BeginPing(pp.DNS_NAME, pp.IP);
                if (hr != null)
                {
                    pp.STATUS = hr.Status;
                    pp.STATUS_MESSAGE = hr.StatusMessage;
                    if (hr.Status < 4)
                    {
                        pp.IP = hr.IP;
                        pp.TIMEOUT = int.Parse(hr.Timeout.ToString());
                    }
                }
            });

            foreach (ParallelPing pp in lpp)
            {
                HOSTS host = hosts.Find(pp.HOST_ID);
                bool isNewMon = false;
                MONITORING mon = host.MONITORING;
                if (mon == null)
                {
                    mon = new MONITORING();
                    db.MONITORING.Add(mon);
                    db.SaveChanges();
                    isNewMon = true;
                }

                if (!(isNewMon) && (mon.STATUS == pp.STATUS) && (mon.STATUS_MESSAGE == pp.STATUS_MESSAGE) && (host.IP == pp.IP))
                {
                    
                }
                else
                {
                    mon.STATUS = pp.STATUS;
                    mon.HOST_ID = host.HOST_ID;
                    mon.STATUS_MESSAGE = pp.STATUS_MESSAGE;
                    mon.LASTUPDATED = DateTime.Now;
                    if (pp.STATUS < 4)
                    {
                        if (host.IP != pp.IP)
                        {
                            host.IP = pp.IP;
                            host.LASTUPDATED = DateTime.Now;
                        }
                        mon.TIMEOUT = int.Parse(pp.TIMEOUT.ToString());
                    }
                }
                mon.LASTCHECKED = DateTime.Now;
                //host.LASTCHECKED = DateTime.Now;
                mon.TYPE_ID = 0;
                mon.USER_ID = 0;
                //host.USER_ID = 0;

                if (isNewMon) { mon.VALUE_INT_ALERT = 2; host.MONITORING_ID = mon.MONITORING_ID; mon.ENABLED = true; }
            }
            try
            {
                db.SaveChanges();
            }
            catch (Exception ex) { Console.WriteLine(ex.InnerException); }
            Console.WriteLine("{0} Hosts successfully pinged", DateTime.Now);


            var rules = db.RULES.Where(r => r.HOSTS.TYPE == "C" && (r.MONITORING == null | r.MONITORING.ENABLED == true));

            List<ParallelConnect> lpc = new List<ParallelConnect>();
            foreach (RULES rule in rules)
            {
                var Hosts = db.HOSTS.Where(h => ((h.NODE_ID == rule.HOSTS.HOST_ID & h.TYPE == "N") || h.HOST_ID == rule.HOSTS.HOST_ID) & (h.MONITORING.STATUS < 2 & h.MONITORING.ENABLED == true));
                foreach (HOSTS h in Hosts)
                {
                    ParallelConnect p_connect = new ParallelConnect
                    {
                        RULE_ID = rule.RULE_ID,
                        HOST_ID = h.HOST_ID,
                        DNS_NAME = h.DNS_NAME.ToString(),
                        PORT = rule.PORT.Value,
                        IP = h.IP,

                        LASTCHECKED = DateTime.Now
                    };
                    lpc.Add(p_connect);
                }
            }

            var dlpc = lpc.Distinct(new LpcEqualityComprer()).ToList();
            Parallel.ForEach(lpc, pc =>
            {
                var hr = BeginConnect2(pc.PORT, pc.IP);
                if (hr != null)
                {
                    pc.S_STATUS = hr.Status;
                    pc.S_STATUS_MESSAGE = hr.StatusMessage;
                    if (hr.Status < 4)
                    {
                        pc.S_TIMEOUT = int.Parse(hr.Timeout.ToString());
                    }
                }
            });

            foreach (ParallelConnect pc in lpc)
            {
                RULES rule = db.RULES.Find(pc.RULE_ID);
                bool isNewMon = false;
                MONITORING mon = rule.MONITORING;
                if (mon == null)
                {
                    mon = new MONITORING();
                    db.MONITORING.Add(mon);
                    db.SaveChanges();
                    isNewMon = true;
                }
                if (!(isNewMon) && (rule.MONITORING.STATUS == pc.S_STATUS) && (rule.MONITORING.STATUS_MESSAGE == pc.S_STATUS_MESSAGE) && (rule.HOST_ID == pc.HOST_ID))
                {

                }
                else
                {
                    mon.STATUS = pc.S_STATUS;
                    mon.STATUS_MESSAGE = pc.S_STATUS_MESSAGE;
                    mon.HOST_ID = pc.HOST_ID;
                    //rule.RULE_ID = pc.RULE_ID;                    
                    //rule.LASTUPDATED = DateTime.Now;
                    if (pc.S_STATUS < 4)
                    {
                        mon.TIMEOUT = int.Parse(pc.S_TIMEOUT.ToString());
                    }
                }
                mon.LASTCHECKED = DateTime.Now;
                mon.TYPE_ID = 1;
                mon.USER_ID = 0;
                //rule.LASTCHECKED = DateTime.Now;
                //rule.USER_ID = 0;

                if (isNewMon) { mon.VALUE_INT_ALERT = 2; rule.MONITORING_ID = mon.MONITORING_ID; mon.ENABLED = true; }
            }
            try
            {
                db.SaveChanges();
            }
            catch (Exception ex) { Console.WriteLine(ex.InnerException); }
            Console.WriteLine("{0} Ports successfully checked", DateTime.Now);
        }        


        /// <summary>
        /// Получение информации хостов, используя паттерн Factory Method
        /// </summary>
        /// <param name="args">
        /// Аргумент содержит название ресурса
        /// </param>
        private void GetRes(string args)
        {                     
            HostInfoCreator hic = null;
            if (args == "SocketAvailability")
            {
                hic = new RulesHostInfo(args);
            }
            else if (args == "OS" | args == "DriveSpace")
            {
                hic = new WMIHostInfo(args);              
            }
            else if (args == "Services")
            {
                hic = new ServicesHostInfo(args);
            }

            new HostInfoAssembler().AssembleHostInfo(hic);
        }

        private void SendEmail()
        {
            TSDBEntities db = new TSDBEntities();
            string body = null;
            List<HOSTS> hosts = new List<HOSTS>();
            var mon = db.MONITORING.Where(m => (m.VALUE_INT <= m.VALUE_INT_ALERT | m.STATUS >= m.VALUE_INT_ALERT) & m.ENABLED == true);
            foreach (var m in mon)
            {
                hosts.Add(m.HOSTS1);
            }
            hosts = hosts.Distinct().ToList();
            mon = mon.OrderBy(m => m.HOSTS1.FARM1.FARM_NAME).ThenBy(m => m.HOSTS1.TS_ID).ThenBy(m => m.HOSTS1.DNS_NAME);

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            MemoryStream objMemoryStream = new MemoryStream();
            using (XmlWriter writer = XmlWriter.Create(objMemoryStream, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("FARMS");
                string previous = null;
                string current = null;
                string farmCurrent = null;
                string farmPrevious = null;
                foreach (MONITORING m in mon)
                {
                    current = m.HOSTS1.DNS_NAME;
                    farmCurrent = m.HOSTS1.FARM1.FARM_NAME;
                    if (previous != current)
                    {
                        if (previous != null)
                        {
                            writer.WriteEndElement();
                            writer.WriteEndElement();
                        }
                        if (farmPrevious != farmCurrent)
                        {
                            if (farmPrevious != null)
                            {
                                writer.WriteEndElement();
                                writer.WriteEndElement();
                            }
                            writer.WriteStartElement("FARM");
                            writer.WriteElementString("FARM_NAME", m.HOSTS1.FARM1.FARM_NAME);
                            writer.WriteStartElement("HOSTS");
                        }
                        writer.WriteStartElement("HOST");
                        writer.WriteElementString("DNS_NAME", m.HOSTS1.DNS_NAME);
                        writer.WriteElementString("IP", m.HOSTS1.IP);
                        writer.WriteStartElement("Alerts");
                    }
                    foreach (var mt in db.MONITORING_TYPES)
                    {
                        if (mt.TYPE_ID == m.TYPE_ID)
                        {
                            if (m.VALUE_INT <= m.VALUE_INT_ALERT)
                            {
                                writer.WriteElementString(mt.NAME, (m.STATUS_MESSAGE != null) ? m.STATUS_MESSAGE : m.VALUE_INT.ToString());
                            }
                            if (m.STATUS >= m.VALUE_INT_ALERT)
                            {
                                writer.WriteElementString(mt.NAME, m.STATUS_MESSAGE);
                            }
                        }
                    }
                    previous = current;
                    farmPrevious = farmCurrent;
                }
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
            objMemoryStream.Seek(0, SeekOrigin.Begin);
            //Create attachment
            ContentType contentType = new ContentType();
            contentType.MediaType = MediaTypeNames.Application.Octet;
            contentType.Name = "Monitoring.xls";
            Attachment attachment = new Attachment(objMemoryStream, contentType);

            if (hosts != null)
            {
                string subjList = null;
                foreach (var h in hosts.Take(4))
                {
                    subjList += h.DNS_NAME + ", ";
                }
                subjList = subjList.Remove(subjList.Length - 2, 2) + "...";
                var subj = string.Format("ALERT: {0}", subjList);
                /* body += "<table border=\"1\"><tr>";
                 body += "<thead><tr><th>DNS_NAME</th><th>IP</th>";
                 foreach (MONITORING_TYPES mt in db.MONITORING_TYPES)
                 {
                     body += "<th>" + mt.NAME + "<th>";
                 }
                 body += "</tr></thead>";
                 body += "<tbody>";
                 foreach (MONITORING m in mon)
                 {
                     body += "<tr>";
                     body += "<td>";
                     body += m.HOSTS1.DNS_NAME;
                     body += "</td>";
                     body += "<td>";
                     body += m.HOSTS1.IP;
                     body += "</td>";
                    
                     foreach (var mt in db.MONITORING_TYPES)
                     {
                         body += "<td>";
                         if (mt.TYPE_ID == m.TYPE_ID)
                         {
                             if (m.VALUE_INT <= m.VALUE_INT_ALERT) { body +=  (m.STATUS_MESSAGE != null) ? m.STATUS_MESSAGE : m.VALUE_INT.ToString(); }
                             if (m.STATUS >= m.VALUE_INT_ALERT) { body += m.STATUS_MESSAGE; }
                         }
                         body += "</td>";
                     }
                     body += "</tr>";
                 }
                 */

                body += "Настройки мониторинга и оповещений (доступно с ТС): <a href=\"http://10.10.10.1/passport/monitoring/alerts\">http://10.10.10.1/passport/monitoring/alerts</a>";

                CreateTestMessage2("10.10.10.1", body, subj, attachment);
                objMemoryStream.Dispose();
            }


        }

        public static void CreateTestMessage2(string server, string body, string subj, Attachment attachment)
        {            
            //string to = "list@abc.de";
            string to = "svkhlope@abc.de";
            string from = "MONITORING <svkhlope@abc.de>";            
            MailMessage message = new MailMessage(from, to);            
            message.Subject = subj;
            message.IsBodyHtml = true;
            message.Body = body;
            if(attachment != null){
            message.Attachments.Add(attachment);
            }
            SmtpClient client = new SmtpClient(server);         
            client.UseDefaultCredentials = true;

            try
            {
                client.Send(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in CreateTestMessage2(): {0}",
                      ex.ToString());
            }
        }
        
    }
}

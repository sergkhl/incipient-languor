//------------------------------------------------------------------------------
// <auto-generated>
//    Этот код был создан из шаблона.
//
//    Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//    Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace NetInfoCollect
{
    using System;
    using System.Collections.Generic;
    
    public partial class HOSTS
    {
        public HOSTS()
        {
            this.RULES = new HashSet<RULES>();
            this.FARM = new HashSet<FARM>();
            this.SOCKET_STATUS = new HashSet<SOCKET_STATUS>();
            this.HOSTS1 = new HashSet<HOSTS>();
            this.RESOURCES = new HashSet<RESOURCES>();
            this.MONITORING1 = new HashSet<MONITORING>();
            this.SERVICES = new HashSet<SERVICES>();
        }
    
        public int HOST_ID { get; set; }
        public Nullable<int> NODE_ID { get; set; }
        public string TYPE { get; set; }
        public string DNS_NAME { get; set; }
        public string IP { get; set; }
        public string DESCRIPTION { get; set; }
        public Nullable<int> TS_ID { get; set; }
        public Nullable<System.DateTime> LASTUPDATED { get; set; }
        public Nullable<int> USER_ID { get; set; }
        public Nullable<int> MONITORING_ID { get; set; }
        public int FARM_ID { get; set; }
    
        public virtual ICollection<RULES> RULES { get; set; }
        public virtual ICollection<FARM> FARM { get; set; }
        public virtual ICollection<SOCKET_STATUS> SOCKET_STATUS { get; set; }
        public virtual ICollection<HOSTS> HOSTS1 { get; set; }
        public virtual HOSTS HOSTS2 { get; set; }
        public virtual TS TS { get; set; }
        public virtual ICollection<RESOURCES> RESOURCES { get; set; }
        public virtual MONITORING MONITORING { get; set; }
        public virtual USERS USERS { get; set; }
        public virtual ICollection<MONITORING> MONITORING1 { get; set; }
        public virtual FARM FARM1 { get; set; }
        public virtual ICollection<SERVICES> SERVICES { get; set; }
    }
}

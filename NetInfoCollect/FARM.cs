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
    
    public partial class FARM
    {
        public FARM()
        {
            this.RULES = new HashSet<RULES>();
            this.HOSTS1 = new HashSet<HOSTS>();
        }
    
        public int FARM_ID { get; set; }
        public string FARM_NAME { get; set; }
        public string DESCRIPTION { get; set; }
        public Nullable<int> HOST_ID { get; set; }
        public Nullable<int> TS_ID { get; set; }
        public string PRODUCT_VERSION { get; set; }
    
        public virtual HOSTS HOSTS { get; set; }
        public virtual TS TS { get; set; }
        public virtual ICollection<RULES> RULES { get; set; }
        public virtual ICollection<HOSTS> HOSTS1 { get; set; }
    }
}

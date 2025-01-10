using System;

namespace ClassModel.Model
{
    public partial class INP_CUSTOMER
    {
        public long CUST_SID { get; set; }
        public string CUST_ID { get; set; }
        public long SBS_SID { get; set;}
        public short SBS_NO { get; set; }
        public long STORE_SID { get; set; }
        public short STORE_NO { get; set; }
        public string CUST_TYPE { get; set; }
        public string FIRST_NAME { get; set; }
        public string LAST_NAME { get; set;}
        public short STATUS_SID { get; set; }
        public short ACTIVE { get; set; } = 1;
        public DateTime CREATED_DATETIME { get; set; }
    }
}

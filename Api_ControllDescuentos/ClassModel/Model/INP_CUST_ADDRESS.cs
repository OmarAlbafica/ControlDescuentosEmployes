using System;

namespace ClassModel.Model
{
    public partial class INP_CUST_ADDRESS
    {
        public long SID { get; set; }
        public string ADDRESS_TYPE { get; set; }
        public short ADDRESS_NO { get; set; }
        public string ADDRESS_NAME { get; set; }
        public short ADDRESS_PRIMARY { get; set; }
        public string ADDRESS1 { get; set; }
        public string ADDRESS2 { get; set; }
        public string ADDRESS3 { get; set; }
        public string POSTAL_CODE { get; set; }
        public short ACTIVE { get; set; }
        public DateTime CREATED_DATETIME { get; set; }
        public long CUST_ID { get; set; }
    }
}

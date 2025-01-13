using System;

namespace ClassModel.Model
{
    public partial class INP_CUST_PHONE
    {
        public long SID { get; set; }
        public string PHONE_TYPE { get; set; }
        public short PHONE_NO { get; set; }
        public short PHONE_PRIMARY { get; set; }
        public string PHONE_NUMBER { get; set; }
        public long CUST_SID { get; set; }
        public DateTime CREATED_DATETIME { get; set; }
    }
}

using System;

namespace ClassModel.Model
{
    public partial class INP_CUST_EMAIL
    {
        public long SID { get; set; }
        public string EMAIL_TYPE { get; set; }
        public short EMAIL_NO { get; set; }
        public short EMAIL_PRIMARY { get; set; }
        public string EMAIL_ADDRESS { get; set; }
        public long CUST_SID { get; set; }
        public DateTime CREATED_DATETIME { get; set; }
    }
}

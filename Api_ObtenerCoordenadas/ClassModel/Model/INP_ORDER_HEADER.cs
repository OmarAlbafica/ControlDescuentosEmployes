using System;

namespace ClassModel.Model
{
    public partial class INP_ORDER_HEADER
    {
        public long DOC_SID { get; set; }
        public long SBS_SID { get; set; }
        public short SBS_NO { get; set; }
        public long STORE_SID { get; set; }
        public short STORE_NO { get; set;}
        public long WKS_SID { get; set; }
        public short WKS_NO { get; set; }
        public string WKS_NAME { get; set; }
        public long DOC_NO { get; set; }
        public short ORDER_TYPE { get; set; }
        public DateTime CREATED_DATETIME { get; set; }
        public short USE_VAT { get; set; }
        public decimal ORDER_QTY { get; set; }
        public decimal TOTAL_TAX_AMT { get; set; }
        public decimal TOTAL_AMT { get; set; }
        public DateTime ORDERED_DATETIME { get; set; }
        public string COMMENT2 { get; set; }
        public string ORDER_TRACKING_NO { get; set; }
        public short ARCHIVED { get; set; }
        public short IS_HELD { get; set; }
        public short STATUS { get; set; }
        public short STATUS_NEBULA { get; set; }
        public string STATUS_MESSAGE { get; set; }
        public long CUST_BILLING_SID { get; set; }
        public long CUST_SHIPPING_SID { get; set; }
        public long CUST_SID { get; set; }
        public short ACTIVE { get; set; } = 1;
        public string FO_NO { get; set; } 
        public string RO_NO { get; set; }
        public string SHIP_METHOD { get; set; }
    }
}

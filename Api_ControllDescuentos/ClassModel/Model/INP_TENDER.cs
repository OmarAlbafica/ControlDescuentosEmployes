using System;

namespace ClassModel.Model
{
    public partial class INP_TENDER
    {
        public long SID { get; set; }
        public long DOC_SID { get; set; }
        public short TENDER_POS { get; set; }
        public short TENDER_TYPE { get; set; }
        public decimal AMOUNT { get; set; }
        public long DEPOSIT_REF_DOC_SID { get; set; }
        public long BT_CUID { get; set; }
        public long CURRENCY_SID { get; set; }
        public string CURRENCY_NAME { get; set; }
        public string CURRENCY_ALPHA_CODE { get; set; }
        public int SBS_NO { get; set; }
        public long SBS_SID { get; set; }
        public int STORE_NO { get; set; }
        public long STORE_SID { get; set; }
        public int WKS_NO { get; set; }
        public long WKS_SID { get; set; }
        public string WKS_NAME { get; set; }
        public string COMMENT2 { get; set; } = "DEPOSIT";
        public short RECEIPT_TYPE { get; set; } = 2;
        public short HAS_DEPOSIT { get; set; } = 1;
        public short ACTIVE { get; set; } = 1;
        public DateTime CREATED_DATETIME { get; set; } = DateTime.Now;
        public short USE_VAT { get; set; }
        public short ARCHIVED { get; set; }
        public short IS_HELD { get; set; }
    }
}

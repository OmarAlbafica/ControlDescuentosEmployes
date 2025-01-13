using System;

namespace ClassModel.Model
{
    public partial class INP_ORDER_DETAIL
    {
        public long ORDER_DOC_SID { get; set; }
        public long SID { get; set; }
        public short ITEM_POS { get; set; }
        public long ITEM_SID { get; set; }
        public long ITEM_UID { get; set; }
        public short ITEM_TYPE { get; set; }
        public long UPC { get; set; }
        public string ALU { get; set; }
        public string TAX_CODE { get; set; }
        public long TAX_CODE_SID { get; set; }
        public decimal TAX_PERC { get; set; }
        public decimal PRICE { get; set; }
        public decimal TAX_AMT { get; set; }
        public decimal COST { get; set; }
        public decimal QTY { get; set; }
        public DateTime CREATED_DATETIME { get; set; }
        public short DETAX_FLAG { get; set; }
        public short KIT_FLAG { get; set; }
        public short FULFILL_STORE_NO { get; set; }
        public short STORE_NO { get; set; }
        public short SBS_NO { get; set; }
        public short FULFILL_SBS_NO { get; set; }
        public short ITEM_STATUS { get; set; }
        public short STATUS { get; set; }
        public short ACTIVE { get; set; } = 1;
        public string ORDER_TYPE { get; set; }
        public short USE_QTY_DECIMALS { get; set; }
    }
}

namespace ClassModel.Model
{
    public partial class PRODUCT
    {
        public long SID { get; set; }
        public long UPC { get; set; }
        public string ALU { get; set; }
        public long INVN_ITEM_UID { get; set; }
        public long TAX_CODE_SID { get; set; }
        public string TAX_CODE { get; set; }
        public decimal COST { get; set; }
        public short USE_QTY_DECIMALS { get; set; }
    }
}

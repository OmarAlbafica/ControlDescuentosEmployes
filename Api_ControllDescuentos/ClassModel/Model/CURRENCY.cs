namespace ClassModel.Model
{
    public partial class CURRENCY
    {
        public long SID { get; set; }
        public string CURRENCY_NAME { get; set; }
        public string ALPHABETIC_CODE { get; set; }
        public long DOC_SID { get; set; } = 0;
    }
}

using System;

namespace ClassModel.Model
{
    public class INP_FULFILLMENT_INFO
    {
        public long SID { get; set; }
        public string DELIVERY_METHOD { get; set; }
        public string DELIVERY_SPEED { get; set; }
        public string CARRIER_NAME { get; set; }
        public DateTime ESTIMATED_SHIPPING_DATE { get; set; }
        public long ORDER_DOC_SID { get; set; }
        public DateTime CREATED_DATETIME { get; set; }
        public short ACTIVE { get; set; } = 1;
    }
}

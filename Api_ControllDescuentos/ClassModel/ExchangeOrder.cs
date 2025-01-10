using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ClassModel
{
    public class ExchangeItem
    {
        [JsonProperty("docsid")]
        public long DocSid { get; set; }

        [JsonProperty("itempos")]
        public int ItemPos { get; set; }

        [JsonProperty("qty")]
        public decimal Qty { get; set; }

        [JsonProperty("scanupc")]
        public string ScanUpc { get; set; }

        [JsonProperty("exScanupc")]
        public string ExScanUpc { get; set; }

        [JsonProperty("storeno")]
        public string StoreNo { get; set; }

        [JsonProperty("sbsno")]
        public short SbsNo { get; set; }

        [JsonProperty("itemType")]
        public int ItemType { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("taxperc")]
        public decimal TaxPerc { get; set; }

        [JsonProperty("taxamt")]
        public decimal TaxAmt { get; set; }
    }

    public class ExchangeOrder
    {
        [JsonProperty("requestid")]
        public string RequestId { get; set; }

        [JsonProperty("sid")]
        public long Sid { get; set; }

        [JsonProperty("sbsno")]
        public short SbsNo { get; set; }

        [JsonProperty("storeno")]
        public string StoreNo { get; set; }

        [JsonProperty("origstoreno")]
        public string OrigStoreNo { get; set; }

        [JsonProperty("workstationno")]
        public int WorkstationNo { get; set; }

        [JsonProperty("ordertype")]
        public short OrderType { get; set; } = -1;

        [JsonProperty("OrderedDate")]
        public DateTime OrderedDate { get; set; }

        [JsonProperty("ordertrackingno")]
        public string OrderTrackingNumber { get; set; }

        [JsonProperty("fono")]
        public string FoNumber { get; set; }

        [JsonProperty("rono")]
        public string RoNumber { get; set; }

        [JsonProperty("orig_ordertrackingno")]
        public string OrigOrderTrackingNumber { get; set; }

        [JsonProperty("origfono")]
        public string OrigFoNumber { get; set; }

        [JsonProperty("origrono")]
        public string OrigRoNumber { get; set; }

        [JsonProperty("orderstatus")]
        public string OrderStatus { get; set; }

        [JsonProperty("shipmethod")]
        public string ShipMethod { get; set; }

        [JsonProperty("docitem")]
        public List<ExchangeItem> DocItems { get; set; }

        public ExchangeOrder()
        { 
            DocItems = new List<ExchangeItem>();
        }
    }
}

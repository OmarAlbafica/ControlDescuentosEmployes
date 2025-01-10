using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ClassModel
{
    public class CancelItem
    {
        [JsonProperty("docsid")]
        public long DocSid { get; set; }

        [JsonProperty("itempos")]
        public int ItemPos { get; set; }

        [JsonProperty("qty")]
        public decimal Qty { get; set; }

        [JsonProperty("scanupc")]
        public string ScanUpc { get; set; }

        [JsonProperty("itemtype")]
        public int ItemType { get; set; }
    }

    public class CancelOrder
    {
        [JsonProperty("requestid")]
        public string RequestId { get; set; }

        [JsonProperty("sid")]
        public long Sid { get; set; }

        [JsonProperty("sbsno")]
        public int SbsNo { get; set; }

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

        [JsonProperty("orderstatus")]
        public string OrderStatus { get; set; }

        [JsonProperty("ordertrackingno")]
        public string OrderTrackingNumber { get; set; }

        [JsonProperty("fono")]
        public string FoNumber { get; set; }

        [JsonProperty("rono")]
        public string RoNumber { get; set; }

        [JsonProperty("docitem")]
        public List<CancelItem> DocItems { get; set; }

        public CancelOrder()
        { 
            DocItems = new List<CancelItem>();
        }
    }
}

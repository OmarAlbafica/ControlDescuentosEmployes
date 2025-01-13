using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ClassModel
{
    public class CustAddress
    {
        [JsonProperty("primaryflag")]
        public bool PrimaryFlag { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; } = true;

        [JsonProperty("address1")]
        public string Address1 { get; set; }

        [JsonProperty("address2")]
        public string Address2 { get; set; }

        [JsonProperty("address3")]
        public string Address3 { get; set; }

        [JsonProperty("postalcode")]
        public string PostalCode { get; set; }

        [JsonProperty("seqno")]
        public int SeqNo { get; set; }

        [JsonProperty("addresstype")]
        public string AddressType { get; set; }
    }

    public class CustEmail
    {
        [JsonProperty("emailaddress")]
        public string EmailAddress { get; set; }

        [JsonProperty("primaryflag")]
        public bool PrimaryFlag { get; set; }

        [JsonProperty("seqno")]
        public int SeqNo { get; set; }

        [JsonProperty("emailtype")]
        public string EmailType { get; set; }
    }

    public class Customer
    {
        [JsonProperty("sid")]
        public long? Sid { get; set; }

        [JsonProperty("custid")]
        public string CustId { get; set; }

        [JsonProperty("lastname")]
        public string LastName { get; set; }

        [JsonProperty("firstname")]
        public string FirstName { get; set; }

        [JsonProperty("maxdiscperc")]
        public int MaxDiscPerc { get; set; } = 100;

        [JsonProperty("custaddress")]
        public List<CustAddress> CustAddresses { get; set; }

        [JsonProperty("custemail")]
        public List<CustEmail> CustEmails { get; set; }

        [JsonProperty("custphone")]
        public List<CustPhone> CustPhones { get; set; }

        [JsonProperty("sbsno")]
        public int? SbsNo { get; set; }

        [JsonProperty("storeno")]
        public string StoreNo { get; set; }
    }

    public class CustPhone
    {
        [JsonProperty("phoneno")]
        public string PhoneNo { get; set; }

        [JsonProperty("primaryflag")]
        public bool PrimaryFlag { get; set; }

        [JsonProperty("seqno")]
        public int SeqNo { get; set; }

        [JsonProperty("phonetype")]
        public string PhoneType { get; set; }
    }

    public class DocItem
    {
        [JsonProperty("docsid")]
        public long? DocSid { get; set; }

        [JsonProperty("itempos")]
        public int ItemPos { get; set; }

        [JsonProperty("invnsbsitemsid")]
        public long? InvnSbsItemSid { get; set; }

        [JsonProperty("detaxflag")]
        public bool DetaxFlag { get; set; }

        [JsonProperty("kitflag")]
        public int KitFlag { get; set; }

        [JsonProperty("archived")]
        public int? Archived { get; set; }

        [JsonProperty("qty")]
        public decimal Qty { get; set; }

        [JsonProperty("scanupc")]
        public string ScanUpc { get; set; }

        [JsonProperty("itemtype")]
        public int ItemType { get; set; }

        [JsonProperty("taxcode")]
        public string TaxCode { get; set; }

        [JsonProperty("pricelvl")]
        public int? PriceLvl { get; set; }

        [JsonProperty("storeno")]
        public string StoreNo { get; set; }

        [JsonProperty("sbsno")]
        public int SbsNo { get; set; }

        [JsonProperty("fulfillstoreno")]
        public string FulfillStoreNo { get; set; }

        [JsonProperty("fulfillsbsno")]
        public int? FulfillSbsNo { get; set; }

        [JsonProperty("itemstatus")]
        public int? ItemStatus { get; set; }

        [JsonProperty("enhanceditempos")]
        public int? EnhancedItemPos { get; set; }

        [JsonProperty("invnuseqtydecimals")]
        public int? InvnUseQtyDecimals { get; set; }

        [JsonProperty("invnsbsitemuid")]
        public string InvnSbsItemUid { get; set; }

        [JsonProperty("internalitempos")]
        public int? InternalItemPos { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("taxperc")]
        public decimal TaxPerc { get; set; }

        [JsonProperty("taxamt")]
        public decimal TaxAmt { get; set; }
    }

    public class DocTender
    {
        [JsonProperty("tendertype")]
        public int TenderType { get; set; }

        [JsonProperty("tenderpos")]
        public int TenderPos { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }
    }

    public class SalesOrder
    {
        [JsonProperty("requestid")]
        public string RequestId { get; set; }

        [JsonProperty("sid")]
        public long? Sid { get; set; }

        [JsonProperty("status")]
        public int? Status { get; set; }

        [JsonProperty("usevat")]
        public bool? UseVat { get; set; }

        [JsonProperty("isheld")]
        public bool? IsHeld { get; set; }

        [JsonProperty("archived")]
        public int? Archived { get; set; }

        [JsonProperty("sbsno")]
        public int? SbsNo { get; set; }

        [JsonProperty("storeno")]
        public string StoreNo { get; set; }

        [JsonProperty("origstoreno")]
        public string OrigStoreNo { get; set; }

        [JsonProperty("workstationno")]
        public int? WorkstationNo { get; set; }

        [JsonProperty("btcuid")]
        public int? BtCuid { get; set; }

        [JsonProperty("btid")]
        public int? BtId { get; set; }

        [JsonProperty("ordertype")]
        public short OrderType { get; set; } = -1;

        [JsonProperty("comment2")]
        public string Comment2 { get; set; }

        [JsonProperty("OrderedDate")]
        public string OrderedDate { get; set; }

        [JsonProperty("ordertrackingno")]
        public string OrderTrackingNumber { get; set; }

        [JsonProperty("fono")]
        public string FoNumber { get; set; }

        [JsonProperty("rono")]
        public string RoNumber { get; set; }

        [JsonProperty("orderstatus")]
        public string OrderStatus { get; set; }

        [JsonProperty("customer")]
        public Customer Customer { get; set; }

        [JsonProperty("docitem")]
        public List<DocItem> DocItems { get; set; }

        [JsonProperty("doctender")]
        public List<DocTender> DocTenders { get; set; }

        [JsonProperty("fulfilmentinfo")]
        public FulfilmentInfo FulfilmentInfo { get; set; }

        public SalesOrder()
        { 
            FulfilmentInfo = new FulfilmentInfo();
            DocTenders = new List<DocTender>();
            DocItems = new List<DocItem>();
            Customer = new Customer();
        }
    }

    public class FulfilmentInfo
    {
        [JsonProperty("shipnode")]
        public string ShipNode { get; set; }

        [JsonProperty("shipnodename")]
        public string ShipNodeName { get; set; }

        [JsonProperty("promisedeliverydate")]
        public DateTime PromiseDeliveryDate { get; set; }

        [JsonProperty("deliveryslotwindowstart")]
        public string DeliverysLotSindowStart { get; set; }

        [JsonProperty("deliveryslotwindowend")]
        public string DeliverysLotWindowEnd { get; set; }

        [JsonProperty("estimatedshippingdate")]
        public string EstimatedShippingDate { get; set; }

        [JsonProperty("deliverymethod")]
        public string DeliveryMethod { get; set; }

        [JsonProperty("deliveryspeed")]
        public string DeliverySpeed { get; set; }

        [JsonProperty("transportactions")]
        public List<Transportaction> Transportactions { get; set; }
    }

    public class Transportaction
    {
        [JsonProperty("deliverynotes")]
        public string DeliveryNotes { get; set; }

        [JsonProperty("transportationmode")]
        public string TransportationMode { get; set; }

        [JsonProperty("distanceinkm")]
        public string DistanceInKm { get; set; }

        [JsonProperty("carrierid")]
        public string CarrierId { get; set; }

        [JsonProperty("carriername")]
        public string CarrierName { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Models.Coupang
{
    public class ManifestRequestModel
    {
        public string SendId { get; set; }
        [Required(ErrorMessage = "DeclData is null")]
        public DeclData DeclData { get; set; }

    }

    public class CargoManifestRequestModel
    {
        [Required(ErrorMessage= "To is null")]
        public string To { get; set; }
        [Required(ErrorMessage = "Broker is null")]
        public string Broker { get; set; }
        [Required(ErrorMessage = "Date is null")]
        public string Date { get; set; }
        [Required(ErrorMessage = "BillingCode is null")]
        public string BillingCode { get; set; }
        [Required(ErrorMessage = "Tel is null")]
        public string Tel { get; set; }
        [Required(ErrorMessage = "Fax is null")]
        public string Fax { get; set; }
        [Required(ErrorMessage = "FlightNo is null")]
        public string FlightNo { get; set; }
        [Required(ErrorMessage = "MawbNo is null")]
        public string MawbNo { get; set; }
        [Required(ErrorMessage= "TotalCnt is null")]
        public string TotalCnt { get; set; }
        [Required(ErrorMessage = "TotalGrossWeight is null")]
        public string TotalGrossWeight { get; set; }
        [Required(ErrorMessage = "ItemDtoList is null")]
        public List<ItemDto> ItemDtoList { get; set; }
    }

    public class DeclData
    {
        public string CreateDate { get; set; }
        public string BrokerCode { get; set; }
        public string MawbNo { get; set; }
        public string FlightNo { get; set; }
        public string ImportDate { get; set; }
        public string DeclDate { get; set; }
        public string Currency { get; set; }
        public string OrigPort { get; set; }
        [Required(ErrorMessage = "MasterBags is null")]
        public List<Bag> Bags { get; set; }
    }

    public class Bag
    {
        [Required(ErrorMessage = "MasterBags.DeclType is null")]
        public string DeclType { get; set; }
        public string DeclNo { get; set; }
        public string BagNo { get; set; }
        public string BagWeight { get; set; }
        [Required(ErrorMessage = "MasterBags.HawbList is null")]
        public List<HawbItem> HawbList { get; set; }
    }

    public class HawbItem
    {
        [Required(ErrorMessage = "HawbList.HawbNo is null")]
        public string HawbNo { get; set; }
        public string MainHawbNo { get; set; }
        public string DeliveryType { get; set; }
        [Required(ErrorMessage = "HawbList.Ctns is null")]
        public string Ctns { get; set; }
        [Required(ErrorMessage = "HawbList.CtnUnit is null")]
        public string CtnUnit { get; set; }
        [Required(ErrorMessage = "HawbList.GrossWeight is null")]
        public string GrossWeight { get; set; }
        public string NetWeight { get; set; }
        public string TermsSales { get; set; }
        public string FreightAmt { get; set; }
        [Required(ErrorMessage = "MasterBags.Consignee is null")]
        public Consignee Consignee { get; set; }
        [Required(ErrorMessage = "MasterBags.Shipper is null")]
        public Shipper Shipper { get; set; }
        [Required(ErrorMessage = "MasterBags.Items is null")]
        public List<Item> Items { get; set; }
        public string DutyExemption { get; set; }
    }

    public struct Consignee
    {
        //[Required(ErrorMessage = "Consignee.TaxNo is null")]
        public string TaxNo { get; set; }
        [Required(ErrorMessage = "Consignee.Name is null")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Consignee.Addr is null")]
        public string Addr { get; set; }
        [Required(ErrorMessage = "Consignee.Tel is null")]
        public string Tel { get; set; }
    }

    public class Shipper
    {
        public string TaxNo { get; set; }
        [Required(ErrorMessage = "Shipper.Name is null")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Shipper.Addr is null")]
        public string Addr { get; set; }
    }

    public class Item
    {
        public string ItemNo { get; set; }
        [Required(ErrorMessage = "Items.VendorItemId is null")]
        public string VendorItemId { get; set; }
        [Required(ErrorMessage = "Items.CategoryName is null")]
        public string CategoryName { get; set; }
        [Required(ErrorMessage = "Items.GoodsDesc is null")]
        public string GoodsDesc { get; set; }
        [Required(ErrorMessage = "Items.Uprice is null")]
        public string Uprice { get; set; }
        [Required(ErrorMessage = "Items.Qty is null")]
        public string Qty { get; set; }
        [Required(ErrorMessage = "Items.QtyUnit is null")]
        public string QtyUnit { get; set; }
        public string TotalPrice { get; set; }
        [Required(ErrorMessage = "Items.MfrCountry is null")]
        public string MfrCountry { get; set; }
        [Required(ErrorMessage = "Items.TaxMethod is null")]
        public string TaxMethod { get; set; }
        public string CCCCode { get; set; }
        public string LicenseNo1 { get; set; }
        public string LicenseNo2 { get; set; }
        public string LicenseNo3 { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Specification { get; set; }
        public string DesignatedCode { get; set; }
        public string ElementLabel { get; set; }
        public string ElementModel { get; set; }
        public string FrequenctRange { get; set; }
        public string OutPut { get; set; }
        public string NccRemark { get; set; }
        public string NccUrl { get; set; }
    }

    public class ItemDto
    {
        [Required(ErrorMessage = "ItemNo is null")]
        public string ItemNo { get; set; }
        [Required(ErrorMessage = "MasterBagNo is null")]
        public string MasterBagNo { get; set; }
        [Required(ErrorMessage = "Ctn is null")]
        public string Ctn { get; set; }
        [Required(ErrorMessage = "GrossWeight is null")]
        public string GrossWeight { get; set; }
        [Required(ErrorMessage = "Description is null")]
        public string Description { get; set; }
        [Required(ErrorMessage = "DeclaredTo is null")]
        public string DeclaredTo { get; set; }
        public string Remark { get; set; }
    }
}

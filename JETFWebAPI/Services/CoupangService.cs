using JETFWebAPI.Models.Coupang;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Text;
using JETFWebAPI.Models;
using System.Security.Cryptography;

namespace JETFWebAPI.Services
{
    public class CoupangService
    {
        private SqlConnection conn;
        /// <summary>
        /// 建構式
        /// </summary>
        public CoupangService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        public ManifestResponseModel PostManifest(ManifestRequestModel body)
        {
            string result = "";
            ManifestResponseModel response = new ManifestResponseModel();
            response.resultCode = "SUCCESS";
            response.data = "true";
            try
            {
                //檢查資料
                result = CheckManifest(body);
                if (result == "")
                {
                    //KEY是否重複
                    string hawbNo = body.DeclData.Bags[0].HawbList[0].HawbNo;
                    result = GetManifest(hawbNo);
                    if (result == "")
                    {
                        List<ManifestModel> modelList = new List<ManifestModel>();
                        //for (int i = 0; i < body.DeclData.Bags.Count; i++)
                        //{
                        //    for (int j = 0; j < body.DeclData.Bags[i].HawbList.Count; j++)
                        //    {
                        //        for (int k = 0; k < body.DeclData.Bags[i].HawbList[j].Items.Count; k++)
                        //        {
                        //            modelList.Add(new ManifestModel()
                        //            {
                        //                SendId = body.SendId,
                        //                CreateDate = body.DeclData.CreateDate,
                        //                BrokerCode = body.DeclData.BrokerCode,
                        //                MawbNo = body.DeclData.MawbNo,
                        //                FlightNo = body.DeclData.FlightNo,
                        //                ImportDate = body.DeclData.ImportDate,
                        //                DeclDate = body.DeclData.DeclDate,
                        //                Currency = body.DeclData.Currency,
                        //                OrigPort = body.DeclData.OrigPort,

                        //                DeclType = body.DeclData.Bags[i].DeclType,
                        //                DeclNo = body.DeclData.Bags[i].DeclNo,
                        //                BagNo = body.DeclData.Bags[i].BagNo,
                        //                BagWeight = body.DeclData.Bags[i].BagWeight,

                        //                HawbNo = body.DeclData.Bags[i].HawbList[j].HawbNo,
                        //                DeliveryType = body.DeclData.Bags[i].HawbList[j].DeliveryType,
                        //                Ctns = body.DeclData.Bags[i].HawbList[j].Ctns,
                        //                CtnUnit = body.DeclData.Bags[i].HawbList[j].CtnUnit,
                        //                GrossWeight = body.DeclData.Bags[i].HawbList[j].GrossWeight,
                        //                NetWeight = body.DeclData.Bags[i].HawbList[j].NetWeight,
                        //                TermsSales = body.DeclData.Bags[i].HawbList[j].TermsSales,
                        //                FreightAmt = body.DeclData.Bags[i].HawbList[j].FreightAmt,
                        //                DutyExemption = body.DeclData.Bags[i].HawbList[j].DutyExemption,

                        //                CTaxNo = body.DeclData.Bags[i].HawbList[j].Consignee.TaxNo,
                        //                CName = body.DeclData.Bags[i].HawbList[j].Consignee.Name,
                        //                CAddr = body.DeclData.Bags[i].HawbList[j].Consignee.Addr,
                        //                CTel = body.DeclData.Bags[i].HawbList[j].Consignee.Tel,

                        //                SName = body.DeclData.Bags[i].HawbList[j].Shipper.Name,
                        //                SAddr = body.DeclData.Bags[i].HawbList[j].Shipper.Addr,

                        //                ItemNo = body.DeclData.Bags[i].HawbList[j].Items[k].ItemNo,
                        //                VendorItemId = body.DeclData.Bags[i].HawbList[j].Items[k].VendorItemId,
                        //                CategoryName = body.DeclData.Bags[i].HawbList[j].Items[k].CategoryName,
                        //                GoodsDesc = body.DeclData.Bags[i].HawbList[j].Items[k].GoodsDesc,
                        //                Uprice = body.DeclData.Bags[i].HawbList[j].Items[k].Uprice,
                        //                Qty = body.DeclData.Bags[i].HawbList[j].Items[k].Qty,
                        //                QtyUnit = body.DeclData.Bags[i].HawbList[j].Items[k].QtyUnit,
                        //                TotalPrice = body.DeclData.Bags[i].HawbList[j].Items[k].TotalPrice,
                        //                MfrCountry = body.DeclData.Bags[i].HawbList[j].Items[k].MfrCountry,
                        //                TaxMethod = body.DeclData.Bags[i].HawbList[j].Items[k].TaxMethod,
                        //                CCCCode = body.DeclData.Bags[i].HawbList[j].Items[k].CCCCode,
                        //                LicenseNo1 = body.DeclData.Bags[i].HawbList[j].Items[k].LicenseNo1,
                        //                LicenseNo2 = body.DeclData.Bags[i].HawbList[j].Items[k].LicenseNo2,
                        //                LicenseNo3 = body.DeclData.Bags[i].HawbList[j].Items[k].LicenseNo3,
                        //                Brand = body.DeclData.Bags[i].HawbList[j].Items[k].Brand,
                        //                Model = body.DeclData.Bags[i].HawbList[j].Items[k].Model,
                        //                Specification = body.DeclData.Bags[i].HawbList[j].Items[k].Specification,
                        //                DesignatedCode = body.DeclData.Bags[i].HawbList[j].Items[k].DesignatedCode,
                        //            });
                        //        }
                        //    }
                        //}
                        for (int k = 0; k < body.DeclData.Bags[0].HawbList[0].Items.Count; k++)
                        {
                            modelList.Add(new ManifestModel()
                            {
                                SendId = body.SendId,
                                CreateDate = body.DeclData.CreateDate,
                                BrokerCode = body.DeclData.BrokerCode,
                                MawbNo = body.DeclData.MawbNo,
                                FlightNo = body.DeclData.FlightNo,
                                ImportDate = body.DeclData.ImportDate,
                                DeclDate = body.DeclData.DeclDate,
                                Currency = body.DeclData.Currency,
                                OrigPort = body.DeclData.OrigPort,

                                DeclType = body.DeclData.Bags[0].DeclType,
                                DeclNo = body.DeclData.Bags[0].DeclNo,
                                BagNo = body.DeclData.Bags[0].BagNo,
                                BagWeight = body.DeclData.Bags[0].BagWeight,

                                HawbNo = body.DeclData.Bags[0].HawbList[0].HawbNo,
                                MainHawbNo = body.DeclData.Bags[0].HawbList[0].MainHawbNo,
                                DeliveryType = body.DeclData.Bags[0].HawbList[0].DeliveryType,
                                Ctns = body.DeclData.Bags[0].HawbList[0].Ctns,
                                CtnUnit = body.DeclData.Bags[0].HawbList[0].CtnUnit,
                                GrossWeight = body.DeclData.Bags[0].HawbList[0].GrossWeight,
                                NetWeight = body.DeclData.Bags[0].HawbList[0].NetWeight,
                                TermsSales = body.DeclData.Bags[0].HawbList[0].TermsSales,
                                FreightAmt = body.DeclData.Bags[0].HawbList[0].FreightAmt,
                                DutyExemption = body.DeclData.Bags[0].HawbList[0].DutyExemption,

                                CTaxNo = body.DeclData.Bags[0].HawbList[0].Consignee.TaxNo,
                                CName = body.DeclData.Bags[0].HawbList[0].Consignee.Name,
                                CAddr = body.DeclData.Bags[0].HawbList[0].Consignee.Addr,
                                CTel = body.DeclData.Bags[0].HawbList[0].Consignee.Tel,

                                SName = body.DeclData.Bags[0].HawbList[0].Shipper.Name,
                                SAddr = body.DeclData.Bags[0].HawbList[0].Shipper.Addr,

                                ItemNo = body.DeclData.Bags[0].HawbList[0].Items[k].ItemNo,
                                VendorItemId = body.DeclData.Bags[0].HawbList[0].Items[k].VendorItemId,
                                CategoryName = body.DeclData.Bags[0].HawbList[0].Items[k].CategoryName,
                                GoodsDesc = body.DeclData.Bags[0].HawbList[0].Items[k].GoodsDesc,
                                Uprice = body.DeclData.Bags[0].HawbList[0].Items[k].Uprice,
                                Qty = body.DeclData.Bags[0].HawbList[0].Items[k].Qty,
                                QtyUnit = body.DeclData.Bags[0].HawbList[0].Items[k].QtyUnit,
                                TotalPrice = body.DeclData.Bags[0].HawbList[0].Items[k].TotalPrice,
                                MfrCountry = body.DeclData.Bags[0].HawbList[0].Items[k].MfrCountry,
                                TaxMethod = body.DeclData.Bags[0].HawbList[0].Items[k].TaxMethod,
                                CCCCode = body.DeclData.Bags[0].HawbList[0].Items[k].CCCCode,
                                LicenseNo1 = body.DeclData.Bags[0].HawbList[0].Items[k].LicenseNo1,
                                LicenseNo2 = body.DeclData.Bags[0].HawbList[0].Items[k].LicenseNo2,
                                LicenseNo3 = body.DeclData.Bags[0].HawbList[0].Items[k].LicenseNo3,
                                Brand = body.DeclData.Bags[0].HawbList[0].Items[k].Brand,
                                Model = body.DeclData.Bags[0].HawbList[0].Items[k].Model,
                                Specification = body.DeclData.Bags[0].HawbList[0].Items[k].Specification,
                                DesignatedCode = body.DeclData.Bags[0].HawbList[0].Items[k].DesignatedCode,
                            });
                        }
                        //新增資料
                        if (modelList.Count > 0)
                        {
                            result = InsertManifest(modelList);
                        }
                    }
                    else {
                        //資料重複回復成功
                        result = "";
                    }
                }
                
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }

            if (result != "")
            {
                response.resultCode = "FAIL";
                response.data = "false";
                response.resultMessage = result;
            }
            return response;
        }


        public CargoManifestResponseModel PostCargoManifest(CargoManifestRequestModel body)
        {
            string result = "";
            CargoManifestResponseModel response = new CargoManifestResponseModel();
            response.resultCode = "SUCCESS";
            response.data = "true";
            try
            {
                //檢查資料
                result = CheckCargoManifest(body);
                if (result == "")
                {
                    List<CargoManifestModel> modelList = new List<CargoManifestModel>();
                    for (int i = 0; i < body.ItemDtoList.Count; i++)
                    {
                        modelList.Add(new CargoManifestModel()
                        {
                            To = body.To,
                            Broker = body.Broker,
                            Date = body.Date,
                            BillingCode = body.BillingCode,
                            Tel = body.Tel,
                            Fax = body.Fax,
                            FlightNo = body.FlightNo,
                            MawbNo = body.MawbNo,
                            TotalCnt = body.TotalCnt,
                            TotalGrossWeight = body.TotalGrossWeight,
                            ItemNo = body.ItemDtoList[i].ItemNo,
                            MasterBagNo = body.ItemDtoList[i].MasterBagNo,
                            Ctn = body.ItemDtoList[i].Ctn,
                            GrossWeight = body.ItemDtoList[i].GrossWeight,
                            Description = body.ItemDtoList[i].Description,
                            DeclaredTo = body.ItemDtoList[i].DeclaredTo,
                            Remark = body.ItemDtoList[i].Remark
                        });
                    }

                    if (modelList.Count > 0)
                    {
                        result = InsertCargoManifest(modelList);
                    }
                }
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }

            if (result != "")
            {
                response.resultCode = "FAIL";
                response.data = "false";
                response.resultMessage = result;
            }
            return response;
        }

        //檢查資料
        string CheckManifest(ManifestRequestModel body)
        {
            string result="";
            if (body.DeclData.Bags.Count > 1)
            {
                result = "bags count is more than 1";
            }
            else if (body.DeclData.Bags[0].HawbList.Count > 1)
            {
                result = "hawbList count is more than 1";
            }
            return result;
        }

        //檢查資料
        string CheckCargoManifest(CargoManifestRequestModel body)
        {
            string result = "";

            return result;
        }

        string GetManifest(string hawbNo)
        {
            string result = "";
            using (SqlDataAdapter da = new SqlDataAdapter("select * from [DATA_CENTER].[dbo].[ORDER_MANIFEST] where HawbNo=@HawbNo", conn))
            {
                DataTable dt = new DataTable();
                da.SelectCommand.Parameters.Add("@HawbNo", SqlDbType.NVarChar).Value = hawbNo;
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    result = $"HawbNo：{hawbNo}重複";
                }
            }
            return result;
        }

        string InsertManifest(List<ManifestModel> modelList)
        {
            string resule = "";
            conn.Open();
            using (SqlTransaction tran = conn.BeginTransaction())
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("insert [DATA_CENTER].[dbo].[ORDER_MANIFEST](SendId,CreateDate,BrokerCode,MawbNo,FlightNo,ImportDate,DeclDate,Currency,OrigPort,DeclType,DeclNo,BagNo,BagWeight,HawbNo,DeliveryType,Ctns,CtnUnit,GrossWeight,NetWeight,TermsSales,FreightAmt,DutyExemption,CTaxNo,CName,CAddr,CTel,SName,SAddr,ItemNo,VendorItemId,CategoryName,GoodsDesc,Uprice,Qty,QtyUnit,TotalPrice,MfrCountry,TaxMethod,CCCCode,LicenseNo1,LicenseNo2,LicenseNo3,Brand,Model,Specification,DesignatedCode,MainHawbNo) ");
                    sb.Append("values(@SendId,@CreateDate,@BrokerCode,@MawbNo,@FlightNo,@ImportDate,@DeclDate,@Currency,@OrigPort,@DeclType,@DeclNo,@BagNo,@BagWeight,@HawbNo,@DeliveryType,@Ctns,@CtnUnit,@GrossWeight,@NetWeight,@TermsSales,@FreightAmt,@DutyExemption,@CTaxNo,@CName,@CAddr,@CTel,@SName,@SAddr,@ItemNo,@VendorItemId,@CategoryName,@GoodsDesc,@Uprice,@Qty,@QtyUnit,@TotalPrice,@MfrCountry,@TaxMethod,@CCCCode,@LicenseNo1,@LicenseNo2,@LicenseNo3,@Brand,@Model,@Specification,@DesignatedCode,@MainHawbNo) ");
                    using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                    {
                        cmd.Transaction = tran;
                        for (int i = 0; i < modelList.Count; i++)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@SendId", SqlDbType.NVarChar).Value = modelList[i].SendId ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@CreateDate", SqlDbType.NVarChar).Value = modelList[i].CreateDate ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@BrokerCode", SqlDbType.NVarChar).Value = modelList[i].BrokerCode ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@MawbNo", SqlDbType.NVarChar).Value = modelList[i].MawbNo ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@MainHawbNo", SqlDbType.NVarChar).Value = modelList[i].MainHawbNo ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@FlightNo", SqlDbType.NVarChar).Value = modelList[i].FlightNo ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@ImportDate", SqlDbType.NVarChar).Value = modelList[i].ImportDate ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@DeclDate", SqlDbType.NVarChar).Value = modelList[i].DeclDate ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@Currency", SqlDbType.NVarChar).Value = modelList[i].Currency ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@OrigPort", SqlDbType.NVarChar).Value = modelList[i].OrigPort ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@DeclType", SqlDbType.NVarChar).Value = modelList[i].DeclType ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@DeclNo", SqlDbType.NVarChar).Value = modelList[i].DeclNo ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@BagNo", SqlDbType.NVarChar).Value = modelList[i].BagNo ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@BagWeight", SqlDbType.NVarChar).Value = modelList[i].BagWeight ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@HawbNo", SqlDbType.NVarChar).Value = modelList[i].HawbNo ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@DeliveryType", SqlDbType.NVarChar).Value = modelList[i].DeliveryType ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@Ctns", SqlDbType.NVarChar).Value = modelList[i].Ctns ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@CtnUnit", SqlDbType.NVarChar).Value = modelList[i].CtnUnit ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@GrossWeight", SqlDbType.NVarChar).Value = modelList[i].GrossWeight ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@NetWeight", SqlDbType.NVarChar).Value = modelList[i].NetWeight ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@TermsSales", SqlDbType.NVarChar).Value = modelList[i].TermsSales ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@FreightAmt", SqlDbType.NVarChar).Value = modelList[i].FreightAmt ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@DutyExemption", SqlDbType.NVarChar).Value = modelList[i].DutyExemption ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@CTaxNo", SqlDbType.NVarChar).Value = modelList[i].CTaxNo ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@CName", SqlDbType.NVarChar).Value = modelList[i].CName ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@CAddr", SqlDbType.NVarChar).Value = modelList[i].CAddr ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@CTel", SqlDbType.NVarChar).Value = modelList[i].CTel ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@SName", SqlDbType.NVarChar).Value = modelList[i].SName ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@SAddr", SqlDbType.NVarChar).Value = modelList[i].SAddr ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@ItemNo", SqlDbType.NVarChar).Value = modelList[i].ItemNo ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@VendorItemId", SqlDbType.NVarChar).Value = modelList[i].VendorItemId ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@CategoryName", SqlDbType.NVarChar).Value = modelList[i].CategoryName ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@GoodsDesc", SqlDbType.NVarChar).Value = modelList[i].GoodsDesc ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@Uprice", SqlDbType.NVarChar).Value = modelList[i].Uprice ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@Qty", SqlDbType.NVarChar).Value = modelList[i].Qty ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@QtyUnit", SqlDbType.NVarChar).Value = modelList[i].QtyUnit ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@TotalPrice", SqlDbType.NVarChar).Value = modelList[i].TotalPrice ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@MfrCountry", SqlDbType.NVarChar).Value = modelList[i].MfrCountry ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@TaxMethod", SqlDbType.NVarChar).Value = modelList[i].TaxMethod ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@CCCCode", SqlDbType.NVarChar).Value = modelList[i].CCCCode ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@LicenseNo1", SqlDbType.NVarChar).Value = modelList[i].LicenseNo1 ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@LicenseNo2", SqlDbType.NVarChar).Value = modelList[i].LicenseNo2 ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@LicenseNo3", SqlDbType.NVarChar).Value = modelList[i].LicenseNo3 ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@Brand", SqlDbType.NVarChar).Value = modelList[i].Brand ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@Model", SqlDbType.NVarChar).Value = modelList[i].Model ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@Specification", SqlDbType.NVarChar).Value = modelList[i].Specification ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@DesignatedCode", SqlDbType.NVarChar).Value = modelList[i].DesignatedCode ?? (object)DBNull.Value;
                            cmd.ExecuteNonQuery();
                        }
                        tran.Commit();
                    }
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    resule = ex.Message;
                }
            }
            conn.Close();
            return resule;
        }

        string InsertCargoManifest(List<CargoManifestModel> modelList)
        {
            string resule = "";
            conn.Open();
            using (SqlTransaction tran = conn.BeginTransaction())
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("insert [DATA_CENTER].[dbo].[ORDER_CARGO_MANIFEST]([To],Broker,Date,BillingCode,Tel,Fax,FlightNo,MawbNo,TotalCnt,TotalGrossWeight,ItemNo,MasterBagNo,Ctn,GrossWeight,Description,DeclaredTo,Remark) ");
                    sb.Append("values(@To,@Broker,@Date,@BillingCode,@Tel,@Fax,@FlightNo,@MawbNo,@TotalCnt,@TotalGrossWeight,@ItemNo,@MasterBagNo,@Ctn,@GrossWeight,@Description,@DeclaredTo,@Remark) ");
                    using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                    {
                        cmd.Transaction = tran;
                        for (int i = 0; i < modelList.Count; i++)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@To", SqlDbType.NVarChar).Value = modelList[i].To ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@Broker", SqlDbType.NVarChar).Value = modelList[i].Broker ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@Date", SqlDbType.NVarChar).Value = modelList[i].Date ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@BillingCode", SqlDbType.NVarChar).Value = modelList[i].BillingCode ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@Tel", SqlDbType.NVarChar).Value = modelList[i].Tel ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@Fax", SqlDbType.NVarChar).Value = modelList[i].Fax ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@FlightNo", SqlDbType.NVarChar).Value = modelList[i].FlightNo ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@MawbNo", SqlDbType.NVarChar).Value = modelList[i].MawbNo ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@TotalCnt", SqlDbType.NVarChar).Value = modelList[i].TotalCnt ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@TotalGrossWeight", SqlDbType.NVarChar).Value = modelList[i].TotalGrossWeight ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@ItemNo", SqlDbType.NVarChar).Value = modelList[i].ItemNo ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@MasterBagNo", SqlDbType.NVarChar).Value = modelList[i].MasterBagNo ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@Ctn", SqlDbType.NVarChar).Value = modelList[i].Ctn ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@GrossWeight", SqlDbType.NVarChar).Value = modelList[i].GrossWeight ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@Description", SqlDbType.NVarChar).Value = modelList[i].Description ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@DeclaredTo", SqlDbType.NVarChar).Value = modelList[i].DeclaredTo ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@Remark", SqlDbType.NVarChar).Value = modelList[i].Remark ?? (object)DBNull.Value;
                            cmd.ExecuteNonQuery();
                        }

                        //更新Manifest 航班 主號
                        sb = new StringBuilder();
                        sb.Append("update [DATA_CENTER].[dbo].[ORDER_MANIFEST] set CreateDate=@CreateDate,MawbNo=@MawbNo,FlightNo=@FlightNo,EditDateTime=getdate() ");
                        sb.Append("where BagNo=@BagNo ");
                        cmd.CommandText = sb.ToString();
                        for (int i = 0; i < modelList.Count; i++)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@CreateDate", SqlDbType.NVarChar).Value = modelList[i].Date ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@MawbNo", SqlDbType.NVarChar).Value = modelList[i].MawbNo ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@FlightNo", SqlDbType.NVarChar).Value = modelList[i].FlightNo ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@BagNo", SqlDbType.NVarChar).Value = modelList[i].MasterBagNo ?? (object)DBNull.Value;
                            cmd.ExecuteNonQuery();
                        }
                        tran.Commit();
                    }
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    resule = ex.Message;
                }
            }
            conn.Close();
            return resule;
        }

        public bool CheckToken(string apiPath, string body, string token)
        {
            bool result = false;
            string check = GetToken(apiPath, body);
            if (check == token)
            {
                result = true;
            }
            return result;
        }

        public string GetToken(string apiPath, string body)
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM DATA_CENTER.dbo.SYS_API_CODE WHERE CUST_CODE = 'CN00096'", conn))
            {
                da.Fill(dt);
            }
            string key = dt.Rows[0]["KEY"].ToString();
            string code = dt.Rows[0]["CODE"].ToString();
            string token = HMACSHA256(code + apiPath + body, key);
            return token;
        }

        public string HMACSHA256(string message, string key)
        {
            var encoding = new System.Text.UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(key);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacSHA256 = new HMACSHA256(keyByte))
            {
                byte[] hashMessage = hmacSHA256.ComputeHash(messageBytes);
                return ToHexString(hashMessage);
                //return Convert.ToBase64String(hashMessage);
            }
        }

        public static string ToHexString(byte[] bytes)
        {
            string hexString = string.Empty;
            if (bytes != null)
            {
                StringBuilder strB = new StringBuilder();
                foreach (byte b in bytes)
                {
                    strB.AppendFormat("{0:x2}", b);
                }
                hexString = strB.ToString();
            }
            return hexString;
        }


    }
}
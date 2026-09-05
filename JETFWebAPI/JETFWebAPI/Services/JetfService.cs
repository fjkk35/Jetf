using iText.Kernel.Pdf;
using iText.Layout.Element;
using iTextSharp.text;
using iTextSharp.text.pdf;
using JETFWebAPI.Models;
using JETFWebAPI.Models.Jetf;
using JETFWebAPI.Models.SimpleDeclaration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace JETFWebAPI.Services
{
    public class JetfService : _BaseService
    {
        /// <summary>
        /// 稅金編號查詢
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public TaxNumberResponseModel PostTaxNumber(TaxNumberModel body)
        {
            TaxNumberResponseModel response = new TaxNumberResponseModel();
            response.Status = "Success";
            response.TrackingNo = body.TrackingNo;
            response.TaxNumberList = new List<TaxNumberItem>();
            conn.Open();
            try
            {
                //用分提單號稅金編號
                DataTable dt = GetTaxNumber(body.TrackingNo);
                if (dt.Rows.Count == 0)
                {
                    //用袋號查詢稅金編號
                    string bagNumber = GetEtlBagNumber(body.TrackingNo);
                    if (bagNumber != "")
                    {
                        dt = GetTaxNumber(body.TrackingNo, bagNumber);
                    }
                }

                if (dt.Rows.Count > 0)
                {
                    response.ResultCode = "Y";
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        response.TaxNumberList.Add(new TaxNumberItem()
                        {
                            TaxNumber = dt.Rows[i]["TAX_NUMBER"].ToString().Trim()
                        });
                    }
                }
                else
                {
                    response.ResultCode = "N";
                    response.ResultMessage = "查無稅金編號";
                }

                dt.Dispose();
            }
            catch (Exception ex)
            {
                response.Status = "Fail";
                response.ResultMessage = ex.Message;
            }
            conn.Close();
            return response;
        }

        /// <summary>
        /// 稅金編號Pdf查詢
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public TaxNumberPdfResponseModel PostTaxNumberPdf(TaxNumberPdfModel body)
        {
            string taxNumber = body.TaxNumber;
            TaxNumberPdfResponseModel response = new TaxNumberPdfResponseModel();
            response.Status = "Success";
            response.TaxNumber = taxNumber;
            conn.Open();
            try
            {
                if (GetClearance_Tax_Pdf(taxNumber) != "")
                {
                    //測試
                    //string url = "http://localhost:56641/api/Jetf/GetTaxNumberPdf";
                    string url = "https://service.jet-f.com/JETFWebAPI/api/Jetf/GetTaxNumberPdf";
                    response.ResultCode = "Y";
                    response.Url = $"{url}?taxNumber={taxNumber}&token={GetToken("GetTaxNumberPdf", taxNumber)}";
                }
                else
                {
                    response.ResultCode = "N";
                    response.ResultMessage = "查無稅金編號Pdf";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Fail";
                response.ResultMessage = ex.Message;
            }
            conn.Close();
            return response;
        }

        /// <summary>
        /// 簽收單查詢
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public CargoSignReceiptResponseModel PostCargoSignReceipt(CargoSignReceiptModel body)
        {
            string cargoNumber = body.CargoNumber;
            CargoSignReceiptResponseModel response = new CargoSignReceiptResponseModel();
            response.Status = "Success";
            response.CargoNumber = cargoNumber;
            conn.Open();
            try
            {
                ArrayList list = GetCargo_Sign_Receipt(cargoNumber);
                if (list.Count > 0)
                {
                    //測試
                    //string url = "http://localhost:56641/api/Jetf/GetCargoSignReceipt";
                    string url = "https://service.jet-f.com/JETFWebAPI/api/Jetf/GetCargoSignReceipt";
                    response.ResultCode = "Y";
                    response.UrlList = new List<UrlItem>();
                    for (int i = 0; i < list.Count; i++)
                    {
                        response.UrlList.Add(new UrlItem()
                        {
                            Url = $"{url}?cargoNumber={cargoNumber}&fileName={list[i].ToString()}&token={GetToken("GetCargoSignReceipt", cargoNumber)}"
                        });
                    }
                }
                else
                {
                    response.ResultCode = "N";
                    response.ResultMessage = "查無簽收單編號";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Fail";
                response.ResultMessage = ex.Message;
            }
            conn.Close();
            return response;
        }

        /// <summary>
        /// 尾程配送基礎资料
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public ShippingInformationResponseModel ShippingInformation(ShippingInformationModel model)
        {
            ShippingInformationResponseModel response = new ShippingInformationResponseModel();
            response.Status = "true";
            response.Message = "成功";
            conn.Open();
            try
            {
                //新增資料
                InsertSjlShippingInformation(model);
            }
            catch (Exception ex)
            {
                response.Status = "false";
                response.Message = ex.Message;
            }
            conn.Close();
            return response;
        }

        /// <summary>
        /// 取得稅單PDF路徑
        /// </summary>
        /// <param name="taxNumber"></param>
        /// <returns></returns>
        public string GetClearance_Tax_Pdf(string taxNumber)
        {
            string filePath = "";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT FilePath FROM [jetf].[dbo].[Clearance_Tax_Pdf] where TaxNumber=@TaxNumber ", conn))
            {
                da.SelectCommand.Parameters.Add("@TaxNumber", SqlDbType.NVarChar).Value = taxNumber;
                da.Fill(dt);
            }
            if (dt.Rows.Count > 0)
            {
                filePath = dt.Rows[0]["FilePath"].ToString();
            }
            dt.Dispose();
            return filePath;
        }

        /// <summary>
        /// 取得簽收單路徑
        /// </summary>
        /// <param name="taxNumber"></param>
        /// <returns></returns>
        public ArrayList GetCargo_Sign_Receipt(string cargoNumber)
        {
            ArrayList list = new ArrayList();
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT FileName FROM [jetf].[dbo].[Cargo_Sign_Receipt] where Jetf_Serial=@Jetf_Serial ", conn))
            {
                da.SelectCommand.Parameters.Add("@Jetf_Serial", SqlDbType.NVarChar).Value = cargoNumber;
                da.Fill(dt);
            }
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                list.Add(dt.Rows[i]["FileName"].ToString());
            }
            dt.Dispose();
            return list;
        }

        /// <summary>
        /// 取得簽收單路徑
        /// </summary>
        /// <param name="cargoNumber"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetCargo_Sign_Receipt(string cargoNumber, string fileName)
        {
            string filePath = "";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT FilePath FROM [jetf].[dbo].[Cargo_Sign_Receipt] where Jetf_Serial=@Jetf_Serial and FileName=@FileName ", conn))
            {
                da.SelectCommand.Parameters.Add("@Jetf_Serial", SqlDbType.NVarChar).Value = cargoNumber;
                da.SelectCommand.Parameters.Add("@FileName", SqlDbType.NVarChar).Value = fileName;
                da.Fill(dt);
            }
            if (dt.Rows.Count > 0)
            {
                filePath = dt.Rows[0]["FilePath"].ToString();
            }
            dt.Dispose();
            return filePath;
        }

        /// <summary>
        /// 查詢空快袋號
        /// </summary>
        public string GetEtlBagNumber(string tackingNo)
        {
            string bagNumber = "";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT distinct BAGNO FROM [DATA_CENTER].[dbo].[ORIGINALLIST] where TRACKINGNO=@TRACKINGNO ", conn))
            {
                da.SelectCommand.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = tackingNo;
                da.Fill(dt);
            }
            if (dt.Rows.Count > 0)
            {
                bagNumber = dt.Rows[0]["BAGNO"].ToString();
            }
            return bagNumber;
        }

        /// <summary>
        /// 查詢稅金編號
        /// </summary>
        public DataTable GetTaxNumber(string tackingNo, string bagNumber = "")
        {
            DataTable dt = new DataTable();
            if (bagNumber == "")
            {
                using (SqlDataAdapter da = new SqlDataAdapter("SELECT distinct TAX_NUMBER FROM [DATA_CENTER].[dbo].[CLEARANCE_TAX] where MERGE_NUMBER=@MERGE_NUMBER ", conn))
                {
                    da.SelectCommand.Parameters.Add("@MERGE_NUMBER", SqlDbType.NVarChar).Value = tackingNo;
                    da.Fill(dt);
                }
            }
            else
            {
                using (SqlDataAdapter da = new SqlDataAdapter("SELECT distinct TAX_NUMBER FROM [DATA_CENTER].[dbo].[CLEARANCE_TAX] where BAG_NUMBER=@BAG_NUMBER and MERGE_NUMBER='' ", conn))
                {
                    da.SelectCommand.Parameters.Add("@BAG_NUMBER", SqlDbType.NVarChar).Value = bagNumber;
                    da.Fill(dt);
                }
            }
            return dt;
        }

        /// <summary>
        /// 新增尾程配送基础资料
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        void InsertSjlShippingInformation(ShippingInformationModel model)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("insert jetf.dbo.SjlShippingInformation(Fdate, AwbNo, TrackingNumber,BigbagId, ConsigneeName, ConsigneePhone, ConsigneeAddress, PackagePic, PackageWeight, PackageLength, PackageWidth, PackageHeight, DaocuCash, CarrierType, CustomsCop, PsAccount, LogisPort, BizType, Status) ");
            sb.Append("values(@Fdate, @AwbNo, @TrackingNumber,@BigbagId, @ConsigneeName, @ConsigneePhone, @ConsigneeAddress, @PackagePic, @PackageWeight, @PackageLength, @PackageWidth, @PackageHeight, @DaocuCash, @CarrierType, @CustomsCop, @PsAccount, @LogisPort, @BizType, @Status) ");
            using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
            {
                cmd.Parameters.Clear();
                cmd.Parameters.Add("@Fdate", System.Data.SqlDbType.NVarChar).Value = model.Fdate ?? (object)DBNull.Value;
                cmd.Parameters.Add("@AwbNo", System.Data.SqlDbType.NVarChar).Value = model.AwbNo ?? (object)DBNull.Value;
                cmd.Parameters.Add("@TrackingNumber", System.Data.SqlDbType.NVarChar).Value = model.TrackingNumber ?? (object)DBNull.Value;
                cmd.Parameters.Add("@BigbagId", System.Data.SqlDbType.NVarChar).Value = model.BigbagId ?? (object)DBNull.Value;
                cmd.Parameters.Add("@ConsigneeName", System.Data.SqlDbType.NVarChar).Value = model.ConsigneeName ?? (object)DBNull.Value;
                cmd.Parameters.Add("@ConsigneePhone", System.Data.SqlDbType.NVarChar).Value = model.ConsigneePhone ?? (object)DBNull.Value;
                cmd.Parameters.Add("@ConsigneeAddress", System.Data.SqlDbType.NVarChar).Value = model.ConsigneeAddress ?? (object)DBNull.Value;
                cmd.Parameters.Add("@PackagePic", System.Data.SqlDbType.NVarChar).Value = model.PackagePic ?? (object)DBNull.Value;
                cmd.Parameters.Add("@PackageWeight", System.Data.SqlDbType.NVarChar).Value = model.PackageWeight ?? (object)DBNull.Value;
                cmd.Parameters.Add("@PackageLength", System.Data.SqlDbType.NVarChar).Value = model.PackageLength ?? (object)DBNull.Value;
                cmd.Parameters.Add("@PackageWidth", System.Data.SqlDbType.NVarChar).Value = model.PackageWidth ?? (object)DBNull.Value;
                cmd.Parameters.Add("@PackageHeight", System.Data.SqlDbType.NVarChar).Value = model.PackageHeight ?? (object)DBNull.Value;
                cmd.Parameters.Add("@DaocuCash", System.Data.SqlDbType.NVarChar).Value = model.DaocuCash ?? (object)DBNull.Value;
                cmd.Parameters.Add("@CarrierType", System.Data.SqlDbType.NVarChar).Value = model.CarrierType ?? (object)DBNull.Value;
                cmd.Parameters.Add("@CustomsCop", System.Data.SqlDbType.NVarChar).Value = model.CustomsCop ?? (object)DBNull.Value;
                cmd.Parameters.Add("@PsAccount", System.Data.SqlDbType.NVarChar).Value = model.PsAccount ?? (object)DBNull.Value;
                cmd.Parameters.Add("@LogisPort", System.Data.SqlDbType.NVarChar).Value = model.LogisPort ?? (object)DBNull.Value;
                cmd.Parameters.Add("@BizType", System.Data.SqlDbType.NVarChar).Value = model.BizType ?? (object)DBNull.Value;
                cmd.Parameters.Add("@Status", System.Data.SqlDbType.NVarChar).Value = model.Status ?? (object)DBNull.Value;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Models;
using Service.Services.Tax;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Service.Services
{
    public class UploadService : _BaseService
    {
        private readonly TaxService _taxService;
        private readonly CustomerService _customerService;
        private readonly DownloadService _downloadService;

        public UploadService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext, TaxService taxService, CustomerService customerService, DownloadService downloadService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _taxService = taxService;
            _customerService = customerService;
            _downloadService = downloadService;
        }

        /// <summary>
        /// 上傳檔案 海運-台北貨櫃、台灣港務、高雄郵務
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileType"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel UploadFile(string dataDate, string filePath, SeaTaxType taxType, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            DataTable dt_Upload = new DataTable();
            switch (taxType)
            {
                case SeaTaxType.WAHA:  //海運-萬海
                    dt_Upload = ReadExcelWaha(filePath);
                    break;
                case SeaTaxType.TIPC:  //海運-台灣港務
                    dt_Upload = ReadExcelTipc(filePath);
                    break;
                case SeaTaxType.TPCT:  //海運-台北貨櫃
                case SeaTaxType.IPOST:  //海運-高雄郵聯
                case SeaTaxType.JFKH:  //海運-高雄郵聯(捷豐)
                case SeaTaxType.CHWN:  //海運-高雄郵聯(全旺)
                case SeaTaxType.UNIJ:  //海運-連捷
                case SeaTaxType.JFKL:  //基隆港務(捷豐)
                    dt_Upload = ReadExcelIpost(filePath);
                    break;
            }

            //寫入上傳資料
            string upload_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            resopnseModel = InsertSea_Tax_Upload(dt_Upload, upload_time, userId);

            var seaTaxTypes = Enum.GetNames(typeof(SeaTaxType)).ToList();
            if (seaTaxTypes.Contains(taxType.ToString()))
            {
                //檢查上傳資料是否有少
                resopnseModel = InsertSea_Tax_Upload_Modify(dt_Upload, dataDate, taxType.ToString(), upload_time, userId);
            }

            //更新菜鳥海運、空運，稅金方式P
            resopnseModel = _downloadService.UpdateCainiaoTaxEdit();
            if (resopnseModel.status != Status.success)
            {
                return resopnseModel;
            }

            //新增
            if (dt_Upload.Rows.Count > 0)
            {
                if (resopnseModel.status == Status.success)
                {
                    //取得寫入Fee_Master資料
                    DataTable dt_Fee_Master = GetFee_Master(dt_Upload, taxType.ToString(), upload_time, userId);
                    //新增資料
                    resopnseModel = InsertFee_Master(dt_Fee_Master, dataDate, userId);
                    if (resopnseModel.status == Status.success)
                    {
                        resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
                    }
                }
            }
            else
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
            }
            conn.Close();
            return resopnseModel;
        }

        /// <summary>
        /// 上傳檔案 海運-G類資料
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileType"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel UploadFileG(string dataDate, string filePath, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            DataTable dt_Upload = ReadExcelG(filePath);

            //新增
            if (dt_Upload.Rows.Count > 0)
            {
                //寫入Fee_Master
                resopnseModel = InsertFee_MasterG(dt_Upload, dataDate, userId);

                if (resopnseModel.status == Status.success)
                {
                    resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
                }
            }
            else
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
            }

            conn.Close();
            return resopnseModel;
        }

        /// <summary>
        /// 物流代收金額上傳
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel UploadFileReceive(string filePath, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("update [jetf].[dbo].[FEE_MASTER] set DLV_COD=@DLV_COD,DLV_COD_CODE=@DLV_COD_CODE,DLV_COD_TIME=@DLV_COD_TIME,DLV_COD_OPE=@DLV_COD_OPE ");
            sb.Append("where DLV_INV=@DLV_INV ");

            StringBuilder sb_Repeat = new StringBuilder();
            sb_Repeat.Append("insert jetf.dbo.FEE_MASTER_LOG([ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER], [INS_TIME],[ARRIVAL]) ");
            sb_Repeat.Append("select [ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER],getdate() as [INS_TIME],[ARRIVAL] from jetf.dbo.FEE_MASTER where DLV_INV=@DLV_INV ");
            sb_Repeat.Append("update [jetf].[dbo].[FEE_MASTER] set DLV_COD=@DLV_COD,DLV_COD_CODE=@DLV_COD_CODE,DLV_COD_TIME=@DLV_COD_TIME,DLV_COD_OPE=@DLV_COD_OPE ");
            sb_Repeat.Append("where DLV_INV=@DLV_INV ");

            StringBuilder sb_Insert = new StringBuilder();
            sb_Insert.Append("insert [jetf].[dbo].[FEE_MASTER](SOURCE_TYPE,DLV_INV,COD,TO_DLV_COD,DLV_COD,DLV_COD_CODE,DLV_COD_TIME,DLV_COD_OPE) ");
            sb_Insert.Append("values('4',@DLV_INV,@COD,@TO_DLV_COD,@DLV_COD,@DLV_COD_CODE,@DLV_COD_TIME,@DLV_COD_OPE) ");

            string dlv_inv, dlv_cod, dlv_cod_code, to_dlv_cod, dlv_cod_time, dlv_remit_code;
            DataTable dt_Upload = ReadExcelReceive(filePath);
            SqlCommand cmd = new SqlCommand(sb.ToString(), conn);
            SqlCommand cmd_Repeat = new SqlCommand(sb_Repeat.ToString(), conn);
            SqlCommand cmd_Insert = new SqlCommand(sb_Insert.ToString(), conn);
            SqlDataAdapter da = new SqlDataAdapter("select * from [jetf].[dbo].[FEE_MASTER] where DLV_INV=@DLV_INV ", conn);
            DataTable dt_Data;

            if (dt_Upload.Rows.Count > 0)
            {
                dlv_cod_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                for (int i = 0; i < dt_Upload.Rows.Count; i++)
                {
                    //物流貨號
                    dlv_inv = dt_Upload.Rows[i]["DLV_INV"].ToString();
                    //預訂代收金額
                    dlv_cod = dt_Upload.Rows[i]["DLV_COD"].ToString();

                    da.SelectCommand.Parameters.Clear();
                    da.SelectCommand.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = dlv_inv;
                    dt_Data = new DataTable();
                    da.Fill(dt_Data);
                    if (dt_Data.Rows.Count > 0)
                    {
                        //派件公司匯款金額檢核碼
                        dlv_remit_code = dt_Data.Rows[0]["DLV_REMIT_CODE"].ToString();
                        //代收金額檢查
                        dlv_cod_code = dt_Data.Rows[0]["DLV_COD_CODE"].ToString();
                        //代收貨款金額
                        to_dlv_cod = dt_Data.Rows[0]["TO_DLV_COD"].ToString();
                        //派件公司匯款金額檢核碼 不等於Y再檢查
                        if (dlv_remit_code != "Y")
                        {
                            //第一次上傳
                            if (dlv_cod_code == "")
                            {
                                //預訂代收金額=代收貨款金額
                                if (dlv_cod == to_dlv_cod)
                                {
                                    dlv_cod_code = "Y";
                                }
                                else
                                {
                                    dlv_cod_code = "N";
                                }
                                cmd.Parameters.Clear();
                                cmd.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = dlv_inv;
                                cmd.Parameters.Add("@DLV_COD", SqlDbType.NVarChar).Value = dlv_cod;
                                cmd.Parameters.Add("@DLV_COD_CODE", SqlDbType.NVarChar).Value = dlv_cod_code;
                                cmd.Parameters.Add("@DLV_COD_TIME", SqlDbType.NVarChar).Value = dlv_cod_time;
                                cmd.Parameters.Add("@DLV_COD_OPE", SqlDbType.NVarChar).Value = userId;
                                cmd.ExecuteNonQuery();
                            }
                            else
                            {
                                //重複上傳 預定代收和之前上傳不同 才需要更新
                                if (dlv_cod != dt_Data.Rows[0]["DLV_COD"].ToString())
                                {
                                    if (dlv_cod == to_dlv_cod)
                                    {
                                        dlv_cod_code = "Y";
                                    }
                                    else
                                    {
                                        dlv_cod_code = "N";
                                    }
                                    cmd_Repeat.Parameters.Clear();
                                    cmd_Repeat.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = dlv_inv;
                                    cmd_Repeat.Parameters.Add("@DLV_COD", SqlDbType.NVarChar).Value = dlv_cod;
                                    cmd_Repeat.Parameters.Add("@DLV_COD_CODE", SqlDbType.NVarChar).Value = dlv_cod_code;
                                    cmd_Repeat.Parameters.Add("@DLV_COD_TIME", SqlDbType.NVarChar).Value = dlv_cod_time;
                                    cmd_Repeat.Parameters.Add("@DLV_COD_OPE", SqlDbType.NVarChar).Value = userId;
                                    cmd_Repeat.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    else
                    {
                        //查無資料=>新增
                        cmd_Insert.Parameters.Clear();
                        cmd_Insert.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = dlv_inv;
                        cmd_Insert.Parameters.Add("@TO_DLV_COD", SqlDbType.NVarChar).Value = dlv_cod;
                        cmd_Insert.Parameters.Add("@COD", SqlDbType.NVarChar).Value = dlv_cod;
                        cmd_Insert.Parameters.Add("@DLV_COD", SqlDbType.NVarChar).Value = dlv_cod;
                        cmd_Insert.Parameters.Add("@DLV_COD_CODE", SqlDbType.NVarChar).Value = "Y";
                        cmd_Insert.Parameters.Add("@DLV_COD_TIME", SqlDbType.NVarChar).Value = dlv_cod_time;
                        cmd_Insert.Parameters.Add("@DLV_COD_OPE", SqlDbType.NVarChar).Value = userId;
                        cmd_Insert.ExecuteNonQuery();
                    }
                }
            }


            if (dt_Upload.Rows.Count > 0)
            {
                resopnseModel.status = Status.success;
                resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
            }
            else
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
            }

            cmd.Dispose();
            cmd_Repeat.Dispose();
            conn.Close();
            return resopnseModel;
        }

        /// <summary>
        /// 物流代收匯款上傳
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel UploadFileTransfer(string filePath, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("update [jetf].[dbo].[FEE_MASTER] set DLV_REMIT_DATE=@DLV_REMIT_DATE,DLV_REMIT_AMOUT=@DLV_REMIT_AMOUT,DLV_REMIT_AMOUT_FEE=@DLV_REMIT_AMOUT_FEE,DLV_REMIT_CODE=@DLV_REMIT_CODE,DLV_REMIT_TIME=@DLV_REMIT_TIME,DLV_REMIT_OPE=@DLV_REMIT_OPE ");
            sb.Append("where DLV_INV=@DLV_INV ");

            StringBuilder sb_Repeat = new StringBuilder();
            sb_Repeat.Append("insert jetf.dbo.FEE_MASTER_LOG([ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER], [INS_TIME],[ARRIVAL]) ");
            sb_Repeat.Append("select [ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER],getdate() as [INS_TIME],[ARRIVAL] from jetf.dbo.FEE_MASTER where DLV_INV=@DLV_INV ");
            sb_Repeat.Append("update [jetf].[dbo].[FEE_MASTER] set DLV_REMIT_DATE=@DLV_REMIT_DATE,DLV_REMIT_AMOUT=@DLV_REMIT_AMOUT,DLV_REMIT_AMOUT_FEE=@DLV_REMIT_AMOUT_FEE,DLV_REMIT_CODE=@DLV_REMIT_CODE,DLV_REMIT_TIME=@DLV_REMIT_TIME,DLV_REMIT_OPE=@DLV_REMIT_OPE ");
            sb_Repeat.Append("where DLV_INV=@DLV_INV ");

            StringBuilder sb_Insert = new StringBuilder();
            sb_Insert.Append("insert [jetf].[dbo].[FEE_MASTER](SOURCE_TYPE,DLV_INV,TO_DLV_COD,COD,DLV_COD,DLV_COD_CODE,DLV_COD_TIME,DLV_COD_OPE,DLV_REMIT_DATE,DLV_REMIT_AMOUT,DLV_REMIT_AMOUT_FEE,DLV_REMIT_CODE,DLV_REMIT_TIME,DLV_REMIT_OPE) ");
            sb_Insert.Append("values('5',@DLV_INV,@TO_DLV_COD,@COD,@DLV_COD,@DLV_COD_CODE,@DLV_COD_TIME,@DLV_COD_OPE,@DLV_REMIT_DATE,@DLV_REMIT_AMOUT,@DLV_REMIT_AMOUT_FEE,@DLV_REMIT_CODE,@DLV_REMIT_TIME,@DLV_REMIT_OPE) ");

            string dlv_inv, dlv_remit_date, dlv_remit_amout, dlv_remit_amout_fee, dlv_remit_code, dlv_remit_time, to_dlv_cod;
            //讀取EXCEL
            DataTable dt_Upload = ReadExcelTransfer(filePath);
            SqlCommand cmd = new SqlCommand(sb.ToString(), conn);
            SqlCommand cmd_Repeat = new SqlCommand(sb_Repeat.ToString(), conn);
            SqlCommand cmd_Insert = new SqlCommand(sb_Insert.ToString(), conn);
            SqlDataAdapter da = new SqlDataAdapter("select * from [jetf].[dbo].[FEE_MASTER] where DLV_INV=@DLV_INV ", conn);
            DataTable dt_Data;

            if (dt_Upload.Rows.Count > 0)
            {
                dlv_remit_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                for (int i = 0; i < dt_Upload.Rows.Count; i++)
                {
                    //物流貨號
                    dlv_inv = dt_Upload.Rows[i]["DLV_INV"].ToString();
                    //匯款日期
                    dlv_remit_date = dt_Upload.Rows[i]["DLV_REMIT_DATE"].ToString();
                    //代收貨款金額
                    dlv_remit_amout = dt_Upload.Rows[i]["DLV_REMIT_AMOUT"].ToString();
                    //代收貨款手續費
                    dlv_remit_amout_fee = dt_Upload.Rows[i]["DLV_REMIT_AMOUT_FEE"].ToString();
                    da.SelectCommand.Parameters.Clear();
                    da.SelectCommand.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = dlv_inv;
                    dt_Data = new DataTable();
                    da.Fill(dt_Data);
                    if (dt_Data.Rows.Count > 0)
                    {
                        //派件公司匯款金額檢核碼
                        dlv_remit_code = dt_Data.Rows[0]["DLV_REMIT_CODE"].ToString();
                        //代收貨款金額
                        to_dlv_cod = dt_Data.Rows[0]["TO_DLV_COD"].ToString();
                        //第一次上傳
                        if (dlv_remit_code == "")
                        {
                            //預訂代收金額=代收貨款金額
                            if (dlv_remit_amout == to_dlv_cod)
                            {
                                dlv_remit_code = "Y";
                            }
                            else
                            {
                                dlv_remit_code = "N";
                            }

                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = dlv_inv;
                            cmd.Parameters.Add("@DLV_REMIT_AMOUT", SqlDbType.NVarChar).Value = dlv_remit_amout;
                            cmd.Parameters.Add("@DLV_REMIT_AMOUT_FEE", SqlDbType.NVarChar).Value = dlv_remit_amout_fee;
                            cmd.Parameters.Add("@DLV_REMIT_DATE", SqlDbType.NVarChar).Value = dlv_remit_date;
                            cmd.Parameters.Add("@DLV_REMIT_CODE", SqlDbType.NVarChar).Value = dlv_remit_code;
                            cmd.Parameters.Add("@DLV_REMIT_TIME", SqlDbType.NVarChar).Value = dlv_remit_time;
                            cmd.Parameters.Add("@DLV_REMIT_OPE", SqlDbType.NVarChar).Value = userId;
                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            //重複上傳 
                            //代收貨款金額 代收貨款手續費 匯款日期 值不同才需要更新
                            if (dlv_remit_amout != dt_Data.Rows[0]["DLV_REMIT_AMOUT"].ToString() || dlv_remit_amout_fee != dt_Data.Rows[0]["DLV_REMIT_AMOUT_FEE"].ToString() || dlv_remit_date != dt_Data.Rows[0]["DLV_REMIT_DATE"].ToString())
                            {
                                //預訂代收金額=代收貨款金額
                                if (dlv_remit_amout == to_dlv_cod)
                                {
                                    dlv_remit_code = "Y";
                                }
                                else
                                {
                                    dlv_remit_code = "N";
                                }

                                cmd_Repeat.Parameters.Clear();
                                cmd_Repeat.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = dlv_inv;
                                cmd_Repeat.Parameters.Add("@DLV_REMIT_AMOUT", SqlDbType.NVarChar).Value = dlv_remit_amout;
                                cmd_Repeat.Parameters.Add("@DLV_REMIT_AMOUT_FEE", SqlDbType.NVarChar).Value = dlv_remit_amout_fee;
                                cmd_Repeat.Parameters.Add("@DLV_REMIT_DATE", SqlDbType.NVarChar).Value = dlv_remit_date;
                                cmd_Repeat.Parameters.Add("@DLV_REMIT_CODE", SqlDbType.NVarChar).Value = dlv_remit_code;
                                cmd_Repeat.Parameters.Add("@DLV_REMIT_TIME", SqlDbType.NVarChar).Value = dlv_remit_time;
                                cmd_Repeat.Parameters.Add("@DLV_REMIT_OPE", SqlDbType.NVarChar).Value = userId;
                                cmd_Repeat.ExecuteNonQuery();
                            }
                        }
                    }
                    else
                    {
                        //查無資料=>新增
                        cmd_Insert.Parameters.Clear();
                        cmd_Insert.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = dlv_inv;

                        cmd_Insert.Parameters.Add("@TO_DLV_COD", SqlDbType.NVarChar).Value = dlv_remit_amout;
                        cmd_Insert.Parameters.Add("@COD", SqlDbType.NVarChar).Value = dlv_remit_amout;
                        cmd_Insert.Parameters.Add("@DLV_COD", SqlDbType.NVarChar).Value = dlv_remit_amout;
                        cmd_Insert.Parameters.Add("@DLV_COD_CODE", SqlDbType.NVarChar).Value = "Y";
                        cmd_Insert.Parameters.Add("@DLV_COD_TIME", SqlDbType.NVarChar).Value = dlv_remit_time;
                        cmd_Insert.Parameters.Add("@DLV_COD_OPE", SqlDbType.NVarChar).Value = userId;

                        cmd_Insert.Parameters.Add("@DLV_REMIT_AMOUT", SqlDbType.NVarChar).Value = dlv_remit_amout;
                        cmd_Insert.Parameters.Add("@DLV_REMIT_AMOUT_FEE", SqlDbType.NVarChar).Value = dlv_remit_amout_fee;
                        cmd_Insert.Parameters.Add("@DLV_REMIT_DATE", SqlDbType.NVarChar).Value = dlv_remit_date;
                        cmd_Insert.Parameters.Add("@DLV_REMIT_CODE", SqlDbType.NVarChar).Value = "Y";
                        cmd_Insert.Parameters.Add("@DLV_REMIT_TIME", SqlDbType.NVarChar).Value = dlv_remit_time;
                        cmd_Insert.Parameters.Add("@DLV_REMIT_OPE", SqlDbType.NVarChar).Value = userId;
                        cmd_Insert.ExecuteNonQuery();
                    }
                }
            }


            if (dt_Upload.Rows.Count > 0)
            {
                resopnseModel.status = Status.success;
                resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
            }
            else
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
            }

            cmd.Dispose();
            cmd_Repeat.Dispose();
            conn.Close();
            return resopnseModel;
        }

        /// <summary>
        /// 上傳檔案 回倉重出貨明細表
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileType"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel UploadFileAgainCargo(string filePath, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            //讀取檔案
            DataTable dt_Upload = ReadExcelAgainCargo(filePath);

            //新增
            if (dt_Upload.Rows.Count > 0)
            {
                //寫入
                resopnseModel = InsertAgainCargo(dt_Upload, userId);

                if (resopnseModel.status == Status.success)
                {
                    resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
                }
            }
            else
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
            }

            conn.Close();
            return resopnseModel;
        }

        /// <summary>
        /// 菜鳥包稅稅金方式修改上傳
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel CainiaoTaxEdit(string filePath, string fileName, string source, string column, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            //讀取檔案
            DataTable dt_Upload = ReadExcelCainiaoTaxEdit(filePath);

            //新增
            if (dt_Upload.Rows.Count > 0)
            {
                //寫入資料
                string upload_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                resopnseModel = InsertCainiaoTaxEdit(dt_Upload, source, upload_time, userId);

                if (resopnseModel.status == Status.success)
                {
                    //更新稅金方式
                    if (source == "Sea")
                    {
                        //海運-分提單號
                        if (column == "TrackingNo")
                        {
                            resopnseModel = UpdateCainiaoTaxEditSea(upload_time, userId);
                        }
                        else
                        {
                            //物流貨號
                            resopnseModel = UpdateCainiaoTaxEditSea_JetfSerial(upload_time, userId);
                        }
                    }
                    else
                    {
                        //空運
                        resopnseModel = UpdateCainiaoTaxEditEtl(upload_time, userId);
                    }

                    if (resopnseModel.status == Status.success)
                    {
                        resopnseModel.msg = $"{upload_time}︿{userId}︿{source}︿{column}";
                    }
                }
            }
            else
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
            }

            conn.Close();
            return resopnseModel;
        }

        /// <summary>
        /// 讀取菜鳥包稅稅金方式修改上傳檔案
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadExcelCainiaoTaxEdit(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = new DataTable();
            dt_Data.Columns.Add("TrackingNo", typeof(string));

            bool read = false;
            string trackingno;
            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    //分提單號
                    trackingno = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "分提單號")
                    {
                        read = true;
                        continue;
                    }
                    if (read && trackingno != "")
                    {
                        dr = dt_Data.NewRow();
                        dr["TrackingNo"] = trackingno;
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        /// <summary>
        /// 新增菜鳥包稅稅金方式修改
        /// </summary>
        /// <param name="dt_Upload"></param>
        /// <param name="upload_time"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public ResponseModel InsertCainiaoTaxEdit(DataTable dt_Upload, string source, string upload_time, string user_Id)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "新增成功";

            DateTime date = DateTime.Now;
            string dataDate = date.ToString("yyyyMMdd");
            StringBuilder sb = new StringBuilder();
            sb.Append("insert [jetf].[dbo].[CainiaoTaxEdit]([Source],TrackingNo, Upload_Time, Upload_Ope) ");
            sb.Append("values(@Source,@TrackingNo, @Upload_Time, @Upload_Ope) ");

            using (SqlTransaction tran = conn.BeginTransaction())
            {
                using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                {
                    cmd.Transaction = tran;
                    try
                    {
                        for (int i = 0; i < dt_Upload.Rows.Count; i++)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@Source", SqlDbType.NVarChar).Value = source;
                            cmd.Parameters.Add("@TrackingNo", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["TrackingNo"].ToString();
                            cmd.Parameters.Add("@Upload_Time", SqlDbType.NVarChar).Value = upload_time;
                            cmd.Parameters.Add("@Upload_Ope", SqlDbType.NVarChar).Value = user_Id;
                            cmd.ExecuteNonQuery();
                        }
                        //確認寫入
                        tran.Commit();
                        resopnseModel.status = Status.success;
                    }
                    catch (Exception ex)
                    {
                        resopnseModel.status = Status.error;
                        resopnseModel.msg = ex.Message;
                        //取消寫入
                        tran.Rollback();
                    }
                }
            }

            return resopnseModel;
        }

        /// <summary>
        /// 修改菜鳥包稅稅金方式-海運-分提單號
        /// </summary>
        /// <param name="source"></param>
        /// <param name="upload_time"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public ResponseModel UpdateCainiaoTaxEditSea(string upload_time, string user_Id)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "更新成功";

            StringBuilder sb = new StringBuilder();
            sb.Append("update DATA_CENTER.dbo.SEA_ORDER_ORIGINAL set TAX_PAYMENT='P',TRANS_TAXPAYMENT=rtrim(TRANS_NAME)+'P' ");
            sb.Append("from [jetf].[dbo].[CainiaoTaxEdit] a,[DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] b ");
            sb.Append("where a.TrackingNo=b.BL_NO and a.Upload_Ope=@Upload_Ope and a.Upload_Time=@Upload_Time ");

            //更新貨況查詢
            sb.Append("update jetf.dbo.MERGE_ORIGINALLIST set TAX_PAYMENT='P',TRANS_TAXPAYMENT=rtrim(TRANS_NAME)+'P' ");
            sb.Append("from [jetf].[dbo].[CainiaoTaxEdit] a,[jetf].[dbo].[MERGE_ORIGINALLIST] b ");
            sb.Append("where b.ORIGINAL='SEA' and a.TrackingNo=b.BL_NO and a.Upload_Ope=@Upload_Ope and a.Upload_Time=@Upload_Time ");

            using (SqlTransaction tran = conn.BeginTransaction())
            {
                using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                {
                    cmd.Transaction = tran;
                    try
                    {
                        cmd.Parameters.Clear();
                        cmd.CommandTimeout = 1200;
                        cmd.Parameters.Add("@Upload_Time", SqlDbType.NVarChar).Value = upload_time;
                        cmd.Parameters.Add("@Upload_Ope", SqlDbType.NVarChar).Value = user_Id;
                        cmd.ExecuteNonQuery();
                        //確認寫入
                        tran.Commit();
                        resopnseModel.status = Status.success;
                    }
                    catch (Exception ex)
                    {
                        resopnseModel.status = Status.error;
                        resopnseModel.msg = ex.Message;
                        //取消寫入
                        tran.Rollback();
                    }
                }
            }
            return resopnseModel;
        }

        /// <summary>
        /// 修改菜鳥包稅稅金方式-海運-物流貨號
        /// </summary>
        /// <param name="source"></param>
        /// <param name="upload_time"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public ResponseModel UpdateCainiaoTaxEditSea_JetfSerial(string upload_time, string user_Id)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "更新成功";

            StringBuilder sb = new StringBuilder();
            sb.Append("update DATA_CENTER.dbo.SEA_ORDER_ORIGINAL set TAX_PAYMENT='P',TRANS_TAXPAYMENT=rtrim(TRANS_NAME)+'P' ");
            sb.Append("from [jetf].[dbo].[CainiaoTaxEdit] a,[DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] b ");
            sb.Append("where a.TrackingNo=b.JETF_SERIAL and a.Upload_Ope=@Upload_Ope and a.Upload_Time=@Upload_Time ");
            //更新貨況查詢
            sb.Append("update jetf.dbo.MERGE_ORIGINALLIST set TAX_PAYMENT='P',TRANS_TAXPAYMENT=rtrim(TRANS_NAME)+'P' ");
            sb.Append("from [jetf].[dbo].[CainiaoTaxEdit] a,[jetf].[dbo].[MERGE_ORIGINALLIST] b ");
            sb.Append("where b.ORIGINAL='SEA' and a.TrackingNo=b.JETF_SERIAL and a.Upload_Ope=@Upload_Ope and a.Upload_Time=@Upload_Time ");

            using (SqlTransaction tran = conn.BeginTransaction())
            {
                using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                {
                    cmd.Transaction = tran;
                    try
                    {
                        cmd.Parameters.Clear();
                        cmd.CommandTimeout = 600;
                        cmd.Parameters.Add("@Upload_Time", SqlDbType.NVarChar).Value = upload_time;
                        cmd.Parameters.Add("@Upload_Ope", SqlDbType.NVarChar).Value = user_Id;
                        cmd.ExecuteNonQuery();
                        //確認寫入
                        tran.Commit();
                        resopnseModel.status = Status.success;
                    }
                    catch (Exception ex)
                    {
                        resopnseModel.status = Status.error;
                        resopnseModel.msg = ex.Message;
                        //取消寫入
                        tran.Rollback();
                    }
                }
            }
            return resopnseModel;
        }

        /// <summary>
        /// 修改菜鳥包稅稅金方式-空運
        /// </summary>
        /// <param name="source"></param>
        /// <param name="upload_time"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public ResponseModel UpdateCainiaoTaxEditEtl(string upload_time, string user_Id)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "更新成功";

            StringBuilder sb = new StringBuilder();
            sb.Append("update DATA_CENTER.dbo.ORIGINALLIST set TAX_PAYMENT='P',TRANS_TAXPAYMENT=rtrim(CLEARANCEWAREHOUSING)+'P' ");
            sb.Append("from [jetf].[dbo].[CainiaoTaxEdit] a,[DATA_CENTER].[dbo].[ORIGINALLIST] b ");
            sb.Append("where a.TrackingNo=b.TrackingNo and a.Upload_Ope=@Upload_Ope and a.Upload_Time=@Upload_Time ");
            //更新貨況查詢
            sb.Append("update jetf.dbo.MERGE_ORIGINALLIST set TAX_PAYMENT='P',TRANS_TAXPAYMENT=rtrim(CLEARANCEWAREHOUSING)+'P' ");
            sb.Append("from [jetf].[dbo].[CainiaoTaxEdit] a,[jetf].[dbo].[MERGE_ORIGINALLIST] b ");
            sb.Append("where b.ORIGINAL='ETL' and a.TrackingNo=b.JETF_SERIAL and a.Upload_Ope=@Upload_Ope and a.Upload_Time=@Upload_Time ");

            using (SqlTransaction tran = conn.BeginTransaction())
            {
                using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                {
                    cmd.Transaction = tran;
                    try
                    {
                        cmd.Parameters.Clear();
                        cmd.CommandTimeout = 600;
                        cmd.Parameters.Add("@Upload_Time", SqlDbType.NVarChar).Value = upload_time;
                        cmd.Parameters.Add("@Upload_Ope", SqlDbType.NVarChar).Value = user_Id;
                        cmd.ExecuteNonQuery();
                        //確認寫入
                        tran.Commit();
                        resopnseModel.status = Status.success;
                    }
                    catch (Exception ex)
                    {
                        resopnseModel.status = Status.error;
                        resopnseModel.msg = ex.Message;
                        //取消寫入
                        tran.Rollback();
                    }
                }
            }
            return resopnseModel;
        }


        /// <summary>
        /// 取得菜鳥包稅稅金方式修改
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public DataTableModel GetCainiaoTaxEdit(string source, string column, string upload_time, string user_Id)
        {
            DataTable dt = new DataTable();
            DataTableModel dataTableModel = new DataTableModel();
            dataTableModel.status = Status.success;
            dataTableModel.msg = "成功";
            try
            {
                StringBuilder sb = new StringBuilder();
                if (source == "Sea")
                {
                    //海運-分提單號
                    if (column == "TrackingNo")
                    {
                        sb.Append("SELECT a.TrackingNo,b.TRANS_NAME,b.TAX_PAYMENT,b.TRANS_TAXPAYMENT FROM [jetf].[dbo].[CainiaoTaxEdit] a ");
                        sb.Append("left join DATA_CENTER.dbo.SEA_ORDER_ORIGINAL b on a.TrackingNo=b.BL_NO ");
                        sb.Append("where a.Upload_Ope=@Upload_Ope and a.Upload_Time=@Upload_Time ");
                        sb.Append("group by a.Id,a.TrackingNo,b.TRANS_NAME,b.TAX_PAYMENT,b.TRANS_TAXPAYMENT ");
                        sb.Append("order by a.Id ");
                    }
                    else
                    {
                        //海運-物流貨號
                        sb.Append("SELECT a.TrackingNo,b.TRANS_NAME,b.TAX_PAYMENT,b.TRANS_TAXPAYMENT FROM [jetf].[dbo].[CainiaoTaxEdit] a ");
                        sb.Append("left join DATA_CENTER.dbo.SEA_ORDER_ORIGINAL b on a.TrackingNo=b.JETF_SERIAL ");
                        sb.Append("where a.Upload_Ope=@Upload_Ope and a.Upload_Time=@Upload_Time ");
                        sb.Append("group by a.Id,a.TrackingNo,b.TRANS_NAME,b.TAX_PAYMENT,b.TRANS_TAXPAYMENT ");
                        sb.Append("order by a.Id ");
                    }

                }
                else
                {
                    //空運
                    sb.Append("SELECT a.TrackingNo,b.TAX_PAYMENT,b.TRANS_TAXPAYMENT FROM [jetf].[dbo].[CainiaoTaxEdit] a ");
                    sb.Append("left join DATA_CENTER.dbo.ORIGINALLIST b on a.TrackingNo=b.TrackingNo ");
                    sb.Append("where a.Upload_Ope=@Upload_Ope and a.Upload_Time=@Upload_Time ");
                    sb.Append("order by a.Id ");
                }
                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.CommandTimeout = 600;
                    da.SelectCommand.Parameters.Add("@Upload_Ope", SqlDbType.NVarChar).Value = user_Id;
                    da.SelectCommand.Parameters.Add("@Upload_Time", SqlDbType.NVarChar).Value = upload_time;
                    da.Fill(dt);
                }
                dataTableModel.dt = dt;
            }
            catch (Exception ex)
            {
                dataTableModel.status = Status.error;
                dataTableModel.msg = ex.Message;
            }

            return dataTableModel;
        }

        public ResponseModel InsertSea_Tax_Upload(DataTable dt_Upload, string upload_Time, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            //新增SEA_TAX_UPLOAD
            StringBuilder sb = new StringBuilder();
            sb.Append("insert [jetf].[dbo].[SEA_TAX_UPLOAD](MAIN_NUMBER,CLEARANCE_NUMBER,CLEARANCE_TYPE,BL_NO,REG_NO,MAINFEST,TAX_NUMBER,TAX_PAYER,TAX,PRT_TIME,UPLOAD_TIME,UPLOAD_OPE,TAX_RECID) ");
            sb.Append("values(@MAIN_NUMBER,@CLEARANCE_NUMBER,@CLEARANCE_TYPE,@BL_NO,@REG_NO,@MAINFEST,@TAX_NUMBER,@TAX_PAYER,@TAX,@PRT_TIME,@UPLOAD_TIME,@UPLOAD_OPE,@TAX_RECID) ");
            if (dt_Upload.Rows.Count > 0)
            {
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    SqlCommand cmd = new SqlCommand(sb.ToString(), conn);
                    cmd.Transaction = tran;
                    try
                    {
                        for (int i = 0; i < dt_Upload.Rows.Count; i++)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@MAIN_NUMBER", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["main_number"].ToString();
                            cmd.Parameters.Add("@CLEARANCE_NUMBER", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["clearance_number"].ToString();
                            cmd.Parameters.Add("@CLEARANCE_TYPE", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["clearance_type"] ?? DBNull.Value;
                            cmd.Parameters.Add("@BL_NO", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["bl_no"].ToString();
                            cmd.Parameters.Add("@REG_NO", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["reg_no"] ?? DBNull.Value;
                            cmd.Parameters.Add("@MAINFEST", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["mainfest"] ?? DBNull.Value;
                            cmd.Parameters.Add("@TAX_NUMBER", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["tax_number"] ?? DBNull.Value;
                            cmd.Parameters.Add("@TAX_PAYER", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["tax_payer"] ?? DBNull.Value;
                            cmd.Parameters.Add("@TAX_RECID", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["tax_recid"] ?? DBNull.Value;
                            cmd.Parameters.Add("@TAX", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["tax"].ToString();
                            cmd.Parameters.Add("@PRT_TIME", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["prt_time"] ?? DBNull.Value;
                            cmd.Parameters.Add("@UPLOAD_TIME", SqlDbType.NVarChar).Value = upload_Time;
                            cmd.Parameters.Add("@UPLOAD_OPE", SqlDbType.NVarChar).Value = userId;
                            cmd.ExecuteNonQuery();
                        }
                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();

                        resopnseModel.status = Status.error;
                        resopnseModel.msg = ex.Message;
                    }
                }
            }
            return resopnseModel;
        }

        public ResponseModel InsertSea_Tax_Upload_Modify(DataTable dt_Upload, string dataDate, string taxType, string upload_Time, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();
            DataRow dr_Upload;
            DataTable dt_Modify = new DataTable();
            var sql = @"
select * from DATA_CENTER.dbo.CLEARANCE_TAX a
where DATA_TYPE=@DATA_TYPE and MODIFY_TIME between @SDate and @EDate
and not exists ( 
select 1 from jetf.[dbo].[SEA_TAX_UPLOAD]  
where UPLOAD_TIME=@UPLOAD_TIME and UPLOAD_OPE=@UPLOAD_OPE
and a.BAG_NUMBER=BL_NO and a.MAIN_NUMBER = MAIN_NUMBER) 
";

            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@SDate", SqlDbType.DateTime).Value = DateTime.ParseExact($"{dataDate}000000", "yyyyMMddHHmmss", null);
                da.SelectCommand.Parameters.Add("@EDate", SqlDbType.DateTime).Value = DateTime.ParseExact($"{dataDate}235959", "yyyyMMddHHmmss", null); ;
                da.SelectCommand.Parameters.Add("@DATA_TYPE", SqlDbType.NVarChar).Value = taxType;
                da.SelectCommand.Parameters.Add("@UPLOAD_TIME", SqlDbType.NVarChar).Value = upload_Time;
                da.SelectCommand.Parameters.Add("@UPLOAD_OPE", SqlDbType.NVarChar).Value = userId;
                da.SelectCommand.CommandTimeout = 600;
                da.Fill(dt_Modify);
            }

            using (SqlTransaction tran = conn.BeginTransaction())
            {
                try
                {
                    if (dt_Modify.Rows.Count > 0)
                    {
                        //新增FEE_MASTER_MODIFY
                        sql = @"
delete from jetf.dbo.FEE_MASTER_MODIFY where DATA_TYPE=@DATA_TYPE and MODIFY_DATADATE=@MODIFY_DATADATE 
insert jetf.dbo.FEE_MASTER_MODIFY 
select @MODIFY_DATADATE,a.ROW_ID, DATA_TYPE, MAIN_NUMBER, BAG_NUMBER, MERGE_NUMBER, TAX_NUMBER, TAX_BASE, TAX_AMOUNT, FREQ_SIGN, a.STATUS, MODIFY_SEQ, MODIFY_FILE, MODIFY_TIME,b.JETF_SERIAL 
from DATA_CENTER.dbo.CLEARANCE_TAX a 
left join DATA_CENTER.dbo.SEA_ORDER_ORIGINAL b on b.MAINNUMBER =a.MAIN_NUMBER and b.BL_NO=a.BAG_NUMBER and MODIFTYDATE  =(select MAX(MODIFTYDATE)from DATA_CENTER.dbo.SEA_ORDER_ORIGINAL where BL_NO=a.BAG_NUMBER) and GW > 0 
where DATA_TYPE=@DATA_TYPE and MODIFY_TIME between @SDate and @EDate 
and not exists ( 
select 1 from jetf.[dbo].[SEA_TAX_UPLOAD]  
where UPLOAD_TIME=@UPLOAD_TIME and UPLOAD_OPE=@UPLOAD_OPE 
and a.BAG_NUMBER=BL_NO and a.MAIN_NUMBER = MAIN_NUMBER) 
";
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Transaction = tran;
                            cmd.Parameters.Add("@MODIFY_DATADATE", SqlDbType.NVarChar).Value = dataDate;
                            cmd.Parameters.Add("@SDate", SqlDbType.DateTime).Value = DateTime.ParseExact($"{dataDate}000000", "yyyyMMddHHmmss", null);
                            cmd.Parameters.Add("@EDate", SqlDbType.DateTime).Value = DateTime.ParseExact($"{dataDate}235959", "yyyyMMddHHmmss", null); ;
                            cmd.Parameters.Add("@DATA_TYPE", SqlDbType.NVarChar).Value = taxType;
                            cmd.Parameters.Add("@UPLOAD_TIME", SqlDbType.NVarChar).Value = upload_Time;
                            cmd.Parameters.Add("@UPLOAD_OPE", SqlDbType.NVarChar).Value = userId;
                            cmd.CommandTimeout = 600;
                            cmd.ExecuteNonQuery();
                        }
                    }

                    //新增SEA_TAX_UPLOAD
                    StringBuilder sb_Insert = new StringBuilder();
                    sb_Insert.Append("insert [jetf].[dbo].[SEA_TAX_UPLOAD](MAIN_NUMBER,BL_NO,TAX,TAX_NUMBER,UPLOAD_TIME,UPLOAD_OPE) ");
                    sb_Insert.Append("values(@MAIN_NUMBER,@BL_NO,@TAX,@TAX_NUMBER,@UPLOAD_TIME,@UPLOAD_OPE) ");

                    using (SqlCommand cmd = new SqlCommand(sb_Insert.ToString(), conn))
                    {
                        cmd.Transaction = tran;

                        for (int i = 0; i < dt_Modify.Rows.Count; i++)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@MAIN_NUMBER", SqlDbType.NVarChar).Value = dt_Modify.Rows[i]["MAIN_NUMBER"].ToString();
                            cmd.Parameters.Add("@BL_NO", SqlDbType.NVarChar).Value = dt_Modify.Rows[i]["BAG_NUMBER"].ToString();
                            cmd.Parameters.Add("@TAX", SqlDbType.NVarChar).Value = dt_Modify.Rows[i]["TAX_AMOUNT"].ToString();
                            cmd.Parameters.Add("@TAX_NUMBER", SqlDbType.NVarChar).Value = dt_Modify.Rows[i]["TAX_NUMBER"].ToString();
                            cmd.Parameters.Add("@UPLOAD_TIME", SqlDbType.NVarChar).Value = upload_Time;
                            cmd.Parameters.Add("@UPLOAD_OPE", SqlDbType.NVarChar).Value = userId;
                            cmd.ExecuteNonQuery();

                            //新增到上傳table資料，不然併單會無法比對
                            dr_Upload = dt_Upload.NewRow();
                            dr_Upload["main_number"] = dt_Modify.Rows[i]["MAIN_NUMBER"].ToString();
                            dr_Upload["bl_no"] = dt_Modify.Rows[i]["BAG_NUMBER"].ToString();
                            dr_Upload["tax"] = dt_Modify.Rows[i]["TAX_AMOUNT"].ToString();
                            dr_Upload["tax_number"] = dt_Modify.Rows[i]["TAX_NUMBER"].ToString();
                            dt_Upload.Rows.Add(dr_Upload);
                        }
                        tran.Commit();
                        resopnseModel.status = Status.success;
                    }
                }
                catch (Exception ex)
                {
                    tran.Rollback();

                    resopnseModel.status = Status.error;
                    resopnseModel.msg = ex.Message;
                }
            }
            return resopnseModel;
        }

        ResponseModel InsertFee_Master(DataTable dt_Fee_Master, string dataDate, string userId)
        {
            //DataRow dr_error;
            //int error = 0;
            ResponseModel resopnseModel = new ResponseModel();
            using (SqlTransaction tran = conn.BeginTransaction())
            {
                //新增FEE_MASTER
                StringBuilder sb = new StringBuilder();
                sb.Append("insert [jetf].[dbo].[FEE_MASTER](DATADATE,SOURCE,SOURCE_TYPE,TYPE, CUSTOMER, MAIN_NUMBER, TRACKINGNO, CLEARANCE_NUMBER,COMBINE, IN_DATE, IN_DATETIME, OUT_DATETIME,TAX_BASE,TAX1, TAX2, DLV_COM,TAX_NUMBER,FEE,INCLUDE_TAX,RECIPIENT,RECPHONE,RECADDRESS,RECID,COD,TO_DLV_COD,DLV_INV,TAX_PAYER,ARRIVAL,CUSTOMER_COD,TRANS_COD,TAX_RECID) ");
                sb.Append("values(@DATADATE,@SOURCE,@SOURCE_TYPE,@TYPE,@CUSTOMER,@MAIN_NUMBER,@TRACKINGNO,@CLEARANCE_NUMBER,@COMBINE,@IN_DATE,@IN_DATETIME,@OUT_DATETIME,@TAX_BASE,@TAX1,@TAX2,@DLV_COM,@TAX_NUMBER,@FEE,@INCLUDE_TAX,@RECIPIENT,@RECPHONE,@RECADDRESS,@RECID,@COD,@TO_DLV_COD,@DLV_INV,@TAX_PAYER,@ARRIVAL,@CUSTOMER_COD,@TRANS_COD,@TAX_RECID) ");

                try
                {
                    if (dt_Fee_Master.Rows.Count > 0)
                    {
                        StringBuilder sb_Delete = new StringBuilder();
                        sb_Delete.Append("insert jetf.dbo.FEE_MASTER_LOG([ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER], [INS_TIME],[ARRIVAL],[CUSTOMER_COD],[TRANS_COD]) ");
                        sb_Delete.Append("select [ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER],getdate() as [INS_TIME],[ARRIVAL],[CUSTOMER_COD],[TRANS_COD] from jetf.dbo.FEE_MASTER where DATADATE=@DATADATE and [SOURCE]=@SOURCE and SOURCE_TYPE='1' ");
                        sb_Delete.Append("delete from jetf.dbo.FEE_MASTER where DATADATE=@DATADATE and [SOURCE]=@SOURCE and SOURCE_TYPE='1' ");
                        //刪除資料
                        using (SqlCommand cmd = new SqlCommand(sb_Delete.ToString(), conn))
                        {
                            cmd.Transaction = tran;
                            cmd.Parameters.Add("@DATADATE", SqlDbType.NVarChar).Value = dataDate;
                            cmd.Parameters.Add("@SOURCE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[0]["source"].ToString();
                            cmd.ExecuteNonQuery();
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                    {
                        cmd.Transaction = tran;
                        for (int i = 0; i < dt_Fee_Master.Rows.Count; i++)
                        {
                            //error = i;
                            var dr_error = dt_Fee_Master.Rows[i];
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@DATADATE", SqlDbType.NVarChar).Value = dataDate;
                            cmd.Parameters.Add("@SOURCE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["source"].ToString();
                            cmd.Parameters.Add("@SOURCE_TYPE", SqlDbType.NVarChar).Value = "1";
                            cmd.Parameters.Add("@TYPE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["type"].ToString();
                            cmd.Parameters.Add("@CUSTOMER", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["customer"].ToString();
                            cmd.Parameters.Add("@MAIN_NUMBER", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["main_number"].ToString();
                            cmd.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["trackingno"].ToString();
                            cmd.Parameters.Add("@CLEARANCE_NUMBER", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["clearance_number"].ToString();
                            cmd.Parameters.Add("@COMBINE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["combine"].ToString();
                            cmd.Parameters.Add("@IN_DATE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["in_date"].ToString();
                            cmd.Parameters.Add("@IN_DATETIME", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["in_datetime"] ?? DBNull.Value;
                            cmd.Parameters.Add("@OUT_DATETIME", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["out_datetime"] ?? DBNull.Value;
                            cmd.Parameters.Add("@TAX_BASE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["tax_base"].ToString();
                            cmd.Parameters.Add("@TAX1", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["tax1"].ToString();
                            cmd.Parameters.Add("@TAX2", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["tax2"].ToString();
                            cmd.Parameters.Add("@DLV_COM", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["dlv_com"].ToString();
                            cmd.Parameters.Add("@TAX_NUMBER", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["tax_number"].ToString();
                            cmd.Parameters.Add("@COD", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["cod"].ToString();
                            cmd.Parameters.Add("@FEE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["fee"].ToString();
                            cmd.Parameters.Add("@INCLUDE_TAX", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["include_tax"].ToString();
                            cmd.Parameters.Add("@RECIPIENT", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["recipient"].ToString();
                            cmd.Parameters.Add("@RECPHONE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["recphone"].ToString();
                            cmd.Parameters.Add("@RECADDRESS", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["recaddress"].ToString();
                            cmd.Parameters.Add("@RECID", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["recid"].ToString();
                            cmd.Parameters.Add("@TO_DLV_COD", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["to_dlv_cod"].ToString();
                            cmd.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["dlv_inv"].ToString();
                            cmd.Parameters.Add("@TAX_PAYER", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["tax_payer"] ?? DBNull.Value;
                            cmd.Parameters.Add("@TAX_RECID", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["tax_recid"] ?? DBNull.Value;
                            cmd.Parameters.Add("@ARRIVAL", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["arrival"] ?? DBNull.Value;
                            cmd.Parameters.Add("@CUSTOMER_COD", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["Customer_Cod"].ToString();
                            cmd.Parameters.Add("@TRANS_COD", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["Trans_Cod"].ToString();
                            cmd.ExecuteNonQuery();
                        }
                        //確認寫入
                        tran.Commit();
                        resopnseModel.status = Status.success;
                    }
                }
                catch (Exception ex)
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = ex.Message;
                    //取消寫入
                    tran.Rollback();
                }
            }
            return resopnseModel;
        }

        /// <summary>
        /// 寫入上傳檔案 G類資料
        /// </summary>
        /// <param name="dt_Fee_Master"></param>
        /// <param name="userId"></param>
        ResponseModel InsertFee_MasterG(DataTable dt_Fee_Master, string dataDate, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

            using (SqlTransaction tran = conn.BeginTransaction())
            {
                //新增FEE_MASTER
                StringBuilder sb = new StringBuilder();
                sb.Append("declare @Select_DLV_REMIT_CODE nvarchar(2) ");
                sb.Append("declare @Select_SOURCE_TYPE nvarchar(2) ");
                sb.Append("declare @Select_DATADATE nvarchar(8) ");
                sb.Append("select * from [jetf].[dbo].[FEE_MASTER] where SOURCE_TYPE='2' and DLV_INV=@DLV_INV ");
                sb.Append("if @@ROWCOUNT>0 ");
                sb.Append("begin ");
                //sb.Append(" 	insert jetf.dbo.FEE_MASTER_LOG([ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER], [INS_TIME],[ARRIVAL],[CUSTOMER_COD],[TRANS_COD]) ");
                //sb.Append(" 	select [ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER],getdate() as [INS_TIME],[ARRIVAL],[CUSTOMER_COD],[TRANS_COD] from jetf.dbo.FEE_MASTER where SOURCE_TYPE='2' and DLV_INV=@DLV_INV ");
                sb.Append("        delete detail from [jetf].[dbo].[FEE_MASTER_DETAIL] detail ");
                sb.Append("        inner join [jetf].[dbo].[FEE_MASTER] master on master.ID=detail.FEE_MASTER_ID ");
                sb.Append("        where master.SOURCE_TYPE='2' and master.DLV_INV=@DLV_INV ");
                sb.Append("	    delete from [jetf].[dbo].[FEE_MASTER] where SOURCE_TYPE='2' and DLV_INV=@DLV_INV ");
                sb.Append("end ");
                sb.Append("select @Select_DATADATE=DATADATE,@Select_SOURCE_TYPE=SOURCE_TYPE,@Select_DLV_REMIT_CODE=DLV_REMIT_CODE from [jetf].[dbo].[FEE_MASTER] where SOURCE_TYPE='1' and DLV_INV=@DLV_INV ");
                sb.Append("if @@ROWCOUNT>0 ");
                sb.Append("begin ");
                sb.Append("     insert FEE_MASTER_MODIFY_G([MODIFY_DATADATE], [ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER], [MEMO], [INS_TIME],[ARRIVAL],[CUSTOMER_COD],[TRANS_COD]) ");
                sb.Append("     select @MODIFY_DATADATE,[ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER],'刪除' as [MEMO],getdate() as [INS_TIME],[ARRIVAL],[CUSTOMER_COD],[TRANS_COD] from jetf.dbo.FEE_MASTER where SOURCE_TYPE='1' and DLV_INV=@DLV_INV ");
                sb.Append("     update [jetf].[dbo].[FEE_MASTER] set Download='0' where SOURCE_TYPE='1' and DLV_INV=@DLV_INV ");
                sb.Append("end ");
                sb.Append("     insert [jetf].[dbo].[FEE_MASTER](DATADATE,SOURCE,SOURCE_TYPE,CUSTOMER,TRACKINGNO,TYPE,DLV_INV,OUT_DATETIME,TAX1,FEE,CCFEE,COD,INCLUDE_TAX,TO_DLV_COD,RECIPIENT,TRANS_COD) ");
                sb.Append("     values(@DATADATE,@SOURCE,@SOURCE_TYPE,@CUSTOMER,@TRACKINGNO,@TYPE,@DLV_INV,@OUT_DATETIME,@TAX1,@FEE,@CCFEE,@COD,@INCLUDE_TAX,@TO_DLV_COD,@RECIPIENT,@TRANS_COD) ");
                sb.Append("     declare @FeeMasterId int=cast(scope_identity() as int) ");
                sb.Append("     insert [jetf].[dbo].[FEE_MASTER_DETAIL](FEE_MASTER_ID,MAIN_NUMBER,TRACKINGNO,CLEARANCE_NUMBER,BAG_NUMBER,TAX_NUMBER,TAX_PAYER,TAX_RECID,DLV_INV,TAX_BASE,TAX,CCFEE,COD,FEE,RECIPIENT,RECPHONE,RECADDRESS,TO_DLV_COD,TRANS_COD,CUSTOMER_COD) ");
                sb.Append("     select ID,MAIN_NUMBER,TRACKINGNO,CLEARANCE_NUMBER,BAG_NUMBER,TAX_NUMBER,TAX_PAYER,TAX_RECID,DLV_INV,TAX_BASE,TAX1,CCFEE,COD,FEE,RECIPIENT,RECPHONE,RECADDRESS,TO_DLV_COD,TRANS_COD,CUSTOMER_COD ");
                sb.Append("     from [jetf].[dbo].[FEE_MASTER] where ID=@FeeMasterId ");
                sb.Append("     insert FEE_MASTER_MODIFY_G([MODIFY_DATADATE], [ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER], [MEMO], [INS_TIME],[ARRIVAL],[CUSTOMER_COD],[TRANS_COD]) ");
                sb.Append("     select @MODIFY_DATADATE,[ID], [DATADATE], [SOURCE], [SOURCE_TYPE], [TYPE], [CUSTOMER], [MAIN_NUMBER], [TRACKINGNO], [CLEARANCE_NUMBER], [BAG_NUMBER], [TAX_NUMBER], [DLV_INV], [IN_DATE], [IN_DATETIME], [OUT_DATETIME], [COMBINE], [TAX_BASE], [TAX1], [TAX2], [CCFEE], [COD], [FEE], [INCLUDE_TAX], [RECIPIENT], [RECPHONE], [RECADDRESS], [RECID], [TO_DLV_COD], [DLV_COM], [DLV_COM_STN], [DLV_COD], [DLV_COD_CODE], [DLV_COD_TIME], [DLV_COD_OPE], [DLV_REMIT_DATE], [DLV_REMIT_AMOUT], [DLV_REMIT_AMOUT_FEE], [DLV_REMIT_CODE], [DLV_REMIT_TIME], [DLV_REMIT_OPE], [UPDATEDATE], [MODIFTYDATE], [Download], [RECORD_FEE_MASTER], [TAX_PAYER],'新增' as [MEMO] ,getdate() as [INS_TIME],[ARRIVAL],[CUSTOMER_COD],[TRANS_COD] from jetf.dbo.FEE_MASTER where SOURCE_TYPE='2' and DLV_INV=@DLV_INV ");

                try
                {
                    if (dt_Fee_Master.Rows.Count > 0)
                    {
                        StringBuilder sb_Delete = new StringBuilder();
                        sb_Delete.Append("delete from [jetf].[dbo].[FEE_MASTER_MODIFY_G] where MODIFY_DATADATE=@MODIFY_DATADATE  ");
                        //刪除調整表資料
                        using (SqlCommand cmd = new SqlCommand(sb_Delete.ToString(), conn))
                        {
                            cmd.Transaction = tran;
                            cmd.Parameters.Add("@MODIFY_DATADATE", SqlDbType.NVarChar).Value = dataDate;
                            cmd.ExecuteNonQuery();
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                    {
                        cmd.Transaction = tran;
                        for (int i = 0; i < dt_Fee_Master.Rows.Count; i++)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@MODIFY_DATADATE", SqlDbType.NVarChar).Value = dataDate;
                            cmd.Parameters.Add("@DATADATE", SqlDbType.NVarChar).Value = dataDate;
                            cmd.Parameters.Add("@SOURCE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["source"].ToString();
                            cmd.Parameters.Add("@SOURCE_TYPE", SqlDbType.NVarChar).Value = "2";
                            cmd.Parameters.Add("@CUSTOMER", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["customer"].ToString();
                            cmd.Parameters.Add("@TRACKINGNO", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["trackingno"].ToString();
                            cmd.Parameters.Add("@TYPE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["type"].ToString();
                            cmd.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["dlv_inv"].ToString();
                            cmd.Parameters.Add("@OUT_DATETIME", SqlDbType.DateTime).Value = DateTime.ParseExact(dataDate, "yyyyMMdd", CultureInfo.InvariantCulture);
                            cmd.Parameters.Add("@TAX1", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["tax1"].ToString();
                            cmd.Parameters.Add("@TAX2", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["tax2"].ToString();
                            cmd.Parameters.Add("@FEE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["fee"].ToString();
                            cmd.Parameters.Add("@CCFEE", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["ccfee"].ToString();
                            cmd.Parameters.Add("@COD", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["cod"].ToString();
                            cmd.Parameters.Add("@INCLUDE_TAX", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["include_tax"].ToString();
                            cmd.Parameters.Add("@TO_DLV_COD", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["to_dlv_cod"].ToString();
                            cmd.Parameters.Add("@RECIPIENT", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["recipient"].ToString();
                            //G類都是跟派件收
                            cmd.Parameters.Add("@TRANS_COD", SqlDbType.NVarChar).Value = dt_Fee_Master.Rows[i]["tax1"].ToString();
                            cmd.ExecuteNonQuery();
                        }
                        //確認寫入  
                        tran.Commit();
                        resopnseModel.status = Status.success;
                    }
                }
                catch (Exception ex)
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = ex.Message;
                    //取消寫入
                    tran.Rollback();
                }
            }
            return resopnseModel;
        }

        /// <summary>
        /// 寫入上傳檔案 回倉重出貨明細表
        /// </summary>
        /// <param name="dt_Fee_Master"></param>
        /// <param name="userId"></param>
        ResponseModel InsertAgainCargo(DataTable dt_AgainCargo, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

            using (SqlTransaction tran = conn.BeginTransaction())
            {
                //新增FEE_MASTER
                StringBuilder sb = new StringBuilder();

                try
                {
                    using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                    {
                        cmd.Transaction = tran;
                        for (int i = 0; i < dt_AgainCargo.Rows.Count; i++)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@RECIPIENT", SqlDbType.NVarChar).Value = dt_AgainCargo.Rows[i]["recipient"].ToString();
                            cmd.ExecuteNonQuery();
                        }
                        //確認寫入  
                        tran.Commit();
                        resopnseModel.status = Status.success;
                    }
                }
                catch (Exception ex)
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = ex.Message;
                    //取消寫入
                    tran.Rollback();
                }
            }
            return resopnseModel;
        }


        DataTable GetFee_Master(DataTable dt_Upload, string taxType, string upload_Time, string userId)
        {
            DateTime in_date, out_date;
            DataRow[] dr_Data, dr_Upload;
            DataRow dr_Fee_Master;

            DataTable dt_Fee_Master = new DataTable();
            dt_Fee_Master.Columns.Add("source", typeof(string));//CLEARANCE_INFO.DATA_TYPE
            dt_Fee_Master.Columns.Add("type", typeof(string));//CLEARANCE_INFO.CLEARANCE_TYPE
            dt_Fee_Master.Columns.Add("customer", typeof(string));//SEA_ORDER_ORIGINAL.DESPATCH_NAME
            dt_Fee_Master.Columns.Add("main_number", typeof(string));//CLEARANCE_INFO.MAIN_NUMBER
            dt_Fee_Master.Columns.Add("trackingno", typeof(string));//上傳	BL_NO(分號)
            dt_Fee_Master.Columns.Add("clearance_number", typeof(string));//上傳	CLEARANCE_NUMBER
            dt_Fee_Master.Columns.Add("combine", typeof(string));//併單
            dt_Fee_Master.Columns.Add("in_date", typeof(string));//CLEARANCE_INFO.SIGN_IN_TIME
            dt_Fee_Master.Columns.Add("in_datetime", typeof(string));//CLEARANCE_INFO.SIGN_IN_TIME
            dt_Fee_Master.Columns.Add("out_datetime", typeof(string));//CLEARANCE_INFO.SIGN_OUT_TIME
            dt_Fee_Master.Columns.Add("tax_base", typeof(string));
            dt_Fee_Master.Columns.Add("tax1", typeof(string));//上傳	TAX
            dt_Fee_Master.Columns.Add("tax2", typeof(string));//上傳	TAX
            dt_Fee_Master.Columns.Add("dlv_com", typeof(string));//SEA_ORDER_ORIGINAL.TRANS_TAXPAYMENT
            dt_Fee_Master.Columns.Add("tax_number", typeof(string));//ETL_TIPC_TAX.TAX_NUMBER
            dt_Fee_Master.Columns.Add("tax_recid", typeof(string));//上傳 tax_recid
            dt_Fee_Master.Columns.Add("tax_payer", typeof(string));//上傳 tax_payer
            dt_Fee_Master.Columns.Add("cod", typeof(string));
            dt_Fee_Master.Columns.Add("fee", typeof(string));
            dt_Fee_Master.Columns.Add("include_tax", typeof(string));
            dt_Fee_Master.Columns.Add("recipient", typeof(string));
            dt_Fee_Master.Columns.Add("recphone", typeof(string));
            dt_Fee_Master.Columns.Add("recaddress", typeof(string));
            dt_Fee_Master.Columns.Add("recid", typeof(string));
            dt_Fee_Master.Columns.Add("to_dlv_cod", typeof(string));
            dt_Fee_Master.Columns.Add("dlv_inv", typeof(string));
            dt_Fee_Master.Columns.Add("memo", typeof(string));
            dt_Fee_Master.Columns.Add("arrival", typeof(string));
            dt_Fee_Master.Columns.Add("Trans_Cod", typeof(string));
            dt_Fee_Master.Columns.Add("Customer_Cod", typeof(string));

            //特殊客戶
            DataTable dt_Customer_Special = _customerService.GetCustomer_Special("海運");

            //海運上傳資料
            DataTable dt_Data = GetSeaTaxUpload(taxType, upload_Time, userId);

            if (dt_Data.Rows.Count > 0)
            {
                var dt_Group = from t in dt_Data.AsEnumerable()
                               group t by new
                               {
                                   bl_no = t.Field<string>("bl_no"),
                                   main_number = t.Field<string>("MAIN_NUMBER")
                               } into g
                               select new
                               {
                                   bl_no = g.Key.bl_no,
                                   main_number = g.Key.main_number
                               };

                foreach (var item in dt_Group)
                {
                    dr_Fee_Master = dt_Fee_Master.NewRow();
                    //找最後出倉日的
                    dr_Data = dt_Data.AsEnumerable()
                        .Where(x => x.Field<string>("BL_NO") == item.bl_no && x.Field<string>("MAIN_NUMBER") == item.main_number)
                        .OrderByDescending(x => x.Field<object>("SIGN_OUT_TIME"))
                        .ToArray();
                    //判斷併單使用
                    dr_Upload = dt_Upload.AsEnumerable()
                        .Where(x => x.Field<string>("BL_NO") == item.bl_no && x.Field<string>("MAIN_NUMBER") == item.main_number)
                        .ToArray();
                    if (dr_Data.Length > 0)
                    {
                        //來源
                        dr_Fee_Master["source"] = taxType;
                        //報關類型
                        dr_Fee_Master["type"] = dr_Data[0]["CLEARANCE_TYPE"].ToString();
                        //主提單號
                        dr_Fee_Master["main_number"] = dr_Data[0]["MAIN_NUMBER"].ToString();
                        //分提單號
                        dr_Fee_Master["trackingno"] = dr_Data[0]["bl_no"].ToString();
                        //清關單號
                        dr_Fee_Master["clearance_number"] = dr_Data[0]["clearance_number"].ToString();
                        //稅金號碼
                        dr_Fee_Master["tax_number"] = dr_Data[0]["tax_number"].ToString();
                        //稅基
                        dr_Fee_Master["tax_base"] = dr_Data[0]["TAX_BASE"].ToString();
                        //進口納稅義務人身分證號
                        dr_Fee_Master["tax_recid"] = dr_Data[0]["tax_recid"].ToString();
                        //進口納稅義務人
                        dr_Fee_Master["tax_payer"] = dr_Data[0]["tax_payer"].ToString();
                        //進倉日
                        if (DateTime.TryParse(dr_Data[0]["SIGN_IN_TIME"].ToString(), out in_date))
                        {
                            dr_Fee_Master["in_date"] = in_date.ToString("yyyyMMdd");
                            //進倉時間
                            dr_Fee_Master["in_datetime"] = in_date.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        //出倉時間
                        if (DateTime.TryParse(dr_Data[0]["SIGN_OUT_TIME"].ToString(), out out_date))
                        {
                            dr_Fee_Master["out_datetime"] = out_date.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        //手續費
                        dr_Fee_Master["fee"] = dr_Data[0]["COD_FEE"].ToString();
                        //是否包稅
                        dr_Fee_Master["include_tax"] = dr_Data[0]["INCLUDE_TAX"].ToString();

                        //客戶名稱
                        dr_Fee_Master["customer"] = dr_Data[0]["DESPATCH_NAME"].ToString();
                        //派件公司
                        dr_Fee_Master["dlv_com"] = ConvertLanguage(dr_Data[0]["TRANS_TAXPAYMENT"].ToString(), "Big5");
                        dr_Fee_Master["recipient"] = dr_Data[0]["IMPORTER"].ToString();
                        dr_Fee_Master["recphone"] = dr_Data[0]["IM_PHONENO"].ToString();
                        dr_Fee_Master["recaddress"] = dr_Data[0]["IM_ADD"].ToString();
                        if (dr_Data[0]["IMPORTER_ID"].ToString().Length > 20)
                        {
                            dr_Data[0]["IMPORTER_ID"] = dr_Data[0]["IMPORTER_ID"].ToString().Substring(0, 20);
                        }
                        dr_Fee_Master["recid"] = dr_Data[0]["IMPORTER_ID"].ToString();
                        dr_Fee_Master["dlv_inv"] = dr_Data[0]["JETF_SERIAL"].ToString();
                        //代收款
                        var cod = 0;
                        Int32.TryParse(dr_Data[0]["CC"].ToString(), out cod);
                        dr_Fee_Master["cod"] = cod;
                        //備註
                        dr_Fee_Master["memo"] = dr_Data[0]["MEMO"].ToString();

                        //菜鳥LP單號
                        dr_Fee_Master["arrival"] = dr_Data[0]["ARRIVAL"].ToString();
                        //兩筆稅金
                        if (dr_Upload.Length > 1)
                        {
                            dr_Fee_Master["combine"] = "Y";
                            dr_Fee_Master["tax1"] = dr_Data[0]["tax"].ToString();
                            //2021-11-11 發現併單會有3筆...
                            var tax2 = 0;
                            for (int i = 1; i < dr_Upload.Length; i++)
                            {
                                tax2 += Convert.ToInt32(dr_Data[i]["tax"]);
                            }
                            dr_Fee_Master["tax2"] = tax2.ToString();
                        }
                        else
                        {
                            dr_Fee_Master["tax1"] = dr_Data[0]["tax"].ToString();
                        }

                        //派件公司代收貨款金額
                        if (dr_Data[0]["INCLUDE_TAX"].ToString() == "Y")
                        {
                            var taxData = _taxService.GetTaxY(dr_Fee_Master);

                            dr_Fee_Master["Trans_Cod"] = taxData.TransCod;
                            dr_Fee_Master["Customer_Cod"] = taxData.CustomerCod;
                            dr_Fee_Master["to_dlv_cod"] = taxData.ToDlvCod;
                        }
                        //菜鳥尊榮服務
                        else if (!string.IsNullOrEmpty(dr_Data[0]["ISCAINIAOP"].ToString()) && Convert.ToBoolean(dr_Data[0]["ISCAINIAOP"]) == true)
                        {
                            var taxData = _taxService.GetTaxP(dr_Fee_Master);
                            //如果要跟派件公司收錢，稅金方式改成N(不包稅)
                            dr_Fee_Master["include_tax"] = taxData.TransCod > 0 ? "N" : dr_Fee_Master["include_tax"];
                            //手續費
                            dr_Fee_Master["fee"] = taxData.TransCod > 0 ? dr_Fee_Master["fee"] : 0;
                            dr_Fee_Master["Trans_Cod"] = taxData.TransCod;
                            dr_Fee_Master["Customer_Cod"] = taxData.CustomerCod;
                            dr_Fee_Master["to_dlv_cod"] = taxData.ToDlvCod;
                        }
                        //稅金D、特殊客戶
                        else if (dr_Data[0]["INCLUDE_TAX"].ToString() == "D" ||
                            _taxService.IsSeaSpecial(dt_Customer_Special, dr_Data[0]["company"].ToString(), dr_Fee_Master["recphone"].ToString().Trim()))
                        {
                            var taxData = _taxService.GetTaxD(dr_Fee_Master);

                            dr_Fee_Master["include_tax"] = "D";
                            dr_Fee_Master["fee"] = "0";
                            dr_Fee_Master["Trans_Cod"] = taxData.TransCod;
                            dr_Fee_Master["Customer_Cod"] = taxData.CustomerCod;
                            dr_Fee_Master["to_dlv_cod"] = taxData.ToDlvCod;
                        }
                        else if (dr_Data[0]["INCLUDE_TAX"].ToString() == "C" ||
                            dr_Fee_Master["memo"].ToString().IndexOf("DDP") > -1)
                        {
                            var taxData = _taxService.GetTaxC(dr_Fee_Master);

                            //是否包稅-C客戶付款
                            dr_Fee_Master["include_tax"] = "C";
                            dr_Fee_Master["fee"] = "0";
                            dr_Fee_Master["Trans_Cod"] = taxData.TransCod;
                            dr_Fee_Master["Customer_Cod"] = taxData.CustomerCod;
                            dr_Fee_Master["to_dlv_cod"] = taxData.ToDlvCod;
                        }
                        else
                        {
                            var taxData = _taxService.GetTaxN(dr_Fee_Master);
                            dr_Fee_Master["Trans_Cod"] = taxData.TransCod;
                            dr_Fee_Master["Customer_Cod"] = taxData.CustomerCod;
                            dr_Fee_Master["to_dlv_cod"] = taxData.ToDlvCod;
                        }

                        dt_Fee_Master.Rows.Add(dr_Fee_Master);
                    }
                }
            }
            return dt_Fee_Master;
        }

        /// <summary>
        /// 取得海運稅金上傳檔案
        /// </summary>
        /// <param name="taxType"></param>
        /// <param name="upload_Time"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        DataTable GetSeaTaxUpload(string taxType, string upload_Time, string userId)
        {
            //報單資料
            //StringBuilder sb = new StringBuilder();
            //if (taxType == "TPCT" || taxType == "TIPC" || taxType == "UNIJ")
            //{
            //    sb.Append("select ");
            //    sb.Append("a.BL_NO,a.CLEARANCE_NUMBER,a.TAX, ");
            //    sb.Append("b.DATA_TYPE,b.CLEARANCE_TYPE,b.MAIN_NUMBER,b.SIGN_IN_TIME,b.SIGN_OUT_TIME, ");
            //    sb.Append("jetf.dbo.udf_getseaorder_New(a.BL_NO) as Consignee,");
            //    sb.Append("d.TAX_NUMBER,d.TAX_BASE,d.TAX_AMOUNT, ");
            //    sb.Append("e.COD_FEE,e.INCLUDE_TAX,e.COMPANY,e.ISCAINIAOP,a.TAX_PAYER,a.TAX_RECID ");
            //    sb.Append("from jetf.dbo.SEA_TAX_UPLOAD a ");
            //    sb.Append("left join DATA_CENTER.dbo.CLEARANCE_INFO b on a.BL_NO=b.BAG_NUMBER ");
            //    sb.Append("left join ( ");
            //    sb.Append("		select ROW_NUMBER() OVER (PARTITION BY MAIN_NUMBER,BAG_NUMBER ORDER BY MAIN_NUMBER ) as ROW_ID,");
            //    sb.Append(" 	MAIN_NUMBER,BAG_NUMBER,TAX_NUMBER,TAX_BASE,TAX_AMOUNT");
            //    sb.Append("		from DATA_CENTER.dbo.ETL_TIPC_TAX ");
            //    sb.Append("	) d on b.MAIN_NUMBER=d.MAIN_NUMBER and b.BAG_NUMBER=d.BAG_NUMBER and d.ROW_ID='1' ");
            //    sb.Append("left join jetf.dbo.customer_master e on jetf.dbo.udf_getseaorder_DESPATCH_NAME_New(a.BL_NO)=e.CUST_ID+e.TRANs_name and e.TRAN_TYPE='海運' ");
            //    sb.Append("where UPLOAD_TIME=@UPLOAD_TIME and a.UPLOAD_OPE=@UPLOAD_OPE ");
            //}
            //else
            //{
            //    sb.Append("select ");
            //    sb.Append("a.BL_NO,a.CLEARANCE_NUMBER,a.CLEARANCE_TYPE,a.TAX,a.TAX_NUMBER,a.MAIN_NUMBER, ");
            //    sb.Append("b.DATA_TYPE,b.SIGN_IN_TIME,b.SIGN_OUT_TIME,d.TAX_BASE, ");
            //    sb.Append("jetf.dbo.udf_getseaorder_New(a.BL_NO) as Consignee,");
            //    sb.Append("e.COD_FEE,e.INCLUDE_TAX,e.COMPANY,e.ISCAINIAOP,a.TAX_PAYER,a.TAX_RECID ");
            //    sb.Append("from jetf.dbo.SEA_TAX_UPLOAD a ");
            //    sb.Append("left join DATA_CENTER.dbo.CLEARANCE_INFO b on a.MAIN_NUMBER=b.MAIN_NUMBER and a.BL_NO=b.BAG_NUMBER ");
            //    sb.Append("left join ( ");
            //    sb.Append("		select ROW_NUMBER() OVER (PARTITION BY MAIN_NUMBER,BAG_NUMBER ORDER BY MAIN_NUMBER ) as ROW_ID,");
            //    sb.Append(" 	MAIN_NUMBER,BAG_NUMBER,TAX_NUMBER,TAX_BASE,TAX_AMOUNT");
            //    sb.Append("		from DATA_CENTER.dbo.ETL_TIPC_TAX ");
            //    sb.Append("	) d on b.MAIN_NUMBER=d.MAIN_NUMBER and b.BAG_NUMBER=d.BAG_NUMBER and d.ROW_ID='1' ");
            //    sb.Append("left join jetf.dbo.customer_master e on jetf.dbo.udf_getseaorder_DESPATCH_NAME_New(a.BL_NO)=e.CUST_ID+e.TRANs_name and e.TRAN_TYPE='海運' ");
            //    sb.Append("where UPLOAD_TIME=@UPLOAD_TIME and a.UPLOAD_OPE=@UPLOAD_OPE ");
            //}

            var sql = @"
with CTE_SEA_ORDER_ORIGINAL as 
(
	select MAINNUMBER,BL_NO,DESPATCH_NAME,TRANS_TAXPAYMENT,IMPORTER,IM_PHONENO,IM_ADD,IMPORTER_ID,JETF_SERIAL,CC,MEMO,ARRIVAL from DATA_CENTER.dbo.SEA_ORDER_ORIGINAL a
	where GW > 0 and MODIFTYDATE  =(select MAX(MODIFTYDATE)
	from DATA_CENTER.dbo.SEA_ORDER_ORIGINAL 
	where BL_NO=a.BL_NO and MAINNUMBER=a.MAINNUMBER)
),
CTE_ETL_TIPC_TAX  as (
	select ROW_ID,MAIN_NUMBER,BAG_NUMBER,TAX_NUMBER,TAX_BASE,TAX_AMOUNT from DATA_CENTER.dbo.ETL_TIPC_TAX a
	where ROW_ID = (select MAX(ROW_ID) 
	from DATA_CENTER.dbo.ETL_TIPC_TAX
	where MAIN_NUMBER=a.MAIN_NUMBER and BAG_NUMBER=a.BAG_NUMBER)
)
select 
a.BL_NO,a.CLEARANCE_NUMBER,a.CLEARANCE_TYPE,a.TAX,a.TAX_NUMBER,a.MAIN_NUMBER, 
b.DATA_TYPE,b.SIGN_IN_TIME,b.SIGN_OUT_TIME,d.TAX_BASE, 
e.COD_FEE,e.INCLUDE_TAX,e.COMPANY,e.ISCAINIAOP,a.TAX_PAYER,a.TAX_RECID ,
DESPATCH_NAME,TRANS_TAXPAYMENT,IMPORTER,IM_PHONENO,IM_ADD,IMPORTER_ID,JETF_SERIAL,CC,MEMO,ARRIVAL
from jetf.dbo.SEA_TAX_UPLOAD a 
left join DATA_CENTER.dbo.CLEARANCE_INFO b on a.MAIN_NUMBER=b.MAIN_NUMBER and a.BL_NO=b.BAG_NUMBER 
left join CTE_SEA_ORDER_ORIGINAL c on a.MAIN_NUMBER=c.MAINNUMBER and a.BL_NO =c.BL_NO
left join CTE_ETL_TIPC_TAX d on b.MAIN_NUMBER=d.MAIN_NUMBER and b.BAG_NUMBER=d.BAG_NUMBER 
left join jetf.dbo.customer_master e on jetf.dbo.udf_getseaorder_DESPATCH_NAME_New(a.BL_NO)=e.CUST_ID+e.TRANs_name and e.TRAN_TYPE='海運' 
where UPLOAD_TIME=@UPLOAD_TIME and a.UPLOAD_OPE=@UPLOAD_OPE
";
            DataTable dt_Data = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@UPLOAD_TIME", SqlDbType.NVarChar).Value = upload_Time;
                da.SelectCommand.Parameters.Add("@UPLOAD_OPE", SqlDbType.NVarChar).Value = userId;
                da.SelectCommand.CommandTimeout = 600;
                da.Fill(dt_Data);
            }

            return dt_Data;

        }

        DataTable ReadExcelTipc(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = GetDataTable();

            bool read = false;
            string clearance_number, bl_no, reg_no, mainfest, tax;
            DateTime prt_time;

            //IWorkbook workbook = new HSSFWorkbook(filePath);
            //IWorkbook workbook = new XSSFWorkbook(filePath);
            //string fileType = Path.GetExtension(filePath);
            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                //if (fileType == ".xls")
                //{
                //    workBook = new HSSFWorkbook(fs);
                //}
                //else
                //{
                workBook = new XSSFWorkbook(fs);
                //}
            }

            var sheet = workBook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    int cCount = sheet.GetRow(i).Cells.Count;
                    //報單號碼  
                    clearance_number = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();
                    //分號
                    bl_no = sheet.GetRow(i).GetCell(2) == null ? "" : sheet.GetRow(i).GetCell(2).ToString().Trim();
                    //海掛
                    reg_no = sheet.GetRow(i).GetCell(3) == null ? "" : sheet.GetRow(i).GetCell(3).ToString().Trim();
                    //艙單
                    mainfest = sheet.GetRow(i).GetCell(4) == null ? "" : sheet.GetRow(i).GetCell(4).ToString().Trim();
                    //稅單金額
                    tax = sheet.GetRow(i).GetCell(5) == null ? "" : sheet.GetRow(i).GetCell(5).ToString().Trim();
                    //列印時間
                    if (sheet.GetRow(i).GetCell(6) != null && sheet.GetRow(i).GetCell(6).CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(sheet.GetRow(i).GetCell(6)))
                    {
                        prt_time = sheet.GetRow(i).GetCell(6) == null ? DateTime.MinValue : sheet.GetRow(i).GetCell(6).DateCellValue;
                    }
                    else
                    {
                        prt_time = DateTime.MinValue;
                    }

                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(1) != null && sheet.GetRow(i).GetCell(1).ToString().Trim() == "報單號碼")
                    {
                        read = true;
                        continue;
                    }
                    if (read && clearance_number != "" && bl_no != "" && reg_no != "" && mainfest != "" && tax != "" && prt_time != DateTime.MinValue)
                    {
                        dr = dt_Data.NewRow();
                        dr["clearance_number"] = clearance_number;
                        dr["bl_no"] = bl_no;
                        dr["reg_no"] = reg_no;
                        dr["mainfest"] = mainfest;
                        dr["tax"] = tax;
                        dr["prt_time"] = prt_time.ToString("yyyy-MM-dd HH:mm:ss");
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        DataTable ReadExcelTpct(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = GetDataTable();

            bool read = false;
            string clearance_number, bl_no, reg_no, mainfest, tax;
            DateTime prt_time;

            //IWorkbook workbook = new HSSFWorkbook(filePath);
            //IWorkbook workbook = new XSSFWorkbook(filePath);
            //string fileType = Path.GetExtension(filePath);
            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                //if (fileType == ".xls")
                //{
                //    workBook = new HSSFWorkbook(fs);
                //}
                //else
                //{
                workBook = new XSSFWorkbook(fs);
                //}
            }

            var sheet = workBook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    int cCount = sheet.GetRow(i).Cells.Count;
                    //報單號碼  
                    clearance_number = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();
                    //分號
                    bl_no = sheet.GetRow(i).GetCell(2) == null ? "" : sheet.GetRow(i).GetCell(2).ToString().Trim();
                    //海掛
                    reg_no = sheet.GetRow(i).GetCell(3) == null ? "" : sheet.GetRow(i).GetCell(3).ToString().Trim();
                    //艙單
                    mainfest = sheet.GetRow(i).GetCell(4) == null ? "" : sheet.GetRow(i).GetCell(4).ToString().Trim();
                    //稅單金額
                    tax = sheet.GetRow(i).GetCell(5) == null ? "" : sheet.GetRow(i).GetCell(5).ToString().Trim();
                    //列印時間
                    if (sheet.GetRow(i).GetCell(6) != null && sheet.GetRow(i).GetCell(6).CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(sheet.GetRow(i).GetCell(6)))
                    {
                        prt_time = sheet.GetRow(i).GetCell(6) == null ? DateTime.MinValue : sheet.GetRow(i).GetCell(6).DateCellValue;
                    }
                    else
                    {
                        prt_time = DateTime.MinValue;
                    }

                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(1) != null && sheet.GetRow(i).GetCell(1).ToString().Trim() == "報單號碼")
                    {
                        read = true;
                        continue;
                    }
                    if (read && clearance_number != "" && bl_no != "" && reg_no != "" && tax != "" && prt_time != DateTime.MinValue)
                    {
                        dr = dt_Data.NewRow();
                        dr["clearance_number"] = clearance_number;
                        dr["bl_no"] = bl_no;
                        dr["reg_no"] = reg_no;
                        dr["mainfest"] = mainfest;
                        dr["tax"] = tax;
                        dr["prt_time"] = prt_time.ToString("yyyy-MM-dd HH:mm:ss");
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        DataTable ReadExcelUnij(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = GetDataTable();

            bool read = false;
            string clearance_number, bl_no, reg_no, mainfest, tax;
            DateTime prt_time;

            //IWorkbook workbook = new HSSFWorkbook(filePath);
            //IWorkbook workbook = new XSSFWorkbook(filePath);
            //string fileType = Path.GetExtension(filePath);
            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                //if (fileType == ".xls")
                //{
                //    workBook = new HSSFWorkbook(fs);
                //}
                //else
                //{
                workBook = new XSSFWorkbook(fs);
                //}
            }

            var sheet = workBook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    int cCount = sheet.GetRow(i).Cells.Count;
                    //報單號碼  
                    clearance_number = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();
                    //分號
                    bl_no = sheet.GetRow(i).GetCell(2) == null ? "" : sheet.GetRow(i).GetCell(2).ToString().Trim();
                    //海掛
                    reg_no = sheet.GetRow(i).GetCell(3) == null ? "" : sheet.GetRow(i).GetCell(3).ToString().Trim();
                    //艙單
                    mainfest = sheet.GetRow(i).GetCell(4) == null ? "" : sheet.GetRow(i).GetCell(4).ToString().Trim();
                    //稅單金額
                    tax = sheet.GetRow(i).GetCell(5) == null ? "" : sheet.GetRow(i).GetCell(5).ToString().Trim();
                    //列印時間
                    if (sheet.GetRow(i).GetCell(6) != null && sheet.GetRow(i).GetCell(6).CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(sheet.GetRow(i).GetCell(6)))
                    {
                        prt_time = sheet.GetRow(i).GetCell(6) == null ? DateTime.MinValue : sheet.GetRow(i).GetCell(6).DateCellValue;
                    }
                    else
                    {
                        prt_time = DateTime.MinValue;
                    }

                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(1) != null && sheet.GetRow(i).GetCell(1).ToString().Trim() == "報單號碼")
                    {
                        read = true;
                        continue;
                    }
                    if (read && clearance_number != "" && bl_no != "" && reg_no != "" && tax != "" && prt_time != DateTime.MinValue)
                    {
                        dr = dt_Data.NewRow();
                        dr["clearance_number"] = clearance_number;
                        dr["bl_no"] = bl_no;
                        dr["reg_no"] = reg_no;
                        dr["mainfest"] = mainfest;
                        dr["tax"] = tax;
                        dr["prt_time"] = prt_time.ToString("yyyy-MM-dd HH:mm:ss");
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        /// <summary>
        /// 讀取Excel海運-高雄郵聯
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadExcelIpost(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = GetDataTable();

            bool read = false;
            string main_number, clearance_number, clearance_type, bl_no, reg_no, mainfest, tax, tax_number, tax_payer, tax_recid;
            DateTime prt_time;

            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    int cCount = sheet.GetRow(i).Cells.Count;
                    //主號
                    main_number = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();
                    //報單號碼  
                    clearance_number = sheet.GetRow(i).GetCell(3) == null ? "" : sheet.GetRow(i).GetCell(3).ToString().Trim();
                    //報單類型
                    clearance_type = sheet.GetRow(i).GetCell(4) == null ? "" : sheet.GetRow(i).GetCell(4).ToString().Trim();
                    //分號
                    bl_no = sheet.GetRow(i).GetCell(2) == null ? "" : sheet.GetRow(i).GetCell(2).ToString().Trim();
                    //稅單號碼
                    tax_number = sheet.GetRow(i).GetCell(6) == null ? "" : sheet.GetRow(i).GetCell(6).ToString().Trim();
                    //進口納稅義務人身分證號
                    tax_recid = sheet.GetRow(i).GetCell(7) == null ? "" : sheet.GetRow(i).GetCell(7).ToString().Trim();
                    //進口納稅義務人
                    tax_payer = sheet.GetRow(i).GetCell(8) == null ? "" : sheet.GetRow(i).GetCell(8).ToString().Trim();
                    //稅單金額
                    tax = sheet.GetRow(i).GetCell(12) == null ? "" : sheet.GetRow(i).GetCell(12).ToString().Trim();
                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(3) != null && sheet.GetRow(i).GetCell(3).ToString().Trim() == "報單號碼")
                    {
                        read = true;
                        continue;
                    }
                    if (read && clearance_number != "" && bl_no != "" && tax != "")
                    {
                        dr = dt_Data.NewRow();
                        dr["main_number"] = main_number;
                        dr["clearance_number"] = clearance_number;
                        dr["clearance_type"] = clearance_type;
                        dr["bl_no"] = bl_no;
                        dr["tax_number"] = tax_number;
                        dr["tax_recid"] = tax_recid;
                        dr["tax_payer"] = tax_payer;
                        dr["tax"] = tax;
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        /// <summary>
        /// 讀取Excel海運-萬海
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadExcelWaha(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = GetDataTable();

            bool read = false;
            string main_number, clearance_number, clearance_type, bl_no, reg_no, mainfest, tax, tax_number;
            DateTime prt_time;

            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    int cCount = sheet.GetRow(i).Cells.Count;
                    //主號
                    main_number = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();
                    //報單號碼  
                    clearance_number = sheet.GetRow(i).GetCell(3) == null ? "" : sheet.GetRow(i).GetCell(3).ToString().Trim();
                    //報單類型
                    clearance_type = sheet.GetRow(i).GetCell(4) == null ? "" : sheet.GetRow(i).GetCell(4).ToString().Trim();
                    //分號
                    bl_no = sheet.GetRow(i).GetCell(2) == null ? "" : sheet.GetRow(i).GetCell(2).ToString().Trim();
                    //稅單號碼
                    tax_number = sheet.GetRow(i).GetCell(6) == null ? "" : sheet.GetRow(i).GetCell(6).ToString().Trim();
                    //稅單金額
                    tax = sheet.GetRow(i).GetCell(11) == null ? "" : sheet.GetRow(i).GetCell(11).ToString().Trim();
                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(3) != null && sheet.GetRow(i).GetCell(3).ToString().Trim() == "報單號碼")
                    {
                        read = true;
                        continue;
                    }
                    if (read && clearance_number != "" && bl_no != "" && tax != "")
                    {
                        dr = dt_Data.NewRow();
                        dr["main_number"] = main_number;
                        dr["clearance_number"] = clearance_number;
                        dr["clearance_type"] = clearance_type;
                        dr["bl_no"] = bl_no;
                        dr["tax_number"] = tax_number;
                        dr["tax"] = tax;
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        /// <summary>
        /// 讀取Excel 物流代收檔
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadExcelReceive(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = new DataTable();
            dt_Data.Columns.Add("dlv_inv", typeof(string)); //查貨號碼
            dt_Data.Columns.Add("dlv_cod", typeof(string));//預定代收金額

            bool read = false;
            string dlv_inv, dlv_cod;

            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);
            //代收檔讀取實際行數會少一行? 需要+1
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    int cCount = sheet.GetRow(i).Cells.Count;
                    //查貨號碼
                    dlv_inv = sheet.GetRow(i).GetCell(3) == null ? "" : sheet.GetRow(i).GetCell(3).ToString().Trim();
                    //代收金額 
                    dlv_cod = sheet.GetRow(i).GetCell(7) == null ? "" : sheet.GetRow(i).GetCell(7).ToString().Trim();
                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(7) != null && sheet.GetRow(i).GetCell(7).ToString().Trim() == "代收金額")
                    {
                        read = true;
                        continue;
                    }
                    if (read && dlv_inv != "" && dlv_cod != "")
                    {
                        dr = dt_Data.NewRow();
                        dr["dlv_inv"] = dlv_inv;
                        dr["dlv_cod"] = dlv_cod;
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        /// <summary>
        /// 讀取Excel 物流匯款檔
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadExcelTransfer(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = new DataTable();
            dt_Data.Columns.Add("dlv_inv", typeof(string)); //查貨號碼
            dt_Data.Columns.Add("dlv_remit_date", typeof(string));//匯款日期
            dt_Data.Columns.Add("dlv_remit_amout", typeof(string));//代收貨款金額
            dt_Data.Columns.Add("dlv_remit_amout_fee", typeof(string));//代收貨款手續費

            bool read = false;
            string dlv_inv, dlv_remit_date, dlv_remit_amout, dlv_remit_amout_fee;

            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);
            //代收檔讀取實際行數會少一行? 需要+1
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    int cCount = sheet.GetRow(i).Cells.Count;
                    //查貨號碼
                    dlv_inv = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                    //匯款日期 
                    dlv_remit_date = sheet.GetRow(i).GetCell(10) == null ? "" : sheet.GetRow(i).GetCell(10).ToString().Trim();
                    //代收貨款金額
                    dlv_remit_amout = sheet.GetRow(i).GetCell(8) == null ? "" : sheet.GetRow(i).GetCell(8).ToString().Trim();
                    //代收貨款手續費
                    dlv_remit_amout_fee = sheet.GetRow(i).GetCell(12) == null ? "" : sheet.GetRow(i).GetCell(12).ToString().Trim();

                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "查貨號碼")
                    {
                        read = true;
                        continue;
                    }
                    if (read && dlv_inv != "" && dlv_remit_date != "" && dlv_remit_amout != "" && dlv_remit_amout_fee != "")
                    {
                        dr = dt_Data.NewRow();
                        dr["dlv_inv"] = dlv_inv;
                        dr["dlv_remit_date"] = dlv_remit_date;
                        dr["dlv_remit_amout"] = Convert.ToInt32(dlv_remit_amout);
                        dr["dlv_remit_amout_fee"] = Convert.ToInt32(dlv_remit_amout_fee);
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        /// <summary>
        /// 讀取G類資料
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadExcelG(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = GetDataTableG();

            bool read = false;
            string source, customer, trackingno, dlv_inv, tax, fee, cod, type, ccfee, include_tax, recipient;
            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    //倉儲
                    source = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();
                    //客戶
                    customer = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();
                    //分提單號  
                    trackingno = sheet.GetRow(i).GetCell(2) == null ? "" : sheet.GetRow(i).GetCell(2).ToString().Trim();
                    //派送單號
                    dlv_inv = sheet.GetRow(i).GetCell(3) == null ? "" : sheet.GetRow(i).GetCell(3).ToString().Trim();
                    //稅金
                    tax = sheet.GetRow(i).GetCell(4) == null || sheet.GetRow(i).GetCell(4).ToString().Trim() == "" ? "0" : sheet.GetRow(i).GetCell(4).ToString().Trim();
                    //報關費
                    ccfee = sheet.GetRow(i).GetCell(5) == null || sheet.GetRow(i).GetCell(5).ToString().Trim() == "" ? "0" : sheet.GetRow(i).GetCell(5).ToString().Trim();
                    //到付款
                    cod = sheet.GetRow(i).GetCell(6) == null || sheet.GetRow(i).GetCell(6).ToString().Trim() == "" ? "0" : sheet.GetRow(i).GetCell(6).ToString().Trim();
                    //代收手續
                    fee = sheet.GetRow(i).GetCell(7) == null || sheet.GetRow(i).GetCell(7).ToString().Trim() == "" ? "0" : sheet.GetRow(i).GetCell(7).ToString().Trim();
                    //if (sheet.GetRow(i).GetCell(8).CellType == CellType.Formula)
                    //{
                    //    sheet.GetRow(i).GetCell(8).SetCellType(CellType.String);
                    //}
                    //稅金類別
                    include_tax = sheet.GetRow(i).GetCell(9) == null ? "" : sheet.GetRow(i).GetCell(9).ToString().Trim();
                    //收件人
                    recipient = sheet.GetRow(i).GetCell(11) == null ? "" : sheet.GetRow(i).GetCell(11).ToString().Trim();
                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(2) != null && sheet.GetRow(i).GetCell(2).ToString().Trim() == "分提單號")
                    {
                        read = true;
                        continue;
                    }
                    if (read && source != "" && trackingno != "" && dlv_inv != "" && tax != "" && ccfee != "" && fee != "" && cod != "")
                    {
                        if (source == "G類")
                        {
                            source = "TPCT";
                            //type = "G";
                        }
                        //else
                        //{
                        //    type = "";
                        //}
                        type = "G";
                        dr = dt_Data.NewRow();
                        dr["source"] = source;
                        //dr["main_number"] = trackingno;
                        dr["trackingno"] = trackingno;
                        dr["type"] = type;
                        dr["dlv_inv"] = dlv_inv;
                        dr["tax1"] = tax;
                        dr["tax2"] = "0";
                        dr["ccfee"] = ccfee;
                        dr["fee"] = fee;
                        dr["cod"] = cod;
                        dr["include_tax"] = include_tax;
                        dr["recipient"] = recipient;
                        dr["to_dlv_cod"] = Convert.ToInt32(dr["tax1"]) + Convert.ToInt32(dr["cod"]) + Convert.ToInt32(dr["ccfee"]) + Convert.ToInt32(dr["fee"]);
                        dr["customer"] = customer;
                        //if (include_tax == "N")
                        //{
                        //    //dr["customer"] = "新竹物流";
                        //    //不包稅：代收貨款+稅金+手續費
                        //    //dr["to_dlv_cod"] = Convert.ToInt32(dr["tax1"]) + Convert.ToInt32(dr["cod"]) + Convert.ToInt32(dr["fee"]);
                        //}
                        //else {
                        //    //包稅：純代收貨款金額
                        //    //dr["to_dlv_cod"] = dr["cod"].ToString();
                        //}
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        /// <summary>
        /// 讀取回倉重出貨明細表
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadExcelAgainCargo(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = GetDataTableG();

            bool read = false;
            string source, customer, trackingno, dlv_inv, tax, fee, cod, type, ccfee, include_tax, recipient;
            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    //倉儲
                    source = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();
                    //客戶
                    customer = sheet.GetRow(i).GetCell(1) == null ? "" : sheet.GetRow(i).GetCell(1).ToString().Trim();
                    //分提單號  
                    trackingno = sheet.GetRow(i).GetCell(2) == null ? "" : sheet.GetRow(i).GetCell(2).ToString().Trim();
                    //派送單號
                    dlv_inv = sheet.GetRow(i).GetCell(3) == null ? "" : sheet.GetRow(i).GetCell(3).ToString().Trim();
                    //稅金
                    tax = sheet.GetRow(i).GetCell(4) == null || sheet.GetRow(i).GetCell(4).ToString().Trim() == "" ? "0" : sheet.GetRow(i).GetCell(4).ToString().Trim();
                    //報關費
                    ccfee = sheet.GetRow(i).GetCell(5) == null || sheet.GetRow(i).GetCell(5).ToString().Trim() == "" ? "0" : sheet.GetRow(i).GetCell(5).ToString().Trim();
                    //到付款
                    cod = sheet.GetRow(i).GetCell(6) == null || sheet.GetRow(i).GetCell(6).ToString().Trim() == "" ? "0" : sheet.GetRow(i).GetCell(6).ToString().Trim();
                    //代收手續
                    fee = sheet.GetRow(i).GetCell(7) == null || sheet.GetRow(i).GetCell(7).ToString().Trim() == "" ? "0" : sheet.GetRow(i).GetCell(7).ToString().Trim();
                    //if (sheet.GetRow(i).GetCell(8).CellType == CellType.Formula)
                    //{
                    //    sheet.GetRow(i).GetCell(8).SetCellType(CellType.String);
                    //}
                    //稅金類別
                    include_tax = sheet.GetRow(i).GetCell(9) == null ? "" : sheet.GetRow(i).GetCell(9).ToString().Trim();
                    //收件人
                    recipient = sheet.GetRow(i).GetCell(11) == null ? "" : sheet.GetRow(i).GetCell(11).ToString().Trim();
                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(2) != null && sheet.GetRow(i).GetCell(2).ToString().Trim() == "分提單號")
                    {
                        read = true;
                        continue;
                    }
                    if (read && source != "" && trackingno != "" && dlv_inv != "" && tax != "" && ccfee != "" && fee != "" && cod != "")
                    {
                        if (source == "G類")
                        {
                            source = "TPCT";
                            //type = "G";
                        }
                        //else
                        //{
                        //    type = "";
                        //}
                        type = "G";
                        dr = dt_Data.NewRow();
                        dr["source"] = source;
                        //dr["main_number"] = trackingno;
                        dr["trackingno"] = trackingno;
                        dr["type"] = type;
                        dr["dlv_inv"] = dlv_inv;
                        dr["tax1"] = tax;
                        dr["tax2"] = "0";
                        dr["ccfee"] = ccfee;
                        dr["fee"] = fee;
                        dr["cod"] = cod;
                        dr["include_tax"] = include_tax;
                        dr["recipient"] = recipient;
                        dr["to_dlv_cod"] = Convert.ToInt32(dr["tax1"]) + Convert.ToInt32(dr["cod"]) + Convert.ToInt32(dr["ccfee"]) + Convert.ToInt32(dr["fee"]);
                        dr["customer"] = customer;
                        //if (include_tax == "N")
                        //{
                        //    //dr["customer"] = "新竹物流";
                        //    //不包稅：代收貨款+稅金+手續費
                        //    //dr["to_dlv_cod"] = Convert.ToInt32(dr["tax1"]) + Convert.ToInt32(dr["cod"]) + Convert.ToInt32(dr["fee"]);
                        //}
                        //else {
                        //    //包稅：純代收貨款金額
                        //    //dr["to_dlv_cod"] = dr["cod"].ToString();
                        //}
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }


        DataTable GetDataTable()
        {
            DataTable dt_Data = new DataTable();
            dt_Data.Columns.Add("main_number", typeof(string));//主號
            dt_Data.Columns.Add("clearance_number", typeof(string));//報單號碼
            dt_Data.Columns.Add("clearance_type", typeof(string));//報單類型
            dt_Data.Columns.Add("bl_no", typeof(string));//分號
            dt_Data.Columns.Add("reg_no", typeof(string));//海掛
            dt_Data.Columns.Add("mainfest", typeof(string));//艙單
            dt_Data.Columns.Add("tax_number", typeof(string));//稅單號碼
            dt_Data.Columns.Add("tax_recid", typeof(string));//納稅義務人身分證號
            dt_Data.Columns.Add("tax_payer", typeof(string));//納稅義務人
            dt_Data.Columns.Add("tax", typeof(string));//稅單金額
            dt_Data.Columns.Add("prt_time", typeof(string));//列印時間
            return dt_Data;
        }

        DataTable GetDataTableG()
        {
            DataTable dt_Data = new DataTable();
            dt_Data.Columns.Add("source", typeof(string));//倉儲
            dt_Data.Columns.Add("customer", typeof(string));//客戶名稱
            dt_Data.Columns.Add("main_number", typeof(string));//分提單號
            dt_Data.Columns.Add("trackingno", typeof(string));//分提單號
            dt_Data.Columns.Add("type", typeof(string));//報關類型
            dt_Data.Columns.Add("dlv_inv", typeof(string));//派送單號
            dt_Data.Columns.Add("tax1", typeof(string)); //稅金1
            dt_Data.Columns.Add("tax2", typeof(string)); //稅金2
            dt_Data.Columns.Add("ccfee", typeof(string)); //報關費
            dt_Data.Columns.Add("fee", typeof(string));//手續費
            dt_Data.Columns.Add("cod", typeof(string));//代收貨款
            dt_Data.Columns.Add("include_tax", typeof(string));//是否包稅
            dt_Data.Columns.Add("to_dlv_cod", typeof(string));//總金額
            dt_Data.Columns.Add("recipient", typeof(string));//收件人
            return dt_Data;
        }

        //繁簡轉換Funtion,參數 Language 為 Big5 則轉繁體、GB2312 則轉簡體，其他狀況則輸出原字串
        private string ConvertLanguage(string sourceString, string language)
        {
            string newString = string.Empty;
            switch (language)
            {
                case "Big5":
                    newString = ChineseConverter.Convert(sourceString, ChineseConversionDirection.SimplifiedToTraditional);
                    break;
                case "GB2312":
                    newString = ChineseConverter.Convert(sourceString, ChineseConversionDirection.TraditionalToSimplified);
                    break;
                default:
                    newString = sourceString;
                    break;
            }
            return newString;
        }
    }
}

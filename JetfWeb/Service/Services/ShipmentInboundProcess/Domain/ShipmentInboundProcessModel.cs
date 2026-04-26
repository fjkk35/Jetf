using Service.EnumTax;
using Service.Extensions;
using System;

namespace Service.Services.ShipmentInboundProcess.Domain
{
    /// <summary>
    /// �f��J�w�B�z��ܼҫ�
    /// �Ω�e�ݬd�ߦC����ܻP����B�z�@�~
    /// </summary>
    public class ShipmentInboundProcessModel
    {
        /// <summary>
        /// �D�� Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// �J�w���
        /// </summary>
        public DateTime InboundDate { get; set; }

        /// <summary>
        /// �i�f�覡�]�Ҧp���B�B�ŹB�^
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// �Ȥ�N�X
        /// </summary>
        public string CustCode { get; set; }

        /// <summary>
        /// �Ȥ�W��
        /// </summary>
        public string CustName { get; set; }

        /// <summary>
        /// ���󤽥q�N�X(�ŹB�~��)
        /// </summary>
        public string TransNo { get; set; }

        /// <summary>
        /// ���󤽥q�W��
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// �l�ܳ渹�ΰl�ܸ�
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// �f��ӷ�����
        /// </summary>
        public ShipmentInboundSourceType SourceType { get; set; }

        /// <summary>
        /// �f��ӷ��W��
        /// </summary>
        public string SourceTypeName => SourceType.ToDescription();

        /// <summary>
        /// �h���]�]�O�d�^
        /// </summary>
        public string ReturnReason { get; set; }

        /// <summary>
        /// ���X�渹
        /// </summary>
        public string ReturnTrackingNo { get; set; }

        /// <summary>
        /// �B�z����
        /// </summary>
        public ShipmentInboundProcessType? ProcessType { get; set; }

        /// <summary>
        /// �H��Ū��r�e�{���B�z�����W��
        /// </summary>
        public string ProcessTypeName => ProcessType?.ToDescription();

        /// <summary>
        /// �|��
        /// </summary>
        public decimal? Tax { get; set; }

        /// <summary>
        /// �����O
        /// </summary>
        public decimal? Ccfee { get; set; }

        /// <summary>
        /// ��I��
        /// </summary>
        public decimal? Cod { get; set; }
    }
}

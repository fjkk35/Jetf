using System.Data.Entity;

namespace Service.Data
{
    public class JetfDbContext : DbContext
    {
        static JetfDbContext()
        {
            Database.SetInitializer<JetfDbContext>(null);
        }

        public JetfDbContext()
            : base("name=DefaultConnection")
        {
            Configuration.ProxyCreationEnabled = false;
            Configuration.LazyLoadingEnabled = false;
        }

        public DbSet<ShipmentInboundEntity> ShipmentInbounds { get; set; }

        public DbSet<ShipmentInboundProcessStageEntity> ShipmentInboundProcessStages { get; set; }

        public DbSet<SeaClearanceEntity> SeaClearances { get; set; }

        public DbSet<SeaClearanceDetailEntity> SeaClearanceDetails { get; set; }

        public DbSet<SeaClearanceAbnormalStateEntity> SeaClearanceAbnormalStates { get; set; }

        public DbSet<SeaClearanceAbnormalStateDetailEntity> SeaClearanceAbnormalStateDetails { get; set; }

        public DbSet<SeaClearanceAuthorizationFormEntity> SeaClearanceAuthorizationForms { get; set; }

        public DbSet<SeaClearanceAuthorizationFormDetailEntity> SeaClearanceAuthorizationFormDetails { get; set; }

        public DbSet<SeaClearanceDetailApprovalCategoryEntity> SeaClearanceDetailApprovalCategories { get; set; }

        public DbSet<SeaClearanceDetailEditHistoryEntity> SeaClearanceDetailEditHistories { get; set; }

        public DbSet<SeaClearanceDetailGb301Entity> SeaClearanceDetailGb301s { get; set; }

        public DbSet<SeaClearanceDetailGb321Entity> SeaClearanceDetailGb321s { get; set; }

        public DbSet<SeaClearanceDetailOriginalMappingEntity> SeaClearanceDetailOriginalMappings { get; set; }

        public DbSet<SeaClearanceFeeEntity> SeaClearanceFees { get; set; }

        public DbSet<SeaClearanceGb301Entity> SeaClearanceGb301s { get; set; }

        public DbSet<SeaClearanceProcessorEntity> SeaClearanceProcessors { get; set; }

        public DbSet<SeaClearanceRemarkEntity> SeaClearanceRemarks { get; set; }

        public DbSet<SeaClearanceStepEntity> SeaClearanceSteps { get; set; }

        public DbSet<SeaClearanceStepDetailEntity> SeaClearanceStepDetails { get; set; }

        public DbSet<CustomsBrokerEntity> CustomsBrokers { get; set; }

        public DbSet<CustomsBrokerageEntity> CustomsBrokerages { get; set; }

        public DbSet<AbnormalStateEntity> AbnormalStates { get; set; }

        public DbSet<AbnormalStateDetailEntity> AbnormalStateDetails { get; set; }

        public DbSet<ApprovalCategoryEntity> ApprovalCategories { get; set; }

        public DbSet<AuthorizationFormEntity> AuthorizationForms { get; set; }

        public DbSet<AuthorityEntity> Authorities { get; set; }

        public DbSet<AuthorityGroupEntity> AuthorityGroups { get; set; }

        public DbSet<AuthorityGroupDetailEntity> AuthorityGroupDetails { get; set; }

        public DbSet<ShipmentInboundExceptionEntity> ShipmentInboundExceptions { get; set; }

        public DbSet<ShipmentInboundExceptionReasonEntity> ShipmentInboundExceptionReasons { get; set; }

        public DbSet<ShipmentInboundEditHistoryEntity> ShipmentInboundEditHistories { get; set; }

        public DbSet<FeeMasterTestEntity> FeeMasterTests { get; set; }

        public DbSet<ShipmentInboundLocationHistoryEntity> ShipmentInboundLocationHistories { get; set; }

        public DbSet<FeeMasterEntity> FeeMasters { get; set; }

        public DbSet<FeeMasterDetailEntity> FeeMasterDetails { get; set; }

        public DbSet<FeeMasterLogEntity> FeeMasterLogs { get; set; }

        public DbSet<FeeMasterModifyEntity> FeeMasterModifies { get; set; }

        public DbSet<CustomerMasterEntity> CustomerMasters { get; set; }

        public DbSet<CustomerSpecialEntity> CustomerSpecials { get; set; }

        public DbSet<BatchSearchCargo2Entity> BatchSearchCargo2s { get; set; }

        public DbSet<SeaTaxUploadEntity> SeaTaxUploads { get; set; }

        public DbSet<SeaShenzhenOriginalEntity> SeaShenzhenOriginals { get; set; }

        public DbSet<ShenzhenFeeMasterEntity> ShenzhenFeeMasters { get; set; }

        public DbSet<ErrorOrderSendEntity> ErrorOrderSends { get; set; }

        public DbSet<ErrorOrderSendDetailEntity> ErrorOrderSendDetails { get; set; }

        public DbSet<ErrorOrderSmsMessageEntity> ErrorOrderSmsMessages { get; set; }

        public DbSet<LineGroupEntity> LineGroups { get; set; }

        public DbSet<ScanCargoArrivalTimeEntity> ScanCargoArrivalTimes { get; set; }

        public DbSet<CargoSignReceiptEntity> CargoSignReceipts { get; set; }

        public DbSet<SeaClearanceCustomerEntity> SeaClearanceCustomers { get; set; }

        public DbSet<SeaClearanceCustTaxPaymentEntity> SeaClearanceCustTaxPayments { get; set; }

        public DbSet<SeaClearanceSjlTaxPaymentEntity> SeaClearanceSjlTaxPayments { get; set; }

        public DbSet<TelegramGroupEntity> TelegramGroups { get; set; }

        public DbSet<StepEntity> Steps { get; set; }

        public DbSet<StepConditionEntity> StepConditions { get; set; }

        public DbSet<StepDetailEntity> StepDetails { get; set; }

        public DbSet<UserAuthorityGroupEntity> UserAuthorityGroups { get; set; }

        public DbSet<UserMasterEntity> UserMasters { get; set; }
    }
}

namespace Service.Services.AccsNew.Domain
{
    public class AccsNewLoginRequest
    {
        public string UserId { get; set; }

        public string UserWd { get; set; }

        public string VerifyCode { get; set; }

        public string CaptchaId { get; set; }
    }
}
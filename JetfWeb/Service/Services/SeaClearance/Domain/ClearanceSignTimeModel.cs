using System;

namespace Service.Services.SeaClearance.Domain
{
    public class ClearanceSignTimeModel
    {
        /// <summary>
        /// 入倉日期
        /// </summary>
        public DateTime? SignInTime { get; set; }

        /// <summary>
        /// 出倉日期
        /// </summary>
        public DateTime? SignOutTime { get; set; }
    }
}

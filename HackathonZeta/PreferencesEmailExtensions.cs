using PX.Data;
using PX.Objects;
using PX.SM;
using System;

namespace PX.SM
{
    public class PreferencesEmailExt : PXCacheExtension<PX.SM.PreferencesEmail>
    {
        public static bool IsActive() => true;

        #region UsrCombinedAttachmentSizeLimit
        [PXDBInt(MaxValue = 150000, MinValue = 1)]
        [PXDefault(25000)]
        [PXUIField(DisplayName = "Combined Attachment Size Limit")]
        public virtual int? UsrCombinedAttachmentSizeLimit { get; set; }
        public abstract class usrCombinedAttachmentSizeLimit : PX.Data.BQL.BqlInt.Field<usrCombinedAttachmentSizeLimit> { }
        #endregion
    }
}
using System;
using PX.Data;
using PX.SM;

namespace HackathonZeta
{
    [Serializable]
    [PXCacheName("TZEmailAddFile")]
    public class TZEmailAddFile : IBqlTable
    {
        #region FileID
        [PXDBGuid(IsKey = true)]
        [PXUIField(DisplayName = "File ID")]
        [PXSelector(typeof(Search<UploadFile.fileID>), DescriptionField = typeof(UploadFile.name), CacheGlobal = true)]
        public virtual Guid? FileID { get; set; }
        public abstract class fileID : PX.Data.BQL.BqlGuid.Field<fileID> { }
        #endregion

        #region NoteID
        [PXDBGuid(IsKey = true)]
        [PXUIField(DisplayName = "Note ID")]
        public virtual Guid? NoteID { get; set; }
        public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
        #endregion

        #region IsIncluded
        [PXBool]
        [PXUIField(DisplayName = "Is Included")]
        public virtual bool? IsIncluded { get; set; }
        public abstract class isIncluded : PX.Data.BQL.BqlBool.Field<isIncluded> { }
        #endregion

        #region Tstamp
        [PXDBTimestamp()]
        [PXUIField(DisplayName = "Tstamp")]
        public virtual byte[] Tstamp { get; set; }
        public abstract class tstamp : PX.Data.BQL.BqlByteArray.Field<tstamp> { }
        #endregion

        #region CreatedByID
        [PXDBCreatedByID()]
        public virtual Guid? CreatedByID { get; set; }
        public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
        #endregion

        #region CreatedByScreenID
        [PXDBCreatedByScreenID()]
        public virtual string CreatedByScreenID { get; set; }
        public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
        #endregion

        #region CreatedDateTime
        [PXDBCreatedDateTime()]
        public virtual DateTime? CreatedDateTime { get; set; }
        public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
        #endregion

        #region LastModifiedByID
        [PXDBLastModifiedByID()]
        public virtual Guid? LastModifiedByID { get; set; }
        public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
        #endregion

        #region LastModifiedByScreenID
        [PXDBLastModifiedByScreenID()]
        public virtual string LastModifiedByScreenID { get; set; }
        public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
        #endregion

        #region LastModifiedDateTime
        [PXDBLastModifiedDateTime()]
        public virtual DateTime? LastModifiedDateTime { get; set; }
        public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
        #endregion
    }
}
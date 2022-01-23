using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.AR;
using PX.Objects.PM;
using PX.SM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HackathonZeta
{
    public class ARInvoiceEntryExt : PXGraphExtension<ARInvoiceEntry>
    {

        public PXSelect<TZEmailAddFile, Where<TZEmailAddFile.noteID, Equal<Current<ARInvoice.noteID>>>> SelectAdditionalEmailAttachments;
        public PXSetup<PreferencesEmail> EmailPreferences;
        public PXSelect<NoteDoc, Where<NoteDoc.noteID, Equal<Current<TZEmailAddFile.noteID>>>> AttachedFilesIDs;
        public static bool IsActive() => true;

        [PXLocalizable]
        public static class Messages
        {
            public const string CombinedAttachmentSizeTooLarge = "The combined size of the attachments is {0} which is larger than the limit set in Email Preferences of {1}";
        }


        public PXAction<ARInvoice> ShowAdditionalEmailAttachments;

        [PXUIField(DisplayName = "Show Email Attachments")]
        [PXButton]
        public virtual IEnumerable showAdditionalEmailAttachments(PXAdapter adapter)
        {
            SelectAdditionalEmailAttachments.AskExt(true);

            return adapter.Get();
        }


        public IEnumerable<TZEmailAddFile> selectAdditionalEmailAttachments()
        {
            List<TZEmailAddFile> files = new List<TZEmailAddFile>();

            //Add all selected files
            foreach (TZEmailAddFile selectedFile in PXSelect<TZEmailAddFile, Where<TZEmailAddFile.noteID, Equal<Current<ARInvoice.noteID>>>>.Select(Base))
            {
                selectedFile.IsIncluded = selectedFile.IsIncluded ?? true;
                files.Add(selectedFile);
            }

            // Add remaining files
            foreach (var fileID in PXNoteAttribute.GetFileNotes(Base.Document.Cache, Base.Document.Current))
            {
                if (!files.Any(file => file.FileID == fileID))
                {
                    files.Add(new TZEmailAddFile
                    {
                        IsIncluded = false,
                        FileID = fileID,
                        NoteID = Base.Document.Current.NoteID
                    });
                }
            }

            return files;
        }

        public delegate void PersistDelegate();

        [PXOverride]
        public void Persist(PersistDelegate baseMethod)
        {
            foreach (TZEmailAddFile file in SelectAdditionalEmailAttachments.Select())
            {
                if (file.IsIncluded == false)
                {
                    SelectAdditionalEmailAttachments.Delete(file);
                }
            }

            baseMethod();
        }

        private List<Guid?> getAdditionalFileIDs()
        {
            var selectedFiles = new List<Guid?>();

            foreach (TZEmailAddFile file in SelectAdditionalEmailAttachments.Select())
            {
                if (file.IsIncluded == true) selectedFiles.Add(file.FileID);
            }

            return selectedFiles;
        }

        private void performSizeCheckOnAdditionalAttachments(List<Guid?> additionalFileAttachments)
        {

            var additionalAttachmentsSize = 0;

            var uploadMaint = PXGraph.CreateInstance<UploadFileMaintenance>();

            foreach (var nullableFileID in additionalFileAttachments)
            {
                if (nullableFileID is Guid fileID)
                {
                    var fileInfo = uploadMaint.GetFile(fileID);
                    additionalAttachmentsSize += fileInfo.BinData.Length / 1000;
                }

            }

            PreferencesEmailExt emailPrefsExt = (EmailPreferences.SelectSingle() as PreferencesEmail).GetExtension<PreferencesEmailExt>();

            if (emailPrefsExt.UsrCombinedAttachmentSizeLimit is int sizeLimit)
            {
                if (sizeLimit < additionalAttachmentsSize)
                {
                    throw new PXException(Messages.CombinedAttachmentSizeTooLarge, additionalAttachmentsSize, sizeLimit);
                }
            }
        }

        public delegate IEnumerable NotificationDelegate(PXAdapter adapter, [PXString] string notificationCD);

        [PXOverride]
        public IEnumerable Notification(PXAdapter adapter, string notificationCD, NotificationDelegate baseMethod)
        {
            var additionalFileAttachments = getAdditionalFileIDs();

            var hasAdditionalFileAttachments = additionalFileAttachments.Count > 0;

            if (hasAdditionalFileAttachments)
            {
                performSizeCheckOnAdditionalAttachments(additionalFileAttachments);

                return HandleAdditionalAttachments(adapter, notificationCD, additionalFileAttachments);

            }
            else
            {
                return baseMethod(adapter, notificationCD);
            }
        }
        
        private IEnumerable HandleAdditionalAttachments(PXAdapter adapter, string notificationCD, List<Guid?> additionalFileAttachments)
        {

            foreach (ARInvoice doc in adapter.Get().RowCast<ARInvoice>())
            {
                Base.Document.Current = doc;

                Dictionary<string, string> parameters = new Dictionary<string, string>
                {
                    ["DocType"] = doc.DocType,
                    ["RefNbr"] = doc.RefNbr
                };

                using (var ts = new PXTransactionScope())
                {
                    if (ProjectDefaultAttribute.IsProject(Base, doc.ProjectID) && Base.Activity.IsProjectSourceActive(doc.ProjectID, notificationCD))
                    {
                        //No isMassProcess parameter on overload
                        Base.Activity.SendNotification(PMNotificationSource.Project, notificationCD, doc.BranchID, parameters, additionalFileAttachments);
                    }
                    else
                    {
                        Base.Activity.SendNotification(ARNotificationSource.Customer, notificationCD, doc.BranchID, parameters, additionalFileAttachments);
                    }
                    Base.Save.Press();

                    ts.Complete();
                }

                yield return doc;
            }
        }
    }


}
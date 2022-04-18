using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Text;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;
using PX.Common;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.TZ;
using PX.SM;
using PX.Objects.IN.Overrides.INDocumentRelease;
using POLineType = PX.Objects.PO.POLineType;
using POReceiptLine = PX.Objects.PO.POReceiptLine;
using PX.CarrierService;
using PX.Data.DependencyInjection;
using PX.LicensePolicy;
using PX.Objects.SO.Services;
using PX.Objects.PO;
using PX.Objects.AR.MigrationMode;
using PX.Objects.Common;
using PX.Objects.Common.Discount;
using PX.Objects.Common.Extensions;
using PX.Common.Collection;
using PX.Objects.SO.GraphExtensions.CarrierRates;
using PX.Api;
using LocationStatus = PX.Objects.IN.Overrides.INDocumentRelease.LocationStatus;
using ShipmentActions = PX.Objects.SO.SOShipmentEntryActionsAttribute;
using PX.Objects;
using PX.Objects.SO;

namespace  CS3HackathonZeta
{
  public class SOShipmentEntry_Extension : PXGraphExtension<SOShipmentEntry>
  {
    public PXSelect<TZEmailAddFile, Where<TZEmailAddFile.noteID, Equal<Current<SOShipment.noteID>>>> SelectAdditionalEmailAttachments;
    public PXSetup<PreferencesEmail> EmailPreferences;
    public PXSelect<NoteDoc, Where<NoteDoc.noteID, Equal<Current<TZEmailAddFile.noteID>>>> AttachedFilesIDs;
    public PXAction<SOShipment> ShowAdditionalEmailAttachments;

    public static bool IsActive() => true;

    [PXButton, PXUIField(DisplayName = "Add Email Attachments to Confirmation")]    
    public virtual IEnumerable showAdditionalEmailAttachments(PXAdapter adapter)
    {
      //PXTrace.WriteInformation("Running");
      int num = (int) this.SelectAdditionalEmailAttachments.AskExt(true);
      return adapter.Get();
    }

    public IEnumerable<TZEmailAddFile> selectAdditionalEmailAttachments()
    {
      List<TZEmailAddFile> source = new List<TZEmailAddFile>();
      PXTrace.WriteInformation("Called Select Files");
      foreach (PXResult<TZEmailAddFile> pxResult in PXSelectBase<TZEmailAddFile, PXSelect<TZEmailAddFile, Where<TZEmailAddFile.noteID, Equal<Current<SOShipment.noteID>>>>.Config>.Select((PXGraph) this.Base))
      {
        TZEmailAddFile tzEmailAddFile = (TZEmailAddFile) pxResult;
        PXTrace.WriteInformation(tzEmailAddFile.FileID.ToString());
        tzEmailAddFile.IsIncluded = new bool?((tzEmailAddFile.IsIncluded ?? true) != false);
        source.Add(tzEmailAddFile);
      }
      foreach (Guid fileNote in PXNoteAttribute.GetFileNotes(this.Base.Document.Cache, (object) this.Base.Document.Current))
      {
        Guid fileID = fileNote;
        if (!source.Any<TZEmailAddFile>((Func<TZEmailAddFile, bool>) (file =>
        {
          Guid? fileId = file.FileID;
          Guid guid = fileID;
          if (!fileId.HasValue)
            return false;
          return !fileId.HasValue || fileId.GetValueOrDefault() == guid;
        })))
          source.Add(new TZEmailAddFile()
          {
            IsIncluded = new bool?(false),
            FileID = new Guid?(fileID),
            NoteID = ((SOShipment) this.Base.Document.Current).NoteID
          });
      }
      return (IEnumerable<TZEmailAddFile>) source;
    }

    [PXOverride]
    public void Persist(SOShipmentEntry_Extension.PersistDelegate baseMethod)
    {
      foreach (PXResult<TZEmailAddFile> pxResult in this.SelectAdditionalEmailAttachments.Select())
      {
        TZEmailAddFile tzEmailAddFile = (TZEmailAddFile) pxResult;
        bool? isIncluded = tzEmailAddFile.IsIncluded;
        bool flag = false;
        if (isIncluded.GetValueOrDefault() == flag & isIncluded.HasValue)
          this.SelectAdditionalEmailAttachments.Delete(tzEmailAddFile);
      }
      baseMethod();
    }

    private List<Guid?> getAdditionalFileIDs()
    {
      List<Guid?> additionalFileIds = new List<Guid?>();
      foreach (PXResult<TZEmailAddFile> pxResult in this.SelectAdditionalEmailAttachments.Select())
      {
        TZEmailAddFile tzEmailAddFile = (TZEmailAddFile) pxResult;
        bool? isIncluded = tzEmailAddFile.IsIncluded;
        bool flag = true;
        if (isIncluded.GetValueOrDefault() == flag & isIncluded.HasValue)
          additionalFileIds.Add(tzEmailAddFile.FileID);
      }
      return additionalFileIds;
    }

    private void performSizeCheckOnAdditionalAttachments(List<Guid?> additionalFileAttachments)
    {
      int num1 = 0;
      UploadFileMaintenance instance = PXGraph.CreateInstance<UploadFileMaintenance>();
      foreach (Guid? additionalFileAttachment in additionalFileAttachments)
      {
        Guid valueOrDefault= new Guid();
        int num2;
        if (additionalFileAttachment.HasValue)
        {
          valueOrDefault = additionalFileAttachment.GetValueOrDefault();
          num2 = 1;
        }
        else
          num2 = 0;
        if (num2 != 0)
        {
          FileInfo file = instance.GetFile(valueOrDefault);
          num1 += file.BinData.Length / 1000;
        }
      }
      long? attachmentSizeLimit = this.EmailPreferences.SelectSingle().GetExtension<PreferencesEmailExt>().UsrCombinedAttachmentSizeLimit;
      long valueOrDefault1=0;
      int num3;
      if (attachmentSizeLimit.HasValue)
      {
        valueOrDefault1 = attachmentSizeLimit.GetValueOrDefault();
        num3 = 1;
      }
      else
        num3 = 0;
      if (num3 != 0 && valueOrDefault1 < num1)
        throw new PXException("The combined size of the attachments is {0} which is larger than the limit set in Email Preferences of {1}", new object[2]
        {
          (object) num1,
          (object) valueOrDefault1
        });
    }

    [PXOverride]
    public IEnumerable Notification(
      PXAdapter adapter,
      string notificationCD,
      SOShipmentEntry_Extension.NotificationDelegate baseMethod)
    {
      List<Guid?> additionalFileIds = this.getAdditionalFileIDs();
      if (additionalFileIds.Count <= 0)
        return baseMethod(adapter, notificationCD);
      this.performSizeCheckOnAdditionalAttachments(additionalFileIds);
      return this.HandleAdditionalAttachments(adapter, notificationCD, additionalFileIds);
    }

    private IEnumerable HandleAdditionalAttachments(
      PXAdapter adapter,
      string notificationCD,
      List<Guid?> additionalFileAttachments)
    {
      foreach (SOShipment doc in adapter.Get<SOShipment>())
      {
        this.Base.Document.Current = doc;
        Dictionary<string, string> parameters = new Dictionary<string, string>()
        {          
          ["SOShipment.ShipmentNbr"] = ((SOShipment) doc).ShipmentNbr
        };
        PX.Objects.GL.Branch branch = PXSelectReadonly2<PX.Objects.GL.Branch, InnerJoin<INSite, On<INSite.branchID, Equal<PX.Objects.GL.Branch.branchID>>>,
            Where<INSite.siteID, Equal<Optional<SOShipment.destinationSiteID>>,
                And<Current<SOShipment.shipmentType>, Equal<SOShipmentType.transfer>,
              Or<INSite.siteID, Equal<Optional<SOShipment.siteID>>,
                And<Current<SOShipment.shipmentType>, NotEqual<SOShipmentType.transfer>>>>>>
          .SelectSingleBound(Base, new object[] {doc});
        
        using (PXTransactionScope ts = new PXTransactionScope())
        {
         // if (ProjectDefaultAttribute.IsProject((PXGraph) this.Base, doc.ProjectID) && this.Base.Activity.IsProjectSourceActive(doc.ProjectID, notificationCD))
         //   ((CRActivityListBase<ARInvoice, CRPMTimeActivity>) this.Base.Activity).SendNotification("Project", notificationCD, ((ARRegister) doc).BranchID, (IDictionary<string, string>) parameters, (IList<Guid?>) additionalFileAttachments);
         // else
            ((CRActivityListBase<SOShipment, CRPMTimeActivity>) this.Base.Activity).SendNotification("Customer", notificationCD, (branch != null && branch.BranchID != null) ? branch.BranchID : Base.Accessinfo.BranchID, (IDictionary<string, string>) parameters, (IList<Guid?>) additionalFileAttachments);
          ((PXGraph<SOShipmentEntry, SOShipment>) this.Base).Save.Press();
          ts.Complete();
        }
        yield return (object) doc;
        parameters = (Dictionary<string, string>) null;
      }
    }

    [PXLocalizable]
    public static class Messages
    {
      public const string CombinedAttachmentSizeTooLarge = "The combined size of the attachments is {0} which is larger than the limit set in Email Preferences of {1}";
    }

    public delegate void PersistDelegate();

    public delegate IEnumerable NotificationDelegate(
      PXAdapter adapter,
      [PXString] string notificationCD);
  }
}

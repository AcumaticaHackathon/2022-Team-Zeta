using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.AR;
using PX.Objects.PM;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HackathonZeta
{
	public class ARInvoiceEntryExt : PXGraphExtension<ARInvoiceEntry>
	{

		public PXSelect<TZEmailAddFile, Where<TZEmailAddFile.noteID, Equal<Current<ARInvoice.noteID>>>> SelectableFiles;
		public static bool IsActive() => true;

		
		public PXAction<ARInvoice> ShowAdditionalFiles;

		[PXUIField(DisplayName = "Show Additional Files")]
		[PXButton]
		public virtual IEnumerable showAdditionalFiles(PXAdapter adapter)
        {
			SelectableFiles.AskExt(true);

			return adapter.Get();
        }
		
		
		public IEnumerable<TZEmailAddFile> selectableFiles()
        {
			List<TZEmailAddFile> files = new List<TZEmailAddFile>();

			//Add all selected files
			foreach(TZEmailAddFile selectedFile in PXSelect<TZEmailAddFile, Where<TZEmailAddFile.noteID, Equal<Current<ARInvoice.noteID>>>>.Select(Base))
            {
				selectedFile.IsIncluded = selectedFile.IsIncluded ?? true;
				files.Add(selectedFile);
            }
			
			// Add remaining files
			foreach(var fileID in PXNoteAttribute.GetFileNotes(Base.Document.Cache, Base.Document.Current))
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
			foreach(TZEmailAddFile file in SelectableFiles.Select())
            {
				if(file.IsIncluded == false)
                {
					SelectableFiles.Delete(file);
                }
            }

			baseMethod();
        }

        private List<string> getAdditionalNotificationCDs()
		{
			return new List<string>();
		}

		private List<Guid?> getAdditionalFileIDs()
		{
			var selectedFiles = new List<Guid?>();

			 foreach(TZEmailAddFile file in SelectableFiles.Select())
            {
				if (file.IsIncluded == true) selectedFiles.Add(file.FileID); 
            }

			return selectedFiles;
		}

		public delegate IEnumerable NotificationDelegate(PXAdapter adapter, [PXString] string notificationCD);

		[PXOverride]
		public IEnumerable Notification(PXAdapter adapter, [PXString] string notificationCD, NotificationDelegate baseMethod)
		{
			var additionalReports = getAdditionalNotificationCDs();
			var additionalFiles = getAdditionalFileIDs();

			var hasRealatedFilesOrReports = additionalReports.Count > 0 || additionalFiles.Count > 0;

			if (hasRealatedFilesOrReports)
			{

				foreach (ARInvoice doc in adapter.Get().RowCast<ARInvoice>())
				{
					Base.Document.Current = doc;

					additionalReports.Add(notificationCD);

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
							Base.Activity.SendNotification(PMNotificationSource.Project, additionalReports, doc.BranchID, parameters, additionalFiles);
						}
						else
						{
							Base.Activity.SendNotification(ARNotificationSource.Customer, additionalReports, doc.BranchID, parameters, additionalFiles);
						}
						Base.Save.Press();

						ts.Complete();
					}

					yield return doc;
				}
			}
			else
			{
				yield return baseMethod(adapter, notificationCD);
			}
		}
		
	}
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using VisualGit.UI;
using VisualGit.Scc;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.IO;
using SharpSvn;
using VisualGit.Selection;
using System.Runtime.InteropServices;

namespace VisualGit.Commands
{
    [Command(VisualGitCommand.ItemDelete)]
    class ItemDelete : CommandBase
    {
        public override void OnUpdate(CommandUpdateEventArgs e)
        {
            foreach (GitItem item in e.Selection.GetSelectedGitItems(true))
            {
                if (item.Exists)
                    return;
            }
            e.Enabled = false;
        }

        public override void OnExecute(CommandEventArgs e)
        {
            List<GitItem> toDelete = new List<GitItem>(e.Selection.GetSelectedGitItems(true));

            VisualGitMessageBox mb = new VisualGitMessageBox(e.Context);

            string body;

            // We do as if we are Visual Studio here: Same texts, same behavior (same chance on data loss)
            if (toDelete.Count == 1)
                body = string.Format(CommandStrings.XWillBeDeletedPermanently, toDelete[0].Name);
            else
                body = CommandStrings.TheSelectedItemsWillBeDeletedPermanently;

            if (DialogResult.OK != mb.Show(body, "", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation))
                return; // No delete

            int hr = VSConstants.S_OK;
            foreach (GitItem item in toDelete)
            {
                {
                    IVsUIHierarchy hier;
                    uint id;
                    IVsWindowFrame frame;

                    if (VsShellUtilities.IsDocumentOpen(e.Context, item.FullPath, Guid.Empty, out hier, out id, out frame))
                    {
                        hr = frame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
                        if (!ErrorHandler.Succeeded(hr))
                            break; // Show error and cancel further actions
                    }
                }

                try
                {
                    if (item.IsVersioned)
                    {
                        using (SvnClient cl = e.GetService<IGitClientPool>().GetNoUIClient())
                        {
                            SvnDeleteArgs da = new SvnDeleteArgs();
                            da.Force = true;
                            cl.Delete(item.FullPath, da);
                        }
                    }
                    else if (item.IsFile)
                        File.Delete(item.FullPath);
                    else if (item.IsDirectory)
                        Directory.Delete(item.FullPath, true); // Recursive delete!!
                }
                finally
                {
                    // TODO: Notify the working copy explorer here!
                    // (Maybe via one of these methods below)

                    e.GetService<IFileStatusCache>().MarkDirtyRecursive(item.FullPath);
                    e.GetService<IFileStatusMonitor>().ScheduleGlyphUpdate(item.FullPath);
                }

                // Ok, now remove the file from projects

                IProjectFileMapper pfm = e.GetService<IProjectFileMapper>();

                List<GitProject> projects = new List<GitProject>(pfm.GetAllProjectsContaining(item.FullPath));

                foreach (GitProject p in projects)
                {
                    IVsProject2 p2 = p.RawHandle as IVsProject2;

                    if (p2 == null)
                        continue;

                    VSDOCUMENTPRIORITY[] prio = new VSDOCUMENTPRIORITY[1];
                    int found;
                    uint id;
                    if (!ErrorHandler.Succeeded(p2.IsDocumentInProject(item.FullPath, out found, prio, out id)) || found == 0)
                        continue; // Probably already removed (mapping out of synch?)

                    hr = p2.RemoveItem(0, id, out found);

                    if (!ErrorHandler.Succeeded(hr))
                        break;
                }
            }

            if (!ErrorHandler.Succeeded(hr))
                mb.Show(Marshal.GetExceptionForHR(hr).Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
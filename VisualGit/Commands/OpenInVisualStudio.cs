// VisualGit\Commands\OpenInVisualStudio.cs
//
// Copyright 2008-2011 The AnkhSVN Project
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//
// Changes and additions made for VisualGit Copyright 2011 Pieter van Ginkel.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using VisualGit.UI;
using VisualGit.Scc;

namespace VisualGit.Commands
{
    [Command(VisualGitCommand.ItemOpenVisualStudio)]
    [Command(VisualGitCommand.ItemOpenFolder)]
    [Command(VisualGitCommand.ItemOpenSolutionExplorer)]
    [Command(VisualGitCommand.ItemOpenTextEditor)]
    [Command(VisualGitCommand.ItemOpenWindows)]
    class OpenInVisualStudio : CommandBase
    {
        public override void OnUpdate(CommandUpdateEventArgs e)
        {
            bool first = true;
            foreach (GitItem item in e.Selection.GetSelectedGitItems(false))
            {
                if (!item.Exists)
                    continue;

                switch (e.Command)
                {
                    case VisualGitCommand.ItemOpenTextEditor:
                    case VisualGitCommand.ItemOpenVisualStudio:
                        if (!item.IsFile)
                        {
                            e.Enabled = false;
                            return;
                        }
                        first = false;
                        break;
                    case VisualGitCommand.ItemOpenSolutionExplorer:
                        if (!item.InSolution)
                            continue;
                        goto default;
                    default:
                        if (!first)
                        {
                            e.Enabled = false;
                            return;
                        }
                        
                        first = false;
                        break;
                }
            }

            e.Enabled = !first;
        }
        public override void OnExecute(CommandEventArgs e)
        {
            VisualGitMessageBox mb = new VisualGitMessageBox(e.Context);
            foreach (GitItem item in e.Selection.GetSelectedGitItems(false))
            {
                if (!item.Exists)
                    continue;

                try
                {
                    switch (e.Command)
                    {
                        case VisualGitCommand.ItemOpenVisualStudio:
                            IProjectFileMapper mapper = e.GetService<IProjectFileMapper>();

                            if (mapper.IsProjectFileOrSolution(item.FullPath))
                                goto case VisualGitCommand.ItemOpenSolutionExplorer;
                            if (item.IsDirectory)
                                goto case VisualGitCommand.ItemOpenFolder;

                            if (!item.IsFile || !item.Exists)
                                continue;

                            VsShellUtilities.OpenDocument(e.Context, item.FullPath);
                            break;
                        case VisualGitCommand.ItemOpenTextEditor:
                            {
                                IVsUIHierarchy hier;
                                IVsWindowFrame frame;
                                uint id;

                                if (!item.IsFile)
                                    continue;

                                VsShellUtilities.OpenDocument(e.Context, item.FullPath, VSConstants.LOGVIEWID_TextView, out hier, out id, out frame);
                            }
                            break;
                        case VisualGitCommand.ItemOpenFolder:
                            if (!item.IsDirectory)
                                System.Diagnostics.Process.Start(Path.GetDirectoryName(item.FullPath));
                            else
                                System.Diagnostics.Process.Start(item.FullPath);
                            break;
                        case VisualGitCommand.ItemOpenWindows:
                            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(item.FullPath);
                            psi.Verb = "open";
                            System.Diagnostics.Process.Start(psi);
                            break;

                        case VisualGitCommand.ItemOpenSolutionExplorer:
                            IVsUIHierarchyWindow hierWindow = VsShellUtilities.GetUIHierarchyWindow(e.Context, new Guid(ToolWindowGuids80.SolutionExplorer));

                            IVsProject project = VsShellUtilities.GetProject(e.Context, item.FullPath) as IVsProject;

                            if (hierWindow != null)
                            {
                                int found;
                                uint id;
                                VSDOCUMENTPRIORITY[] prio = new VSDOCUMENTPRIORITY[1];
                                if (project != null && ErrorHandler.Succeeded(project.IsDocumentInProject(item.FullPath, out found, prio, out id)) && found != 0)
                                {
                                    hierWindow.ExpandItem(project as IVsUIHierarchy, id, EXPANDFLAGS.EXPF_SelectItem);
                                }
                                else if (string.Equals(item.FullPath, e.Selection.SolutionFilename, StringComparison.OrdinalIgnoreCase))
                                    hierWindow.ExpandItem(e.GetService<IVsUIHierarchy>(typeof(SVsSolution)), VSConstants.VSITEMID_ROOT, EXPANDFLAGS.EXPF_SelectItem);

                                // Now try to activate the solution explorer
                                IVsWindowFrame solutionExplorer;
                                Guid solutionExplorerGuid = new Guid(ToolWindowGuids80.SolutionExplorer);
                                IVsUIShell shell = e.GetService<IVsUIShell>(typeof(SVsUIShell));

                                if (shell != null)
                                {
                                    shell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref solutionExplorerGuid, out solutionExplorer);

                                    if (solutionExplorer != null)
                                        solutionExplorer.Show();
                                }

                            }
                            break;
                    }
                }
                catch (IOException ee)
                {
                    e.GetService<IVisualGitErrorHandler>().OnWarning(ee);
                }
                catch (COMException ee)
                {
                    e.GetService<IVisualGitErrorHandler>().OnWarning(ee);
                }
                catch (InvalidOperationException ee)
                {
                    e.GetService<IVisualGitErrorHandler>().OnWarning(ee);
                }
                catch (System.ComponentModel.Win32Exception ee)
                {
                    e.GetService<IVisualGitErrorHandler>().OnWarning(ee);
                }

            }
        }
    }
}

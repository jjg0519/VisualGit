using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using VisualGit.UI.RepositoryOpen;
using System.Windows.Forms;
using VisualGit.VS;
using SharpSvn;
using System.IO;
using VisualGit.Scc;

namespace VisualGit.Commands
{
    [Command(VisualGitCommand.FileFileOpenFromGit, AlwaysAvailable = true, ArgumentDefinition = "u")]
    [Command(VisualGitCommand.FileFileAddFromGit, AlwaysAvailable = true, ArgumentDefinition = "u")]
    [Command(VisualGitCommand.FileSccOpenFromGit)]
    [Command(VisualGitCommand.FileSccAddFromGit)]
    class OpenFromGit : CommandBase
    {
        public override void OnUpdate(CommandUpdateEventArgs e)
        {
            if (e.Command == VisualGitCommand.FileFileAddFromGit || e.Command == VisualGitCommand.FileSccAddFromGit)
            {
                if (!e.State.SolutionExists || e.State.SolutionBuilding || e.State.Debugging)
                    e.Enabled = e.Visible = false;
            }
        }

        public override void OnExecute(CommandEventArgs e)
        {
            Uri selectedUri = null;
            Uri rootUri = null;

            if (e.Argument is string && Uri.TryCreate((string)e.Argument, UriKind.Absolute, out selectedUri))
            { }
            else if (e.Argument is GitOrigin)
            {
                GitOrigin origin = (GitOrigin)e.Argument;
                selectedUri = origin.Uri;
                rootUri = origin.RepositoryRoot;
            }
            else if (e.Argument is Uri)
                selectedUri = (Uri)e.Argument;

            IVisualGitSolutionSettings settings = e.GetService<IVisualGitSolutionSettings>();

            if (e.PromptUser || selectedUri == null)
            {
                using (RepositoryOpenDialog dlg = new RepositoryOpenDialog())
                {
                    dlg.Context = e.Context;
                    dlg.Filter = settings.OpenProjectFilterName + "|" + settings.AllProjectExtensionsFilter + "|All Files (*.*)|*";

                    if (selectedUri != null)
                        dlg.SelectedUri = selectedUri;

                    if (e.Command != VisualGitCommand.FileFileOpenFromGit && e.Command != VisualGitCommand.FileSccOpenFromGit)
                    {
                        foreach (string ext in settings.SolutionFilter.Split(';'))
                        {
                            dlg.Filter = dlg.Filter.Replace(ext.Trim() + ';', "");
                        }
                    }

                    if (dlg.ShowDialog(e.Context) != DialogResult.OK)
                        return;

                    selectedUri = dlg.SelectedUri;
                    rootUri = dlg.SelectedRepositoryRoot;
                }
            }
            else if (rootUri == null)
            {
                if (!e.GetService<IProgressRunner>().RunModal("Retrieving Repository Root",
                    delegate(object sender, ProgressWorkerArgs a)
                    {

                        rootUri = a.Client.GetRepositoryRoot(selectedUri);

                    }).Succeeded)
                {
                    return;
                }
            }

            string path = settings.NewProjectLocation;

            string name = Path.GetFileNameWithoutExtension(SvnTools.GetFileName(selectedUri));

            string newPath;
            int n = 0;
            do
            {
                newPath = Path.Combine(path, name);
                if (n > 0)
                    newPath += string.Format("({0})", n);
                n++;
            }
            while (File.Exists(newPath) || Directory.Exists(newPath));

            using (CheckoutProject dlg = new CheckoutProject())
            {
                dlg.Context = e.Context;
                dlg.ProjectUri = selectedUri;
                dlg.RepositoryRootUri = rootUri;
                dlg.SelectedPath = newPath;
                dlg.GitOrigin = new GitOrigin(selectedUri, rootUri);
                dlg.HandleCreated += delegate
                {
                    FindRoot(e.Context, selectedUri, dlg);
                };

                if (dlg.ShowDialog(e.Context) != DialogResult.OK)
                    return;

                IVsSolution2 sol = e.GetService<IVsSolution2>(typeof(SVsSolution));

                if (sol != null)
                {
                    sol.CloseSolutionElement(VSConstants.VSITEMID_ROOT, null, 0); // Closes the current solution
                }

                IVisualGitSccService scc = e.GetService<IVisualGitSccService>();

                if (scc != null)
                    scc.RegisterAsPrimarySccProvider(); // Make us the current SCC provider!

                CheckOutAndOpenSolution(e, dlg.ProjectTop, null, dlg.ProjectTop, dlg.SelectedPath, dlg.ProjectUri);

                sol = e.GetService<IVsSolution2>(typeof(SVsSolution));

                if (sol != null)
                {
                    string file, user, dir;

                    if (ErrorHandler.Succeeded(sol.GetSolutionInfo(out dir, out file, out user))
                        && !string.IsNullOrEmpty(file))
                    {
                        scc.SetProjectManaged(null, true);
                    }
                }
            }
        }

        private static void CheckOutAndOpenSolution(CommandEventArgs e, SvnUriTarget checkoutLocation, SvnRevision revision, Uri projectTop, string localDir, Uri projectUri)
        {
            IProgressRunner runner = e.GetService<IProgressRunner>();

            runner.RunModal("Checking Out Solution", 
                delegate(object sender, ProgressWorkerArgs ee)
                    {
                        PerformCheckout(ee, checkoutLocation, revision, localDir);
                    });

            Uri file = projectTop.MakeRelativeUri(projectUri);

            string projectFile = SvnTools.GetNormalizedFullPath(Path.Combine(localDir, SvnTools.UriPartToPath(file.ToString())));

            OpenProject(e, projectFile);
        }

        private static void OpenProject(CommandEventArgs e, string projectFile)
        {
            IVisualGitSolutionSettings ss = e.GetService<IVisualGitSolutionSettings>();

            ss.OpenProjectFile(projectFile);
        }

        private static void PerformCheckout(ProgressWorkerArgs e, SvnUriTarget projectTop, SvnRevision revision, string localDir)
        {
            SvnCheckOutArgs a = new SvnCheckOutArgs();
            a.Revision = revision;

            e.Client.CheckOut(projectTop, localDir, a);
        }

        private static void FindRoot(IVisualGitServiceProvider context, Uri selectedUri, CheckoutProject dlg)
        {
            VisualGitAction ds = delegate
            {
                using (SvnClient client = context.GetService<ISvnClientPool>().GetClient())
                {
                    string value;
                    if (client.TryGetProperty(selectedUri, VisualGitSccPropertyNames.ProjectRoot, out value))
                    {
                        if (dlg.IsHandleCreated)
                            dlg.Invoke((VisualGitAction)delegate
                            {
                                try
                                {
                                    dlg.ProjectTop = new Uri(selectedUri, value);
                                }
                                catch { };
                            });
                    }
                }
            };

            ds.BeginInvoke(null, null);
        }
    }
}

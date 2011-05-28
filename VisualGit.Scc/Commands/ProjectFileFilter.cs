using System;
using System.Collections.Generic;
using System.Text;
using VisualGit.Commands;
using VisualGit.Selection;

namespace VisualGit.Scc.Commands
{
    [Command((VisualGitCommand)VisualGitCommandMenu.ProjectFileScc)]
    class ProjectFileFilter : ICommandHandler
    {
        #region ICommandHandler Members

        public void OnUpdate(CommandUpdateEventArgs e)
        {
            if (e.State.SccProviderActive)
                foreach (GitProject p in e.Selection.GetSelectedProjects(false))
                {
                    IGitProjectInfo pi = e.GetService<IProjectFileMapper>().GetProjectInfo(p);

                    if (p == null || pi == null || string.IsNullOrEmpty(pi.ProjectFile))
                    {
                        break; // No project file
                    }

                    if (!string.IsNullOrEmpty(pi.ProjectDirectory) &&
                        string.Equals(pi.ProjectDirectory, pi.ProjectFile, StringComparison.OrdinalIgnoreCase))
                    {
                        break; // Project file is directory
                    }

                    GitItem item = e.GetService<IFileStatusCache>()[pi.ProjectFile];

                    if (item != null && item.IsDirectory)
                        break; // Project file is not file

                    return; // Show the menu
                }

            e.Enabled = e.Visible = false;
        }

        public void OnExecute(CommandEventArgs e)
        {
            throw new InvalidOperationException(); // Never reached; not a real command
        }

        #endregion
    }
}
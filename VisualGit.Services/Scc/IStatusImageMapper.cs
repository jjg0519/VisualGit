using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace VisualGit.Scc
{
    public interface IStatusImageMapper
    {
        VisualGitGlyph GetStatusImageForGitItem(GitItem item);
        ImageList StatusImageList { get; }

        ImageList CreateStatusImageList();
    }
}
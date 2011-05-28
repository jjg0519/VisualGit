using System;
using System.Collections.Generic;
using System.Text;

namespace VisualGit.Scc.UI
{
    public interface IAnnotateSection : IGitRepositoryItem
    {
        string Author { get; }
        new long Revision { get; }
        DateTime Time { get; }
    }
}
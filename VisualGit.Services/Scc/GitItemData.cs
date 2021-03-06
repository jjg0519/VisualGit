// VisualGit.Services\Scc\GitItemData.cs
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
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using VisualGit.Selection;
using System.Diagnostics;

namespace VisualGit.Scc
{
    [DebuggerDisplay("File={FullPath}, Status={Status}")]
    public partial class GitItemData : VisualGitPropertyGridItem
    {
        readonly IVisualGitServiceProvider _context;
        readonly GitItem _item;
        public GitItemData(IVisualGitServiceProvider context, GitItem item)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            else if (item == null)
                throw new ArgumentNullException("item");

            _context = context;
            _item = item;
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public GitItem GitItem
        {
            get { return _item; }
        }

        [DisplayName("Full Path"), Category("Misc")]
        public string FullPath
        {
            get { return _item.FullPath; }
        }

        [DisplayName("File Name"), Category("Misc")]
        public string Name
        {
            get { return _item.Name; }
        }

        [DisplayName("Project"), Category("Visual Studio")]
        public string Project
        {
            get
            {
                IProjectFileMapper mapper = _context.GetService<IProjectFileMapper>();

                if (mapper != null)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (GitProject p in mapper.GetAllProjectsContaining(FullPath))
                    {
                        IGitProjectInfo info = mapper.GetProjectInfo(p);

                        if (info == null)
                        {
                            if (string.Equals(FullPath, mapper.SolutionFilename, StringComparison.OrdinalIgnoreCase))
                                return "<Solution>";
                        }
                        else
                        {
                            if (sb.Length > 0)
                                sb.Append(';');

                            sb.Append(info.UniqueProjectName);
                        }
                    }

                    return sb.ToString();
                }
                return "";
            }
        }

        PendingChangeStatus _chg;
        [DisplayName("Change"), Category("Git")]
        public string Change
        {
            get
            {
                VisualGitStatus status = _item.Status;
                PendingChangeKind kind = PendingChange.CombineStatus(status.State, GitItem.IsTreeConflicted, GitItem);

                if (kind == PendingChangeKind.None)
                    return "";

                if (_chg == null || _chg.State != kind)
                    _chg = new PendingChangeStatus(kind);

                return _chg.Text;
            }
        }

        [Category("Git"), Description("Current Revision")]
        public long? Revision
        {
            get
            {
                if (GitItem.IsVersioned)
                    return GitItem.Status.Revision;
                else
                    return null;
            }
        }

        [Category("Git"), DisplayName("Last Author")]
        [Description("Author of the Last Commit")]
        public string LastCommittedAuthor
        {
            get
            {
                if (GitItem.IsVersioned)
                    return GitItem.Status.LastChangeAuthor;
                else
                    return null;
            }
        }

        [Category("Git"), DisplayName("Last Revision")]
        [Description("Revision number of the Last Commit")]
        public long? LastCommittedRevision
        {
            get
            {
                if (GitItem.IsVersioned)
                    return GitItem.Status.LastChangeRevision;
                else
                    return null;
            }
        }

        [Category("Git"), DisplayName("Last Committed")]
        [Description("Time of the Last Commit")]
        public DateTime LastCommittedDate
        {
            get
            {
                DateTime dt = GitItem.Status.LastChangeTime;
                if (dt != DateTime.MinValue)
                    return dt.ToLocalTime();
                else
                    return DateTime.MinValue;
            }
        }

        protected override string ComponentName
        {
            get { return Name; }
        }

        protected override string ClassName
        {
            get { return "Path Status"; }
        }
    }
}

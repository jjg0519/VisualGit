// VisualGit.UI\PendingChanges\Commits\PendingCommitsView.cs
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
using VisualGit.Selection;
using Microsoft.VisualStudio;
using System.ComponentModel;
using VisualGit.UI.VSSelectionControls;
using VisualGit.VS;
using System.Windows.Forms;
using VisualGit.Scc;
using System.Drawing;
using System.ComponentModel.Design;
using VisualGit.Commands;
using Microsoft.VisualStudio.Shell.Interop;

namespace VisualGit.UI.PendingChanges.Commits
{
    public interface IPendingChangeSource
    {
        bool HasPendingChanges { get; }
        IEnumerable<PendingChange> PendingChanges { get; }
    }

    class PendingCommitsView : ListViewWithSelection<PendingCommitItem>, IPendingChangeSource
    {
        public PendingCommitsView()
        {
            StrictCheckboxesClick = true;
            FullRowSelect = true;
            HideSelection = false;
            AllowColumnReorder = true;
            CheckBoxes = true;
            Sorting = SortOrder.Ascending;
            Initialize();
        }

        public PendingCommitsView(IContainer container)
            : this()
        {
            container.Add(this);
        }


        public void Initialize()
        {
            SmartColumn path = new SmartColumn(this, PCStrings.PathColumn, 288, "Path");
            SmartColumn project = new SmartColumn(this, PCStrings.ProjectColumn, 76, "Project");
            SmartColumn change = new SmartColumn(this, PCStrings.ChangeColumn, 76, "Change");
            SmartColumn fullPath = new SmartColumn(this, PCStrings.FullPathColumn, 327, "FullPath");

            SmartColumn changeList = new SmartColumn(this, PCStrings.ChangeListColumn, 76, "ChangeList");
            SmartColumn folder = new SmartColumn(this, PCStrings.FolderColumn, 196, "Folder");
            SmartColumn locked = new SmartColumn(this, PCStrings.LockedColumn, 38, "Locked");
            SmartColumn modified = new SmartColumn(this, PCStrings.ModifiedColumn, 76, "Modified");
            SmartColumn name = new SmartColumn(this, PCStrings.NameColumn, 76, "Name");
            SmartColumn revision = new SmartColumn(this, PCStrings.RevisionColumn, 38, "Revision");
            SmartColumn type = new SmartColumn(this, PCStrings.TypeColumn, 76, "Type");
            SmartColumn workingCopy = new SmartColumn(this, PCStrings.WorkingCopyColumn, 76, "WorkingCopy");

            Columns.AddRange(new ColumnHeader[]
            {
                path,
                project,
                change,
                fullPath
            });

            modified.Sorter = new SortWrapper(
                delegate(PendingCommitItem x, PendingCommitItem y)
                {
                    return x.PendingChange.GitItem.Modified.CompareTo(y.PendingChange.GitItem.Modified);
                });

            revision.Sorter = new SortWrapper(
                delegate(PendingCommitItem x, PendingCommitItem y)
                {
                    long? xRev, yRev;
                    xRev = x.PendingChange.Revision;
                    yRev = y.PendingChange.Revision;

                    if (xRev.HasValue && yRev.HasValue)
                        return xRev.Value.CompareTo(yRev.Value);
                    else if (!xRev.HasValue)
                        return yRev.HasValue ? 1 : 0;
                    else
                        return -1;
                });

            change.Groupable = true;
            changeList.Groupable = true;
            folder.Groupable = true;
            locked.Groupable = true;
            project.Groupable = true;
            type.Groupable = true;
            workingCopy.Groupable = true;

            path.Hideable = false;

            AllColumns.Add(change);
            AllColumns.Add(changeList);
            AllColumns.Add(folder);
            AllColumns.Add(fullPath);
            AllColumns.Add(locked);
            AllColumns.Add(modified);
            AllColumns.Add(name);
            AllColumns.Add(path);
            AllColumns.Add(project);
            AllColumns.Add(revision);
            AllColumns.Add(type);
            AllColumns.Add(workingCopy);

            SortColumns.Add(path);
            GroupColumns.Add(changeList);

            FinalSortColumn = path;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            HookCommands();
        }

        bool _hooked;
        public void HookCommands()
        {
            if (_hooked)
                return;

            if (Context != null)
            {
                _hooked = true;
                VSCommandHandler.Install(Context, this,
                    new CommandID(VSConstants.GUID_VSStandardCommandSet97, (int)VSConstants.VSStd97CmdID.SelectAll),
                    OnSelectAll);
            }
        }

        void OnSelectAll(object sender, CommandEventArgs e)
        {
            SelectAllItems();
        }

        bool IPendingChangeSource.HasPendingChanges
        {
            get { return CheckedIndices.Count > 0; }
        }

        IEnumerable<PendingChange> IPendingChangeSource.PendingChanges
        {
            get
            {
                List<ListViewItem> list = new List<ListViewItem>();
                foreach (PendingCommitItem pi in CheckedItems)
                {
                    list.Add(pi);
                }

                IComparer<ListViewItem> sorter = ListViewItemSorter as IComparer<ListViewItem>;

                if (sorter != null)
                    list.Sort(sorter);

                foreach (PendingCommitItem pi in list)
                    yield return pi.PendingChange;
            }
        }

        IVisualGitServiceProvider _context;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public IVisualGitServiceProvider Context
        {
            get { return _context; }
            set
            {
                _context = value;
            }
        }

        PendingCommitsSelectionMap _map;
        internal override SelectionItemMap SelectionMap
        {
            get { return _map ?? (_map = new PendingCommitsSelectionMap(this)); }
        }

        sealed class PendingCommitsSelectionMap : SelectionItemMap
        {
            public PendingCommitsSelectionMap(PendingCommitsView view)
                : base(CreateData(view))
            {

            }
        }

        protected override string GetCanonicalName(PendingCommitItem item)
        {
            return item.FullPath;
        }

        protected override void OnRetrieveSelection(ListViewWithSelection<PendingCommitItem>.RetrieveSelectionEventArgs e)
        {
            e.SelectionItem = e.Item.PendingChange;
        }

        public override void OnShowContextMenu(MouseEventArgs e)
        {
            base.OnShowContextMenu(e);

            Point p = e.Location;
            bool showSort = false;
            if (p != new Point(-1, -1))
            {
                // Mouse context menu
                if (PointToClient(p).Y < HeaderHeight)
                    showSort = true;
            }
            else
            {
                ListViewItem fi = FocusedItem;

                if (fi != null)
                    p = PointToScreen(fi.Position);
            }

            IVisualGitCommandService mcs = Context.GetService<IVisualGitCommandService>();
            if (mcs != null)
            {
                if (showSort)
                    mcs.ShowContextMenu(VisualGitCommandMenu.PendingCommitsHeaderContextMenu, p);
                else
                    mcs.ShowContextMenu(VisualGitCommandMenu.PendingCommitsContextMenu, p);
            }
        }

        IVsUIShell _shell;
        protected override void OnItemChecked(ItemCheckedEventArgs e)
        {
            base.OnItemChecked(e);

            if (_shell == null)
            {

                IVisualGitServiceProvider sps = SelectionPublishServiceProvider;
                if (sps != null)
                {
                    _shell = sps.GetService<IVsUIShell>(typeof(SVsUIShell));
                }
            }
            if (_shell != null)
                _shell.UpdateCommandUI(0); // Make sure the toolbar is updated on check actions
        }

        protected override bool IsPartOfSelectAll(ListViewItem i)
        {
            return i is PendingCommitItem;
        }
    }
}

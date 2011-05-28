using System;
using System.Windows.Forms;
using System.Drawing;
using VisualGit.UI.VSSelectionControls;
using VisualGit.VS;
using VisualGit.Scc;
using VisualGit.Commands;
using VisualGit.UI.WorkingCopyExplorer.Nodes;

namespace VisualGit.UI.WorkingCopyExplorer
{
    sealed class FileSystemDetailsView : ListViewWithSelection<FileSystemListViewItem>
    {
        public FileSystemDetailsView()
        {
            View = View.Details;
            HideSelection = false;
            FullRowSelect = true;
            AllowColumnReorder = true;
        }

        IVisualGitServiceProvider _context;
        public IVisualGitServiceProvider Context
        {
            get { return _context; }
            set
            {
                if (_context != value)
                {
                    _context = value;

                    OnContextChanged();
                }
            }
        }

        bool _initialized;
        void TryInitialize()
        {
            if (!_initialized)
            {
                _initialized = true;
                InitializeCharacterWidth();
                InitializeColumns();
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (IsHandleCreated)
            {
                TryInitialize();
            }
        }

        IFileIconMapper _mapper;
        IStatusImageMapper _statusMapper;

        internal IFileIconMapper IconMapper
        {
            get { return _mapper ?? (_mapper = Context.GetService<IFileIconMapper>()); }
        }

        internal IStatusImageMapper StatusMapper
        {
            get { return _statusMapper ?? (_statusMapper = Context.GetService<IStatusImageMapper>()); }
        }

        private void OnContextChanged()
        {
            if (SmallImageList == null)
                SmallImageList = IconMapper.ImageList;

            if (StateImageList == null)
                StateImageList = StatusMapper.StatusImageList;

            SelectionPublishServiceProvider = Context;
        }

        public void SetDirectory(WCTreeNode directory)
        {
            TryInitialize();

            AddChildren(directory);
        }

        public Point GetSelectionPoint()
        {
            if (SelectedItems.Count > 0)
            {
                ListViewItem item = SelectedItems[0];
                int offset = item.Bounds.Height / 3;
                return PointToScreen(new Point(item.Bounds.X + offset + StateImageList.ImageSize.Width,
                    item.Bounds.Y + offset));
            }

            return Point.Empty;
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            ListViewHitTestInfo ht = HitTest(e.Location);

            FileSystemListViewItem li = ht.Item as FileSystemListViewItem;

            if (ht.Location == ListViewHitTestLocations.None || li == null)
                return;

            if (!li.Selected)
            {
                SelectedIndices.Clear();
                li.Selected = true;
            }

            Context.GetService<IVisualGitCommandService>().PostExecCommand(VisualGitCommand.ExplorerOpen);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Enter means open if there's only one selected item
            if (e.KeyCode == Keys.Enter && SelectedItems.Count > 0)
            {
                Context.GetService<IVisualGitCommandService>().PostExecCommand(VisualGitCommand.ExplorerOpen);
            }
        }

        private void AddChildren(WCTreeNode directory)
        {
            BeginUpdate();
            try
            {
                Items.Clear();

                foreach (WCTreeNode item in directory.GetChildren())
                {
                    WCFileSystemNode fsNode = item as WCFileSystemNode;

                    //GitItem GitItem = item.GitItem;
                    if (fsNode == null)
                        continue;

                    FileSystemListViewItem lvi = new FileSystemListViewItem(this, fsNode.GitItem);
                    Items.Add(lvi);
                    lvi.Tag = item;
                }

                if (Items.Count > 0 && _nameColumn.DisplayIndex >= 0)
                    _nameColumn.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            }
            finally
            {
                EndUpdate();
            }
        }

        SmartColumn _nameColumn;
        private void InitializeColumns()
        {
            AllColumns.Clear();
            SortColumns.Clear();
            Columns.Clear();
            _nameColumn = new SmartColumn(this, "&Name", characterWidth * NameColumnNumberOfCharacters, "Name");
            SmartColumn modified = new SmartColumn(this, "&Modified", characterWidth * 15, "Modified");
            SmartColumn type = new SmartColumn(this, "&Type", characterWidth * 20, "Type");
            SmartColumn change = new SmartColumn(this, "&Change", characterWidth * 15, "Change");
            SmartColumn locked = new SmartColumn(this, "&Locked", characterWidth * 8, "Locked");
            SmartColumn revision = new SmartColumn(this, "&Revision", characterWidth * 8, "Revision");
            SmartColumn lastChangeTime = new SmartColumn(this, "Last C&hange", characterWidth * 20, "LastChange");
            SmartColumn lastRev = new SmartColumn(this, "Last Re&vision", characterWidth * 8, "LastRevision");
            SmartColumn lastAuthor = new SmartColumn(this, "Last &Author", characterWidth * 8, "LastAuthor");
            SmartColumn contStatus = new SmartColumn(this, "&Content Status", characterWidth * 15, "ContentStatus");
            SmartColumn propStatus = new SmartColumn(this, "&Property Status", characterWidth * 15, "PropertyStatus");
            SmartColumn isCopied = new SmartColumn(this, "C&opied", characterWidth * 6, "Copied");
            SmartColumn isConficted = new SmartColumn(this, "Co&nflicted", characterWidth * 6, "Conflicted");
            SmartColumn fullPath = new SmartColumn(this, "Fu&ll Path", characterWidth * 60, "FullPath");

            _nameColumn.Sorter = new SortWrapper(
                delegate(FileSystemListViewItem x, FileSystemListViewItem y)
                {
                    if (x.IsDirectory ^ y.IsDirectory)
                        return x.IsDirectory ? -1 : 1;

                    return StringComparer.OrdinalIgnoreCase.Compare(x.Text, y.Text);
                });

            modified.Sorter = new SortWrapper(
                delegate(FileSystemListViewItem x, FileSystemListViewItem y)
                {
                    return x.Modified.CompareTo(y.Modified);
                });

            lastChangeTime.Sorter = new SortWrapper(
                delegate(FileSystemListViewItem x, FileSystemListViewItem y)
                {
                    return x.GitItem.Status.LastChangeTime.CompareTo(y.GitItem.Status.LastChangeTime);
                });

            AllColumns.Add(_nameColumn);
            AllColumns.Add(modified);
            AllColumns.Add(type);
            AllColumns.Add(change);
            AllColumns.Add(locked);
            AllColumns.Add(revision);
            AllColumns.Add(lastChangeTime);
            AllColumns.Add(lastRev);
            AllColumns.Add(lastAuthor);
            AllColumns.Add(contStatus);
            AllColumns.Add(propStatus);
            AllColumns.Add(isCopied);
            AllColumns.Add(isConficted);
            AllColumns.Add(fullPath);

            Columns.AddRange(
                new ColumnHeader[]
                {
                    _nameColumn,
                    modified,
                    type,
                    change,
                    locked,
                    revision
                });

            SortColumns.Add(_nameColumn);
            FinalSortColumn = _nameColumn;
            UpdateSortGlyphs();
        }

        private void InitializeCharacterWidth()
        {
            using (Graphics g = CreateGraphics())
            {
                const string measureString = "Name of Something To Measure";
                characterWidth = (int)(g.MeasureString(measureString, Font).Width / measureString.Length);
            }
        }

        protected override void OnRetrieveSelection(RetrieveSelectionEventArgs e)
        {
            e.SelectionItem = new GitItemData(Context, e.Item.GitItem);
        }

        public override void OnShowContextMenu(MouseEventArgs e)
        {
            base.OnShowContextMenu(e);

            bool isHeaderContext = false;
            Point screen;
            if (e.X == -1 && e.Y == -1)
            {
                // Handle keyboard context menu
                if (SelectedItems.Count > 0)
                {
                    screen = PointToScreen(SelectedItems[SelectedItems.Count - 1].Position);
                }
                else
                {
                    isHeaderContext = true;
                    screen = PointToScreen(new Point(0, 0));
                }
            }
            else
            {
                screen = e.Location;
                isHeaderContext = PointToClient(e.Location).Y < HeaderHeight;
            }

            IVisualGitCommandService sc = Context.GetService<IVisualGitCommandService>();

            VisualGitCommandMenu menu;
            if (isHeaderContext)
            {
                Select(); // Must be the active control for the menu to work
                menu = VisualGitCommandMenu.ListViewHeader;
            }
            else
                menu = VisualGitCommandMenu.WorkingCopyExplorerContextMenu;

            sc.ShowContextMenu(menu, screen);
        }

        protected override void OnResolveItem(ResolveItemEventArgs e)
        {
            GitItemData sid = e.SelectionItem as GitItemData;

            if (sid == null)
                return;

            foreach (FileSystemListViewItem lvi in Items)
            {
                if (lvi.GitItem == sid.GitItem)
                    e.Item = lvi;
            }
        }

        protected override string GetCanonicalName(FileSystemListViewItem item)
        {
            if (item != null)
            {
                GitItem i = item.GitItem;

                string name = i.FullPath;

                if (i.IsDirectory && !name.EndsWith("\\"))
                    name += "\\"; // VS usualy ends returned folders with '\\'

                return name;
            }

            return base.GetCanonicalName(item);
        }

        internal void SelectPath(string path)
        {
            foreach (FileSystemListViewItem i in Items)
            {
                if (string.Equals(i.GitItem.FullPath, path))
                {
                    SelectedItems.Clear();
                    i.Selected = true;
                    i.Focused = true;
                    EnsureVisible(i.Index);
                    Select();
                    Focus();
                    return;
                }
            }
        }

        private int characterWidth;
        private const int NameColumnNumberOfCharacters = 50;
    }
}
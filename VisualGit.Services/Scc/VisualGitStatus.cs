using System;
using System.Collections.Generic;
using System.Text;
using SharpSvn;
using System.Diagnostics;

namespace VisualGit
{
    public enum NoSccStatus
    {
        Unknown,
        NotVersioned,
        NotExisting
    }

    [DebuggerDisplay("Content={LocalContentStatus}, Property={LocalPropertyStatus}, Uri={Uri}")]
    public sealed class VisualGitStatus
    {
        readonly SvnConflictData _treeConflict;
        readonly SvnNodeKind _nodeKind;        
        readonly string _changeList;
        readonly SvnStatus _localContentStatus;
        readonly bool _localCopied;
        readonly bool _localLocked;
        readonly SvnStatus _localPropertyStatus;
        readonly Uri _uri;

        readonly DateTime _lastChangeTime;
        readonly string _lastChangeAuthor;
        readonly long _lastChangeRevision;
        readonly long _revision;

        public VisualGitStatus(SvnStatusEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            _nodeKind = args.NodeKind;
            _localContentStatus = args.LocalContentStatus;
            _localCopied = args.LocalCopied;
            _localPropertyStatus = args.LocalPropertyStatus;
            _uri = args.Uri;

            if (args.WorkingCopyInfo != null)
            {
                _lastChangeTime = args.WorkingCopyInfo.LastChangeTime;
                _lastChangeRevision = args.WorkingCopyInfo.LastChangeRevision;
                _lastChangeAuthor = args.WorkingCopyInfo.LastChangeAuthor;
                _revision = args.WorkingCopyInfo.Revision;
                _changeList = args.WorkingCopyInfo.ChangeList;
                _localLocked = args.WorkingCopyInfo.LockToken != null;
            }

            _treeConflict = args.TreeConflict;
            if(_treeConflict != null)
                _treeConflict.Detach();
        }

        /// <summary>
        /// Create non-locked, non-copied item with status specified
        /// </summary>
        /// <param name="allStatuses"></param>
        private VisualGitStatus(SvnStatus allStatuses)
        {
            _localContentStatus = allStatuses;
            _localPropertyStatus = SvnStatus.None;
            //_localLocked = false;
            //_localCopied = false;
        }

        #region Static instances
        readonly static VisualGitStatus _unversioned = new VisualGitStatus(SvnStatus.NotVersioned);
        readonly static VisualGitStatus _none = new VisualGitStatus(SvnStatus.None);
        /// <summary>
        /// Default status for nodes which do exist but are not managed
        /// </summary>
        internal static VisualGitStatus NotVersioned
        {
            get { return _unversioned; }
        }

        /// <summary>
        /// Default status for nodes which don't exist and are not managed
        /// </summary>
        internal static VisualGitStatus NotExisting
        {
            get { return _none; }
        }
        #endregion

        /// <summary>
        /// Content status in working copy
        /// </summary>
        public SvnStatus LocalContentStatus
        {
            get { return _localContentStatus; }
        }

        /// <summary>
        /// Property status in working copy
        /// </summary>
        public SvnStatus LocalPropertyStatus
        {
            get { return _localPropertyStatus; }
        }

        public SvnStatus CombinedStatus
        {
            get
            {
                switch(_localContentStatus)
                {
                    // High priority statuses on the content
                    case SvnStatus.Obstructed:
                    case SvnStatus.Missing:
                    case SvnStatus.Incomplete: 
                        return _localContentStatus;
                }

                switch(_localPropertyStatus)
                {
                    // High priority on the properties
                    case SvnStatus.Conflicted:
                        return _localPropertyStatus;
                }

                if (_localContentStatus != SvnStatus.Normal)
                    return _localContentStatus;
                else if (_localPropertyStatus != SvnStatus.None)
                    return _localPropertyStatus;
                else
                    return _localContentStatus;
            }
        }

        /// <summary>
        /// Gets the change list in which the file is placed
        /// </summary>
        /// <value>The change list.</value>
        /// <remarks>The changelist value is only valid if the file is modified</remarks>
        public string ChangeList
        {
            get { return _changeList; }
        }

        public SvnNodeKind NodeKind
        {
            get { return _nodeKind; }
        }

        public DateTime LastChangeTime
        {
            get { return _lastChangeTime; }
        }

        public string LastChangeAuthor
        {
            get { return _lastChangeAuthor; }
        }

        public long LastChangeRevision
        {
            get { return _lastChangeRevision; }
        }

        public long Revision
        {
            get { return _revision; }
        }

        /// <summary>
        /// Gets a boolean indicating whether the file is copied in the working copy
        /// </summary>
        public bool IsCopied
        {
            get { return _localCopied; }
        }
        
        /// <summary>
        /// Gets a boolean indicating whether the workingcopy is locked in the local working copy
        /// </summary>
        public bool IsLockedLocal
        {
            get { return _localLocked; }
        }

        internal Uri Uri
        {
            get { return _uri; }
        }

        internal bool HasTreeConflict
        {
            get { return _treeConflict != null; }
        }

        public SvnConflictData TreeConflict
        {
            get { return _treeConflict; }
        }
    }

}
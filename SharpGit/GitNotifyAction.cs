﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGit
{
    public enum GitNotifyAction
    {
        Add,
        Copy,
        Delete,
        Restore,
        Revert,
        RevertFailed,
        Resolved,
        Skip,
        UpdateDelete,
        UpdateAdd,
        UpdateUpdate,
        UpdateCompleted,
        UpdateExternal,
        StatusCompleted,
        StatusExternal,
        CommitModified,
        CommitAdded,
        CommitDeleted,
        CommitReplaced,
        CommitSendData,
        BlameRevision,
        LockLocked,
        LockUnlocked,
        LockFailedLock,
        LockFailedUnlock,
        Exists,
        ChangeListSet,
        ChangeListClear,
        ChangeListMoved,
        MergeBegin,
        MergeBeginForeign,
        UpdateReplace,
        PropertyAdded,
        PropertyModified,
        PropertyDeleted,
        PropertyDeletedNonExistent,
        RevisionPropertySet,
        RevisionPropertyDeleted,
        MergeCompleted,
        TreeConflict,
        ExternalFailed,
        UpdateObstruction,
        ExternalRemoved,
        UpdateAddDeleted,
        UpdateDeleted,
        RecordMergeInfo,
    }
}

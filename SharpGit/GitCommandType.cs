﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGit
{
    public enum GitCommandType
    {
        //Unknown = 0,
        Add = 1,
        //AddToChangeList = 2,
        Blame = 3,
        //CheckOut = 4,
        //CleanUp = 5,
        Commit = 6,
        //Copy = 7,
        //CreateDirectory = 8,
        Delete = 9,
        Diff = 10,
        //DiffMerge = 11,
        //DiffSummary = 12,
        //Export = 13,
        //GetAppliedMergeInfo = 14,
        //GetProperty = 15,
        //GetRevisionProperty = 16,
        //GetSuggestedMergeSources = 17,
        //Import = 18,
        Info = 19,
        //List = 20,
        //ListChangeList = 21,
        //Lock = 22,
        Log = 23,
        Merge = 24,
        //MergesEligible = 25,
        //MergesMerged = 26,
        Move = 27,
        //PropertyList = 28,
        //ReintegrationMerge = 29,
        //Relocate = 30,
        //RemoveFromChangeList = 31,
        Resolved = 32,
        Revert = 33,
        //RevisionPropertyList = 34,
        //SetProperty = 35,
        //SetRevisionProperty = 36,
        Status = 37,
        Switch = 38,
        //Unlock = 39,
        //Update = 40,
        Write = 41,
        //CropWorkingCopy = 42,
        Push = 43,
        Pull = 44,
        RemoteRefs = 45,
        Clone = 46,
        //GetWorkingCopyInfo = 4097,
        //GetWorkingCopyVersion = 4098,
        //GetWorkingCopyEntries = 4099,
        //FileVersions = 8193,
        //ReplayRevision = 8194,
        //WriteRelated = 8195,
    }
}

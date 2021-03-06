// VisualGit.UI\MergeWizard\MergeConflictHandler.cs
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
using System.Windows.Forms.Design;
using System.Windows.Forms;
using VisualGit.Scc.UI;
using System.IO;
using SharpGit;

namespace VisualGit.UI.MergeWizard
{
    public class MergeConflictHandler : VisualGitService
    {  
        /// Conflict resolution preference for binary files
        GitAccept _binaryChoice = GitAccept.Postpone;

        /// Conflict resolution preference for text files
        GitAccept _textChoice = GitAccept.Postpone;

        /// Conflict resolution preference for properties
        GitAccept _propertyChoice = GitAccept.Postpone;

        /// flag (not) to show conflict resolution option dialog for text files
        bool _txt_showDialog/* = false*/;

        /// flag (not) to show conflict resolution option dialog for binary files
        bool _binary_showDialog/* = false*/;

        /// flag (not) to show conflict resolution option dialog for property files
        bool _property_showDialog = true; // prompt for properties initially

        List<string> currentResolutions = new List<string>();
        HashSet<string> _resolvedMergeConflicts = new HashSet<string>();

        public MergeConflictHandler(IVisualGitServiceProvider context, GitAccept binaryChoice, GitAccept textChoice, GitAccept propChoice)
            : this(context, binaryChoice, textChoice)
        {
        }
        
        public MergeConflictHandler(IVisualGitServiceProvider context, GitAccept binaryChoice, GitAccept textChoice)
            : this(context)
        {
            this._binaryChoice = binaryChoice;
            this._textChoice = textChoice;
        }

        public MergeConflictHandler(IVisualGitServiceProvider context)
            : base(context)
        {
        }

        /// <summary>
        /// Gets/sets the conflict resolution preference for text files
        /// </summary>
        public GitAccept TextConflictResolutionChoice
        {
            get
            {
                return this._textChoice;
            }
            set
            {
                this._textChoice = value;
            }
        }

        /// <summary>
        /// Gets/sets the conflict resolution preference for binary files
        /// </summary>
        public GitAccept BinaryConflictResolutionChoice
        {
            get
            {
                return this._binaryChoice;
            }
            set
            {
                this._binaryChoice = value;
            }
        }

        /// <summary>
        /// Gets/sets the conflict resolution preference for properties
        /// </summary>
        public GitAccept PropertyConflictResolutionChoice
        {
            get
            {
                return this._propertyChoice;
            }
            set
            {
                this._propertyChoice = value;
            }
        }

        /// <summary>
        /// Gets/sets the flag to show conflict resolution dialog for text file conflicts.
        /// </summary>
        public bool PromptOnTextConflict
        {
            get
            {
                return this._txt_showDialog;
            }
            set
            {
                this._txt_showDialog = value;
            }
        }

        /// <summary>
        /// Gets/sets the flag to show conflict resolution dialog for binary file conflicts.
        /// </summary>
        public bool PromptOnBinaryConflict
        {
            get
            {
                return this._binary_showDialog;
            }
            set
            {
                this._binary_showDialog = value;
            }
        }

        /// <summary>
        /// Gets/sets the flag to show conflict resolution dialog for property conflicts.
        /// </summary>
        public bool PromptOnPropertyConflict
        {
            get
            {
                return this._property_showDialog;
            }
            set
            {
                this._property_showDialog = value;
            }
        }

        /// <summary>
        /// Gets the dictionary of resolved conflicts.
        /// key: file path
        /// value: list of conflict types
        /// </summary>
        public HashSet<string> ResolvedMergedConflicts
        {
            get
            {
                return this._resolvedMergeConflicts;
            }
        }

        /// <summary>
        /// Resets the handler's cache.
        /// </summary>
        public void Reset()
        {
            // reset current resolutions
            this._resolvedMergeConflicts = new HashSet<string>();
        }

        /// <summary>
        /// Handles the conflict based on the preferences.
        /// </summary>
        public void OnConflict(GitConflictEventArgs args)
        {
            if (args.ConflictReason == GitConflictReason.Edited)
            {
                GitAccept choice = GitAccept.Postpone;
                if (args.IsBinary)
                {
                    if (PromptOnBinaryConflict)
                    {
                        HandleConflictWithDialog(args);
                        return;
                    }
                    else
                    {
                        choice = BinaryConflictResolutionChoice;
                    }
                }
                else
                {
                    if (PromptOnTextConflict)
                    {
                        if (UseExternalMergeTool())
                        {
                            HandleConflictWithExternalMergeTool(args);
                        }
                        else
                        {
                            HandleConflictWithDialog(args);
                        }
                        return;
                    }
                    else
                    {
                        choice = TextConflictResolutionChoice;
                    }
                }
                args.Choice = choice;
            }
            else
            {
                args.Choice = GitAccept.Postpone;
            }
            AddToCurrentResolutions(args);
        }

        private void HandleConflictWithDialog(GitConflictEventArgs e)
        {
            using (MergeConflictHandlerDialog dlg = new MergeConflictHandlerDialog(e))
            {
                if (dlg.ShowDialog(Context) == DialogResult.OK)
                {
                    e.Choice = dlg.ConflictResolution;
                    bool applyToAll = dlg.ApplyToAll;
                    // modify the preferences based on the conflicted file type
                    if (applyToAll)
                    {
                        PropertyConflictResolutionChoice = e.Choice;
                        PromptOnPropertyConflict = false;
                        BinaryConflictResolutionChoice = e.Choice;
                        PromptOnBinaryConflict = false;
                        TextConflictResolutionChoice = e.Choice;
                        PromptOnTextConflict = false;
                    }
                    else
                    {
                        bool applyToType = dlg.ApplyToType;
                        if (applyToType)
                        {
                            if (e.IsBinary)
                            {
                                BinaryConflictResolutionChoice = e.Choice;
                                PromptOnBinaryConflict = false;
                            }
                            else
                            {
                                TextConflictResolutionChoice = e.Choice;
                                PromptOnTextConflict = false;
                            }
                        }
                    }
                    // TODO handle merged file option
                }
                else
                {
                    // Aborts the current operation.
                    e.Cancel = true;
                }
            }

            AddToCurrentResolutions(e);
        }

        private void HandleConflictWithExternalMergeTool(GitConflictEventArgs e)
        {
            IVisualGitDiffHandler handler = GetService<IVisualGitDiffHandler>();
            if (handler == null)
            {
                HandleConflictWithDialog(e);
            }
            else
            {
                VisualGitMergeArgs ama = new VisualGitMergeArgs();
                // Ensure paths are in valid format or the DiffToolMonitor constructor
                // throws argument exception validatig the file path to be monitored.
                ama.BaseFile = GitTools.GetNormalizedFullPath(e.BaseFile);
                ama.TheirsFile = GitTools.GetNormalizedFullPath(e.TheirFile);
                ama.MineFile = GitTools.GetNormalizedFullPath(e.MyFile);
                ama.MergedFile = GitTools.GetNormalizedFullPath(e.MergedFile);
                ama.Mode = DiffMode.PreferExternal;
                ama.BaseTitle = "Base";
                ama.TheirsTitle = "Theirs";
                ama.MineTitle = "Mine";
                ama.MergedTitle = new System.IO.FileInfo(e.Path).Name;
                bool merged = handler.RunMerge(ama);       
                if (merged)
                {
                    IUIService ui = Context.GetService<IUIService>();
                    string message = "Did you resolve all of the conflicts in the file?\n\nAnswering yes marks this file as resolved, no will keep it as conflicted.";
                    string caption = "Resolve Conflict";
                    DialogResult result = ui.ShowMessage(message, caption, MessageBoxButtons.YesNoCancel);
                    e.Cancel = result == DialogResult.Cancel;

                    if(!e.Cancel)
                        merged = result == DialogResult.Yes;
                }
                if (!merged)
                {
                    //Restore original merged file.
                    HandleConflictWithDialog(e);
                }
                else
                {
                    e.Choice = GitAccept.Merged;
                }
            }
        }

        private void AddToCurrentResolutions(GitConflictEventArgs args)
        {
            if (args != null && args.Choice != GitAccept.Postpone)
            {
                if (!_resolvedMergeConflicts.Contains(args.Path))
                {
                    _resolvedMergeConflicts.Add(args.Path.Replace('/', '\\'));
                }
            }
        }

        private bool UseExternalMergeTool()
        {
            IVisualGitConfigurationService cs = GetService<IVisualGitConfigurationService>();
            if (cs == null) { return false; }
            string mergePath = cs.Instance.MergeExePath;
            return !string.IsNullOrEmpty(mergePath);
        }

        private string CreateTempFile()
        {
            return Path.GetTempFileName();
        }

        private void CopyFile(string from, string to)
        {
            File.Copy(from, to, true);
        }

    }
}

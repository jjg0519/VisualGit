// VisualGit\Services\SolutionSettings.cs
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
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using FileVersionInfo = System.Diagnostics.FileVersionInfo;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;


using VisualGit.Scc;
using VisualGit.Selection;
using VisualGit.UI;
using VisualGit.VS;
using SharpGit;

namespace VisualGit.Settings
{
    [GlobalService(typeof(IVisualGitSolutionSettings))]
    partial class SolutionSettings : VisualGitService, IVisualGitSolutionSettings
    {
        bool _inRanu = false;
        string _vsUserRoot;
        string _vsAppRoot;
        string _hiveSuffix;

        public SolutionSettings(IVisualGitServiceProvider context)
            : base(context)
        {
            IVsShell shell = GetService<IVsShell>(typeof(SVsShell));

            if (shell == null)
                throw new InvalidOperationException("IVsShell not available");

            object r;
            if (ErrorHandler.Succeeded(shell.GetProperty((int)__VSSPROPID.VSSPROPID_VirtualRegistryRoot, out r)))
                _vsUserRoot = (string)r;
            else
                _vsUserRoot = @"SOFTWARE\Microsoft\VisualStudio\8.0";

            string baseName = _vsUserRoot;

            if (_vsUserRoot.EndsWith(@"\UserSettings", StringComparison.OrdinalIgnoreCase))
            {
                _inRanu = true;
                baseName = _vsUserRoot.Substring(0, _vsUserRoot.Length - 13);
                _vsAppRoot = baseName + @"\Configuration";
            }
            else
                _vsAppRoot = _vsUserRoot;

            if (baseName.StartsWith(@"SOFTWARE\", StringComparison.OrdinalIgnoreCase))
                baseName = baseName.Substring(9); // Should always trigger

            if (baseName.StartsWith(@"Microsoft\", StringComparison.OrdinalIgnoreCase))
                baseName = baseName.Substring(10); // Give non-ms hives a prefix

            _hiveSuffix = baseName;
        }

        class SettingsCache
        {
            public string SolutionFilename;
            public string ProjectRoot;
            public GitItem ProjectRootItem;

            public int SolutionCookie;
            public int RootCookie;
        }

        ISelectionContext _selectionContext;
        IFileStatusCache _statusCache;
        ISelectionContext SelectionContext
        {
            get { return _selectionContext ?? (_selectionContext = GetService<ISelectionContext>()); }
        }

        IFileStatusCache StatusCache
        {
            get { return _statusCache ?? (_statusCache = GetService<IFileStatusCache>()); }
        }


        SettingsCache _cache = new SettingsCache();

        bool IsDirty()
        {
            string solutionFile = SelectionContext.SolutionFilename;

            SettingsCache cache = _cache;
            if (cache == null)
                return true;

            if (cache.SolutionFilename != solutionFile)
                return true;

            if (solutionFile == null)
                return false;

            GitItem item = StatusCache[solutionFile];

            if (item == null || item.ChangeCookie != cache.SolutionCookie)
                return true;

            string path = _cache.ProjectRoot;

            if (string.IsNullOrEmpty(path))
                return false;

            item = StatusCache[_cache.ProjectRoot];

            if (item == null || item.ChangeCookie != cache.RootCookie)
                return true;

            return false;
        }

        private void RefreshIfDirty()
        {
            if (!IsDirty() && _cache != null)
                return;

            _cache = null;
            SettingsCache cache = new SettingsCache();
            try
            {
                string solutionFile = SelectionContext.SolutionFilename;

                if (string.IsNullOrEmpty(solutionFile))
                    return;

                GitItem item = StatusCache[solutionFile];

                if (item == null)
                    return;

                cache.SolutionFilename = item.FullPath;
                cache.SolutionCookie = item.ChangeCookie;

                if (!item.Exists)
                    return;

                GitWorkingCopy wc = item.WorkingCopy;
                GitItem parent;
                if (wc != null)
                    parent = StatusCache[wc.FullPath];
                else
                    parent = item.Parent;

                if (parent != null)
                {
                    cache.ProjectRoot = parent.FullPath;
                    cache.ProjectRootItem = parent;
                }

                if (cache.ProjectRoot != null)
                {
                    parent = StatusCache[cache.ProjectRoot];

                    if (parent == null)
                        return;

                    cache.ProjectRootItem = parent;
                    cache.RootCookie = parent.ChangeCookie;
                }
            }
            finally
            {
                _cache = cache;
            }
        }

        public string SolutionFilename
        {
            get
            {
                RefreshIfDirty();

                return _cache.SolutionFilename;
            }
        }

        public string ProjectRoot
        {
            get
            {
                RefreshIfDirty();

                SettingsCache cache = _cache;
                if (cache != null)
                    return cache.ProjectRoot;
                else
                    return null;
            }
            set
            {
                if (string.Equals(value, ProjectRoot, StringComparison.OrdinalIgnoreCase))
                    return;

                SettingsCache cache = _cache;
                if (cache == null || string.IsNullOrEmpty(SolutionFilename))
                    throw new InvalidOperationException();

                SetProjectRootValue(value);
            }
        }

        void SetProjectRootValue(string value)
        {
            if (SolutionFilename == null)
                return;

            string sd = GitTools.GetRepositoryPath(GitTools.GetNormalizedDirectoryName(SolutionFilename).TrimEnd('\\') + '\\');
            string v = GitTools.GetRepositoryPath(GitTools.GetNormalizedFullPath(value)).ToString();

            if (!v.EndsWith("/"))
                v += "/";

            if (!sd.StartsWith(v, FileSystemUtil.StringComparison))
                return;

            GetService<IFileStatusCache>().MarkDirty(SolutionFilename);
            _cache = null;
        }

        public string ProjectRootWithSeparator
        {
            get
            {
                string pr = ProjectRoot;

                if (!string.IsNullOrEmpty(pr) && pr[pr.Length - 1] != Path.DirectorySeparatorChar)
                    return pr + Path.DirectorySeparatorChar;
                else
                    return pr;
            }
        }

        public GitItem ProjectRootGitItem
        {
            get
            {
                RefreshIfDirty();

                return _cache.ProjectRootItem;
            }
        }

        string _allProjectTypesFilter;
        public string AllProjectExtensionsFilter
        {
            get
            {
                if (_allProjectTypesFilter != null)
                    return _allProjectTypesFilter;

                IVsSolution solution = GetService<IVsSolution>(typeof(SVsSolution));

                if (solution == null)
                    return null;

                object value;
                if (ErrorHandler.Succeeded(solution.GetProperty((int)__VSPROPID.VSPROPID_RegisteredProjExtns, out value)))
                    _allProjectTypesFilter = value as string;

                return _allProjectTypesFilter;
            }
        }

        string _projectFilterName;
        public string OpenProjectFilterName
        {
            get
            {
                if (_projectFilterName != null)
                    return _projectFilterName;

                IVsSolution solution = GetService<IVsSolution>(typeof(SVsSolution));

                if (solution == null)
                    return null;

                object value;
                if (ErrorHandler.Succeeded(solution.GetProperty((int)__VSPROPID.VSPROPID_OpenProjectFilter, out value)))
                    _projectFilterName = value as string;

                return _projectFilterName;
            }
        }

        public string NewProjectLocation
        {
            get
            {
                IVsShell shell = GetService<IVsShell>(typeof(SVsShell));

                if (shell != null)
                {
                    object r;
                    if (ErrorHandler.Succeeded(shell.GetProperty((int)__VSSPROPID.VSSPROPID_VisualStudioProjDir, out r)))
                        return GitTools.GetNormalizedFullPath((string)r);
                }

                return "C:\\";
            }
        }

        Version _vsVersion;
        public Version VisualStudioVersion
        {
            get
            {
                if (_vsVersion == null)
                {
                    IVsShell shell = GetService<IVsShell>(typeof(SVsShell));

                    if (shell != null)
                    {
                        object r;
                        if (ErrorHandler.Succeeded(shell.GetProperty((int)__VSSPROPID.VSSPROPID_InstallDirectory, out r)))
                        {
                            string path = r as string;

                            if (!string.IsNullOrEmpty(path) && GitItem.IsValidPath(path))
                                path = Path.Combine(path, "msenv.dll");
                            else
                                path = null;

                            if (path != null && File.Exists(path))
                            {
                                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(path);

                                string s = fvi.ProductVersion;

                                if (s != null)
                                {
                                    int i = 0;

                                    while (i < s.Length && (char.IsDigit(s, i) || s[i] == '.'))
                                        i++;

                                    if (i < s.Length)
                                        s = s.Substring(0, i);
                                }

                                if (!string.IsNullOrEmpty(s))
                                    _vsVersion = new Version(s);
                            }
                        }
                    }
                }

                return _vsVersion;
            }
        }

        #region IVisualGitSolutionSettings Members

        public string OpenFileFilter
        {
            get
            {
                IVsShell shell = GetService<IVsShell>(typeof(SVsShell));

                if (shell != null)
                {
                    object r;
                    if (ErrorHandler.Succeeded(shell.GetProperty((int)__VSSPROPID.VSSPROPID_OpenFileFilter, out r)))
                        return ((string)r).Replace('\0', '|').TrimEnd('|');
                }

                return "All Files (*.*)|*.*";
            }
        }

        /// <summary>
        /// Gets a value indicating whether [in ranu mode].
        /// </summary>
        /// <value><c>true</c> if [in ranu mode]; otherwise, <c>false</c>.</value>
        public bool InRanuMode
        {
            get { return _inRanu; }
        }

        /// <summary>
        /// Gets the visual studio registry root.
        /// </summary>
        /// <value>The visual studio registry root.</value>
        public string VisualStudioRegistryRoot
        {
            get { return _vsAppRoot; }
        }

        /// <summary>
        /// Gets the visual studio user registry root.
        /// </summary>
        /// <value>The visual studio user registry root.</value>
        public string VisualStudioUserRegistryRoot
        {
            get { return _vsUserRoot; }
        }

        /// <summary>
        /// Gets the registry hive suffix.
        /// </summary>
        /// <value>The registry hive suffix.</value>
        public string RegistryHiveSuffix
        {
            get { return _hiveSuffix; }
        }

        #endregion

        IVisualGitConfigurationService _config;
        IVisualGitConfigurationService Config
        {
            get { return _config ?? (_config = GetService<IVisualGitConfigurationService>()); }
        }

        string _solutionFilter;
        /// <summary>
        /// Gets the solution filter.
        /// </summary>
        /// <value>The solution filter.</value>
        public string SolutionFilter
        {
            // TODO: Find a way to fetch the real list
            get
            {
                if (_solutionFilter != null)
                    return _solutionFilter;

                _solutionFilter = "*.sln;*.dsw"; // Hardcoded default :(
                ILocalRegistry3 lr = GetService<ILocalRegistry3>(typeof(SLocalRegistry));

                if (lr == null)
                    return _solutionFilter;

                string root;
                if (!ErrorHandler.Succeeded(lr.GetLocalRegistryRoot(out root)))
                    return _solutionFilter;

                RegistryKey baseKey = Registry.LocalMachine;

                // Simple hack via 2005 api to support 2008+ RANU cases.
                if (root.EndsWith("\\UserSettings"))
                {
                    root = root.Substring(0, root.Length - 13) + "\\Configuration";
                    baseKey = Registry.CurrentUser;
                }

                using (RegistryKey rk = baseKey.OpenSubKey(root))
                {
                    if (rk != null)
                    {
                        string ext = rk.GetValue("SolutionFileExt") as string;

                        if (!string.IsNullOrEmpty(ext))
                        {
                            if (!ext.StartsWith("."))
                                return _solutionFilter = "*." + ext; // Normal case for standalone shell
                            else
                                return _solutionFilter = "*" + ext;
                        }
                    }
                }

                return _solutionFilter;
            }
        }

        #region IVisualGitSolutionSettings Members

        public void OpenProjectFile(string projectFile)
        {
            if (string.IsNullOrEmpty(projectFile))
                throw new ArgumentNullException("projectFile");

            string ext = Path.GetExtension(projectFile);
            bool isSolution = false;
            foreach (string x in SolutionFilter.Split(';'))
            {
                if (string.Equals(ext, Path.GetExtension(x), StringComparison.OrdinalIgnoreCase))
                {
                    isSolution = true;
                    break;
                }
            }

            IVsSolution solution = GetService<IVsSolution>(typeof(SVsSolution));

            int hr;
            if (isSolution)
                hr = solution.OpenSolutionFile(0, projectFile);
            else
            {
                Guid gnull = Guid.Empty;
                Guid gInterface = Guid.Empty;
                IntPtr pProj = IntPtr.Zero;

                hr = solution.CreateProject(ref gnull, projectFile, null, null, (uint)__VSCREATEPROJFLAGS.CPF_OPENFILE, ref gInterface, out pProj);
            }

            if (!ErrorHandler.Succeeded(hr))
                GetService<IVisualGitErrorHandler>().OnWarning(Marshal.GetExceptionForHR(hr));
        }

        #endregion
    }
}



using System;
using System.Collections.Generic;
using System.Text;
using VisualGit.Scc;
using SharpSvn;
using System.ComponentModel;
using VisualGit.Configuration;
using SharpGit;

namespace VisualGit.UI.MergeWizard
{
    [GlobalService(typeof(IConflictHandler))]
    class InteractiveConflictService : VisualGitService, IConflictHandler
    {
        public InteractiveConflictService(IVisualGitServiceProvider context)
            : base(context)
        {
        }
        #region IConflictHandler Members

        public void RegisterConflictHandler(IGitConflictsClientArgs args, System.ComponentModel.ISynchronizeInvoke synch)
        {
            args.Conflict += new EventHandler<GitConflictEventArgs>(new Handler(this, synch).OnConflict);
        }

        #endregion

        class Handler : VisualGitService
        {
            ISynchronizeInvoke _synchronizer;
            MergeConflictHandler _currentMergeConflictHandler;

            public Handler(IVisualGitServiceProvider context, ISynchronizeInvoke synchronizer)
                : base(context)
            {
                _synchronizer = synchronizer;
            }

            public void OnConflict(object sender, GitConflictEventArgs e)
            {
                if (_synchronizer != null && _synchronizer.InvokeRequired)
                {
                    // If needed marshall the call to the UI thread

                    _synchronizer.Invoke(new EventHandler<GitConflictEventArgs>(OnConflict), new object[] { sender, e });
                    return;
                }

                VisualGitConfig config = GetService<IVisualGitConfigurationService>().Instance;

                if (config.InteractiveMergeOnConflict)
                {
                    // Only call interactive merge if the user opted in on it
                    if (_currentMergeConflictHandler == null)
                        _currentMergeConflictHandler = CreateMergeConflictHandler();

                    _currentMergeConflictHandler.OnConflict(e);
                }
            }

            private MergeConflictHandler CreateMergeConflictHandler()
            {
                MergeConflictHandler mergeConflictHandler = new MergeConflictHandler(Context);
                mergeConflictHandler.PromptOnBinaryConflict = true;
                mergeConflictHandler.PromptOnTextConflict = true;
                return mergeConflictHandler;
            }
        }
    }
}

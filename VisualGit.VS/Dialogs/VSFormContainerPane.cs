using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell;
using VisualGit.UI;
using System.Windows.Forms;

namespace VisualGit.VS.Dialogs
{
    sealed class VSFormContainerPane : WindowPane
    {
        readonly VSCommandRouting _routing;
        readonly Panel _panel;

        public VSFormContainerPane(IVisualGitServiceProvider context, VSCommandRouting routing, Panel panel)
            : base(context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            else if (routing == null)
                throw new ArgumentNullException("routing");
            else if (panel == null)
                throw new ArgumentNullException("panel");

            _routing = routing;
            _panel = panel;
        }

        public override System.Windows.Forms.IWin32Window Window
        {
            get { return _panel; }
        }

        protected override bool PreProcessMessage(ref Message m)
        {
            return base.PreProcessMessage(ref m);
        }
    }
}
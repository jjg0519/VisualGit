using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Forms.Design;

using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;

using VisualGit.VS;

namespace VisualGit.UI
{
    /// <summary>
    /// 
    /// </summary>
    public interface IVisualGitDialogHelpService
    {
        /// <summary>
        /// Shows generic help for the specified form
        /// </summary>
        /// <param name="form">The form.</param>
        void RunHelp(VSDialogForm form);
    }

    /// <summary>
    /// .Net form which when shown modal let's the VS command routing continue
    /// </summary>
    /// <remarks>If the IVisualGitDialogOwner service is not available this form behaves like any other form</remarks>
    public class VSDialogForm : System.Windows.Forms.Form, IVisualGitServiceProvider, IContextControl
    {
        IVisualGitServiceProvider _context;
        IVisualGitDialogOwner _dlgOwner;

        /// <summary>
        /// Initializes a new instance of the <see cref="VSContainerForm"/> class.
        /// </summary>
        public VSDialogForm()
        {
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterParent;
            base.AutoScaleMode = AutoScaleMode.Font;
            base.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VSContainerForm"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        protected VSDialogForm(IContainer container)
            : this()
        {
            container.Add(this);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false), DefaultValue(AutoScaleMode.Font)]
        public new AutoScaleMode AutoScaleMode
        {
            get { return base.AutoScaleMode; }
            set { base.AutoScaleMode = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public new System.Drawing.SizeF AutoScaleDimensions
        {
            get { return base.AutoScaleDimensions; }
            set { base.AutoScaleDimensions = value; }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IVisualGitServiceProvider Context
        {
            get { return _context; }
            set
            {
                if (_context != value)
                {
                    _context = value;
                    _dlgOwner = null;
                    OnContextChanged(EventArgs.Empty);
                }
            }
        }

        [DefaultValue(false)]
        public new bool ShowInTaskbar
        {
            get { return base.ShowInTaskbar; }
            set { base.ShowInTaskbar = value; }
        }

        IVisualGitConfigurationService _configurationService;
        protected virtual IVisualGitConfigurationService ConfigurationService
        {
            get { return _configurationService ?? (_configurationService = Context.GetService<IVisualGitConfigurationService>()); }
        }

        private bool _preserveWindowPlacement;
        public bool PreserveWindowPlacement
        {
            get { return _preserveWindowPlacement; }
            set { _preserveWindowPlacement = value; }
        }

        protected virtual void OnContextChanged(EventArgs e)
        {
        }

        /// <summary>
        /// Gets the dialog owner service
        /// </summary>
        /// <value>The dialog owner.</value>
        [Browsable(false), CLSCompliant(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected IVisualGitDialogOwner DialogOwner
        {
            get
            {
                if (_dlgOwner == null && _context != null)
                    _dlgOwner = ((IVisualGitServiceProvider)this).GetService<IVisualGitDialogOwner>();

                return _dlgOwner;
            }
        }

        /// <summary>
        /// Gets the name of the dialog for help references
        /// </summary>
        /// <value>The name of the dialog help type.</value>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string DialogHelpTypeName
        {
            get { return GetType().FullName; }
        }

        bool _addedHelp;
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (DesignMode)
                return;

            if (!HelpButton && ControlBox && !_addedHelp)
            {
                IVisualGitDialogHelpService helpService = GetService<IVisualGitDialogHelpService>();

                if (helpService != null)
                {
                    _addedHelp = true;
                    HelpButton = true;
                }
            }

            LoadPlacement();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            SavePlacement();
        }

        private void LoadPlacement()
        {
            if (PreserveWindowPlacement)
            {
                IDictionary<string, int> placement = ConfigurationService.GetWindowPlacement(GetType());
                if (placement != null)
                {
                    SetPlacementFromRegistry(delegate(int i) { Left = i; }, "Left", placement);
                    SetPlacementFromRegistry(delegate(int i) { Top = i; }, "Top", placement);
                    SetPlacementFromRegistry(delegate(int i) { Width = i; }, "Width", placement);
                    SetPlacementFromRegistry(delegate(int i) { Height = i; }, "Height", placement);
                }
            }
        }

        private void SavePlacement()
        {
            if (PreserveWindowPlacement)
            {
                Dictionary<string, int> placement = new Dictionary<string, int>(4);
                placement["Left"] = Left;
                placement["Top"] = Top;
                placement["Width"] = Width;
                placement["Height"] = Height;
                ConfigurationService.SaveWindowPlacement(GetType(), placement);
            }
        }

        private void SetPlacementFromRegistry(Action<int> setter, string key, IDictionary<string, int> placement)
        {
            if (placement.ContainsKey(key) && placement[key] > 0)
            {
                setter(placement[key]);
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Form.HelpButtonClicked"/> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.ComponentModel.CancelEventArgs"/> that contains the event data.</param>
        protected override void OnHelpButtonClicked(CancelEventArgs e)
        {
            base.OnHelpButtonClicked(e);

            if (_addedHelp && !e.Cancel)
            {
                e.Cancel = true; // Don't go in context help mode

                IVisualGitDialogHelpService helpService = GetService<IVisualGitDialogHelpService>();

                helpService.RunHelp(this);
            }
        }

        /// <summary>
        /// Obsolete: Use ShowDialog(Context)
        /// </summary>
        [Obsolete("Always use ShowDialog(Context) even when the context is already set", true)]
        public new DialogResult ShowDialog()
        {
            if (Context != null)
                return ShowDialog(Context);
            else
                return ShowDialog(new VisualGitServiceContainer());
        }

        [Obsolete("Always use ShowDialog(Context) even when the context is already set", true)]
        public new DialogResult ShowDialog(IWin32Window owner)
        {
            if (Context != null)
                return ShowDialog(Context, owner);
            else
                return ShowDialog(new VisualGitServiceContainer(), owner);
        }

        /// <summary>
        /// Shows the form as a modal dialog box with the VS owner window
        /// </summary>
        /// <param name="context">The context.</param>
        public DialogResult ShowDialog(IVisualGitServiceProvider context)
        {
            if (context == null && _context == null)
                throw new ArgumentNullException("context");

            return ShowDialog(context, null);
        }

        /// <summary>
        /// Show the form as a modal dialog with the specified owner window
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="owner">The owner.</param>
        public DialogResult ShowDialog(IVisualGitServiceProvider context, IWin32Window owner)
        {
            bool setContext = false;

            if (context == null)
            {
                if (Context == null)
                    throw new ArgumentNullException("context");
            }
            else if (Context == null)
                setContext = true;
            else if (context != Context)
                throw new ArgumentOutOfRangeException("context", "context must match context or be null");

            if (setContext)
                Context = context;
            try
            {
                using (DialogRunContext(Context))
                {
                    OnBeforeShowDialog(EventArgs.Empty);

                    IUIService uiService = Context.GetService<IUIService>();

                    try
                    {
                        return RunDialog(owner, uiService);
                    }
                    finally
                    {
                        OnAfterShowDialog(EventArgs.Empty);
                    }
                }
            }
            finally
            {
                if (setContext)
                    Context = null;
            }
        }

        protected virtual IDisposable DialogRunContext(IVisualGitServiceProvider context)
        {
            return null;
        }

        /// <summary>
        /// Runs the dialog.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="uiService">The UI service.</param>
        /// <returns></returns>
        internal virtual DialogResult RunDialog(IWin32Window owner, IUIService uiService)
        {
            if (uiService != null)
                return uiService.ShowDialog(this);
            else
                return base.ShowDialog(owner);
        }

        protected virtual void OnBeforeShowDialog(EventArgs e)
        {
        }

        protected virtual void OnAfterShowDialog(EventArgs e)
        {

        }

        #region IVisualGitServiceProvider Members

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to get</typeparam>
        /// <returns>
        /// A service object of type <paramref name="serviceType"/>.-or- null if there is no service object of type <paramref name="serviceType"/>.
        /// </returns>
        [DebuggerStepThrough]
        T IVisualGitServiceProvider.GetService<T>()
        {
            return GetService<T>();
        }

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to get</typeparam>
        /// <returns>
        /// A service object of type <paramref name="serviceType"/>.-or- null if there is no service object of type <paramref name="serviceType"/>.
        /// </returns>
        protected T GetService<T>()
            where T : class
        {
            return ((IVisualGitServiceProvider)this).GetService<T>(typeof(T));
        }

        [DebuggerStepThrough]
        T IVisualGitServiceProvider.GetService<T>(Type serviceType)
        {
            T value;
            if (Context != null)
            {
                value = Context.GetService<T>(serviceType);

                if (value != null)
                    return value;
            }

            return base.GetService(serviceType) as T;
        }

        #endregion

        #region IServiceProvider Members

        [DebuggerStepThrough]
        object System.IServiceProvider.GetService(Type serviceType)
        {
            object value;

            if (Context != null)
            {
                value = Context.GetService(serviceType);

                if (value != null)
                    return value;
            }

            return base.GetService(serviceType);
        }

        #endregion
    }
}
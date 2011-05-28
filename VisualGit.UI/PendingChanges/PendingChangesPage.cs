using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using VisualGit.UI.Services;
using System.ComponentModel.Design;

namespace VisualGit.UI.PendingChanges
{
    partial class PendingChangesPage : UserControl
    {
        bool _registered;
        public PendingChangesPage()
        {
            InitializeComponent();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(true)]
        [Bindable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                base.Text = value;
            }
        }

        [Browsable(false)]
        protected virtual Type PageType
        {
            get { return null; }
        }

        IVisualGitServiceProvider _context;
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IVisualGitServiceProvider Context
        {
            get { return _context; }
            set { _context = value; }
        }

        PendingChangesToolControl _toolControl;
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PendingChangesToolControl ToolControl
        {
            get { return _toolControl; }
            set { _toolControl = value; }
        }

        IVisualGitConfigurationService _configurationService;
        protected virtual IVisualGitConfigurationService ConfigurationService
        {
            get { return _configurationService ?? (_configurationService = Context.GetService<IVisualGitConfigurationService>()); }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Register(true);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Register(true);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
            Register(false);
        }

        IServiceContainer _container;
        private void Register(bool register)
        {
            if (_registered == register)
                return;

            if (register)
            {
                if (_container == null && Context == null)
                    return;

                _container = Context.GetService<IServiceContainer>();

                if (_container == null)
                    return;

                if (null == _container.GetService(PageType))
                {
                    _registered = true;
                    _container.AddService(PageType, this);
                }
            }
            else if (_container != null)
            {
                _registered = false;
                _container.RemoveService(PageType);
                _container = null;
            }
        }

        public virtual bool CanRefreshList
        {
            get { return false; }
        }

        public virtual void RefreshList()
        {
            throw new NotImplementedException();
        }
    }
}
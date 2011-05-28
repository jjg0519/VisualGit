using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;
using Microsoft.VisualStudio.OLE.Interop;
using System.Runtime.InteropServices;
using OLEConstants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace VisualGit.Commands
{
    public sealed class CommandMapper : VisualGitService
    {
        readonly Dictionary<VisualGitCommand, CommandMapItem> _map;
        readonly VisualGitCommandContext _commandContext;


        public CommandMapper(IVisualGitServiceProvider context)
            : base(context)
        {
            _map = new Dictionary<VisualGitCommand, CommandMapItem>();
        }
        public CommandMapper(IVisualGitServiceProvider context, VisualGitCommandContext commandContext)
            : this(context)
        {
            _commandContext = commandContext;
        }

        public bool PerformUpdate(VisualGitCommand command, CommandUpdateEventArgs e)
        {
            EnsureLoaded();
            CommandMapItem item;

            if (_map.TryGetValue(command, out item))
            {
                if (!item.AlwaysAvailable && !e.State.SccProviderActive)
                    e.Enabled = false;
                else
                    try
                    {
                        e.Prepare(item);

                        item.OnUpdate(e);
                    }
                    catch (Exception ex)
                    {
                        IVisualGitErrorHandler eh = GetService<IVisualGitErrorHandler>();

                        if (eh != null && eh.IsEnabled(ex))
                        {
                            eh.OnError(ex, e);
                            return false;
                        }

                        throw;
                    }

                if (item.HiddenWhenDisabled && !e.Enabled)
                    e.Visible = false;

                if (item.DynamicMenuEnd)
                    e.DynamicMenuEnd = true;

                return item.IsHandled;
            }
            else if (_defined.Contains(command))
            {
                e.Enabled = e.Visible = false;
                return true;
            }

            return false;
        }

        public bool Execute(VisualGitCommand command, CommandEventArgs e)
        {
            EnsureLoaded();
            CommandMapItem item;

            if (_map.TryGetValue(command, out item))
            {
                try
                {
                    e.Prepare(item);

                    CommandUpdateEventArgs u = new CommandUpdateEventArgs(command, e.Context);
                    item.OnUpdate(u);
                    if (u.Enabled)
                    {
                        item.OnExecute(e);
                    }
                    else
                        return false;
                }
                catch (Exception ex)
                {
                    IVisualGitErrorHandler eh = GetService<IVisualGitErrorHandler>();

                    if (eh != null && eh.IsEnabled(ex))
                    {
                        eh.OnError(ex, e);
                        return true; // If we return false VS shows another error box!
                    }

                    throw;

                }

                return item.IsHandled;
            }

            return false;
        }

        public bool TryGetParameterList(VisualGitCommand command, out string definition)
        {
            EnsureLoaded();

            CommandMapItem item;

            if (_map.TryGetValue(command, out item))
            {
                definition = item.ArgumentDefinition;

                return !string.IsNullOrEmpty(definition);
            }
            else
                definition = null;

            return false;
        }

        /// <summary>
        /// Gets the <see cref="CommandMapItem"/> for the specified command
        /// </summary>
        /// <param name="command"></param>
        /// <returns>The <see cref="CommandMapItem"/> or null if the command is not valid</returns>
        public CommandMapItem this[VisualGitCommand command]
        {
            get
            {
                CommandMapItem item;

                if (_map.TryGetValue(command, out item))
                    return item;
                else
                {
                    item = new CommandMapItem(command);

                    _map.Add(command, item);

                    return item;
                }
            }
        }

        readonly List<Assembly> _assembliesToLoad = new List<Assembly>();
        readonly List<Assembly> _assembliesLoaded = new List<Assembly>();
        readonly HybridCollection<VisualGitCommand> _defined = new HybridCollection<VisualGitCommand>();

        public void LoadFrom(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            if (!_assembliesToLoad.Contains(assembly) && !_assembliesLoaded.Contains(assembly))
                _assembliesToLoad.Add(assembly);
        }

        private void EnsureLoaded()
        {
            if (_assembliesToLoad.Count == 0)
                return;

            if (_defined.Count == 0)
            {
                foreach (VisualGitCommand cmd in Enum.GetValues(typeof(VisualGitCommand)))
                {
                    if (cmd <= VisualGitCommand.CommandFirst)
                        continue;

                    _defined.Add(cmd);
                }
            }

            while (_assembliesToLoad.Count > 0)
            {
                Assembly asm = _assembliesToLoad[0];
                _assembliesToLoad.RemoveAt(0);
                _assembliesLoaded.Add(asm);
                foreach (Type type in asm.GetTypes())
                {
                    if (!type.IsClass || type.IsAbstract)
                        continue;

                    if (!typeof(ICommandHandler).IsAssignableFrom(type))
                        continue;

                    ICommandHandler instance = null;

                    foreach (CommandAttribute cmdAttr in type.GetCustomAttributes(typeof(CommandAttribute), false))
                    {
                        if (cmdAttr.Context != _commandContext)
                            continue;

                        foreach (VisualGitCommand cmdInstance in cmdAttr.GetAllCommands())
                        {
                            CommandMapItem item = this[cmdInstance];

                            if (item == null)
                                throw new InvalidOperationException("Invalid command " + cmdInstance.ToString());

                            if (cmdAttr.LastCommand == cmdInstance)
                            {
                                item.DynamicMenuEnd = true;
                                continue;
                            }

                            if (instance == null)
                            {
                                instance = (ICommandHandler)Activator.CreateInstance(type);

                                IComponent component = instance as IComponent;

                                if (component != null)
                                    component.Site = CommandSite;
                            }

                            Debug.Assert(item.ICommand == null || item.ICommand == instance, string.Format("No previous ICommand registered on the CommandMapItem for {0}", cmdAttr.Command));

                            item.ICommand = instance; // hooks all events via interface
                            item.AlwaysAvailable = cmdAttr.AlwaysAvailable;
                            item.HiddenWhenDisabled = cmdAttr.HideWhenDisabled;
                            item.CommandTarget = cmdAttr.CommandTarget;
                            item.ArgumentDefinition = cmdAttr.ArgumentDefinition ?? CalculateDefinition(cmdAttr.CommandTarget);
                        }
                    }
                }
            }
        }

        static string CalculateDefinition(CommandTarget commandTarget)
        {
            switch (commandTarget)
            {
                default:
                    return null;
            }
        }

        CommandMapperSite _commandMapperSite;
        CommandMapperSite CommandSite
        {
            get { return _commandMapperSite ?? (_commandMapperSite = new CommandMapperSite(this)); }
        }

        sealed class CommandMapperSite : VisualGitService, ISite
        {
            readonly CommandMapper _mapper;
            readonly Container _container = new Container();

            public CommandMapperSite(CommandMapper context)
                : base(context)
            {
                _mapper = context;
            }

            public IComponent Component
            {
                get { return _mapper; }
            }

            public IContainer Container
            {
                get { return _container; }
            }

            public bool DesignMode
            {
                get { return false; }
            }

            public string Name
            {
                get { return "CommandMapper"; }
                set { throw new InvalidOperationException(); }
            }
        }

        [CLSCompliant(false)]
        public int QueryStatus(VisualGitContext context, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            else if (cCmds != 1 || prgCmds == null)
                return -1;

            TextQueryType textQuery = TextQueryType.None;

            if (pCmdText != IntPtr.Zero)
            {
                // VS Want's some text from us for either the statusbar or the command text
                OLECMDTEXTF textType = GetFlags(pCmdText);

                switch (textType)
                {
                    case OLECMDTEXTF.OLECMDTEXTF_NAME:
                        textQuery = TextQueryType.Name;
                        break;
                    case OLECMDTEXTF.OLECMDTEXTF_STATUS:
                        textQuery = TextQueryType.Status;
                        break;
                }
            }

            CommandUpdateEventArgs updateArgs = new CommandUpdateEventArgs((VisualGitCommand)prgCmds[0].cmdID, context, textQuery);

            OLECMDF cmdf = OLECMDF.OLECMDF_SUPPORTED;

            if (PerformUpdate(updateArgs.Command, updateArgs))
            {
                updateArgs.UpdateFlags(ref cmdf);
            }

            if (updateArgs.DynamicMenuEnd)
                return (int)OLEConstants.OLECMDERR_E_NOTSUPPORTED;

            if (textQuery != TextQueryType.None && !string.IsNullOrEmpty(updateArgs.Text))
            {
                SetText(pCmdText, updateArgs.Text);
            }

            if (_customizeMode && updateArgs.Command != VisualGitCommand.ForceUIShow)
                prgCmds[0].cmdf = (uint)(cmdf & ~OLECMDF.OLECMDF_INVISIBLE);
            else
                prgCmds[0].cmdf = (uint)cmdf;

            return 0; // S_OK
        }

        int _nModal;
        bool _isModal;
        bool _customizeMode;
        public void SetModal(bool modal)
        {
            if (modal)
                _nModal++;
            else
                _nModal--;

            if ((_nModal != 0) == _isModal)
                return; // Not switched

            _isModal = (_nModal != 0);
            _customizeMode = false;
        }

        /// <summary>
        /// Enables Menu Customize mode until the modal state is cleared
        /// </summary>
        public void EnableCustomizeMode()
        {
            if(_isModal)
                _customizeMode = true;
        }


        #region // Interop code from: VS2008SDK\VisualStudioIntegration\Common\Source\CSharp\Project\Misc\NativeMethods.cs

        static readonly Type _OLECMDTEXT_type = typeof(OLECMDTEXT);
        static readonly int _offset_cmdtextf = (int)Marshal.OffsetOf(_OLECMDTEXT_type, "cmdtextf");
        static readonly int _offset_rgwz = (int)Marshal.OffsetOf(_OLECMDTEXT_type, "rgwz");
        static readonly int _offset_cwBuf = (int)Marshal.OffsetOf(_OLECMDTEXT_type, "cwBuf");
        static readonly int _offset_cwActual = (int)Marshal.OffsetOf(_OLECMDTEXT_type, "cwActual");

        /// <summary>
        /// Gets the flags of the OLECMDTEXT structure
        /// </summary>
        /// <param name="pCmdTextInt">The structure to read.</param>
        /// <returns>The value of the flags.</returns>
        static OLECMDTEXTF GetFlags(IntPtr pCmdTextInt)
        {
            OLECMDTEXTF cmdtextf = (OLECMDTEXTF)Marshal.ReadInt32(pCmdTextInt, _offset_cmdtextf);

            if ((cmdtextf & OLECMDTEXTF.OLECMDTEXTF_NAME) != 0)
                return OLECMDTEXTF.OLECMDTEXTF_NAME;

            if ((cmdtextf & OLECMDTEXTF.OLECMDTEXTF_STATUS) != 0)
                return OLECMDTEXTF.OLECMDTEXTF_STATUS;

            return OLECMDTEXTF.OLECMDTEXTF_NONE;
        }

        /// <include file='doc\NativeMethods.uex' path='docs/doc[@for="OLECMDTEXTF.SetText"]/*' />
        /// <devdoc>
        /// Accessing the text of this structure is very cumbersome.  Instead, you may
        /// use this method to access an integer pointer of the structure.
        /// Passing integer versions of this structure is needed because there is no
        /// way to tell the common language runtime that there is extra data at the end of the structure.
        /// </devdoc>
        /// <summary>
        /// Sets the text inside the structure starting from an integer pointer.
        /// </summary>
        /// <param name="pCmdTextInt">The integer pointer to the position where to set the text.</param>
        /// <param name="text">The text to set.</param>
        static void SetText(IntPtr pCmdTextInt, string text)
        {
            char[] menuText = text.ToCharArray();

            uint cwBuf = unchecked((uint)Marshal.ReadInt32(pCmdTextInt, _offset_cwBuf));

            // The max chars we copy is our string, or one less than the buffer size,
            // since we need a null at the end.
            int maxChars = Math.Min((int)cwBuf - 1, menuText.Length);

            Marshal.Copy(menuText, 0, (IntPtr)((long)pCmdTextInt + _offset_rgwz), maxChars);

            // append a null character
            Marshal.WriteInt16(pCmdTextInt, _offset_rgwz + maxChars * sizeof(Char), 0);

            // write out the length
            // +1 for the null char
            Marshal.WriteInt32(pCmdTextInt, _offset_cwActual, maxChars + 1);
        }

        /// <devdoc>
        /// Accessing the text of this structure is very cumbersome.  Instead, you may
        /// use this method to access an integer pointer of the structure.
        /// Passing integer versions of this structure is needed because there is no
        /// way to tell the common language runtime that there is extra data at the end of the structure.
        /// </devdoc>
        static string GetText(IntPtr pCmdTextInt)
        {
            Microsoft.VisualStudio.OLE.Interop.OLECMDTEXT pCmdText = (Microsoft.VisualStudio.OLE.Interop.OLECMDTEXT)Marshal.PtrToStructure(pCmdTextInt, typeof(Microsoft.VisualStudio.OLE.Interop.OLECMDTEXT));

            // Punt early if there is no text in the structure.
            //
            if (pCmdText.cwActual == 0)
            {
                return "";
            }

            char[] text = new char[pCmdText.cwActual - 1];

            Marshal.Copy((IntPtr)((long)pCmdTextInt + _offset_rgwz), text, 0, text.Length);

            StringBuilder s = new StringBuilder(text.Length);
            s.Append(text);
            return s.ToString();
        }
        #endregion
    }
}
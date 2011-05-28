using System;
using System.Collections.Generic;
using System.Windows.Forms;
using VisualGit.Scc.UI;

namespace VisualGit.UI.OptionsPages
{
    public partial class UserToolSettingsControl : VisualGitOptionsPageControl
    {
        public UserToolSettingsControl()
        {
            InitializeComponent();
        }


        protected override void LoadSettingsCore()
        {
            IVisualGitDiffHandler diff = Context.GetService<IVisualGitDiffHandler>();

            LoadBox(diffExeBox, Config.DiffExePath, diff.DiffToolTemplates);
            LoadBox(mergeExeBox, Config.MergeExePath, diff.MergeToolTemplates);
            LoadBox(patchExeBox, Config.PatchExePath, diff.PatchToolTemplates);
        }

        sealed class OtherTool
        {
            string _title;
            public OtherTool(string title)
            {
                _title = string.IsNullOrEmpty(title) ? "Other..." : title;
            }
            public override string ToString()
            {
                return _title;
            }

            public string Title
            {
                get { return _title; }
            }

            public string DisplayName
            {
                get { return Title; }
            }
        }

        static void LoadBox(ComboBox combo, string value, IEnumerable<VisualGitDiffTool> tools)
        {
            if (combo == null)
                throw new ArgumentNullException("combo");

            combo.DropDownStyle = ComboBoxStyle.DropDown;
            combo.Items.Clear();

            string selectedName = string.IsNullOrEmpty(value) ? null : VisualGitDiffTool.GetToolNameFromTemplate(value);
            bool search = !string.IsNullOrEmpty(selectedName);
            bool found = false;
            foreach (VisualGitDiffTool tool in tools)
            {
                // Items are presorted
                combo.Items.Add(tool);

                if (search && string.Equals(tool.Name, selectedName, StringComparison.OrdinalIgnoreCase))
                {
                    search = false;
                    found = true;
                    combo.DropDownStyle = ComboBoxStyle.DropDownList;
                    combo.SelectedItem = tool;
                }
            }

            combo.Items.Add(new OtherTool(null));

            if (!found)
            {
                combo.SelectedItem = null;
                combo.Text = value ?? "";
            }
        }


        protected override void SaveSettingsCore()
        {
            Config.DiffExePath = SaveBox(diffExeBox);
            Config.MergeExePath = SaveBox(mergeExeBox);
            Config.PatchExePath = SaveBox(patchExeBox);
        }

        static string SaveBox(ComboBox box)
        {
            if (box == null)
                throw new ArgumentNullException("box");

            VisualGitDiffTool tool = box.SelectedItem as VisualGitDiffTool;

            if (tool != null)
                return tool.ToolTemplate;

            return box.Text;
        }

        void BrowseCombo(ComboBox box)
        {
            VisualGitDiffTool tool = box.SelectedItem as VisualGitDiffTool;

            string line;
            if (tool != null)
            {
                line = string.Format("\"{0}\" {1}", tool.Program, tool.Arguments);
            }
            else
                line = box.Text;

            using (ToolArgumentDialog dlg = new ToolArgumentDialog())
            {
                dlg.Value = line;
                dlg.SetTemplates(Context.GetService<IVisualGitDiffHandler>().ArgumentDefinitions);

                if (DialogResult.OK == dlg.ShowDialog(Context))
                {
                    string newValue = dlg.Value;

                    if (!string.IsNullOrEmpty(newValue) && newValue != line)
                    {
                        box.DropDownStyle = ComboBoxStyle.DropDown;
                        box.SelectedItem = null;
                        box.Text = newValue;
                    }
                }
            }
        }

        private void diffExeBox_TextChanged(object sender, EventArgs e)
        {
            ComboBox box = (ComboBox)sender;

            if (box.DropDownStyle == ComboBoxStyle.DropDown)
            {
                if (box.SelectedItem == null && !string.IsNullOrEmpty(box.Text))
                {
                    box.Tag = box.Text;
                }
            }
        }
        
        private void tool_selectionCommitted(object sender, EventArgs e)
        {
            ComboBox box = (ComboBox)sender;

            VisualGitDiffTool tool = box.SelectedItem as VisualGitDiffTool;

            if (tool != null)
            {
                box.DropDownStyle = ComboBoxStyle.DropDownList;
                box.Tag = tool.ToolTemplate;
            }
            else
            {
                box.DropDownStyle = ComboBoxStyle.DropDown;
                if (box.SelectedItem != null)
                    box.SelectedItem = null;
                if (box.Tag is string)
                    box.Text = (string)box.Tag;
                else if (box.Tag is VisualGitDiffTool)
                    box.Text = ((VisualGitDiffTool)box.Tag).ToolTemplate;

                if (!string.IsNullOrEmpty(box.Text))
                {
                    box.SelectionStart = 0;
                    box.SelectionLength = box.Text.Length;
                }
            }
        }

        private void diffBrowseBtn_Click(object sender, EventArgs e)
        {
            BrowseCombo(diffExeBox);
        }        

        private void mergeBrowseBtn_Click(object sender, EventArgs e)
        {
            BrowseCombo(mergeExeBox);
        }

        private void patchBrowseBtn_Click(object sender, EventArgs e)
        {
            BrowseCombo(patchExeBox);
        }        
    }
}
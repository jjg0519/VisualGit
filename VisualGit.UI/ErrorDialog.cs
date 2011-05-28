using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace VisualGit.UI
{
    /// <summary>
    /// Summary description for ErrorDialog.
    /// </summary>
    public partial class ErrorDialog : VSDialogForm
    {
        public ErrorDialog()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            this.InternalError = false;
        }

        /// <summary>
        /// The stack trace displayed in the text box.
        /// </summary>
        public string StackTrace
        {
            get { return this.stackTraceTextBox.Text; }
            set { this.stackTraceTextBox.Text = value; }
        }

        /// <summary>
        /// Whether a stack trace should be shown.
        /// </summary>
        public bool ShowStackTrace
        {
            get { return this.stackTraceTextBox.Visible; }
            set
            {
                if (value && !this.stackTraceTextBox.Visible)
                {
                    this.stackTraceTextBox.Visible = true;
                    this.Height += STACKTRACEHEIGHT;                    
                    this.errorReportButton.Visible = this.errorReportButton.Enabled =
                        true;
                }
                else if (!value && this.stackTraceTextBox.Visible)
                {
                    this.Height -= STACKTRACEHEIGHT;
                    this.stackTraceTextBox.Visible = false;
                }
                this.RecalculateSize();
            }
        }

        /// <summary>
        /// The actual error message.
        /// </summary>
        public string ErrorMessage
        {
            get { return this.messageLabel.Text; }
            set
            {
                this.messageLabel.Text = value;
                this.RecalculateSize();
            }
        }



        /// <summary>
        /// Whether the error is internal to VisualGit(encourage the user to report it) or
        /// just a Git error.
        /// </summary>
        public bool InternalError
        {
            get { return this.internalError; }
            set
            {
                this.internalError = value;
                this.headerLabel.Text = this.internalError ?
                    @"An internal error occurred:" :
                    "Git reported an error: ";
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void RecalculateSize()
        {
            /*using (Graphics g = this.messageLabel.CreateGraphics())
            {
                SizeF size = g.MeasureString(this.messageLabel.Text,
                    this.messageLabel.Font, this.messageLabel.Width,
                    StringFormat.GenericDefault);
                int height = (int)size.Height;
                int diff = this.messageLabel.Height - height;

                this.Height -= diff;
                this.messageLabel.Height = (int)size.Height;
                //this.stackTraceTextBox.Top = this.messageLabel.Bottom + STACKTRACEOFFSET;
            }*/
        }

        private bool internalError;

        private const int STACKTRACEHEIGHT = 250;

        private void button2_Click(object sender, System.EventArgs e)
        {
            this.ShowStackTrace = !this.ShowStackTrace;
        }
    }
}
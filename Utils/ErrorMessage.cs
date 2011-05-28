using System;
using System.Diagnostics;
using System.Web;
using System.Windows.Forms;
using System.Reflection;
using System.Collections.Specialized;
using System.Collections;
using System.Text;

namespace Utils
{
    /// <summary>
    /// Performs error handling and reporting.
    /// </summary>
    public static class ErrorMessage
    {
        /// <summary>
        /// Concatenates the error messages and exception types from (potentially)
        /// nested exceptions.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string GetMessage(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            while (ex != null)
            {
                sb.AppendLine(ex.GetType().FullName + ": ");
                sb.AppendLine(ex.Message);
                sb.AppendLine(ex.StackTrace);

                ex = ex.InnerException;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Sends an error message by opening the user's mail client.
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="subject"></param>
        /// <param name="ex"></param>
        /// <param name="assembly">The assembly where the error originated. This will 
        /// be used to extract version information.</param>
        public static void SendByMail(string recipient, string subject, Exception ex,
            Assembly assembly, StringDictionary additionalInfo)
        {
            string attributes = GetAttributes(additionalInfo);

            StringBuilder msg = new StringBuilder();

            msg.AppendLine("[ Please send this as plain text to allow automatic pre-processing ]");
            msg.AppendLine();
            msg.AppendLine(GetMessage(ex));
            msg.AppendLine();
            msg.AppendLine(GetAttributes(additionalInfo));
            msg.AppendLine();
            msg.AppendLine("[ Please send this as plain text to allow automatic pre-processing ]");
            msg.AppendLine();

            string command = string.Format("mailto:{0}?subject={1}&body={2}",
                recipient, 
                Uri.EscapeDataString(subject),
                Uri.EscapeDataString(msg.ToString()));

            Debug.WriteLine(command);
            Process p = new Process();
            p.StartInfo.FileName = command;
            p.StartInfo.UseShellExecute = true;

            p.Start();
        }

        private static string GetAttributes(StringDictionary additionalInfo)
        {
            if (additionalInfo == null)
                return "";

            StringBuilder builder = new StringBuilder();
            foreach (DictionaryEntry de in additionalInfo)
            {
                builder.AppendFormat("{0}={1}", (string)de.Key, Uri.EscapeDataString((string)de.Value));
                builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}
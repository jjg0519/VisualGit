﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.235
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace VisualGit {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class CommandResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal CommandResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("VisualGit.CommandResources", typeof(CommandResources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Would you like to add &apos;{0}&apos; to Git and mark it as managed?.
        /// </summary>
        internal static string AddSolutionXToGit {
            get {
                return ResourceManager.GetString("AddSolutionXToGit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Would you like to add&apos;{0}&apos; to existing working copy &apos;{1}&apos;? 
        ///Click Yes to add to the current working copy, and no to create a new working copy..
        /// </summary>
        internal static string AddXToExistingWcY {
            get {
                return ResourceManager.GetString("AddXToExistingWcY", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to  and .
        /// </summary>
        internal static string FileAnd {
            get {
                return ResourceManager.GetString("FileAnd", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Would you like to mark &apos;{0}&apos; as managed by Git?.
        /// </summary>
        internal static string MarkXAsManaged {
            get {
                return ResourceManager.GetString("MarkXAsManaged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Operation could not be completed successfully.
        /// </summary>
        internal static string OperationNotCompletedSuccessfully {
            get {
                return ResourceManager.GetString("OperationNotCompletedSuccessfully", resourceCulture);
            }
        }
    }
}

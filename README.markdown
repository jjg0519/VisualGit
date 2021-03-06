# VisualGit

## Introduction

VisualGit is an extension for Visual Studio 2010 providing a fully functional source control
provider without any dependencies other than this extension.

## Getting started

After you've installed the extension, it may not be enabled automatically. Go to Tools | Options
and find the Source Control options page. Here you can select the "VisualGit - Git Support for Visual
Studio" source control plug-in.

After enabling the extension, you should provide your name and e-mail address. These can be
entered on the Source Control | Git Configuration page.

Last, when you want to communicate with Git servers, you have to provide the location of the
certificate you use to communicate with this server. This can be done by going to the Source Control |
Git Environment options page and clicking Edit for Manage Certificates.

## Project status

This project is currently in Alpha state and under heavy development. Please report any
bugs or issues you encounter so they can be solved promptly.

## Known issues

Review the issue list at <http://github.com/pvginkel/VisualGit/issues> for a complete list
of issues. The most important issues currently are:

* Project load and refresh is very slow on large projects [#25](http://github.com/pvginkel/VisualGit/issues/25);
* Maintenance on branches and tags is missing [#8](http://github.com/pvginkel/VisualGit/issues/8);
* Maintenance on remotes is missing [#30](http://github.com/pvginkel/VisualGit/issues/30).

## Dependencies

VisualGit is heavily based on the excellent Subversion source control provider AnkhSVN
which can be downloaded from <http://ankhsvn.open.collab.net/>.

Git repository management is provided by NGit, a Sharpen port of JGit. NGit can be found at
<http://github.com/slluis/ngit>, Sharpen can be found at
<http://developer.db4o.com/Projects/html/projectspaces/db4o_product_design/sharpen.html> and
JGit can be found at <http://eclipse.org/jgit>.

## Reporting bugs

Integrated with VisualGit is [CrashReporter.NET](http://github.com/pvginkel/CrashReporter.NET).
If you encounter a bug, send a crash report through the CrashReporter.NET interface.
These issues are collected at http://visualgit-bugs.appspot.com/.

Other issues and requests can be reported through GitHub at 
<http://github.com/pvginkel/VisualGit/issues>.

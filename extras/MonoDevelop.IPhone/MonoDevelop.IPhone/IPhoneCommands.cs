// 
// IPhoneCommand.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Execution;
using System.IO;

namespace MonoDevelop.IPhone
{
	
	public enum IPhoneCommands
	{
		UploadToDevice
	}
	
	class DefaultUploadToDeviceHandler : CommandHandler
	{
		protected override void Update (MonoDevelop.Components.Commands.CommandInfo info)
		{
			var proj = GetActiveProject ();
			info.Visible = proj != null;
			if (proj != null) {
				var conf = (IPhoneProjectConfiguration)proj.GetActiveConfiguration (IdeApp.Workspace.ActiveConfiguration);
				info.Enabled = conf.Platform == IPhoneProject.PLAT_IPHONE && File.Exists (conf.NativeExe);
			} else {
				info.Enabled = false;
			}
		}
		
		protected override void Run ()
		{
			var proj = GetActiveProject ();
			var conf = (IPhoneProjectConfiguration)proj.GetActiveConfiguration (IdeApp.Workspace.ActiveConfiguration);
			
			string mtouchPath = proj.TargetRuntime.GetToolPath (proj.TargetFramework, "mtouch");
			if (string.IsNullOrEmpty (mtouchPath))
				throw new InvalidOperationException ("Cannot upload iPhone application. mtouch tool is missing.");
			
			var console = (IConsole) IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				GettextCatalog.GetString ("Deploy to Device"), MonoDevelop.Core.Gui.Stock.RunProgramIcon, true, true);
			console.Log.WriteLine (String.Format ("{0} -installdev=\"{1}\"", mtouchPath, conf.AppDirectory));
			Runtime.ProcessService.StartConsoleProcess (mtouchPath,
				String.Format ("-installdev=\"{0}\"", conf.AppDirectory), conf.OutputDirectory, console, null);
		}
		
		IPhoneProject GetActiveProject ()
		{
			var proj = IdeApp.ProjectOperations.CurrentSelectedProject;
			if (proj != null)
				return proj as IPhoneProject;
			var sln = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (sln != null)
				return sln.StartupItem as IPhoneProject;
			return null;
		}
	}
}
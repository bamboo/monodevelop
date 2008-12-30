//  VBBindingCompilerServices.cs
//
//  This file was derived from a file from #Develop.
//
//  Authors:
//    Markus Palme <MarkusPalme@gmx.de>
//    Rolf Bjarne Kvinge <RKvinge@novell.com>
//
//  Copyright (C) 2001-2007 Markus Palme <MarkusPalme@gmx.de>
//  Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.CodeDom.Compiler;
using System.Threading;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;

using MonoDevelop.VBNetBinding.Extensions;

namespace MonoDevelop.VBNetBinding {
	
	/// <summary>
	/// This class controls the compilation of VB.net files and VB.net projects
	/// </summary>
	public class VBBindingCompilerServices
	{
		//matches "/home/path/Default.aspx.vb (40,31) : Error VBNC30205: Expected end of statement."
		//and "Error : VBNC99999: vbnc crashed nearby this location in the source code."
		//and "Error : VBNC99999: Unexpected error: Object reference not set to an instance of an object" 
		static Regex regexError = new Regex (@"^\s*((?<file>.*) \((?<line>\d*),(?<column>\d*)\) : )?(?<level>\w+) :? ?(?<number>[^:]*): (?<message>.*)$",
		                                     RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		
		string GenerateOptions (DotNetProjectConfiguration configuration, VBCompilerParameters compilerparameters, string outputFileName)
		{
			DotNetProject project = (DotNetProject) configuration.ParentItem;
			StringBuilder sb = new StringBuilder ();
			
			sb.AppendFormat ("\"-out:{0}\"", outputFileName);
			sb.AppendLine ();
			
			sb.AppendLine ("-nologo");
			sb.AppendLine ("-utf8output");

			sb.AppendFormat ("-debug:{0}", compilerparameters.DebugType);
			sb.AppendLine ();

			if (compilerparameters.Optimize)
				sb.AppendLine ("-optimize+");

			
			switch (project.GetOptionStrict ()) {
			case "On":
				sb.AppendLine ("-optionstrict+");
				break;
			case "Off":
				sb.AppendLine ("-optionstrict-");
				break;				
			}
			
			switch (project.GetOptionExplicit ()) {
			case "On":
				sb.AppendLine ("-optionexplicit+");
				break;
			case "Off":
				sb.AppendLine ("-optionexplicit-");
				break;
			}

			switch (project.GetOptionCompare ()) {
			case "Binary":
				sb.AppendLine ("-optioncompare:binary");
				break;
			case "Text":
				sb.AppendLine ("-optioncompare:text");
				break;
			}

			switch (project.GetOptionInfer ()) {
			case "On":
				sb.AppendLine ("-optioninfer+");
				break;
			case "Off":
				sb.AppendLine ("-optioninfer-");
				break;
			}

			string mytype = project.GetMyType ();
			if (!string.IsNullOrEmpty (mytype)) {
				sb.AppendFormat ("-define:_MYTYPE=\\\"{0}\\\"", mytype);
				sb.AppendLine ();
			}
			
			string win32IconPath = project.GetApplicationIconPath ();
			if (!string.IsNullOrEmpty (win32IconPath) && File.Exists (win32IconPath)) {
				sb.AppendFormat ("\"-win32icon:{0}\"", win32IconPath);
				sb.AppendLine ();
			}

			if (!string.IsNullOrEmpty (project.GetCodePage ())) {
				TextEncoding enc = TextEncoding.GetEncoding (project.GetCodePage ());
				sb.AppendFormat ("-codepage:{0}", enc.CodePage);
				sb.AppendLine ();
			}
			
			if (!string.IsNullOrEmpty (project.GetRootNamespace ())) {
				sb.AppendFormat ("-rootnamespace:{0}", project.GetRootNamespace ());
				sb.AppendLine ();
			}
			
			if (!string.IsNullOrEmpty (compilerparameters.DefineConstants)) {
				sb.AppendFormat ("\"-define:{0}\"", compilerparameters.DefineConstants);
				sb.AppendLine ();
			}

			if (compilerparameters.DefineDebug)
				sb.AppendLine ("-define:DEBUG=-1");

			if (compilerparameters.DefineTrace)
				sb.AppendLine ("-define:TRACE=-1");

			if (compilerparameters.WarningsDisabled) {
				sb.AppendLine ("-nowarn");
			} else if (!string.IsNullOrEmpty (compilerparameters.NoWarn)) {
				sb.AppendFormat ("-nowarn:{0}", compilerparameters.NoWarn);
				sb.AppendLine ();
			}

			if (!string.IsNullOrEmpty (compilerparameters.WarningsAsErrors)) {
				sb.AppendFormat ("-warnaserror+:{0}", compilerparameters.WarningsAsErrors);
				sb.AppendLine ();
			}
			
			if (configuration.SignAssembly) {
				if (File.Exists (configuration.AssemblyKeyFile)) {
					sb.AppendFormat ("\"-keyfile:{0}\"", configuration.AssemblyKeyFile);
					sb.AppendLine ();
				}
			}

			if (!string.IsNullOrEmpty (compilerparameters.DocumentationFile)) {
				sb.AppendFormat ("-doc:{0}", compilerparameters.DocumentationFile);
				sb.AppendLine ();
			}

			string mainClass = project.GetMainClass ();
			if (!string.IsNullOrEmpty (mainClass) && mainClass != "Sub Main") {
				sb.Append ("-main:");
				sb.Append (project.GetMainClass ());
				sb.AppendLine ();
			}

			if (compilerparameters.RemoveIntegerChecks)
				sb.AppendLine ("-removeintchecks+");
			
			if (!string.IsNullOrEmpty (compilerparameters.AdditionalParameters)) {
				sb.Append (compilerparameters.AdditionalParameters);
				sb.AppendLine ();
			}
						
			switch (configuration.CompileTarget) {
				case CompileTarget.Exe:
					sb.AppendLine ("-target:exe");
					break;
				case CompileTarget.WinExe:
					sb.AppendLine ("-target:winexe");
					break;
				case CompileTarget.Library:
					sb.AppendLine ("-target:library");
					break;
				case CompileTarget.Module:
					sb.AppendLine ("-target:module");
					break;
				default:
					throw new NotSupportedException("unknown compile target:" + configuration.CompileTarget);
			}
			
			return sb.ToString();
		}
		
		public BuildResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection references, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			VBCompilerParameters compilerparameters = (VBCompilerParameters) configuration.CompilationParameters;
			if (compilerparameters == null)
				compilerparameters = new VBCompilerParameters ();
			
			string exe = configuration.CompiledOutputName;
			string responseFileName = Path.GetTempFileName();
			StreamWriter writer = new StreamWriter (responseFileName);
			
			writer.WriteLine (GenerateOptions (configuration, compilerparameters, exe));

			// Write references
			foreach (ProjectReference lib in references) {
				foreach (string fileName in lib.GetReferencedFileNames(configuration.Id)) {
					writer.Write ("\"-r:");
					writer.Write (fileName);
					writer.WriteLine ("\"");
				}
			}
			
			// Write source files and embedded resources
			foreach (ProjectFile finfo in projectFiles) {
				if (finfo.Subtype != Subtype.Directory) {
					switch (finfo.BuildAction) {
						case "Compile":
							writer.WriteLine("\"" + finfo.Name + "\"");
						break;
						
						case "EmbeddedResource":
							string fname = finfo.Name;
							if (String.Compare (Path.GetExtension (fname), ".resx", true) == 0)
								fname = Path.ChangeExtension (fname, ".resources");

							writer.WriteLine("\"-resource:{0},{1}\"", fname, finfo.ResourceId);
							break;

						case "Import":
							writer.WriteLine ("-imports:{0}", Path.GetFileName (finfo.Name));
							break;
						
						default:
							continue;
					}
				}
			}
			
			TempFileCollection tf = new TempFileCollection ();
			writer.Close();
			
			string output = "";
			string compilerName = "vbnc";
			string outstr = String.Concat (compilerName, " @", responseFileName);
			
			
			string workingDir = ".";
			if (projectFiles != null && projectFiles.Count > 0)
				workingDir = projectFiles [0].Project.BaseDirectory;
			int exitCode;
			
			monitor.Log.WriteLine (compilerName + " " + string.Join (" ", File.ReadAllLines (responseFileName)));
			exitCode = DoCompilation (outstr, tf, workingDir, ref output);
			
			monitor.Log.WriteLine (output);			                                                          
			BuildResult result = ParseOutput (tf, output);
			if (result.Errors.Count == 0 && exitCode != 0) {
				// Compilation failed, but no errors?
				// Show everything the compiler said.
				result.AddError (output);
			}
			
			FileService.DeleteFile (responseFileName);
			if (configuration.CompileTarget != CompileTarget.Library) {
				WriteManifestFile (exe);
			}
			return result;
		}
		
		// code duplication: see C# backend : CSharpBindingCompilerManager
		void WriteManifestFile(string fileName)
		{
			string manifestFile = String.Concat(fileName, ".manifest");
			if (File.Exists(manifestFile)) {
				return;
			}
			StreamWriter sw = new StreamWriter(manifestFile);
			sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
			sw.WriteLine("");
			sw.WriteLine("<assembly xmlns=\"urn:schemas-microsoft-com:asm.v1\" manifestVersion=\"1.0\">");
			sw.WriteLine("	<dependency>");
			sw.WriteLine("		<dependentAssembly>");
			sw.WriteLine("			<assemblyIdentity");
			sw.WriteLine("				type=\"win32\"");
			sw.WriteLine("				name=\"Microsoft.Windows.Common-Controls\"");
			sw.WriteLine("				version=\"6.0.0.0\"");
			sw.WriteLine("				processorArchitecture=\"X86\"");
			sw.WriteLine("				publicKeyToken=\"6595b64144ccf1df\"");
			sw.WriteLine("				language=\"*\"");
			sw.WriteLine("			/>");
			sw.WriteLine("		</dependentAssembly>");
			sw.WriteLine("	</dependency>");
			sw.WriteLine("</assembly>");
			sw.Close();
		}
		
		BuildResult ParseOutput(TempFileCollection tf, string output)
		{
			CompilerResults results = new CompilerResults (tf);

			using (StringReader sr = new StringReader (output)) {			
				while (true) {
					string curLine = sr.ReadLine();

					if (curLine == null) {
						break;
					}
					
					curLine = curLine.Trim();
					if (curLine.Length == 0) {
						continue;
					}
					
					CompilerError error = CreateErrorFromString (curLine);
					
					if (error != null)
						results.Errors.Add (error);
				}
			}
			return new BuildResult (results, output);
		}
		
		
		private static CompilerError CreateErrorFromString (string error_string)
		{
			Match match;
			int i;
			
			match = regexError.Match (error_string);
			    
			if (match.Success) {
				CompilerError error = new CompilerError ();

				error.IsWarning = match.Result ("${level}").ToLowerInvariant () == "warning";
				error.ErrorNumber = match.Result("${number}");
				error.ErrorText = match.Result("${message}");
				error.FileName = match.Result ("${file}");
				if (int.TryParse (match.Result ("${line}"), out i))
					error.Line = i;
				if (int.TryParse (match.Result ("${column}"), out i))
					error.Column = i;
								
				return error;
			}

			return null;
		}
		
		private int DoCompilation (string outstr, TempFileCollection tf, string working_dir, ref string output)
		{
			StringWriter outwr = new StringWriter ();
			string[] tokens = outstr.Split (' ');			
			try {
				outstr = outstr.Substring (tokens[0].Length+1);
				ProcessWrapper pw = Runtime.ProcessService.StartProcess (tokens[0], "\"" + outstr + "\"", working_dir, outwr, outwr, null);
				pw.WaitForOutput ();
				output = outwr.ToString ();
				
				return pw.ExitCode;
			} finally {
				if (outwr != null)
					outwr.Dispose ();
			}
		}
	}
}

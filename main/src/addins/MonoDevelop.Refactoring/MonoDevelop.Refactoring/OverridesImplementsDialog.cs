// OverridesImplementsDialog.cs
//
//Author:
//    Ankit Jain  <jankit@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using Ambience_ = MonoDevelop.Projects.Dom.Output.Ambience;
using MonoDevelop.Ide;


namespace MonoDevelop.Refactoring
{
	partial class OverridesImplementsDialog : Gtk.Dialog
	{
		IType cls;
		TreeStore store;
		CodeRefactorer refactorer;
		Ambience ambience;

		private const int colCheckedIndex = 0;
		private const int colIconIndex = 1;
		private const int colNameIndex = 2;
		private const int colExplicitIndex = 3;
		private const int colItemIndex = 4;

/*		private const OutputFlags default_conversion_flags =
			OutputFlags.ShowParameterNames |
			OutputFlags.ShowGenericParameters |
			OutputFlags.ShowReturnType |
			OutputFlags.ShowParameters |
			OutputFlags.UseIntrinsicTypeNames;*/
		private const OutputFlags default_conversion_flags =
			OutputFlags.IncludeParameters |
			OutputFlags.IncludeParameterName |
			OutputFlags.IncludeReturnType

;

		public OverridesImplementsDialog (IType cls)
		{
			this.Build();
			this.cls = cls;

			// FIXME: title
			Title = GettextCatalog.GetString ("Override and/or implement members");

			store = new TreeStore (typeof (bool), typeof (Gdk.Pixbuf), typeof (string), typeof (bool), typeof (IMember));

			// Column #1
			TreeViewColumn nameCol = new TreeViewColumn ();
			nameCol.Title = GettextCatalog.GetString ("Name");
			nameCol.Expand = true;
			nameCol.Resizable = true;

			CellRendererToggle cbRenderer = new CellRendererToggle ();
			cbRenderer.Activatable = true;
			cbRenderer.Toggled += OnSelectToggled;
			nameCol.PackStart (cbRenderer, false);
			nameCol.AddAttribute (cbRenderer, "active", colCheckedIndex);

			CellRendererPixbuf iconRenderer = new CellRendererPixbuf ();
			nameCol.PackStart (iconRenderer, false);
			nameCol.AddAttribute (iconRenderer, "pixbuf", colIconIndex);

			CellRendererText nameRenderer = new CellRendererText ();
			nameRenderer.Ellipsize = Pango.EllipsizeMode.End;
			nameCol.PackStart (nameRenderer, true);
			nameCol.AddAttribute (nameRenderer, "text", colNameIndex);

			treeview.AppendColumn (nameCol);

			// Column #2
			CellRendererToggle explicitRenderer = new CellRendererToggle ();
			explicitRenderer.Activatable = true;
			explicitRenderer.Xalign = 0.0f;
			explicitRenderer.Toggled += OnExplicitToggled;
			TreeViewColumn explicitCol = new TreeViewColumn ();
			explicitCol.Title = GettextCatalog.GetString ("Explicit");
			explicitCol.PackStart (explicitRenderer, true);
			explicitCol.SetCellDataFunc (explicitRenderer, new TreeCellDataFunc (RenderExplicitCheckbox));
			explicitCol.AddAttribute (explicitRenderer, "active", colExplicitIndex);
			treeview.AppendColumn (explicitCol);

			store.SetSortColumnId (colNameIndex, SortType.Ascending);
			treeview.Model = store;

			buttonCancel.Clicked += OnCancelClicked;
			buttonOk.Clicked += OnOKClicked;
			buttonSelectAll.Clicked += delegate { SelectAll (true); };
			buttonUnselectAll.Clicked += delegate { SelectAll (false); };

			refactorer = IdeApp.Workspace.GetCodeRefactorer (IdeApp.ProjectOperations.CurrentSelectedSolution);
			ambience = AmbienceService.GetAmbienceForFile (cls.CompilationUnit.FileName);
			PopulateTreeView ();
			UpdateOKButton ();
		}

		void PopulateTreeView ()
		{
			List<IMember> class_members = new List<IMember> ();
			List<IMember> interface_members = new List<IMember> ();

			refactorer.FindOverridables (cls, class_members, interface_members, false, true);

			Dictionary<string, TreeIter> iter_cache = new Dictionary<string,TreeIter> ();
			PopulateTreeView (class_members, iter_cache,
					ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Class, IconSize.Menu));
			PopulateTreeView (interface_members, iter_cache,
					ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Interface, IconSize.Menu));
		}

		void PopulateTreeView (List<IMember> members, Dictionary<string, TreeIter> iter_cache, Gdk.Pixbuf parent_icon)
		{
			foreach (IMember member in members) {
				TreeIter iter;
				if (!iter_cache.TryGetValue (member.DeclaringType.FullName, out iter)) {
					iter = store.AppendValues (false, parent_icon,
			                                   GetDescriptionString (member.DeclaringType), false, member.DeclaringType);
					iter_cache [member.DeclaringType.FullName] = iter;
				}

				store.AppendValues (iter, false,
				                    ImageService.GetPixbuf (member.StockIcon, IconSize.Menu),
				                    GetDescriptionString (member), false, member);
			}
		}

#region Event handlers
		void RenderExplicitCheckbox (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererToggle cellToggle = (CellRendererToggle) cell;
			IMember item = (IMember) store.GetValue (iter, colItemIndex);
			bool parent_is_class = false;

			// Don't show 'explicit' checkbox for Class/interface rows or class methods
			TreeIter parentIter;
			if (item is IMember && store.IterParent (out parentIter, iter)) {
				IType parentClass = store.GetValue (parentIter, colItemIndex) as IType;
				if (parentClass != null && parentClass.ClassType != ClassType.Interface)
					parent_is_class = true;
			}

			if (parent_is_class || item is IType) {
				cellToggle.Visible = false;
			} else {
				cellToggle.Visible = true;
				cellToggle.Active = GetExplicit (iter);
			}
		}

		void OnSelectToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			if (!store.GetIterFromString (out iter, args.Path))
				return;

			bool old_value = GetChecked (iter);
			store.SetValue (iter, colCheckedIndex, !old_value);

			TreeIter parent;
			if (store.IterParent (out parent, iter)) {
				// Member(method/property/etc) row clicked
				// Set the parent's 'checked' state according to the children's state
				bool all_children_checked = true;
				foreach (TreeIter child in GetAllNodes (parent, false)) {
					if (!GetChecked (child)) {
						all_children_checked = false;
						break;
					}
				}

				store.SetValue (parent, colCheckedIndex, all_children_checked);
			} else {
				// Mark children's state to match parent's checked state
				foreach (TreeIter child in GetAllNodes (iter, false))
					store.SetValue (child, colCheckedIndex, !old_value);
			}
			UpdateOKButton ();
		}

		void OnExplicitToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			if (!store.GetIterFromString (out iter, args.Path))
				return;

			store.SetValue (iter, colExplicitIndex, !GetExplicit (iter));
		}

		void OnCancelClicked (object sender, EventArgs e)
		{
			((Widget) this).Destroy ();
		}

		void UpdateOKButton ()
		{
			bool atleast_one_checked = false;
			foreach (TreeIter iter in GetAllNodes (TreeIter.Zero, true)) {
				if (GetChecked (iter)) {
					atleast_one_checked = true;
					break;
				}
			}
			buttonOk.Sensitive = atleast_one_checked;
		}

		void OnOKClicked (object sender, EventArgs e)
		{
			try {
				IType target = this.cls;
				foreach (KeyValuePair<IType, IEnumerable<TreeIter>> kvp in GetAllClasses ()) {
					//update the target class so that new members don't get inserted in weird locations
					target = refactorer.ImplementMembers (target, YieldImpls (kvp), 
							(kvp.Key.ClassType == ClassType.Interface)?
								kvp.Key.Name + " implementation" : null);
				}
			} finally {
				((Widget) this).Destroy ();
			}
		}
#endregion

#region Helper methods
		
		IEnumerable<KeyValuePair<IType, IEnumerable<TreeIter>>> GetAllClasses ()
		{
			TreeIter iter;
			if (!store.GetIterFirst (out iter))
				yield break;
			do {
				TreeIter firstMember;
				if (store.IterChildren (out firstMember, iter)) {
					List<TreeIter> children = new List<TreeIter> (GetCheckedSiblings (firstMember));
					if (children.Count > 0)
						yield return new KeyValuePair<IType, IEnumerable<TreeIter>> (
							(IType) store.GetValue (iter, colItemIndex),
							children);
				}
			} while (store.IterNext (ref iter));
		}
		
		IEnumerable<TreeIter> GetCheckedSiblings (TreeIter firstMember)
		{
			TreeIter iter = firstMember;
			do {
				if (GetChecked (iter)) {
					yield return iter;
				}
			} while (store.IterNext (ref iter));
		}
		
		IEnumerable<KeyValuePair<IMember, IReturnType>> YieldImpls (KeyValuePair<IType, IEnumerable<TreeIter>> kvp)
		{
			bool is_interface = kvp.Key.ClassType == ClassType.Interface;
			IReturnType privateImplementationType = new DomReturnType (kvp.Key.FullName);
			foreach (TreeIter memberIter in kvp.Value) {
				yield return new KeyValuePair<IMember, IReturnType> (
					GetIMember (memberIter),
					(is_interface && GetExplicit (memberIter)) ? privateImplementationType : null);
			}
		}
		
		void SelectAll (bool select)
		{
			foreach (TreeIter iter in GetAllNodes (TreeIter.Zero, true))
				store.SetValue (iter, colCheckedIndex, select);
			UpdateOKButton ();
		}

		bool GetChecked (TreeIter iter)
		{
			return (bool) store.GetValue (iter, colCheckedIndex);
		}
		
		IMember GetIMember (TreeIter iter)
		{
			return (IMember) store.GetValue (iter, colItemIndex);
		}

		bool GetExplicit (TreeIter iter)
		{
			return (bool) store.GetValue (iter, colExplicitIndex);
		}

		IEnumerable<TreeIter> GetAllNodes (TreeIter parent, bool iter_children)
		{
			TreeIter child;
			if (parent.Equals (TreeIter.Zero)) {
				if (!store.IterChildren (out child))
					yield break;
			} else if (!store.IterChildren (out child, parent)) {
				yield break;
			}

			do {
				yield return child;
				if (iter_children && store.IterHasChild (child)) {
					TreeIter iter;
					if (store.IterChildren (out iter, child)) {
						do {
							yield return iter;
						} while (store.IterNext (ref iter));
					}
				}
			} while (store.IterNext (ref child));
		}

		string GetDescriptionString (IType klass)
		{
			return String.Format ("{0} ({1})", ambience.GetString (klass, default_conversion_flags), klass.Namespace);
		}

		string GetDescriptionString (IMember member)
		{
			string sig = null;
			OutputFlags flags = default_conversion_flags & ~OutputFlags.IncludeReturnType;

			sig = ambience.GetString (member, flags);

			if (sig == null)
				throw new InvalidOperationException (String.Format ("Unsupported language member type: {0}", member.GetType ()));

			return sig + " : " + ambience.GetString (member.ReturnType, default_conversion_flags);
		}

#endregion

	}

}

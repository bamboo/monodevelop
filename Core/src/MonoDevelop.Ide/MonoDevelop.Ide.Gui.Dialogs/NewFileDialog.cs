// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.IO;

using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

using Gtk;
using MonoDevelop.Components;
using IconView = MonoDevelop.Components.IconView;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	/// <summary>
	///  This class is for creating a new "empty" file
	/// </summary>
	internal partial class NewFileDialog : Dialog
	{
		ArrayList alltemplates = new ArrayList ();
		ArrayList categories   = new ArrayList ();
		Hashtable icons        = new Hashtable ();
		ArrayList projectLangs = new ArrayList ();

		PixbufList cat_imglist;

		TreeStore catStore;
		
		// Add To Project widgets
		Combine solution;
		string[] projectNames;
		
		Project parentProject;
		string basePath;
		
		string currentProjectType = string.Empty;
		
		public NewFileDialog (Project parentProject, string basePath) : base ()
		{
			Build ();
			this.parentProject = parentProject;
			this.basePath = basePath;
			
			this.TransientFor = IdeApp.Workbench.RootWindow;
			this.BorderWidth = 6;
			this.HasSeparator = false;
			
			InitializeDialog (false);
			InitializeComponents ();
			
			nameEntry.GrabFocus ();
		}
		
		void InitializeDialog (bool update)
		{
			if (update) {
				alltemplates.Clear ();
				projectLangs.Clear ();
				categories.Clear ();
				catStore.Clear ();
				icons.Clear ();
			}
			
			Project project = null;
			
			if (!boxProject.Visible || projectAddCheckbox.Active)
			    project = parentProject;
			
			// if there's a parent project, check whether it wants to filter languages, else use defaults
			if ((project != null) && (project.SupportedLanguages != null) && (project.SupportedLanguages.Length > 0)) {
				projectLangs.AddRange (project.SupportedLanguages);
			} else {
				projectLangs.Add ("");  // match all non-filtered templates
				projectLangs.Add ("*");	// match all .NET langs with CodeDom
			}
			
			ExpandLanguageWildcards (projectLangs);
			
			InitializeTemplates ();
			
			if (update) {
				iconView.Clear ();
				InitializeView ();
			}
		}
		
		public override void Dispose ()
		{
			Destroy ();
			base.Dispose ();
		}
		
		void InitializeView()
		{
			PixbufList smalllist  = new PixbufList();
			PixbufList imglist    = new PixbufList();
			
			smalllist.Add(Services.Resources.GetBitmap("md-empty-file-icon"));
			imglist.Add(Services.Resources.GetBitmap("md-empty-file-icon"));
			
			int i = 0;
			Hashtable tmp = new Hashtable(icons);
			foreach (DictionaryEntry entry in icons) {
				Gdk.Pixbuf bitmap = Services.Resources.GetBitmap(entry.Key.ToString(), Gtk.IconSize.LargeToolbar);
				if (bitmap != null) {
					smalllist.Add(bitmap);
					imglist.Add(bitmap);
					tmp[entry.Key] = ++i;
				} else {
					Runtime.LoggingService.ErrorFormat(GettextCatalog.GetString ("Can't load bitmap {0} using default"), entry.Key.ToString ());
				}
			}
			
			icons = tmp;
			
			InsertCategories(TreeIter.Zero, categories);
			//PropertyService PropertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
			/*for (int j = 0; j < categories.Count; ++j) {
				if (((Category)categories[j]).Name == PropertyService.Get("Dialogs.NewFileDialog.LastSelectedCategory", "C#")) {
					((TreeView)ControlDictionary["categoryTreeView"]).SelectedNode = (TreeNode)((TreeView)ControlDictionary["categoryTreeView"]).Nodes[j];
					break;
				}
			}*/
		}
		
		void InsertCategories(TreeIter node, ArrayList catarray)
		{
			foreach (Category cat in catarray) {
				TreeIter cnode;
				if (node.Equals(Gtk.TreeIter.Zero)) {
					cnode = catStore.AppendValues (cat.Name, cat.Categories, cat.Templates, cat_imglist[1]);
				} else {
					cnode = catStore.AppendValues (node, cat.Name, cat.Categories, cat.Templates, cat_imglist[1]);
				}
				if (cat.Categories.Count > 0)
					InsertCategories (cnode, cat.Categories);
			}
		}
		
		public void SelectTemplate (string id)
		{
			TreeIter iter;
			catStore.GetIterFirst (out iter);
			SelectTemplate (iter, id);
		}
		
		public bool SelectTemplate (TreeIter iter, string id)
		{
			do {
				foreach (TemplateItem item in (ArrayList)(catStore.GetValue (iter, 2))) {
					if (item.Template.Id == id) {
						catView.ExpandToPath (catStore.GetPath (iter));
						catView.Selection.SelectIter (iter);
						CategoryChange (null,null);
						iconView.CurrentlySelected = item;
						return true;
					}
				}
				
				TreeIter citer;
				if (catStore.IterChildren (out citer, iter)) {
					do {
						if (SelectTemplate (citer, id))
							return true;
					} while (catStore.IterNext (ref citer));
				}
				
			} while (catStore.IterNext (ref iter));
			return false;
		}
		
		Category GetCategory (string categoryname)
		{
			return GetCategory (categories, categoryname);
		}
		
		Category GetCategory (ArrayList catList, string categoryname)
		{
			foreach (Category category in catList) {
				if (category.Name == categoryname) {
					return category;
				}
			}
			Category newcategory = new Category(categoryname);
			catList.Add(newcategory);
			return newcategory;
		}
		
		void InitializeTemplates()
		{
			Project project = null;
			
			if (!boxProject.Visible || projectAddCheckbox.Active)
				project = parentProject;
			
			foreach (FileTemplate template in FileTemplate.GetFileTemplates (project)) {
				if (template.Icon != null) {
					icons[template.Icon] = 0; // "create template icon"
				}
				
				// Ignore templates not supported by this project
				if (template.ProjectType != "" && template.ProjectType != currentProjectType)
					continue;
				
				//Find the languages that the template supports
				ArrayList templateLangs = new ArrayList ();
				foreach (string s in template.LanguageName.Split (','))
					templateLangs.Add (s.Trim ());
				ExpandLanguageWildcards (templateLangs);
				
				//Find all matches between the language strings of project and template
				ArrayList langMatches = new ArrayList ();
				
				foreach (string templLang in templateLangs)
					foreach (string projLang in projectLangs)
						if (templLang == projLang)
							langMatches.Add (projLang);
				
				//Eliminate duplicates
				int pos = 0;
				while (pos < langMatches.Count) {
					int next = langMatches.IndexOf (langMatches [pos], pos +1);
					if (next != -1)
						langMatches.RemoveAt (next);
					else
						pos++;
				}
				
				//Add all the possible templates
				foreach (string match in langMatches) {
					AddTemplate (new TemplateItem (template, match), match);	
				}
			}
		}
		
		void ExpandLanguageWildcards (ArrayList list)
		{
			//Template can match all CodeDom .NET languages with a "*"
			if (list.Contains ("*")) {
				ILanguageBinding [] bindings = MonoDevelop.Projects.Services.Languages.GetLanguageBindings ();
				foreach (ILanguageBinding lb in bindings) {
					IDotNetLanguageBinding dnlang = lb as IDotNetLanguageBinding;
					if (dnlang != null && dnlang.GetCodeDomProvider () != null)
						list.Add (dnlang.Language);
					list.Remove ("*");
				}
			}
		}
		
		void AddTemplate (TemplateItem titem, string templateLanguage)
		{
			Project project = null;
			Category cat = null;
			
			if (!boxProject.Visible || projectAddCheckbox.Active)
				project = parentProject;
			
			if (project != null) {
				if ((templateLanguage != "") && (projectLangs.Count != 2) ) {
					// The template requires a language, but the project does not have a single fixed
					// language type (plus empty match), so create a language category
					cat = GetCategory (templateLanguage);
					cat = GetCategory (cat.Categories, titem.Template.Category);
				} else {
					cat = GetCategory (titem.Template.Category);
				}
			} else {
				if (templateLanguage != "") {
					// The template requires a language, but there is no current language set, so
					// create a category for it
					cat = GetCategory (templateLanguage);
					cat = GetCategory (cat.Categories, titem.Template.Category);
				} else {
					cat = GetCategory (titem.Template.Category);
				}
			}

			cat.Templates.Add (titem); 
			
			if (cat.Selected == false && titem.Template.WizardPath == null) {
				cat.Selected = true;
			}
			
			if (!cat.HasSelectedTemplate && titem.Template.Files.Count == 1) {
				if (((FileDescriptionTemplate)titem.Template.Files[0]).Name.StartsWith("Empty")) {
					//titem.Selected = true;
					cat.HasSelectedTemplate = true;
				}
			}
			
			alltemplates.Add(titem);
		}

		//tree view event handler for double-click
		//toggle the expand collapse methods.
		void CategoryActivated(object sender,RowActivatedArgs args)
		{
			if (!catView.GetRowExpanded(args.Path)) {
				catView.ExpandRow(args.Path,false);
			} else {
				catView.CollapseRow(args.Path);
			}
		}
		
		// tree view event handlers
		void CategoryChange(object sender, EventArgs e)
		{
			TreeModel mdl;
			TreeIter iter;
			if (catView.Selection.GetSelected (out mdl, out iter)) {
				FillCategoryTemplates (iter);
				okButton.Sensitive = false;
			}
		}
		
		void FillCategoryTemplates (TreeIter iter)
		{
			iconView.Clear ();
			foreach (TemplateItem item in (ArrayList)(catStore.GetValue (iter, 2))) {
				iconView.AddIcon (new Gtk.Image (Services.Resources.GetBitmap (item.Template.Icon, Gtk.IconSize.Dnd)), item.Name, item);
			}
		}
		
		// list view event handlers
		void SelectedIndexChange (object sender, EventArgs e)
		{
			UpdateOkStatus ();
		}
		
		void NameChanged (object sender, EventArgs e)
		{
			UpdateOkStatus ();
		}
		
		void UpdateOkStatus ()
		{
			try {
				TemplateItem sel = (TemplateItem) iconView.CurrentlySelected;
				if (sel == null) {
					okButton.Sensitive = false;
					return;
				}
				
				FileTemplate item = sel.Template;

				if (item != null) {
					infoLabel.Text = item.Description;
					okButton.Sensitive = item.IsValidName (nameEntry.Text, sel.Language);
				} else {
					okButton.Sensitive = false;
				}
			} catch (Exception ex) {
				Runtime.LoggingService.Error (ex);
			}
		}
		
		// button events
		
		protected void CheckedChange(object sender, EventArgs e)
		{
			//((ListView)ControlDictionary["templateListView"]).View = ((RadioButton)ControlDictionary["smallIconsRadioButton"]).Checked ? View.List : View.LargeIcon;
		}
		
		public event EventHandler OnOked;	
		
		void OpenEvent(object sender, EventArgs e)
		{
			if (!okButton.Sensitive)
				return;
			
			//FIXME: we need to set this up
			//PropertyService PropertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
			//PropertyService.Set("Dialogs.NewProjectDialog.LargeImages", ((RadioButton)ControlDictionary["largeIconsRadioButton"]).Checked);
			//PropertyService.Set("Dialogs.NewFileDialog.LastSelectedCategory", ((TreeView)ControlDictionary["categoryTreeView"]).SelectedNode.Text);
			
			if (iconView.CurrentlySelected != null && nameEntry.Text.Length > 0) {
				TemplateItem titem = (TemplateItem) iconView.CurrentlySelected;
				FileTemplate item = titem.Template;
				Project project = null;
				string path = null;
				
				if (!boxProject.Visible || projectAddCheckbox.Active) {
					project = parentProject;
					path = basePath;
				}
				
				try {
					if (!item.Create (project, path, titem.Language, nameEntry.Text))
						return;
				} catch (Exception ex) {
					Services.MessageService.ShowError (ex);
					return;
				}

				if (OnOked != null)
					OnOked (null, null);
				Respond (Gtk.ResponseType.Ok);
				Destroy ();
			}
		}
		
		/// <summary>
		///  Represents a category
		/// </summary>
		internal class Category
		{
			ArrayList categories = new ArrayList();
			ArrayList templates  = new ArrayList();
			string name;
			
			public bool Selected = false;
			public bool HasSelectedTemplate = false;
			
			public Category(string name)
			{
				this.name = name;
				//ImageIndex = 1;
			}
			
			public string Name {
				get {
					return name;
				}
			}
			
			public ArrayList Categories {
				get {
					return categories;
				}
			}
			
			public ArrayList Templates {
				get {
					return templates;
				}
			}
		}
		
		/// <summary>
		///  Represents a new file template
		/// </summary>
		class TemplateItem
		{
			FileTemplate template;
			string name;
			string language;
			
			public TemplateItem (FileTemplate template, string language)
			{
				this.template = template;
				this.language =  language;
				this.name = template.Name;
			}

			public string Name {
				get {
					return name;
				}
			}
			
			public FileTemplate Template {
				get {
					return template;
				}
			}
			
			public string Language {
				get { return language; }
			}
		}

		void cancelClicked (object o, EventArgs e) {
			Destroy ();
		}
		
		void AddToProjectToggled (object o, EventArgs e)
		{
			projectAddCombo.Sensitive = projectAddCheckbox.Active;
			projectPathLabel.Sensitive = projectAddCheckbox.Active;
			projectFolderEntry.Sensitive = projectAddCheckbox.Active;
			
			TemplateItem titem = (TemplateItem) iconView.CurrentlySelected;
			
			InitializeDialog (true);
			
			if (titem != null)
				SelectTemplate (titem.Template.Id);
		}
		
		void AddToProjectComboChanged (object o, EventArgs e)
		{
			int which = projectAddCombo.Active;
			string projectName = projectNames[which];
			Project project = solution.FindProject (projectName);
			
			if (project != null) {
				if (basePath == null || basePath == String.Empty ||
				    (parentProject != null && basePath == parentProject.BaseDirectory)) {
					basePath = project.BaseDirectory;
					projectFolderEntry.Path = basePath;
				}
				
				parentProject = project;
				
				InitializeDialog (true);
			}
		}
		
		void AddToProjectPathChanged (object o, EventArgs e)
		{
			basePath = projectFolderEntry.Path;
		}
		
		void InitializeComponents()
		{
			catStore = new Gtk.TreeStore (typeof(string), typeof(ArrayList), typeof(ArrayList), typeof(Gdk.Pixbuf));
			catStore.SetSortColumnId (0, SortType.Ascending);
			
			catView.Model = catStore;

			TreeViewColumn catColumn = new TreeViewColumn ();
			catColumn.Title = "categories";
			
			CellRendererText cat_text_render = new CellRendererText ();
			catColumn.PackStart (cat_text_render, true);
			catColumn.AddAttribute (cat_text_render, "text", 0);

			catView.AppendColumn (catColumn);

			okButton.Clicked += new EventHandler (OpenEvent);
			cancelButton.Clicked += new EventHandler (cancelClicked);

			nameEntry.Changed += new EventHandler (NameChanged);
			nameEntry.Activated += new EventHandler (OpenEvent);
			
			CombineEntryCollection projects = null;
			if (parentProject == null) {
				solution = IdeApp.ProjectOperations.CurrentOpenCombine;
				if (solution != null)
					projects = solution.GetAllProjects ();
			}
			
			if (projects != null) {
				boxProject.Visible = true;
				projectAddCheckbox.Toggled += new EventHandler (AddToProjectToggled);
				
				Project curProject = IdeApp.ProjectOperations.CurrentSelectedProject;
				projectNames = new string [projects.Count];
				int i = 0;
				
				foreach (Project project in projects)
					projectNames[i++] = project.Name;
				
				Array.Sort (projectNames);
				if (curProject != null) {
					for (i = 0; i < projectNames.Length; i++) {
						if (projectNames[i] == curProject.Name)
							break;
					}
				}
				
				foreach (string pn in projectNames)
					projectAddCombo.AppendText (pn);
				
				projectAddCombo.Active = i;
				projectAddCombo.Sensitive = false;
				projectAddCombo.Changed += new EventHandler (AddToProjectComboChanged);
				
				projectPathLabel.Sensitive = false;
				projectFolderEntry.Sensitive = false;
				if (curProject != null)
					projectFolderEntry.Path = curProject.BaseDirectory;
				projectFolderEntry.PathChanged += new EventHandler (AddToProjectPathChanged);
				
				if (curProject != null) {
					basePath = curProject.BaseDirectory;
					parentProject = curProject;
				}
			}
			else {
				boxProject.Visible = false;
			}
			
			cat_imglist = new PixbufList();
			cat_imglist.Add(Services.Resources.GetBitmap("md-open-folder"));
			cat_imglist.Add(Services.Resources.GetBitmap("md-closed-folder"));
			catView.Selection.Changed += new EventHandler (CategoryChange);
			catView.RowActivated += new RowActivatedHandler (CategoryActivated);
			iconView.IconSelected += new EventHandler(SelectedIndexChange);
			iconView.IconDoubleClicked += new EventHandler(OpenEvent);
			InitializeView ();
			UpdateOkStatus ();
		}
	}
}

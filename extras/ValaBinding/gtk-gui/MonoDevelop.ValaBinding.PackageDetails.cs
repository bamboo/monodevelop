// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

namespace MonoDevelop.ValaBinding {
    
    
    public partial class PackageDetails {
        
        private Gtk.VBox vbox3;
        
        private Gtk.Table table1;
        
        private Gtk.Label descriptionLabel;
        
        private Gtk.Label label7;
        
        private Gtk.Label label8;
        
        private Gtk.Label label9;
        
        private Gtk.Label nameLabel;
        
        private Gtk.Label versionLabel;
        
        private Gtk.VBox vbox4;
        
        private Gtk.Label label13;
        
        private Gtk.ScrolledWindow scrolledwindow1;
        
        private Gtk.TreeView requiresTreeView;
        
        private Gtk.Button buttonOk;
        
        protected virtual void Build() {
            Stetic.Gui.Initialize(this);
            // Widget MonoDevelop.ValaBinding.PackageDetails
            this.Name = "MonoDevelop.ValaBinding.PackageDetails";
            this.Title = Mono.Unix.Catalog.GetString("Package Details");
            this.WindowPosition = ((Gtk.WindowPosition)(4));
            this.HasSeparator = false;
            // Internal child MonoDevelop.ValaBinding.PackageDetails.VBox
            Gtk.VBox w1 = this.VBox;
            w1.Name = "dialog1_VBox";
            w1.BorderWidth = ((uint)(2));
            // Container child dialog1_VBox.Gtk.Box+BoxChild
            this.vbox3 = new Gtk.VBox();
            this.vbox3.Name = "vbox3";
            this.vbox3.Spacing = 6;
            this.vbox3.BorderWidth = ((uint)(3));
            // Container child vbox3.Gtk.Box+BoxChild
            this.table1 = new Gtk.Table(((uint)(3)), ((uint)(2)), false);
            this.table1.Name = "table1";
            this.table1.RowSpacing = ((uint)(6));
            this.table1.ColumnSpacing = ((uint)(6));
            // Container child table1.Gtk.Table+TableChild
            this.descriptionLabel = new Gtk.Label();
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Xalign = 0F;
            this.descriptionLabel.Yalign = 0F;
            this.descriptionLabel.LabelProp = Mono.Unix.Catalog.GetString("label12");
            this.table1.Add(this.descriptionLabel);
            Gtk.Table.TableChild w2 = ((Gtk.Table.TableChild)(this.table1[this.descriptionLabel]));
            w2.TopAttach = ((uint)(2));
            w2.BottomAttach = ((uint)(3));
            w2.LeftAttach = ((uint)(1));
            w2.RightAttach = ((uint)(2));
            w2.XOptions = ((Gtk.AttachOptions)(4));
            w2.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table1.Gtk.Table+TableChild
            this.label7 = new Gtk.Label();
            this.label7.Name = "label7";
            this.label7.Xalign = 0F;
            this.label7.LabelProp = Mono.Unix.Catalog.GetString("Name:");
            this.table1.Add(this.label7);
            Gtk.Table.TableChild w3 = ((Gtk.Table.TableChild)(this.table1[this.label7]));
            w3.XOptions = ((Gtk.AttachOptions)(4));
            w3.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table1.Gtk.Table+TableChild
            this.label8 = new Gtk.Label();
            this.label8.Name = "label8";
            this.label8.Xalign = 0F;
            this.label8.LabelProp = Mono.Unix.Catalog.GetString("Version:");
            this.table1.Add(this.label8);
            Gtk.Table.TableChild w4 = ((Gtk.Table.TableChild)(this.table1[this.label8]));
            w4.TopAttach = ((uint)(1));
            w4.BottomAttach = ((uint)(2));
            w4.XOptions = ((Gtk.AttachOptions)(4));
            w4.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table1.Gtk.Table+TableChild
            this.label9 = new Gtk.Label();
            this.label9.Name = "label9";
            this.label9.Xalign = 0F;
            this.label9.Yalign = 0F;
            this.label9.LabelProp = Mono.Unix.Catalog.GetString("Description:");
            this.table1.Add(this.label9);
            Gtk.Table.TableChild w5 = ((Gtk.Table.TableChild)(this.table1[this.label9]));
            w5.TopAttach = ((uint)(2));
            w5.BottomAttach = ((uint)(3));
            w5.XOptions = ((Gtk.AttachOptions)(4));
            w5.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table1.Gtk.Table+TableChild
            this.nameLabel = new Gtk.Label();
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Xalign = 0F;
            this.nameLabel.LabelProp = Mono.Unix.Catalog.GetString("label10");
            this.table1.Add(this.nameLabel);
            Gtk.Table.TableChild w6 = ((Gtk.Table.TableChild)(this.table1[this.nameLabel]));
            w6.LeftAttach = ((uint)(1));
            w6.RightAttach = ((uint)(2));
            w6.XOptions = ((Gtk.AttachOptions)(4));
            w6.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table1.Gtk.Table+TableChild
            this.versionLabel = new Gtk.Label();
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Xalign = 0F;
            this.versionLabel.LabelProp = Mono.Unix.Catalog.GetString("label11");
            this.table1.Add(this.versionLabel);
            Gtk.Table.TableChild w7 = ((Gtk.Table.TableChild)(this.table1[this.versionLabel]));
            w7.TopAttach = ((uint)(1));
            w7.BottomAttach = ((uint)(2));
            w7.LeftAttach = ((uint)(1));
            w7.RightAttach = ((uint)(2));
            w7.XOptions = ((Gtk.AttachOptions)(4));
            w7.YOptions = ((Gtk.AttachOptions)(4));
            this.vbox3.Add(this.table1);
            Gtk.Box.BoxChild w8 = ((Gtk.Box.BoxChild)(this.vbox3[this.table1]));
            w8.Position = 0;
            w8.Expand = false;
            w8.Fill = false;
            // Container child vbox3.Gtk.Box+BoxChild
            this.vbox4 = new Gtk.VBox();
            this.vbox4.Name = "vbox4";
            this.vbox4.Spacing = 6;
            // Container child vbox4.Gtk.Box+BoxChild
            this.label13 = new Gtk.Label();
            this.label13.Name = "label13";
            this.label13.Xalign = 0F;
            this.label13.LabelProp = Mono.Unix.Catalog.GetString("Requires:");
            this.vbox4.Add(this.label13);
            Gtk.Box.BoxChild w9 = ((Gtk.Box.BoxChild)(this.vbox4[this.label13]));
            w9.Position = 0;
            w9.Expand = false;
            w9.Fill = false;
            // Container child vbox4.Gtk.Box+BoxChild
            this.scrolledwindow1 = new Gtk.ScrolledWindow();
            this.scrolledwindow1.CanFocus = true;
            this.scrolledwindow1.Name = "scrolledwindow1";
            this.scrolledwindow1.ShadowType = ((Gtk.ShadowType)(1));
            // Container child scrolledwindow1.Gtk.Container+ContainerChild
            Gtk.Viewport w10 = new Gtk.Viewport();
            w10.ShadowType = ((Gtk.ShadowType)(0));
            // Container child GtkViewport.Gtk.Container+ContainerChild
            this.requiresTreeView = new Gtk.TreeView();
            this.requiresTreeView.CanFocus = true;
            this.requiresTreeView.Name = "requiresTreeView";
            w10.Add(this.requiresTreeView);
            this.scrolledwindow1.Add(w10);
            this.vbox4.Add(this.scrolledwindow1);
            Gtk.Box.BoxChild w13 = ((Gtk.Box.BoxChild)(this.vbox4[this.scrolledwindow1]));
            w13.Position = 1;
            this.vbox3.Add(this.vbox4);
            Gtk.Box.BoxChild w14 = ((Gtk.Box.BoxChild)(this.vbox3[this.vbox4]));
            w14.Position = 1;
            w1.Add(this.vbox3);
            Gtk.Box.BoxChild w15 = ((Gtk.Box.BoxChild)(w1[this.vbox3]));
            w15.Position = 0;
            // Internal child MonoDevelop.ValaBinding.PackageDetails.ActionArea
            Gtk.HButtonBox w16 = this.ActionArea;
            w16.Name = "dialog1_ActionArea";
            w16.Spacing = 6;
            w16.BorderWidth = ((uint)(5));
            w16.LayoutStyle = ((Gtk.ButtonBoxStyle)(4));
            // Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
            this.buttonOk = new Gtk.Button();
            this.buttonOk.CanDefault = true;
            this.buttonOk.CanFocus = true;
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.UseStock = true;
            this.buttonOk.UseUnderline = true;
            this.buttonOk.Label = "gtk-ok";
            this.AddActionWidget(this.buttonOk, -5);
            Gtk.ButtonBox.ButtonBoxChild w17 = ((Gtk.ButtonBox.ButtonBoxChild)(w16[this.buttonOk]));
            w17.Expand = false;
            w17.Fill = false;
            if ((this.Child != null)) {
                this.Child.ShowAll();
            }
            this.DefaultWidth = 608;
            this.DefaultHeight = 518;
            this.Show();
            this.buttonOk.Clicked += new System.EventHandler(this.OnButtonOkClicked);
        }
    }
}

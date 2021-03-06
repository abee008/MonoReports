// 
// ReportExplorer.cs
//  
// Author:
//       Tomasz Kubacki <Tomasz.Kubacki (at) gmail.com>
// 
// Copyright (c) 2010 Tomasz Kubacki 2010
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
using MonoReports.ControlView;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using MonoReports.Services;
using Gdk;
using Gtk;
using MonoReports.Model.Data;
using System.Linq;
using Mono.Unix;

namespace MonoReports.Gui.Widgets
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ReportExplorer : Gtk.Bin
	{
		Gtk.TreeStore theModel;
		
		Gtk.TreeIter reportNode;
		Gtk.TreeIter expressionsNode;
		Gtk.TreeIter dataFieldsNode;
		Gtk.TreeIter groupsNode;
		Gtk.TreeIter parametersNode;
		Gtk.TreeIter imagesNode;
		DesignService designService;

		public DesignService DesignService {

			get { return designService; }

				
			set { 	
				
				if (designService != null) {
					designService.OnReportChanged -= HandleDesignServiceOnReportChanged;	
					designService.OnReportDataFieldsRefreshed -= HandleDesignServiceOnReportDataFieldsRefreshed;
				}
				
				designService = value; 
				
				if (designService != null) {
					designService.OnReportChanged += HandleDesignServiceOnReportChanged;				
					designService.OnReportDataFieldsRefreshed += HandleDesignServiceOnReportDataFieldsRefreshed;
				}
			}
		}

		void HandleDesignServiceOnReportDataFieldsRefreshed (object sender, EventArgs e)
		{
			updateTreeNode (parametersNode,designService.Report.Parameters); 
			updateTreeNode (dataFieldsNode,designService.Report.DataFields); 			
			updateTreeNode (expressionsNode,designService.Report.ExpressionFields); 
		}
 
		void HandleDesignServiceOnReportChanged (object sender, EventArgs e)
		{		
			
			updateTreeNode (parametersNode,designService.Report.Parameters); 
			updateTreeNode (dataFieldsNode,designService.Report.DataFields); 			
			updateTreeNode (expressionsNode,designService.Report.ExpressionFields); 
			updateTreeNode (groupsNode,designService.Report.Groups); 
			updateTreeNode (imagesNode,designService.Report.ResourceRepository); 
		}

		public IWorkspaceService Workspace {get; set;}

		public ReportExplorer ()
		{
			this.Build ();
		   	var reportCellRenderer = new Gtk.CellRendererText ();
			var reportColumn = exporerTreeview.AppendColumn (Catalog.GetString("Report"), reportCellRenderer);
			reportColumn.SetCellDataFunc(reportCellRenderer,new Gtk.TreeCellDataFunc (renderReportCell));
			theModel = new Gtk.TreeStore (typeof(TreeItemWrapper));	
			exporerTreeview.Model = theModel;

			reportNode =  theModel.AppendValues(new TreeItemWrapper(Catalog.GetString("Report")));
			parametersNode = theModel.AppendValues (reportNode,new TreeItemWrapper(Catalog.GetString("Parameters")));
			dataFieldsNode = theModel.AppendValues (reportNode,new TreeItemWrapper(Catalog.GetString("Data")));
			expressionsNode = theModel.AppendValues (reportNode,new TreeItemWrapper(Catalog.GetString("Expressions")));
			
 		
			groupsNode = theModel.AppendValues (reportNode,new TreeItemWrapper(Catalog.GetString("Groups")));
			imagesNode = theModel.AppendValues (reportNode,new TreeItemWrapper(Catalog.GetString("Images")));
			exporerTreeview.Selection.Changed += HandleExporerTreeviewSelectionChanged;

			Gtk.Drag.SourceSet (exporerTreeview, 
				ModifierType.Button1Mask, 
				new TargetEntry[]{new TargetEntry ("Field", TargetFlags.OtherWidget,2)}, 
			DragAction.Copy);
			
			exporerTreeview.RowActivated += HandleExporerTreeviewRowActivated;
			exporerTreeview.ExpandAll();
		}
		
		//TODO: 3tk needs to be cleaned
		void HandleExporerTreeviewRowActivated (object o, RowActivatedArgs args)
		{
			
			//Indices [0] = Data Fields
			//if (args.Path.Indices [0] == 1 && args.Path.Depth == 2) {
			//	var field = DesignService.Report.Fields [args.Path.Indices [1]];										
			//}
		}

		void HandleExporerTreeviewSelectionChanged (object sender, EventArgs e)
		{
//			TreeIter item;
//			exporerTreeview.Selection.GetSelected (out item);
//			var path = theModel.GetPath(item);
//		    if(path.Depth == 3) {
//				if (path.Indices[1] == 1) {
//					Gtk.Drag.SourceSet (
//								exporerTreeview, ModifierType.None, new TargetEntry[]{new TargetEntry ("Field", TargetFlags.OtherWidget,2)}, 
//							DragAction.Copy);
//				}
//			}
				
		}

		void updateTreeNode<T> (TreeIter theNode , IEnumerable<T> objects)
		{
			TreeIter item;
			if (theModel.IterChildren (out item, theNode)) {
				int depth = theModel.IterDepth (theNode);
	
				while (theModel.Remove (ref item) && 
					theModel.IterDepth (item) > depth)
					;
			}
			int i=0;
			foreach (T o in objects) {
				 
				theModel.AppendValues (theNode, new TreeItemWrapper(o));
				i++;
			}
			exporerTreeview.ExpandRow(theModel.GetPath(theNode),true);				
		}
		
		private void renderReportCell (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			TreeItemWrapper w = (TreeItemWrapper) model.GetValue (iter, 0); 
			(cell as Gtk.CellRendererText).Text = w.ToString();
		}

		protected virtual void OnUpdateFieldsFromDataSourceButtonButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			
		}

		protected virtual void OnUpdateFieldsFromDataSourceButtonActivated (object sender, System.EventArgs e)
		{
			
		}

		protected virtual void OnUpdateFieldsFromDataSourceButtonClicked (object sender, System.EventArgs e)
		{
			
		}

		[GLib.ConnectBefore]
		protected virtual void OnExporerTreeviewButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			TreePath path;
			exporerTreeview.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path);
			if (path != null) {
				if (args.Event.Button == 3) { 					
					Gtk.MenuItem addNewMenuItem = null;
					if(path.Depth > 1 ) {
					int index = path.Indices [1];
					if ((index == 0 || index == 1) && path.Depth == 2) {
						Gtk.Menu jBox = new Gtk.Menu ();
						if (index == 1) {
							addNewMenuItem = new MenuItem (Catalog.GetString("Add data field"));
								
						} else if (index == 0){
							addNewMenuItem = new MenuItem (Catalog.GetString("Add parameter field"));
						}
						
						jBox.Add (addNewMenuItem);		
								
						addNewMenuItem.Activated += delegate(object sender, EventArgs e) {					
							PropertyFieldEditor pfe = new PropertyFieldEditor ();
							pfe.Response += delegate(object oo, ResponseArgs argss) {						
								if (argss.ResponseId == ResponseType.Ok) {
									if (index == 0){
										DesignService
										.Report
										.Parameters
										.Add (
											new Field () {
												FieldKind = FieldKind.Parameter,
												Name = pfe.PropertyName,
												DefaultValue = pfe.DefaultValue 
												}
										);
										updateTreeNode(parametersNode,designService.Report.Parameters); 	
		
									} else if (index == 1){
										DesignService
										.Report
										.DataFields
										.Add (
											new Field () { 
												FieldKind = FieldKind.Data,
												Name = pfe.PropertyName
												}
										);
											
										updateTreeNode(dataFieldsNode,designService.Report.DataFields); 
											
									} 
									
								}
									
								pfe.Destroy ();
								
							};
							pfe.Show ();
						}; 
						
						jBox.ShowAll ();
						jBox.Popup ();	
						
					}else if (index == 2 && path.Depth == 2) {
						Gtk.Menu jBox = new Gtk.Menu ();
						addNewMenuItem = new MenuItem (Catalog.GetString("Add expression field"));								
						jBox.Add (addNewMenuItem);		
								
						addNewMenuItem.Activated += delegate(object sender, EventArgs e) {					
							ExpressionFieldEditor efe = new ExpressionFieldEditor ();
							efe.Response += delegate(object oo, ResponseArgs argss) {						
								if (argss.ResponseId == ResponseType.Ok) {
									  if (index == 2){
										ExpressionField f =  new ExpressionField () {
												FieldKind = FieldKind.Expression,
												Name = efe.PropertyName,											
												ExpressionScript = efe.ExpressionScript};
										
										f.DataProvider = new ExpressionFieldValueProvider(f);
							
										DesignService
										.Report
										.ExpressionFields
										.Add (
											f
										);
											
										updateTreeNode(expressionsNode,designService.Report.ExpressionFields); 
									}
									
								}
									
								efe.Destroy ();
								
							};
							efe.Show ();
						}; 
						
						jBox.ShowAll ();
						jBox.Popup ();	
						
					}else if ( (index == 0 || index == 1 || index == 2 )  && path.Depth == 3) {
						Gtk.Menu jBox = new Gtk.Menu ();
						 
						
						string menuText = String.Empty;
						if (index == 0)
							menuText = Catalog.GetString("Delete parameter field");
						else if (index == 1)
							menuText = Catalog.GetString("Delete data field");
						else if (index == 2)
							menuText = Catalog.GetString("Delete expression field");
							
						Gtk.MenuItem deleteFieldItem = new MenuItem (menuText);
						 
						jBox.Add (deleteFieldItem);		
								
						deleteFieldItem.Activated += delegate(object sender, EventArgs e) {		
							TreeIter fieldIter;
							theModel.GetIter(out fieldIter,path);
							var fieldNodeWrapper = theModel.GetValue(fieldIter,0) as TreeItemWrapper;
							var f =  (Field) fieldNodeWrapper.Object;
							if (index == 0) {
								designService.Report.Parameters.Remove(f);
 								updateTreeNode(parametersNode,designService.Report.Parameters); 
							} else if ( index == 1 ) {
								designService.Report.DataFields.Remove(f);
 								updateTreeNode(dataFieldsNode,designService.Report.DataFields); 
							} else if ( index == 2 ) {
								designService.Report.ExpressionFields.Remove(f);
 								updateTreeNode(expressionsNode,designService.Report.DataFields); 	
							}
							Workspace.ShowInPropertyGrid(null);
						}; 
						
						jBox.ShowAll ();
						jBox.Popup ();		
						
					} else if (index == 4 && path.Depth == 2) {
						 Gtk.Menu jBox = new Gtk.Menu ();
						 
							addNewMenuItem = new MenuItem (Catalog.GetString("Add image"));
							jBox.Add (addNewMenuItem);
							addNewMenuItem.Activated += delegate(object sender, EventArgs e) {
								
								
								Gtk.FileChooserDialog fc = new Gtk.FileChooserDialog (Catalog.GetString("Choose monoreports file to open"),null, FileChooserAction.Open , Catalog.GetString("Cancel"), ResponseType.Cancel, Catalog.GetString("Open"), ResponseType.Accept);
								var fileFilter = new FileFilter { Name = Catalog.GetString("Images") };
								fileFilter.AddPattern ("*.jpg");
								fileFilter.AddPattern ("*.png");
								fileFilter.AddPattern ("*.gif");
								fileFilter.AddPattern ("*.JPG");
								fileFilter.AddPattern ("*.PNG");
								fileFilter.AddPattern ("*.GIF");				
								fc.AddFilter (fileFilter);
		
								if (fc.Run () == (int)ResponseType.Accept) {
									System.IO.FileStream file = System.IO.File.OpenRead (fc.Filename);
								 
									byte[] bytes = new byte[file.Length];
									file.Read (bytes, 0, (int)file.Length);
									string fileName = System.IO.Path.GetFileName(fc.Filename);
									designService.Report.ResourceRepository.Add(fileName, bytes);
									designService.PixbufRepository.AddOrUpdatePixbufByName(fileName);
									file.Close ();
								}
		
								fc.Destroy ();																								
								updateTreeNode(imagesNode,designService.Report.ResourceRepository); 
							};
						jBox.ShowAll ();
						jBox.Popup ();	
					}else if (index == 4 && path.Depth == 3) {
						Gtk.Menu jBox = new Gtk.Menu ();
						 
						Gtk.MenuItem deleteImageItem = new MenuItem (Catalog.GetString("Delete image"));
						 
						jBox.Add (deleteImageItem);		
								
						deleteImageItem.Activated += delegate(object sender, EventArgs e) {		
							TreeIter imageIter;
							theModel.GetIter(out imageIter,path);
							var imageNodeWrapper = theModel.GetValue(imageIter,0) as TreeItemWrapper;
							var kvp =  (KeyValuePair<string,byte[]>) imageNodeWrapper.Object;
							designService.Report.ResourceRepository.Remove(kvp.Key);							
 							updateTreeNode(imagesNode,designService.Report.ResourceRepository); 
						}; 
						
						jBox.ShowAll ();
						jBox.Popup ();		
						
					} 
				} 
	
				} else if ( args.Event.Button == 1 ) {
					
					if (path.Depth == 3) {
						int index = path.Indices [1];
						try{
						if (index == 0) {								
							Workspace.ShowInPropertyGrid (designService.Report.Parameters [path.Indices [2]]);
						} else if (index == 1) {
							Workspace.ShowInPropertyGrid (designService.Report.DataFields [path.Indices [2]]);
						} else if (index == 2) {
							Workspace.ShowInPropertyGrid (designService.Report.ExpressionFields [path.Indices [2]]);
						}
							
						}catch (Exception exp){
							//3tk to be found why sometimes can't be show in PG
							Console.WriteLine(exp.ToString());
						}
					}
				}
	
				
			}
		

		}
	}
	
	public class TreeItemWrapper {
		
		public TreeItemWrapper () {
		}
		
		public TreeItemWrapper (object obj) {
			this.obj = obj;
		}
		
		object obj;
		public object Object {
			get { return obj; }
			set { obj = value; }
		}
		
		public override string ToString ()
		{
			 return  obj.ToString();
		}
	}

}
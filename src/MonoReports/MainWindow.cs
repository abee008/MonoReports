// 
// MainWindow.cs
//  
// Author:
//       Tomasz Kubacki <Tomasz.Kubacki(at)gmail.com>
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
using MonoReports.Model;
using Controls = MonoReports.Model.Controls;
using MonoReports.Core;
using Gtk;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Cairo;
using Model = MonoReports.Model.Engine;
using MonoReports.Gui.Widgets;
using MonoReports.ControlView;
using MonoReports.Tools;
using MonoReports.Gui;
using MonoReports.Services;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using MonoReports.Renderers;
using MonoReports.Model.Controls;

public partial class MainWindow : Gtk.Window
{

	DesignService designService;
	ToolBoxService toolBoxService;
	WorkspaceService workspaceService;
	CompilerService compilerService;

	public MainWindow () : base(Gtk.WindowType.Toplevel)
	{
		Build ();
		compilerService = new CompilerService();
		workspaceService = new WorkspaceService (this,maindesignview1.DesignDrawingArea,maindesignview1.PreviewDrawingArea,mainPropertygrid);
		designService = new DesignService (workspaceService,new Report());
		toolBoxService = new ToolBoxService ();
		designService.ToolBoxService = toolBoxService;
		maindesignview1.DesignService = designService;
		maindesignview1.WorkSpaceService = workspaceService;
		maindesignview1.Compiler = compilerService;
		
		var reportRenderer = new ReportRenderer(designService.CurrentContext);
        reportRenderer.RegisterRenderer(typeof(TextBlock), new TextBlockRenderer());
        reportRenderer.RegisterRenderer(typeof(Line), new LineRenderer());
		reportRenderer.RegisterRenderer(typeof(MonoReports.Model.Controls.Image), new ImageRenderer(){ PixbufRepository = designService.PixbufRepository});
			
		maindesignview1.ReportRenderer = reportRenderer;
		workspaceService.InvalidateDesignArea ();		
		reportExplorer.DesignService = designService;
		reportExplorer.Workspace = workspaceService;
		toolBoxService.AddTool (new ZoomTool (designService));		
		toolBoxService.AddTool (new LineTool (designService));		
		toolBoxService.AddTool (new TextBlockTool (designService));
		toolBoxService.AddTool (new SubreportTool (designService));
		toolBoxService.AddTool (new SectionTool (designService));
		toolBoxService.AddTool (new ImageTool (designService));
		toolBoxService.AddTool (new RectTool (designService));
		toolBoxService.BuildToolBar (mainToolbar);
 		
		
		ToolBarButton toolButton = new ToolBarButton ("pdf.png","exportPdf","export to pdf");
			toolButton.Clicked += delegate(object sender, EventArgs e) {
			
			object r;
			string msg;
			compilerService.Evaluate(designService.Report.DataScript,out r,out msg);					
			designService.Report.DataSource = r;
			
			using (PdfSurface pdfSurface = new PdfSurface("report_" + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString() + ".pdf",designService.Report.Width,designService.Report.Height)){
				Cairo.Context cr = new Cairo.Context (pdfSurface);
				ReportRenderer renderer = new ReportRenderer(cr);
				renderer.RegisterRenderer(typeof(TextBlock), new TextBlockRenderer());
        		renderer.RegisterRenderer(typeof(Line), new LineRenderer());
				renderer.RegisterRenderer(typeof(MonoReports.Model.Controls.Image), new ImageRenderer(){ PixbufRepository = designService.PixbufRepository});
				MonoReports.Model.Engine.ReportEngine engine = new MonoReports.Model.Engine.ReportEngine(designService.Report,renderer);
				engine.Process();
				for (int i = 0; i < designService.Report.Pages.Count; ++i) {
					renderer.RenderPage(designService.Report.Pages[i]);
					cr.ShowPage();
				}			
				pdfSurface.Finish();
			
				}
			};
		
		mainToolbar.Insert (toolButton,4);		
		
		mainPropertygrid.AddPropertyEditor(typeof(MonoReports.Model.Controls.Point),typeof(MonoReports.Extensions.PropertyGridEditors.PointEditorCell));
		mainPropertygrid.AddPropertyEditor(typeof(MonoReports.Model.Border),typeof(MonoReports.Extensions.PropertyGridEditors.BorderEditorCell));
		mainPropertygrid.AddPropertyEditor(typeof(MonoReports.Model.Controls.Padding),typeof(MonoReports.Extensions.PropertyGridEditors.PaddingEditorCell));
		mainPropertygrid.AddPropertyEditor(typeof(MonoReports.Model.Controls.Color),typeof(MonoReports.Extensions.PropertyGridEditors.MonoreportsColorEditorCell));
		
		
		
		designService.Report = exampleReport();
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}

	Report exampleReport ()
	{
			
		var currentReport = new Report ();
		
		
		currentReport.DataScript = @"
new[]{ new { Name = ""Alfred"", Surname = ""Tarski"", Age = 33 }, 
        new { Name =""Gotlob"", Surname = ""Frege"", Age = 42 }, 
        new { Name = ""Kurt"", Surname =""Gödel"", Age = 22 }, 
        new { Name = ""Unknown"", Surname = ""Logican"", Age = 33 }, 
        new { Name = ""Józef"", Surname = ""Bocheński"", Age = 22 }, 
        new { Name = ""Stanisław"", Surname = ""Leśniewski"", Age = 79 }, 
        new { Name = ""Saul"", Surname = ""Kripke"", Age = 40 }, 
        new { Name = ""George"", Surname = ""Boolos"", Age = 79 } 
	 };";
		
		currentReport.ReportHeaderSection.Controls.Add (
			new Controls.TextBlock { FontSize = 16, FontName = "Helvetica", Text = "First textblock - mono zelot", FontColor = new Controls.Color(1,0,0),
			CanGrow = true, Location = new Controls.Point (1, 2), Size = new Controls.Size (200, 25) });
		
		
		var _assembly = Assembly.GetExecutingAssembly ();				
		var _imageStream = _assembly.GetManifestResourceStream ("tarski.png");
		byte[] bytes = new byte[_imageStream.Length];
		_imageStream.Read (bytes, 0, (int)_imageStream.Length);		
		currentReport.ResourceRepository.Add (bytes);
		var img = new MonoReports.Model.Controls.Image { ImageIndex = 0, Location = new Controls.Point (3, 2), Size = new Controls.Size (298, 272) };
		currentReport.DetailSection.Controls.Add (img);
			
		
		currentReport.PageHeaderSection.Controls.Add (new Controls.TextBlock { FontSize = 24, FontName = "Helvetica", 
			Text = "Second example section - żwawy żółw", FontColor = new Controls.Color(1,0,0), Location = new Controls.Point (123, 1), CanGrow = false, Size = new Controls.Size (160, 30) });
		
		currentReport.PageHeaderSection.Controls.Add (new Controls.TextBlock { FontSize = 12, FontName = "Helvetica", Text = "third example text - chyży ślimak", FontColor = new Controls.Color(1,0,0), Location = new Controls.Point (300, 7), CanGrow = true, Size = new Controls.Size (100, 20) });
		
		currentReport.DetailSection.Size = new Controls.Size (600, 300);
		
		currentReport.DetailSection.Controls.Add (new Controls.TextBlock { FontSize = 12, FontName = "Helvetica", Text = "Chars", FontColor = new Controls.Color(1,0,0), Location = new Controls.Point (300, 42), Size = new Controls.Size (200, 30), FieldName = "Name", BackgroundColor = new Controls.Color(0,0,0,0), HorizontalAlignment = Controls.HorizontalAlignment.Left, Border = new Border { WidthAll = 0 },
		CanGrow = true });
		
		currentReport.DetailSection.Controls.Add (new Controls.TextBlock { FontSize = 12, FontName = "Helvetica", Text = "Surname", FontColor = new Controls.Color(1,0,0), Location = new Controls.Point (300, 12), Size = new Controls.Size (200, 30), FieldName = "Surname", BackgroundColor = new Controls.Color(1,1,0), HorizontalAlignment = Controls.HorizontalAlignment.Left, Border = new Border { WidthAll = 0 },
		CanGrow = true });
		
		currentReport.PageFooterSection.Controls.Add (new Controls.TextBlock { FontSize = 12, FontName = "Times", Text = "fourth text - szybki jeż", FontColor = new Controls.Color(1,1,1), Location = new Controls.Point (23, 3), Size = new Controls.Size (400,25), BackgroundColor = new Controls.Color(0.2,1,0), HorizontalAlignment = Controls.HorizontalAlignment.Left, Border = new Border { WidthAll = 0 }, CanGrow = false });
		
		
		currentReport.PageFooterSection.Controls.Add (new Controls.Line { Location = new Controls.Point (20, 1), End = new Controls.Point (200, 1) });
	//	currentReport.AddGroup ("Age");
		return currentReport;
	}

	protected virtual void OnQuitActionActivated (object sender, System.EventArgs e)
	{
		Application.Quit ();
	}

	public void Status (string message)
	{
		bottomStatusbar.Push (1, message);
	}

	public void ShowInPropertyGrid (object o)
	{
		mainPropertygrid.CurrentObject = o;
	}

	public void SetCursor (Gdk.CursorType cursorType)
	{
		this.GdkWindow.Cursor = new Gdk.Cursor (cursorType);
	}

	protected virtual void OnEditActionActivated (object sender, System.EventArgs e)
	{
		toolBoxService.SetToolByName ("LineTool");
	}

	protected virtual void OnSaveActionActivated (object sender, System.EventArgs e)
	{
		Gtk.FileChooserDialog fc = new Gtk.FileChooserDialog ("Choose the Monoreports file to save", this, FileChooserAction.Save, "Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);
		var fileFilter = new FileFilter { Name = "Monoreports project" };
		fileFilter.AddPattern ("*.mrp");
		fc.AddFilter (fileFilter);
		designService.Report.DataSource = null;
		if (fc.Run () == (int)ResponseType.Accept) {
			
			using (System.IO.FileStream file = System.IO.File.OpenWrite (fc.Filename)) {
			
				var serializedProject = JsonConvert.SerializeObject (designService.Report, Formatting.None, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects });
				byte[] bytes = System.Text.Encoding.UTF8.GetBytes (serializedProject);
				file.SetLength (bytes.Length);
				file.Write (bytes, 0, bytes.Length);
			
				file.Close ();
			}
		}
		
		fc.Destroy ();
		
	}

	protected virtual void OnOpenActionActivated (object sender, System.EventArgs e)
	{
		Gtk.FileChooserDialog fc = new Gtk.FileChooserDialog ("Choose the Monoreports file to open", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
		var fileFilter = new FileFilter { Name = "Monoreports project" };
		fileFilter.AddPattern ("*.mrp");
		fc.AddFilter (fileFilter);
		
		if (fc.Run () == (int)ResponseType.Accept) {
			System.IO.FileStream file = System.IO.File.OpenRead (fc.Filename);
			byte[] bytes = new byte[file.Length];
			file.Read (bytes, 0, (int)file.Length);
			ShowInPropertyGrid (null);
			var report = JsonConvert.DeserializeObject<Report> (System.Text.Encoding.UTF8.GetString (bytes), 
				new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects,
				Converters = new List<JsonConverter> (
					new JsonConverter[] { 
					new MonoReports.Extensions.PointConverter (), 
					new MonoReports.Extensions.SizeConverter (),
					new MonoReports.Extensions.ColorConverter (),
				}) 
			});
			designService.Report = report;
			file.Close ();
		}
		
		fc.Destroy ();
		workspaceService.InvalidateDesignArea ();
	}

	 
	
	protected virtual void OnMainPropertygridChanged (object sender, System.EventArgs e)
	{
		workspaceService.InvalidateDesignArea();
	}
	
	
}

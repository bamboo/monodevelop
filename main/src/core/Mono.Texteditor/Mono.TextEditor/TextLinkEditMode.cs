// 
// TextLinkEditMode.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.Collections.Generic;
using Mono.TextEditor.PopupWindow;

namespace Mono.TextEditor
{
	public class TextLink : IListDataProvider<string>
	{
		public ISegment PrimaryLink {
			get {
				if (links.Count == 0)
					return null;
				return links[0];
			}
		}
		
		List<Segment> links = new List<Segment> ();
		public IList<Segment> Links {
			get {
				return links;
			}
		}

		public bool IsIdentifier {
			get;
			set;
		}
		
		public bool IsEditable {
			get;
			set;
		}
		
		public string Name {
			get;
			set;
		}
		
		public string CurrentText {
			get;
			set;
		}
		
		public string Tooltip {
			get;
			set;
		}
		
		public IListDataProvider<string> Values {
			get;
			set;
		}
		
		public Func<Func<string, string>, IListDataProvider<string>> GetStringFunc {
			get;
			set;
		}
		
		public TextLink (string name)
		{
			IsEditable = true;
			this.Name  = name;
			this.IsIdentifier = false;
		}
		
		public override string ToString ()
		{
			return string.Format("[TextLink: Name={0}, Links={1}, IsEditable={2}, Tooltip={3}, CurrentText={4}, Values=({5})]", 
			                     Name, 
			                     Links.Count, 
			                     IsEditable, 
			                     Tooltip, 
			                     CurrentText, 
			                     Values.Count);
		}
		
		public void AddLink (Segment segment)
		{
			links.Add (segment);
		}
		
		#region IListDataProvider implementation
		public string GetText (int n)
		{
			return Values != null ? Values.GetText (n) : "";
		}
		
		public string this [int n] {
			get {
				return Values != null ? Values[n] : "";
			}
		}
		
		public Gdk.Pixbuf GetIcon (int n)
		{
			return Values != null ? Values.GetIcon (n) : null;
		}
		
		public int Count {
			get {
				return Values != null ? Values.Count : 0;
			}
		}
		#endregion
	}
	
	public class TextLinkEditMode : SimpleEditMode
	{
		List<TextLink> links;
		int baseOffset;
		int endOffset;
		int undoDepth = -1;
		bool resetCaret = true;
		
		public EditMode OldMode {
			get;
			set;
		}
		
		public List<TextLink> Links  {
			get {
				return links;
			}
		}
		
		public int BaseOffset {
			get {
				return baseOffset;
			}
		}
		
		public bool ShouldStartTextLinkMode {
			get {
				return !(Editor.CurrentMode is TextLinkEditMode) && links.Any (l => l.IsEditable);
			}
		}
		
		public new TextEditor Editor {
			get;
			set;
		}
		
		public bool SetCaretPosition {
			get;
			set;
		}
		public bool SelectPrimaryLink {
			get;
			set;
		}
		
		TextLinkTooltipProvider tooltipProvider;
		public TextLinkEditMode (TextEditor editor, int baseOffset, List<TextLink> links)
		{
			this.Editor = editor;
			this.links = links;
			this.baseOffset = baseOffset;
			this.endOffset = editor.Caret.Offset;
			tooltipProvider = new TextLinkTooltipProvider (this);
			this.Editor.TooltipProviders.Insert (0, tooltipProvider);
			this.SetCaretPosition = true;
			this.SelectPrimaryLink = true;
		}

		TextLink closedLink = null;
		void HandlePositionChanged (object sender, DocumentLocationEventArgs e)
		{
			int caretOffset = Editor.Caret.Offset - baseOffset;
			TextLink link = links.Find (l => l.PrimaryLink != null && l.PrimaryLink.Offset <= caretOffset && caretOffset <= l.PrimaryLink.EndOffset);
			if (link != null && link.Count > 0 && link.IsEditable) {
				if (window != null && window.DataProvider != link) {
					DestroyWindow ();
				}
				if (closedLink == link)
					return;
				closedLink = null;
				if (window == null) {
					window = new ListWindow<string> ();
					window.DataProvider = link;
					
					DocumentLocation loc = Editor.Document.OffsetToLocation (BaseOffset + link.PrimaryLink.Offset);
					Editor.ShowListWindow (window, loc);
					
				} 
			} else {
				DestroyWindow ();
				closedLink = null;
			}
		}
		
		public void StartMode ()
		{
			foreach (TextLink link in links) {
				if (link.PrimaryLink != null)
					link.CurrentText = Editor.Document.GetTextAt (link.PrimaryLink.Offset + baseOffset, link.PrimaryLink.Length);
				foreach (ISegment segment in link.Links) {
					Editor.Document.EnsureOffsetIsUnfolded (baseOffset + segment.Offset);
					LineSegment line = Editor.Document.GetLineByOffset (baseOffset + segment.Offset);
					if (line.GetMarker (typeof(TextLinkMarker)) != null)
						continue;
					TextLinkMarker marker = (TextLinkMarker)line.GetMarker (typeof(TextLinkMarker));
					if (marker == null) {
						marker = new TextLinkMarker (this);
						marker.BaseOffset = baseOffset;
						line.AddMarker (marker);
					}
				}
			}
			TextLink firstLink = links.First (l => l.IsEditable);
			if (SelectPrimaryLink)
				Setlink (firstLink);
			Editor.Document.TextReplaced += UpdateLinksOnTextReplace;
			this.Editor.Caret.PositionChanged += HandlePositionChanged;
			this.UpdateTextLinks ();
			this.HandlePositionChanged (null, null);
			Editor.Document.CommitUpdateAll ();
			this.undoDepth = Editor.Document.GetCurrentUndoDepth ();
		}
		
		void Setlink (TextLink link)
		{
			if (link.PrimaryLink == null)
				return;
			Editor.Caret.Offset = baseOffset + link.PrimaryLink.Offset;
			Editor.ScrollToCaret ();
			Editor.Caret.Offset = baseOffset + link.PrimaryLink.EndOffset;
			Editor.MainSelection = new Selection (Editor.Document.OffsetToLocation (baseOffset + link.PrimaryLink.Offset),
			                                      Editor.Document.OffsetToLocation (baseOffset + link.PrimaryLink.EndOffset));
			Editor.Document.CommitUpdateAll ();
		}
		
		void ExitTextLinkMode ()
		{
			isExited = true;
			DestroyWindow ();
			foreach (TextLink link in links) {
				foreach (ISegment segment in link.Links) {
					LineSegment line = Editor.Document.GetLineByOffset (baseOffset + segment.Offset);
					line.RemoveMarker (typeof(TextLinkMarker));
				}
			}
			if (SetCaretPosition && resetCaret)
				Editor.Caret.Offset = endOffset;
			
			Editor.Document.TextReplaced -= UpdateLinksOnTextReplace;
			this.Editor.Caret.PositionChanged -= HandlePositionChanged;
			this.Editor.TooltipProviders.Remove (tooltipProvider);
			if (undoDepth >= 0)
				Editor.Document.StackUndoToDepth (undoDepth);
			Editor.CurrentMode = OldMode;
			Editor.Document.CommitUpdateAll ();
		}
		
		public bool IsInUpdate {
			get;
			set;
		}
		
		bool isExited = false;
		bool wasReplaced = false;
		
		void UpdateLinksOnTextReplace (object sender, ReplaceEventArgs e)
		{
			wasReplaced = true;
			int offset = e.Offset - baseOffset;
			int delta = -e.Count + (!string.IsNullOrEmpty (e.Value) ? e.Value.Length : 0);
			if (!IsInUpdate && !links.Where (link => link.Links.Where (segment => segment.Contains (offset) || segment.EndOffset == offset).Any ()).Any ()) {
				SetCaretPosition = false;
				ExitTextLinkMode ();
				return;
			}
			AdjustLinkOffsets (offset, delta);
			UpdateTextLinks ();
		}
		
		void AdjustLinkOffsets (int offset, int delta)
		{
			foreach (TextLink link in links) {
				foreach (Segment s in link.Links) {
					if (offset < s.Offset) {
						s.Offset += delta;
					} else if (offset < s.EndOffset) {
						s.Length += delta;
					} else if (offset == s.EndOffset && delta > 0) {
						s.Length += delta;
					}
				}
			}
			if (baseOffset + offset < endOffset)
				endOffset += delta;
		}
		
		void GotoNextLink (TextLink link)
		{
			int caretOffset = Editor.Caret.Offset - baseOffset;
			TextLink nextLink = links.Find (l => l.IsEditable && l.PrimaryLink.Offset > (link != null ? link.PrimaryLink.EndOffset : caretOffset));
			if (nextLink == null)
				nextLink = links.Find (l => l.IsEditable);
			closedLink = null;
			Setlink (nextLink);
		}
		
		void GotoPreviousLink (TextLink link)
		{
			int caretOffset = Editor.Caret.Offset - baseOffset;
			var prevLink = links.FindLast (l => l.IsEditable && l.PrimaryLink.Offset < (link != null ? link.PrimaryLink.Offset : caretOffset));
			if (prevLink == null)
				prevLink = links.FindLast (l => l.IsEditable);
			Setlink (prevLink);
		}
		
		void CompleteWindow ()
		{
			if (window == null)
				return;
			TextLink lnk = (TextLink)window.DataProvider;
			//int line = Editor.Caret.Line;
			lnk.CurrentText = (string)window.CurrentItem;
			UpdateLinkText (lnk);
			UpdateTextLinks ();
			Editor.Document.CommitUpdateAll ();
		}
		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			if (window != null) {
				ListWindowKeyAction action = window.ProcessKey (key, modifier);
				if ((action & ListWindowKeyAction.Complete) == ListWindowKeyAction.Complete)
					CompleteWindow ();
				if ((action & ListWindowKeyAction.CloseWindow) == ListWindowKeyAction.CloseWindow) {
					closedLink = (TextLink)window.DataProvider;
					DestroyWindow ();
				}
				if ((action & ListWindowKeyAction.Complete) == ListWindowKeyAction.Complete)
					GotoNextLink (closedLink);

				if ((action & ListWindowKeyAction.Ignore) == ListWindowKeyAction.Ignore)
					return;
			}
			int caretOffset = Editor.Caret.Offset - baseOffset;
			TextLink link = links.Find (l => l.Links.Any (s => s.Offset <= caretOffset && caretOffset <= s.EndOffset));
			
			switch (key) {
			case Gdk.Key.BackSpace:
				if (link != null && caretOffset == link.PrimaryLink.Offset)
					return;
				goto default;
			case Gdk.Key.space:
				if (link == null || !link.IsIdentifier)
					goto default;
				return;
			case Gdk.Key.Delete:
				if (link != null && caretOffset == link.PrimaryLink.EndOffset)
					return;
				goto default;
			case Gdk.Key.Tab:
				if ((modifier & Gdk.ModifierType.ControlMask) != 0)
					if (link != null && !link.IsIdentifier)
						goto default;
				if ((modifier & Gdk.ModifierType.ShiftMask) == 0)
					GotoNextLink (link);
				else
					GotoPreviousLink (link);
				return;
			case Gdk.Key.Escape:
			case Gdk.Key.Return:
				if ((modifier & Gdk.ModifierType.ControlMask) != 0)
					if (link != null && !link.IsIdentifier)
						goto default;
				if (window != null) {
					CompleteWindow ();
				} else {
					ExitTextLinkMode ();
				}
				return;
			default:
				wasReplaced = false;
				base.HandleKeypress (key, unicodeKey, modifier);
				if (wasReplaced && link == null) {
					resetCaret = false;
					ExitTextLinkMode ();
				}
				break;
			}
/*			if (link != null)
				UpdateTextLink (link);
			UpdateTextLinks ();
			Editor.Document.CommitUpdateAll ();*/
		}
		
		ListWindow<string> window;
		void DestroyWindow ()
		{
			if (window != null) {
				window.Destroy ();
				window = null;
			}
		}
		
		public string GetStringCallback (string linkName)
		{
			foreach (TextLink link in links) {
				if (link.Name == linkName)
					return link.CurrentText;
			}
			return null;
		}
		
		public void UpdateTextLinks ()
		{
			if (isExited)
				return;
			foreach (TextLink l in links) {
				UpdateTextLink (l);
			}
		}

		void UpdateTextLink (TextLink link)
		{
			if (link.GetStringFunc != null) {
				link.Values = link.GetStringFunc (GetStringCallback);
			}
			if (!link.IsEditable && link.Values.Count > 0) {
				link.CurrentText = (string)link.Values[link.Values.Count - 1];
			} else {
				if (link.PrimaryLink != null) {
					int offset = link.PrimaryLink.Offset + baseOffset;
					if (offset >= 0 && link.PrimaryLink.Length >= 0)
						link.CurrentText = Editor.Document.GetTextAt (offset, link.PrimaryLink.Length);
				}
			}
			UpdateLinkText (link);
		}

		public void UpdateLinkText (TextLink link)
		{
			Editor.Document.TextReplaced -= UpdateLinksOnTextReplace;
			for (int i = link.Links.Count - 1; i >= 0; i--) {
				Segment s = link.Links[i];
				int offset = s.Offset + baseOffset;
				if (offset < 0 || s.Length < 0 || offset + s.Length > Editor.Document.Length)
					continue;
				if (Editor.Document.GetTextAt (offset, s.Length) != link.CurrentText) {
					Editor.Replace (offset, s.Length, link.CurrentText);
					int delta = link.CurrentText.Length - s.Length;
					AdjustLinkOffsets (s.Offset, delta);
					Editor.Document.CommitLineUpdate (Editor.Document.OffsetToLineNumber (offset));
				}
			}
			Editor.Document.TextReplaced += UpdateLinksOnTextReplace;
		}
	}
	
	public class TextLinkTooltipProvider : ITooltipProvider
	{
		TextLinkEditMode mode;
		
		public TextLinkTooltipProvider (TextLinkEditMode mode)
		{
			this.mode = mode;
		}

		#region ITooltipProvider implementation 
		public TooltipItem GetItem (TextEditor Editor, int offset)
		{
			int o = offset - mode.BaseOffset;
			for (int i = 0; i < mode.Links.Count; i++) {
				TextLink l = mode.Links[i];
				if (l.PrimaryLink != null && l.PrimaryLink.Offset <= o && o <= l.PrimaryLink.EndOffset)
					return new TooltipItem (l, l.PrimaryLink.Offset, l.PrimaryLink.Length);
			}
			return null;
			//return mode.Links.First (l => l.PrimaryLink != null && l.PrimaryLink.Offset <= o && o <= l.PrimaryLink.EndOffset);
		}
		
		public Gtk.Window CreateTooltipWindow (TextEditor Editor, int offset, Gdk.ModifierType modifierState, TooltipItem item)
		{
			TextLink link = item.Item as TextLink;
			if (link == null || string.IsNullOrEmpty (link.Tooltip))
				return null;
			
			TooltipWindow window = new TooltipWindow ();
			window.Markup = link.Tooltip;
			return window;
		}
		
		public void GetRequiredPosition (TextEditor Editor, Gtk.Window tipWindow, out int requiredWidth, out double xalign)
		{
			TooltipWindow win = (TooltipWindow) tipWindow;
			requiredWidth = win.SetMaxWidth (win.Screen.Width);
			xalign = 0.5;
		}
		
		public bool IsInteractive (TextEditor Editor, Gtk.Window tipWindow)
		{
			return false;
		}
		#endregion 
	}
	
	public class TextLinkMarker : TextMarker, IBackgroundMarker
	{
		TextLinkEditMode mode;
		
		public int BaseOffset {
			get;
			set;
		}
		
		public TextLinkMarker (TextLinkEditMode mode)
		{
			this.mode = mode;
		}
		/*
		void InternalDrawBackground (TextEditor Editor, Gdk.Drawable win, Pango.Layout layout, bool selected, int startOffset, int endOffset, int y, ref int startXPos, int endXPos, ref bool drawBg)
		{
			Gdk.Rectangle clipRectangle = new Gdk.Rectangle (mode.Editor.TextViewMargin.XOffset, 0, 
			                                                 Editor.Allocation.Width - mode.Editor.TextViewMargin.XOffset, Editor.Allocation.Height);
			
			// draw default background
			using (Gdk.GC fillGc = new Gdk.GC (win)) {
				fillGc.ClipRectangle = clipRectangle;
				fillGc.RgbFgColor = selected ? Editor.ColorStyle.Selection.BackgroundColor : Editor.ColorStyle.Default.BackgroundColor;
				win.DrawRectangle (fillGc, true, startXPos, y, endXPos, Editor.LineHeight);
			}
			
			if (startOffset >= endOffset)
				return;
			
			int caretOffset = Editor.Caret.Offset - BaseOffset;
			foreach (TextLink link in mode.Links) {
				if (!link.IsEditable)
					continue;
				bool isPrimaryHighlighted = link.PrimaryLink.Offset <= caretOffset && caretOffset <= link.PrimaryLink.EndOffset;
				
				foreach (ISegment segment in link.Links) {
					
					if ((BaseOffset + segment.Offset <= startOffset && startOffset < BaseOffset + segment.EndOffset) ||
					    (startOffset <= BaseOffset + segment.Offset && BaseOffset + segment.Offset < endOffset)) {
						int strOffset    = BaseOffset + segment.Offset - startOffset;
						int strEndOffset = BaseOffset + segment.EndOffset - startOffset;
						
						int lineNr, x_pos, x_pos2;
						layout.IndexToLineX (strOffset, false, out lineNr, out x_pos);
						layout.IndexToLineX (strEndOffset, false, out lineNr, out x_pos2);
						x_pos  = (int)(x_pos / Pango.Scale.PangoScale);
						x_pos2 = (int)(x_pos2 / Pango.Scale.PangoScale);
						using (Gdk.GC rectangleGc = new Gdk.GC (win)) {
							rectangleGc.ClipRectangle = clipRectangle;
							using (Gdk.GC fillGc = new Gdk.GC (win)) {
								fillGc.ClipRectangle = clipRectangle;
								drawBg = false;
								
								if (segment == link.PrimaryLink) {
									fillGc.RgbFgColor      = isPrimaryHighlighted ? Editor.ColorStyle.PrimaryTemplateHighlighted.BackgroundColor : Editor.ColorStyle.PrimaryTemplate.BackgroundColor;
									rectangleGc.RgbFgColor = isPrimaryHighlighted ? Editor.ColorStyle.PrimaryTemplateHighlighted.Color           : Editor.ColorStyle.PrimaryTemplate.Color;
								} else {
									fillGc.RgbFgColor      = isPrimaryHighlighted ? Editor.ColorStyle.SecondaryTemplateHighlighted.BackgroundColor : Editor.ColorStyle.SecondaryTemplate.BackgroundColor;
									rectangleGc.RgbFgColor = isPrimaryHighlighted ? Editor.ColorStyle.SecondaryTemplateHighlighted.Color           : Editor.ColorStyle.SecondaryTemplate.Color;
								}
								// Draw segment
								if (!selected)
									win.DrawRectangle (fillGc, true, startXPos, y, x_pos2 - x_pos, Editor.LineHeight);
								
								int x1 = startXPos + x_pos - 1;
								int x2 = startXPos + x_pos2 - 1;
								int y2 = y + Editor.LineHeight - 1;
								
								win.DrawLine (rectangleGc, x1, y, x2, y);
								win.DrawLine (rectangleGc, x1, y2, x2, y2);
								win.DrawLine (rectangleGc, x1, y, x1, y2);
								win.DrawLine (rectangleGc, x2, y, x2, y2);
							}
						}
					}
				}
			}
			startXPos += Editor.GetWidth (Editor.Document.GetTextBetween (startOffset, endOffset));
		}
		*/
	/*	bool Overlaps (ISegment segment, int start, int end)
		{
			return segment.Offset <= start && start < segment.EndOffset || 
				    segment.Offset <= end && end < segment.EndOffset ||
					start <= segment.Offset && segment.Offset < end ||
					start < segment.EndOffset && segment.EndOffset < end;
		}*/
		
	public bool DrawBackground (TextEditor Editor, Gdk.Drawable win, TextViewMargin.LayoutWrapper layout, int selectionStart, int selectionEnd, int startOffset, int endOffset, int y, int startXPos, int endXPos, ref bool drawBg)
	{
		int caretOffset = Editor.Caret.Offset - BaseOffset;

		foreach (TextLink link in mode.Links) {
			if (!link.IsEditable) 
				continue; 
			bool isPrimaryHighlighted = link.PrimaryLink.Offset <= caretOffset && caretOffset <= link.PrimaryLink.EndOffset;

			foreach (ISegment segment in link.Links) {

				if ((BaseOffset + segment.Offset <= startOffset && startOffset < BaseOffset + segment.EndOffset) || (startOffset <= BaseOffset + segment.Offset && BaseOffset + segment.Offset < endOffset)) {
					int strOffset = BaseOffset + segment.Offset - startOffset;
					int strEndOffset = BaseOffset + segment.EndOffset - startOffset;

					int x_pos = layout.Layout.IndexToPos (strOffset).X;
					int x_pos2 = layout.Layout.IndexToPos (strEndOffset).X;
					
					x_pos = (int)(x_pos / Pango.Scale.PangoScale);
					x_pos2 = (int)(x_pos2 / Pango.Scale.PangoScale);
					using (Gdk.GC rectangleGc = new Gdk.GC(win)) {
						//	rectangleGc.ClipRectangle = clipRectangle;
						using (Gdk.GC fillGc = new Gdk.GC(win)) {
							//		fillGc.ClipRectangle = clipRectangle;
							drawBg = false;

							if (segment == link.PrimaryLink) {
								fillGc.RgbFgColor = isPrimaryHighlighted ? Editor.ColorStyle.PrimaryTemplateHighlighted.BackgroundColor : Editor.ColorStyle.PrimaryTemplate.BackgroundColor;
								rectangleGc.RgbFgColor = isPrimaryHighlighted ? Editor.ColorStyle.PrimaryTemplateHighlighted.Color : Editor.ColorStyle.PrimaryTemplate.Color;
							} else {
								fillGc.RgbFgColor = isPrimaryHighlighted ? Editor.ColorStyle.SecondaryTemplateHighlighted.BackgroundColor : Editor.ColorStyle.SecondaryTemplate.BackgroundColor;
								rectangleGc.RgbFgColor = isPrimaryHighlighted ? Editor.ColorStyle.SecondaryTemplateHighlighted.Color : Editor.ColorStyle.SecondaryTemplate.Color;
							}
							// Draw segment

							int x1 = startXPos + x_pos - 1;
							int x2 = startXPos + x_pos2 - 1;
							int y2 = y + Editor.LineHeight - 1;

							if (selectionStart < 0) {
						//		Console.WriteLine ("Draw BG at " + y + "//" + Editor.GetTextEditorData ().VAdjustment.Value);
						//		Console.WriteLine (Environment.StackTrace);
								win.DrawRectangle (fillGc, true, x1, y, x2 - x1, Editor.LineHeight); 
							}

							win.DrawLine (rectangleGc, x1, y, x2, y);
							win.DrawLine (rectangleGc, x1, y2, x2, y2);
							win.DrawLine (rectangleGc, x1, y, x1, y2);
							win.DrawLine (rectangleGc, x2, y, x2, y2);
						}
					}
				}
			}
		}
		/*
			int curOffset = startOffset;
			foreach (TextLink link in mode.Links) {
				if (!link.IsEditable)
					continue;
				ISegment segment = link.Links.Where (s => Overlaps (s, curOffset - BaseOffset, endOffset - BaseOffset)).FirstOrDefault ();
				if (segment == null) {
					break;
				}
				InternalDrawBackground (Editor, win, layout, selected, curOffset, segment.Offset + BaseOffset, y, ref startXPos, endXPos, ref drawBg);
				curOffset = segment.EndOffset + BaseOffset;
				InternalDrawBackground (Editor, win, layout, selected, segment.Offset + BaseOffset, curOffset, y, ref startXPos, endXPos, ref drawBg);
			}
			InternalDrawBackground (Editor, win, layout, selected, curOffset, endOffset, y, ref startXPos, endXPos, ref drawBg);
			*/
		return true;
	}
	}
}

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using SqlPad.Commands;

namespace SqlPad
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private readonly ColorizeAvalonEdit _colorizeAvalonEdit = new ColorizeAvalonEdit();
		private readonly SqlDocument _sqlDocument = new SqlDocument();
		private readonly ISqlParser _sqlParser;
		private readonly IInfrastructureFactory _infrastructureFactory;
		private readonly ICodeCompletionProvider _codeCompletionProvider;
		private readonly ICodeSnippetProvider _codeSnippetProvider;
		private readonly IContextActionProvider _contextActionProvider;
		private readonly IStatementFormatter _statementFormatter;
		private readonly IDatabaseModel _databaseModel;
		private readonly IToolTipProvider _toolTipProvider;
		private readonly INavigationService _navigationService;
		
		private readonly ToolTip _toolTip = new ToolTip();
		private bool _isToolTipOpenByShortCut;

		public MainWindow()
		{
			InitializeComponent();

			_infrastructureFactory = ConfigurationProvider.InfrastructureFactory;
			_sqlParser = _infrastructureFactory.CreateSqlParser();
			_codeCompletionProvider = _infrastructureFactory.CreateCodeCompletionProvider();
			_codeSnippetProvider = _infrastructureFactory.CreateSnippetProvider();
			_contextActionProvider = _infrastructureFactory.CreateContextActionProvider();
			_statementFormatter = _infrastructureFactory.CreateSqlFormatter(new SqlFormatterOptions());
			_toolTipProvider = _infrastructureFactory.CreateToolTipProvider();
			_navigationService = _infrastructureFactory.CreateNavigationService();
			_databaseModel = _infrastructureFactory.CreateDatabaseModel(ConfigurationProvider.ConnectionStrings["Default"]);
			
			_timer.Elapsed += TimerOnElapsed;

			SetDeleteLineCommand();
		}

		private void SetDeleteLineCommand()
		{
			var deleteLineCommand = (RoutedCommand)Editor.TextArea.DefaultInputHandler.Editing.CommandBindings
				.Single(b => b.Command == AvalonEditCommands.DeleteLine)
				.Command;

			deleteLineCommand.InputGestures[0] = new KeyGesture(Key.L, ModifierKeys.Control);
		}

		private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
		{
			if (_isParsing)
				return;
			
			_timer.Stop();

			Editor.Dispatcher.Invoke(() => Task.Factory.StartNew(DoWork, Editor.Text));
		}

		private void WindowLoadedHandler(object sender, RoutedEventArgs e)
		{
			SqlPad.Resources.Initialize(Resources);

			Editor.TextArea.TextView.LineTransformers.Add(_colorizeAvalonEdit);

			Editor.TextArea.TextEntering += TextEnteringHandler;
			Editor.TextArea.TextEntered += TextEnteredHandler;
			
			Editor.TextArea.Caret.PositionChanged += CaretOnPositionChanged;

			Editor.Focus();
		}

		private void CaretOnPositionChanged(object sender, EventArgs eventArgs)
		{
			var parenthesisNodes = new List<StatementDescriptionNode>();

			if (!_isParsing)
			{
				var parenthesisTerminal = _sqlDocument.StatementCollection == null
					? null
					: _sqlDocument.ExecuteStatementAction(s => s.GetTerminalAtPosition(Editor.CaretOffset, n => n.Token.Value.In("(", ")")));

				if (parenthesisTerminal != null)
				{
					var childNodes = parenthesisTerminal.ParentNode.ChildNodes.ToList();
					var index = childNodes.IndexOf(parenthesisTerminal);
					var increment = parenthesisTerminal.Token.Value == "(" ? 1 : -1;
					var otherParenthesis = parenthesisTerminal.Token.Value == "(" ? ")" : "(";

					while (0 <= index && index < childNodes.Count)
					{
						index += increment;

						if (index < 0 || index >= childNodes.Count)
							break;

						var otherParenthesisTerminal = childNodes[index];
						if (otherParenthesisTerminal.Token != null && otherParenthesisTerminal.Token.Value == otherParenthesis)
						{
							parenthesisNodes.Add(parenthesisTerminal);
							parenthesisNodes.Add(otherParenthesisTerminal);
							break;
						}
					}
				}
			}

			var oldNodes = _colorizeAvalonEdit.HighlightParenthesis.ToArray();
			_colorizeAvalonEdit.SetHighlightParenthesis(parenthesisNodes);

			RedrawNodes(oldNodes.Concat(parenthesisNodes));
		}

		private void RedrawNodes(IEnumerable<StatementDescriptionNode> nodes)
		{
			foreach (var node in nodes)
			{
				Editor.TextArea.TextView.Redraw(node.SourcePosition.IndexStart, node.SourcePosition.Length);
			}
		}

		private bool _isParsing;
		private readonly System.Timers.Timer _timer = new System.Timers.Timer(100);

		private void EditorTextChangedHandler(object sender, EventArgs e)
		{
			if (_isParsing)
			{
				if (!_timer.Enabled)
				{
					_timer.Start();
				}

				return;
			}

			_isParsing = true;

			Task.Factory.StartNew(DoWork, Editor.Text);
		}

		private void DoWork(object text)
		{
			var statements = _sqlParser.Parse((string)text);
			_sqlDocument.UpdateStatements(statements);
			_colorizeAvalonEdit.SetStatementCollection(statements);

			Dispatcher.Invoke(() =>
			                  {
				                  TextBlockToken.Text = String.Join(", ", statements.SelectMany(s => s.AllTerminals).Select(t => "{" + t.Token.Value + "}"));
								  Editor.TextArea.TextView.Redraw();
								  _isParsing = false;
			                  });
		}

		private CompletionWindow _completionWindow;

		void TextEnteredHandler(object sender, TextCompositionEventArgs e)
		{
			if (Editor.Document.IsInUpdate)
			{
				Editor.Document.EndUpdate();
			}

			var snippets = _codeSnippetProvider.GetSnippets(_sqlDocument, Editor.Text, Editor.CaretOffset).Select(i => new CompletionData(i)).ToArray();
			if (_completionWindow == null && snippets.Length > 0)
			{
				CreateSnippetCompletionWindow(snippets);
				_completionWindow.Closed += (o, args) =>
					// Workaround to display completion menu after the snippet is inserted
					Task.Factory.StartNew(() =>
					                      {
						                      Thread.Sleep(20);
						                      Dispatcher.Invoke(CreateCodeCompletionWindow);
					                      });
				return;
			}

			if (e.Text == "(" &&
				(Editor.Text.Length == Editor.CaretOffset || Editor.Text[Editor.CaretOffset].In(' ', '\t', '\n')))
			{
				Editor.Document.Insert(Editor.CaretOffset, ")");
				Editor.CaretOffset--;
			}

			if (e.Text != "." && e.Text != " " && e.Text != "\n")
			{
				if (_completionWindow != null && _completionWindow.CompletionList.ListBox.Items.Count == 0)
					_completionWindow.Close();

				return;
			}

			// Open code completion after the user has pressed dot:
			CreateCodeCompletionWindow();
		}

		void TextEnteringHandler(object sender, TextCompositionEventArgs e)
		{
			if ((Keyboard.IsKeyDown(Key.Oem2) || Keyboard.IsKeyDown(Key.D)) && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt))
			{
				e.Handled = true;
			}

			if (e.Text == ")" && Editor.Text.Length > Editor.CaretOffset && Editor.CaretOffset >= 1 && Editor.Text[Editor.CaretOffset] == ')' && Editor.Text[Editor.CaretOffset - 1] == '(')
			{
				Editor.CaretOffset++;
				e.Handled = true;
				return;
			}

			if (_multiNodeEditor != null)
			{
				Editor.Document.BeginUpdate();

				if (!_multiNodeEditor.Replace(e.Text))
					_multiNodeEditor = null;
			}

			if (e.Text.Length == 1 && _completionWindow != null)
			{
				if (!Char.IsLetterOrDigit(e.Text[0]))
				{
					// Whenever a non-letter is typed while the completion window is open,
					// insert the currently selected element.
					_completionWindow.CompletionList.RequestInsertion(e);
				}
			}
			// Do not set e.Handled=true.
			// We still want to insert the character that was typed.

			if (e.Text == " " && Keyboard.Modifiers == ModifierKeys.Control)
			{
				e.Handled = true;
				CreateCodeCompletionWindow();
			}
		}

		private void CreateCodeCompletionWindow()
		{
			CreateCompletionWindow(() => _codeCompletionProvider.ResolveItems(_sqlDocument, _databaseModel, Editor.Text, Editor.CaretOffset).Select(i => new CompletionData(i)), true);
		}

		private void CreateSnippetCompletionWindow(IEnumerable<ICompletionData> items)
		{
			CreateCompletionWindow(() => items, true);
		}

		private void CreateCompletionWindow(Func<IEnumerable<ICompletionData>> getCompletionDataFunc, bool show)
		{
			_completionWindow = new CompletionWindow(Editor.TextArea) { SizeToContent = SizeToContent.WidthAndHeight };
			var data = _completionWindow.CompletionList.CompletionData;

			foreach (var item in getCompletionDataFunc())
			{
				data.Add(item);
			}

			_completionWindow.Closed += delegate { _completionWindow = null; };

			if (show && data.Count > 0)
			{
				if (data.Count == 1)
				{
					_completionWindow.CompletionList.ListBox.SelectedIndex = 0;
				}

				_completionWindow.Show();
			}
		}

		void MouseHoverHandler(object sender, MouseEventArgs e)
		{
			if (_isToolTipOpenByShortCut)
				return;

			var position = Editor.GetPositionFromPoint(e.GetPosition(Editor));
			if (!position.HasValue || _sqlDocument.StatementCollection == null)
				return;

			var offset = Editor.Document.GetOffset(position.Value.Line, position.Value.Column);
			//var lineByOffset = Editor.Document.GetLineByOffset(offset);

			var toolTip = _toolTipProvider.GetToolTip(_databaseModel, _sqlDocument, offset);
			if (toolTip == null)
				return;

			_toolTip.Placement = PlacementMode.Mouse;
			_toolTip.PlacementTarget = this; // required for property inheritance
			_toolTip.Content = toolTip;
			_toolTip.IsOpen = true;
			e.Handled = true;
		}

		void MouseHoverStoppedHandler(object sender, MouseEventArgs e)
		{
			if (!_isToolTipOpenByShortCut)
				_toolTip.IsOpen = false;
		}

		private void ContextMenuOpeningHandler(object sender, ContextMenuEventArgs args)
		{
			if (!PopulateContextActionMenu())
				args.Handled = true;
		}

		private bool PopulateContextActionMenu()
		{
			var menuItems = _contextActionProvider.GetContextActions(_databaseModel, _sqlDocument, Editor.SelectionStart, Editor.SelectionLength)
				.Select(a => new MenuItem { Header = a.Name, Command = a.Command, CommandParameter = Editor });

			Editor.ContextMenu.Items.Clear();

			foreach (var menuItem in menuItems)
			{
				Editor.ContextMenu.Items.Add(menuItem);
			}

			if (Editor.ContextMenu.Items.Count == 1)
			{
				Editor.ContextMenu.Opened += (sender, args) => ((MenuItem)Editor.ContextMenu.Items[0]).Focus();
			}

			Editor.ContextMenu.PlacementTarget = Editor;
			var position = Editor.TextArea.Caret.CalculateCaretRectangle().TopRight;
			Editor.ContextMenu.HorizontalOffset = position.X - 24;
			Editor.ContextMenu.VerticalOffset = position.Y - 32;

			return Editor.ContextMenu.Items.Count > 0;
		}

		private void EditorKeyDownHandler(object sender, KeyEventArgs e)
		{
			_isToolTipOpenByShortCut = false;

			//Console.WriteLine(e.Key);

			if (_toolTip != null)
			{
				_toolTip.IsOpen = false;
			}

			if (e.SystemKey == Key.Return && Keyboard.Modifiers == ModifierKeys.Alt)
			{
				Trace.WriteLine("ALT + ENTER");
				Editor.ContextMenu.IsOpen = PopulateContextActionMenu();
			}
			else if (e.Key == Key.Return || e.Key == Key.Escape)
			{
				Trace.WriteLine(e.Key);
				_multiNodeEditor = null;

				if (e.Key == Key.Escape)
				{
					_colorizeAvalonEdit.SetHighlightSegments(null);
					Editor.TextArea.TextView.Redraw();
				}
			}
			else if (_multiNodeEditor == null && e.Key == Key.F6 && Keyboard.Modifiers == ModifierKeys.Shift)
			{
				Trace.WriteLine("SHIFT + F6");

				MultiNodeEditor.TryCreateMultiNodeEditor(Editor, _infrastructureFactory.CreateMultiNodeEditorDataProvider(), _databaseModel, out _multiNodeEditor);
			}
			else if (e.SystemKey == Key.F11 && Keyboard.Modifiers == (ModifierKeys.Alt | ModifierKeys.Shift))
			{
				Trace.WriteLine("ALT SHIFT + F11");

				FindUsages();
			}
			else if (e.Key == Key.F && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt))
			{
				Trace.WriteLine("CONTROL ALT + F");
				var textSegments = _statementFormatter.FormatStatement(_sqlDocument.StatementCollection, Editor.SelectionStart, Editor.SelectionLength);
				Editor.ReplaceTextSegments(textSegments);
			}
			else if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
			{
				Trace.WriteLine("CONTROL + D");

				int caretOffset;
				if (Editor.SelectionLength > 0)
				{
					caretOffset = Editor.SelectionStart + Editor.SelectionLength + Editor.SelectedText.Length;
					Editor.Document.Insert(Editor.SelectionStart + Editor.SelectionLength, Editor.SelectedText);
				}
				else
				{
					var currentLine = Editor.Document.GetLineByOffset(Editor.CaretOffset);
					var currentLineText = Editor.Document.GetText(currentLine) + "\n";
					Editor.Document.Insert(currentLine.EndOffset + 1, currentLineText);
					caretOffset = Editor.SelectionStart + Editor.SelectionLength + currentLineText.Length;
				}

				Editor.SelectionLength = 0;
				Editor.CaretOffset = caretOffset;
			}
			else if (e.Key == Key.Home && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt))
			{
				Trace.WriteLine("CONTROL ALT + HOME");

				var queryBlockRootIndex = _navigationService.NavigateToQueryBlockRoot(_sqlDocument.StatementCollection, Editor.CaretOffset);
				if (queryBlockRootIndex.HasValue)
				{
					Editor.CaretOffset = queryBlockRootIndex.Value;
				}
			}
			else if (e.Key == Key.Space && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
			{
				Trace.WriteLine("CONTROL SHIFT + SPACE");

				ShowFunctionOverloads();
			}
			else if (e.Key == Key.U && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
			{
				Trace.WriteLine("CONTROL SHIFT + U");

				// TODO: Add to upper implementation
			}
			else if (e.Key.In(Key.Left, Key.Right) && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift))
			{
				Trace.WriteLine("CONTROL ALT SHIFT + " + (e.Key == Key.Left ? "Left" : "Right"));
				// TODO: move element to right/left/up/down
			}
			else if (e.Key == Key.Oem2 && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
			{
				Trace.WriteLine("CONTROL SHIFT + /");

				HandleBlockComments();
			}
			else if (e.Key == Key.Oem2 && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt))
			{
				Trace.WriteLine("CONTROL ALT + /");

				HandleLineComments();
			}
			else if (e.SystemKey == Key.Delete && Keyboard.Modifiers == ModifierKeys.Alt)
			{
				Trace.WriteLine("ALT + DELETE");
				var safeDeleteCommand = _infrastructureFactory.CommandFactory.CreateSafeDeleteCommand(_sqlDocument.StatementCollection, Editor.CaretOffset, _databaseModel);
				if (safeDeleteCommand.CanExecute(null))
				{
					safeDeleteCommand.Execute(Editor);
				}
			}

			if ((e.Key == Key.Back || e.Key == Key.Delete) && _multiNodeEditor != null)
			{
				Editor.Document.BeginUpdate();
				if (!_multiNodeEditor.RemoveCharacter(e.Key == Key.Back))
					_multiNodeEditor = null;
			}
			else if (e.Key == Key.Back && Editor.Text.Length > Editor.CaretOffset)
			{
				if (Editor.Text[Editor.CaretOffset] == ')' && Editor.Text[Editor.CaretOffset - 1] == '(')
				{
					Editor.Document.Remove(Editor.CaretOffset, 1);
				}
			}
		}

		private void FindUsages()
		{
			var findUsagesCommand = _infrastructureFactory.CommandFactory.CreateFindUsagesCommand(Editor.Text, Editor.CaretOffset, _databaseModel);
			if (!findUsagesCommand.CanExecute(null))
				return;
			
			var highlightSegments = new List<TextSegment>();
			findUsagesCommand.Execute(highlightSegments);
			_colorizeAvalonEdit.SetHighlightSegments(highlightSegments);
			Editor.TextArea.TextView.Redraw();
		}

		private void ShowFunctionOverloads()
		{
			var functionOverloads = _codeCompletionProvider.ResolveFunctionOverloads(_sqlDocument.StatementCollection, _databaseModel, Editor.CaretOffset);
			if (functionOverloads.Count <= 0)
				return;
			
			_toolTip.Content = new FunctionOverloadList { FunctionOverloads = functionOverloads };
			_isToolTipOpenByShortCut = true;

			var rectangle = Editor.TextArea.Caret.CalculateCaretRectangle();
			_toolTip.PlacementTarget = this;
			_toolTip.Placement = PlacementMode.Relative;
			_toolTip.HorizontalOffset = rectangle.Left;
			_toolTip.VerticalOffset = rectangle.Top + Editor.TextArea.TextView.DefaultLineHeight;

			_toolTip.IsOpen = true;
		}

		private void HandleBlockComments()
		{
			Editor.BeginChange();

			int caretOffset;
			if (Editor.TryRemoveBlockComment())
			{
				caretOffset = Editor.CaretOffset;
			}
			else
			{
				Editor.Document.Insert(Editor.SelectionStart, "/*");
				caretOffset = Editor.CaretOffset;
				Editor.Document.Insert(Editor.SelectionStart + Editor.SelectionLength, "*/");
			}

			Editor.SelectionLength = 0;
			Editor.CaretOffset = caretOffset;
			Editor.EndChange();
		}

		private void HandleLineComments()
		{
			var startLine = Editor.Document.GetLineByOffset(Editor.SelectionStart);
			var endLine = Editor.Document.GetLineByOffset(Editor.SelectionStart + Editor.SelectionLength);

			var lines = Enumerable.Range(startLine.LineNumber, endLine.LineNumber - startLine.LineNumber + 1)
				.Select(l => Editor.Document.GetLineByNumber(l)).ToArray();

			var allLinesCommented = lines
				.All(l => Editor.Text.Substring(startLine.Offset, startLine.Length).TrimStart().StartsWith("//"));

			Editor.BeginChange();

			foreach (var line in lines)
			{
				if (allLinesCommented)
				{
					Editor.Document.Remove(line.Offset + Editor.Text.Substring(line.Offset, line.Length).IndexOf("//", StringComparison.InvariantCulture), 2);
				}
				else
				{
					Editor.Document.Insert(line.Offset, "//");
				}
			}

			var caretOffset = Editor.CaretOffset;
			Editor.SelectionLength = 0;
			Editor.CaretOffset = caretOffset;

			Editor.EndChange();
		}

		private void EditorKeyUpHandler(object sender, KeyEventArgs e)
		{
			if (Editor.Document.IsInUpdate)
			{
				Editor.Document.EndUpdate();
			}
		}

		private MultiNodeEditor _multiNodeEditor;
	}
}

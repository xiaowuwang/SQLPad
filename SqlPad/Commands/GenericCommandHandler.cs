﻿using System;
using System.Linq;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;

namespace SqlPad.Commands
{
	internal class GenericCommandHandler
	{
		private const string LineCommentPrefix = "--";

		private static SqlTextEditor GetEditorFromSender(object sender)
		{
			return (SqlTextEditor)((TextArea)sender).TextView.Services.GetService(typeof(TextEditor));
		}

		public static ExecutedRoutedEventHandler CreateRoutedEditCommandHandler(CommandExecutionHandler handler, Func<SqlDocumentRepository> getDocumentRepositoryFunction)
		{
			return
				(sender, args) =>
				{
					var editor = GetEditorFromSender(sender);
					ExecuteEditCommand(getDocumentRepositoryFunction(), editor, handler.ExecutionHandler);
				};
		}

		public static void ExecuteEditCommand(SqlDocumentRepository documentRepository, SqlTextEditor editor, Action<ActionExecutionContext> executionHandler)
		{
			if (editor.IsReadOnly || !String.Equals(documentRepository.StatementText, editor.Text))
			{
				return;
			}

			var executionContext = ActionExecutionContext.Create(editor, documentRepository);

			try
			{
				executionHandler(executionContext);
				UpdateDocument(editor, executionContext);
			}
			catch (Exception exception)
			{
				App.LogErrorAndShowMessage(exception);
			}
		}

		public static void UpdateDocument(SqlTextEditor editor, ActionExecutionContext executionContext)
		{
			var caretOffset = editor.CaretOffset;
			var selectionLength = editor.SelectionLength;
			editor.ReplaceTextSegments(executionContext.SegmentsToReplace);

			var caretOffsetChanged = executionContext.CaretOffset != caretOffset;

			if (executionContext.SelectionLength != selectionLength)
			{
				editor.SelectionStart = caretOffsetChanged ? executionContext.CaretOffset : editor.CaretOffset;
				editor.SelectionLength = executionContext.SelectionLength;
			}

			if (caretOffsetChanged)
			{
				editor.CaretOffset = executionContext.CaretOffset;
				editor.ScrollToCaret();
			}
		}

		public static void DuplicateText(object sender, ExecutedRoutedEventArgs args)
		{
			var editor = GetEditorFromSender(sender);

			int caretOffset;
			if (editor.SelectionLength > 0)
			{
				caretOffset = editor.SelectionStart + editor.SelectionLength + editor.SelectedText.Length;
				editor.Document.Insert(editor.SelectionStart + editor.SelectionLength, editor.SelectedText);
			}
			else
			{
				var currentLine = editor.Document.GetLineByOffset(editor.CaretOffset);
				var currentLineText = "\n" + editor.Document.GetText(currentLine);
				caretOffset = editor.SelectionStart + currentLineText.Length;
				editor.Document.Insert(currentLine.EndOffset, currentLineText);
			}

			editor.SelectionLength = 0;
			editor.CaretOffset = caretOffset;
		}

		public static void HandleBlockComments(object sender, ExecutedRoutedEventArgs args)
		{
			var editor = GetEditorFromSender(sender);

			editor.BeginChange();

			int caretOffset;
			if (editor.TryRemoveBlockComment())
			{
				caretOffset = editor.CaretOffset;
			}
			else
			{
				editor.Document.Insert(editor.SelectionStart, "/*");
				caretOffset = editor.CaretOffset;
				editor.Document.Insert(editor.SelectionStart + editor.SelectionLength, "*/");
			}

			editor.SelectionLength = 0;
			editor.CaretOffset = caretOffset;
			editor.EndChange();
		}

		public static void HandleLineComments(object sender, ExecutedRoutedEventArgs args)
		{
			var editor = GetEditorFromSender(sender);

			var startLine = editor.Document.GetLineByOffset(editor.SelectionStart);
			var endLine = editor.Document.GetLineByOffset(editor.SelectionStart + editor.SelectionLength);

			var lines = Enumerable.Range(startLine.LineNumber, endLine.LineNumber - startLine.LineNumber + 1)
				.Select(l => editor.Document.GetLineByNumber(l)).ToArray();

			var allLinesCommented = lines
				.All(l => editor.Text.Substring(startLine.Offset, startLine.Length).TrimStart().StartsWith(LineCommentPrefix));

			editor.BeginChange();

			foreach (var line in lines)
			{
				if (allLinesCommented)
				{
					editor.Document.Remove(line.Offset + editor.Text.Substring(line.Offset, line.Length).IndexOf(LineCommentPrefix, StringComparison.InvariantCulture), 2);
				}
				else
				{
					editor.Document.Insert(line.Offset, LineCommentPrefix);
				}
			}

			var caretOffset = editor.CaretOffset;
			editor.SelectionLength = 0;
			editor.CaretOffset = caretOffset;

			editor.EndChange();
		}
	}
}

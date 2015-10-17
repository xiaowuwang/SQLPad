﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace SqlPad
{
	public class CompletionData : ICompletionData
	{
		private readonly string _completionText;
		private readonly int _insertOffset;
		private readonly int _caretOffset;
		private readonly List<Run> _inlines = new List<Run>();

		private readonly DocumentPage _documentPage;

		public ICodeSnippet Snippet { get; set; }

		public CompletionData(ICodeCompletionItem codeCompletion)
		{
			Text = codeCompletion.Name;
			_completionText = codeCompletion.Text;
			Node = codeCompletion.StatementNode;
			_insertOffset = codeCompletion.InsertOffset;
			_caretOffset = codeCompletion.CaretOffset;
			Description = codeCompletion.Category;
		}

		public CompletionData(ICodeSnippet codeSnippet, DocumentPage documentPage)
		{
			_documentPage = documentPage;
			Snippet = codeSnippet;
			Text = codeSnippet.Name;
			Application.Current.Dispatcher.Invoke(BuildDecription);
		}

		private void BuildDecription()
		{
			var descriptionText = String.IsNullOrEmpty(Snippet.Description) ? null : $"{Environment.NewLine}{Snippet.Description}";
			var description = new TextBlock();
			description.Inlines.Add(new Bold(new Run("Code Snippet")));
			description.Inlines.Add(new Run(descriptionText));
			Description = description;
		}

		public void InitializeContent()
		{
			if (Content == null)
			{
				Content = new TextBlock { IsEnabled = false };
			}
		}

		public void Highlight(string text)
		{
			var startIndex = 0;
			var textBlock = (TextBlock)Content;
			var inlineCount = _inlines.Count;
			var inlineIndex = 0;
			if (String.IsNullOrEmpty(text))
			{
				SetInline(textBlock, Text, ref inlineIndex, ref inlineCount, false);
			}
			else
			{
				int index;
				while ((index = Text.IndexOf(text, startIndex, StringComparison.OrdinalIgnoreCase)) != -1)
				{
					if (index > startIndex)
					{
						var normalText = Text.Substring(startIndex, index - startIndex);
						SetInline(textBlock, normalText, ref inlineIndex, ref inlineCount, false);
					}

					SetInline(textBlock, Text.Substring(index, text.Length), ref inlineIndex, ref inlineCount, true);
					startIndex = index + text.Length;
				}

				if (Text.Length > startIndex)
				{
					SetInline(textBlock, Text.Substring(startIndex), ref inlineIndex, ref inlineCount, false);
				}
			}
			
			for (var i = inlineIndex; i < inlineCount; i++)
			{
				_inlines[i].Text = String.Empty;
			}
		}

		private void SetInline(TextBlock textBlock, string text, ref int inlineIndex, ref int inlineCount, bool isHighlight)
		{
			Run run;
			if (inlineIndex + 1 > inlineCount)
			{
				run = new Run();
				_inlines.Add(run);
				textBlock.Inlines.Add(run);
				inlineCount++;
			}
			else
			{
				run = _inlines[inlineIndex];
			}

			run.Text = text;
			run.Foreground = isHighlight ? Brushes.Red : Brushes.Black;
			inlineIndex++;
		}

		internal static string FormatSnippetText(ICodeSnippet codeSnippet)
		{
			return String.Format(codeSnippet.BaseText, codeSnippet.Parameters.Select(p => (object)p.DefaultValue).ToArray());
		}

		public StatementGrammarNode Node { get; }

		public ImageSource Image => null;

		public string Text { get; }

		public object Content { get; private set; }

		public object Description { get; private set; }

		public void Complete(TextArea textArea, ISegment completionSegment, EventArgs args)
		{
			if (Snippet != null)
			{
				_documentPage.ActivateSnippet(completionSegment, this);
				return;
			}

			if (Node != null)
			{
				var remainingLength = textArea.Document.TextLength - Node.SourcePosition.IndexStart;
				var replacedLength = Math.Min(Math.Max(Node.SourcePosition.Length, completionSegment.Length), remainingLength);
				textArea.Document.Replace(Node.SourcePosition.IndexStart, replacedLength, _completionText.Trim());
			}
			else
			{
				textArea.Document.Replace(completionSegment, new String(' ', _insertOffset) + _completionText.Trim());
			}

			if (_caretOffset != 0)
			{
				textArea.Caret.Offset += _caretOffset;
			}
		}

		public double Priority => 0;
	}

	internal class ActiveSnippet
	{
		private readonly TextArea _textArea;
		private readonly CompletionData _completionData;
		private readonly List<Tuple<TextAnchor, TextAnchor>> _followingAnchors = new List<Tuple<TextAnchor, TextAnchor>>();

		public Tuple<TextAnchor, TextAnchor> ActiveAnchors { get; private set; }

		public IReadOnlyList<Tuple<TextAnchor, TextAnchor>> FollowingAnchors => _followingAnchors;

		public bool ActiveAnchorsValid => !ActiveAnchors.Item1.IsDeleted && !ActiveAnchors.Item2.IsDeleted;

		public ActiveSnippet(ISegment completionSegment, TextArea textArea, CompletionData completionData)
		{
			textArea.Document.BeginUpdate();
			textArea.Document.Replace(completionSegment.Offset, completionSegment.Length, completionData.Snippet.BaseText);

			if (completionData.Snippet.Parameters.Count > 0)
			{
				_completionData = completionData;
				_textArea = textArea;

				var text = _completionData.Snippet.BaseText;
				foreach (var parameter in completionData.Snippet.Parameters.OrderBy(p => p.Index))
				{
					var parameterPlaceholder = $"{{{parameter.Index}}}";
					var parameterOffset = text.IndexOf(parameterPlaceholder, StringComparison.InvariantCulture);
					var documentStartOffset = completionSegment.Offset + parameterOffset;
					text = text.Replace(parameterPlaceholder, parameter.DefaultValue);
					textArea.Document.Replace(documentStartOffset, 3, parameter.DefaultValue);

					var anchorStart = textArea.Document.CreateAnchor(documentStartOffset);
					var anchorEnd = textArea.Document.CreateAnchor(anchorStart.Offset + parameter.DefaultValue.Length);
					_followingAnchors.Add(Tuple.Create(anchorStart, anchorEnd));
				}

				SelectNextParameter();
			}

			textArea.Document.EndUpdate();
		}

		public bool SelectNextParameter()
		{
			if (_followingAnchors.Count == 0 ||
				(ActiveAnchors = _followingAnchors[0]).Item1.IsDeleted || ActiveAnchors.Item2.IsDeleted)
			{
				SelectText(_textArea.Caret.Offset, _textArea.Caret.Offset);
				return false;
			}

			SelectText(ActiveAnchors.Item1.Offset, ActiveAnchors.Item2.Offset);
			return true;
		}

		private void SelectText(int startOffset, int endOffset)
		{
			_textArea.Selection = Selection.Create(_textArea, startOffset, endOffset);
			_textArea.Caret.Offset = endOffset;

			if (_followingAnchors.Count > 0)
			{
				_followingAnchors.RemoveAt(0);
			}
		}
	}
}

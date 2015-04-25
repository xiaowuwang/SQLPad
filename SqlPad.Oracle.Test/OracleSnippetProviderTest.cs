﻿using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace SqlPad.Oracle.Test
{
	[TestFixture]
	public class OracleSnippetProviderTest
	{
		private static readonly OracleSnippetProvider SnippetProvider = new OracleSnippetProvider();

		public OracleSnippetProviderTest()
		{
			var sqlPadDirectory = new Uri(Path.GetDirectoryName(typeof(Snippets).Assembly.CodeBase)).LocalPath;
			ConfigurationProvider.SetSnippetsFolder(Path.Combine(sqlPadDirectory, Snippets.SnippetDirectoryName));
		}

		[Test(Description = @"")]
		public void TestSnippetSuggestionWithinStatementWhileTyping()
		{
			const string statementText = "SELECT DUMMY FROM\r\nD\r\nDUAL";
			var snippets = SnippetProvider.GetSnippets(statementText, 21, TestFixture.DatabaseModel).ToArray();
			snippets.Length.ShouldBe(0);
		}
	}
}

﻿using System;
using System.Collections.Generic;
using System.Configuration;

namespace SqlPad
{
	public interface IDatabaseModel : IDisposable
	{
		ConnectionStringSettings ConnectionString { get; }

		string CurrentSchema { get; set; }

		ICollection<string> Schemas { get; }

		bool CanExecute { get; }

		bool IsExecuting { get; }

		void Refresh();

		event EventHandler RefreshStarted;

		event EventHandler RefreshFinished;

		void ExecuteStatement(string commandText);
	}

	public interface IDatabaseObject
	{
		string Name { get; }

		string Type { get; }

		string Owner { get; }
	}

	public interface IColumn
	{
		string Name { get; }

		string FullTypeName { get; }
	}
}

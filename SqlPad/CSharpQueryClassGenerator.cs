﻿using System;
using System.IO;
using System.Text;

namespace SqlPad
{
	public static class CSharpQueryClassGenerator
	{
		private const string ExportClassTemplate =
@"using System;
using System.Data;

public class Query
{{
	private IDbConnection _connection;

	private const string CommandText =
@""{0}"";
{1}	
	private IEnumerable<ResultRow> Execute()
	{{
		using (var command = _connection.CreateCommand())
		{{
			command.CommandText = CommandText;
			{2}			_connection.Open();

			using (var reader = command.ExecuteReader())
			{{
				while (reader.Read())
				{{
					var row =
						new ResultRow
						{{
{3}
						}};

					yield return row;
				}}
			}}

			_connection.Close();
		}}
	}}

	private static T GetReaderValue<T>(object value)
	{{
		return value == DBNull.Value
			? default(T)
			: (T)value;
	}}
}}
";
		public static void Generate(StatementExecutionResult executionResult, string fileName)
		{
			var columnMapBuilder = new StringBuilder();
			var resultRowPropertyBuilder = new StringBuilder();
			var bindVariableBuilder = new StringBuilder();
			var parameterBuilder = new StringBuilder();

			if (executionResult.Statement.BindVariables.Count > 0)
			{
				bindVariableBuilder.AppendLine();
				parameterBuilder.AppendLine();

				foreach (var bindVariable in executionResult.Statement.BindVariables)
				{
					bindVariableBuilder.Append("\tpublic ");
					bindVariableBuilder.Append(bindVariable.InputType);
					bindVariableBuilder.Append(" ");
					bindVariableBuilder.Append(bindVariable.Name);
					bindVariableBuilder.AppendLine(" { get; set; }");

					var parameterName = String.Format("parameter{0}", bindVariable.Name);
					parameterBuilder.Append("\t\t\tvar ");
					parameterBuilder.Append(parameterName);
					parameterBuilder.AppendLine(" = command.CreateParameter();");
					parameterBuilder.Append("\t\t\t");
					parameterBuilder.Append(parameterName);
					parameterBuilder.Append(".Value = ");
					parameterBuilder.Append(bindVariable.Name);
					parameterBuilder.AppendLine(";");
					parameterBuilder.Append("\t\t\tcommand.Parameters.Add(");
					parameterBuilder.Append(parameterName);
					parameterBuilder.AppendLine(");");
					parameterBuilder.AppendLine();
				}
			}

			var index = 0;
			foreach (var column in executionResult.ColumnHeaders)
			{
				index++;

				var dataTypeName = String.Equals(column.DataType.Namespace, "System")
					? column.DataType.Name
					: column.DataType.FullName;

				if (column.DataType.IsValueType)
				{
					dataTypeName = String.Format("{0}?", dataTypeName);
				}

				columnMapBuilder.Append("\t\t\t\t\t\t\t");
				columnMapBuilder.Append(column.Name);
				columnMapBuilder.Append(" = GetReaderValue<");
				columnMapBuilder.Append(dataTypeName);
				columnMapBuilder.Append(">(reader[\"");
				columnMapBuilder.Append(column.Name);
				columnMapBuilder.Append("\"])");

				if (index < executionResult.ColumnHeaders.Count)
				{
					columnMapBuilder.AppendLine(",");
				}

				resultRowPropertyBuilder.Append("\tpublic ");
				resultRowPropertyBuilder.Append(dataTypeName);
				resultRowPropertyBuilder.Append(" ");
				resultRowPropertyBuilder.Append(column.Name);
				resultRowPropertyBuilder.AppendLine(" { get; set; }");
			}

			var statementText = executionResult.Statement.StatementText.Replace("\"", "\"\"");
			var queryClass = String.Format(ExportClassTemplate, statementText, bindVariableBuilder, parameterBuilder, columnMapBuilder);

			using (var writer = File.CreateText(fileName))
			{
				writer.WriteLine(queryClass);
				writer.WriteLine("public class ResultRow");
				writer.WriteLine("{");
				writer.Write(resultRowPropertyBuilder);
				writer.WriteLine("}");
			}
		}
	}
}

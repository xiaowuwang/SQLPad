using System.Collections.Generic;
using System.Diagnostics;

namespace SqlPad.Oracle
{
	[DebuggerDisplay("OracleQueryBlock (Alias={Alias}; Type={Type}; RootNode={RootNode})")]
	public class OracleQueryBlock
	{
		public OracleQueryBlock()
		{
			TableReferences = new List<OracleTableReference>();
			Columns = new List<OracleSelectListColumn>();
		}

		public string Alias { get; set; }

		public QueryBlockType Type { get; set; }

		public StatementDescriptionNode RootNode { get; set; }

		public ICollection<OracleTableReference> TableReferences { get; set; }

		public ICollection<OracleSelectListColumn> Columns { get; set; }
	}
}
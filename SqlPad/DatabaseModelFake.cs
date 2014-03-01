﻿using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;

namespace SqlPad
{
	public class DatabaseModelFake : IDatabaseModel
	{
		private const string CurrentSchemaInternal = "\"HUSQVIK\"";
		private static readonly ConnectionStringSettings ConnectionStringInternal = new ConnectionStringSettings("ConnectionFake", "DATA SOURCE=HQ_PDB_TCP;PASSWORD=MMA_DEV;PERSIST SECURITY INFO=True;USER ID=HUSQVIK", "Oracle.DataAccess.Client");
		public const string SchemaPublic = "\"PUBLIC\"";

		private static readonly HashSet<string> SchemasInternal = new HashSet<string> { "\"SYS\"", "\"SYSTEM\"", CurrentSchemaInternal, SchemaPublic };

		private static readonly IDatabaseObject[] AllObjectsInternal =
		{
			new OracleDatabaseObject
			{
				Name = "\"DUAL\"",
				Owner = "\"SYS\"",
				Type = "TABLE",
				Properties = new HashSet<IDatabaseObjectProperty>
				             {
					             new OracleColumn { Name = "\"DUMMY\"" }
				             }
			},
			new OracleDatabaseObject { Name = "\"V_$SESSION\"", Owner = "\"SYS\"", Type = "VIEW" },
			new OracleDatabaseObject { Name = "\"V$SESSION\"", Owner = SchemaPublic, Type = "SYNONYM" },
			new OracleDatabaseObject { Name = "\"DUAL\"", Owner = SchemaPublic, Type = "SYNONYM" },
			new OracleDatabaseObject { Name = "\"COUNTRY\"", Owner = CurrentSchemaInternal, Type = "TABLE" },
			new OracleDatabaseObject { Name = "\"ORDERS\"", Owner = CurrentSchemaInternal, Type = "TABLE" },
			new OracleDatabaseObject { Name = "\"VIEW_INSTANTSEARCH\"", Owner = CurrentSchemaInternal, Type = "VIEW" },
		};

		private static readonly IDictionary<OracleObjectIdentifier, IDatabaseObject> AllObjectDictionary = AllObjectsInternal.ToDictionary(o => OracleObjectIdentifier.Create(o.Owner, o.Name), o => o);

		private static readonly IDictionary<OracleObjectIdentifier, IDatabaseObject> ObjectsInternal = AllObjectDictionary
			.Values.Where(o => o.Owner == SchemaPublic || o.Owner == CurrentSchemaInternal)
			.ToDictionary(o => OracleObjectIdentifier.Create(o.Owner, o.Name), o => o);
		
		#region Implementation of IDatabaseModel
		public ConnectionStringSettings ConnectionString { get { return ConnectionStringInternal; } }
		
		public string CurrentSchema { get { return CurrentSchemaInternal; } }
		
		public ICollection<string> Schemas { get { return SchemasInternal; } }

		public IDictionary<OracleObjectIdentifier, IDatabaseObject> Objects { get { return ObjectsInternal; } }

		public IDictionary<OracleObjectIdentifier, IDatabaseObject> AllObjects { get { return AllObjectDictionary; } }
		
		public void Refresh()
		{
		}
		#endregion
	}

	[DebuggerDisplay("DebuggerDisplay (Owner={Owner}; Name={Name}; Type={Type})")]
	public class OracleDatabaseObject : IDatabaseObject
	{
		public OracleDatabaseObject()
		{
			Properties = new List<IDatabaseObjectProperty>();
		}

		#region Implementation of IDatabaseObject
		public string Name { get; set; }
		public string Type { get; set; }
		public string Owner { get; set; }
		public ICollection<IDatabaseObjectProperty> Properties { get; set; }
		#endregion
	}

	public class OracleColumn : IDatabaseObjectProperty
	{
		#region Implementation of IDatabaseObjectProperty
		public string Name { get; set; }
		public object Value { get; set; }
		#endregion
	}

	[DebuggerDisplay("OracleObjectIdentifier (Owner={Owner,nq}.Name={Name,nq})")]
	public struct OracleObjectIdentifier
	{
		public static readonly OracleObjectIdentifier Empty = new OracleObjectIdentifier();

		public string Owner { get; private set; }

		public string Name { get; private set; }

		public OracleObjectIdentifier(string owner, string name) : this()
		{
			Owner = owner.ToOracleIdentifier();
			Name = name.ToOracleIdentifier();
		}

		public static OracleObjectIdentifier Create(string owner, string name)
		{
			return new OracleObjectIdentifier(owner, name);
		}
	}
}
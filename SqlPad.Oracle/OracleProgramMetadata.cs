using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SqlPad.Oracle
{
	[DebuggerDisplay("OracleProgramMetadata (Identifier={Identifier.FullyQualifiedIdentifier}; Overload={Identifier.Overload}; DataType={DataType}; IsAnalytic={IsAnalytic}; IsAggregate={IsAggregate}; MinimumArguments={MinimumArguments}; MaximumArguments={MaximumArguments})")]
	public class OracleProgramMetadata
	{
		public const string DisplayTypeNormal = "NORMAL";
		public const string DisplayTypeParenthesis = "PARENTHESIS";
		public const string DisplayTypeNoParenthesis = "NOPARENTHESIS";

		private readonly int? _metadataMinimumArguments;

		private readonly int? _metadataMaximumArguments;

		private List<OracleProgramParameterMetadata> _parameters;

		internal OracleProgramMetadata(ProgramType type, OracleProgramIdentifier identifier, bool isAnalytic, bool isAggregate, bool isPipelined, bool isOffloadable, bool parallelSupport, bool isDeterministic, int? metadataMinimumArguments, int? metadataMaximumArguments, AuthId authId, string displayType, bool isBuiltIn)
		{
			Type = type;
			Identifier = identifier;
			IsAnalytic = isAnalytic;
			IsAggregate = isAggregate;
			IsPipelined = isPipelined;
			IsOffloadable = isOffloadable;
			ParallelSupport = parallelSupport;
			IsDeterministic = isDeterministic;
			_metadataMinimumArguments = metadataMinimumArguments;
			_metadataMaximumArguments = metadataMaximumArguments;
			AuthId = authId;
			DisplayType = displayType;
			IsBuiltIn = isBuiltIn;
		}

		public IList<OracleProgramParameterMetadata> Parameters { get { return _parameters ?? (_parameters = new List<OracleProgramParameterMetadata>()); } }

		public bool IsBuiltIn { get; private set; }
		
		public ProgramType Type { get; private set; }

		public OracleProgramIdentifier Identifier { get; private set; }

		public bool IsAnalytic { get; private set; }

		public bool IsAggregate { get; private set; }

		public bool IsPipelined { get; private set; }

		public bool IsOffloadable { get; private set; }

		public bool ParallelSupport { get; private set; }

		public bool IsDeterministic { get; private set; }

		public int MinimumArguments
		{
			get { return Parameters.Count > 1 ? Parameters.Count(p => !p.IsOptional) - 1 : (_metadataMinimumArguments ?? 0); }
		}

		public int MaximumArguments
		{
			get { return Parameters.Count > 1 && _metadataMaximumArguments == null ? Parameters.Count - 1 : (_metadataMaximumArguments ?? 0); }
		}

		public bool IsPackageFunction
		{
			get { return !String.IsNullOrEmpty(Identifier.Package); }
		}

		public AuthId AuthId { get; private set; }

		public string DisplayType { get; private set; }
		
		public OracleSchemaObject Owner { get; set; }
	}

	[DebuggerDisplay("OracleProgramParameterMetadata (Name={Name}; Position={Position}; DataType={DataType}; Direction={Direction}; IsOptional={IsOptional})")]
	public class OracleProgramParameterMetadata
	{
		internal OracleProgramParameterMetadata(string name, int position, ParameterDirection direction, string dataType, OracleObjectIdentifier customDataType, bool isOptional)
		{
			Name = name;
			Position = position;
			DataType = dataType;
			CustomDataType = customDataType;
			Direction = direction;
			IsOptional = isOptional;
		}

		public string Name { get; private set; }

		public int Position { get; private set; }

		public string DataType { get; private set; }
		
		public OracleObjectIdentifier CustomDataType { get; private set; }

		public ParameterDirection Direction { get; private set; }

		public bool IsOptional { get; private set; }

		public string FullDataTypeName { get { return String.IsNullOrEmpty(CustomDataType.Owner) ? DataType.Trim('"') : CustomDataType.ToString(); } }
	}

	public enum ParameterDirection
	{
		Input,
		Output,
		InputOutput,
		ReturnValue,
	}

	public enum AuthId
	{
		CurrentUser,
		Definer
	}

	public enum ProgramType
	{
		Procedure,
		Function
	}
}
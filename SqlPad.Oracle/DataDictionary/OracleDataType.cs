﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using SqlPad.Oracle.DatabaseConnection;
using Terminals = SqlPad.Oracle.OracleGrammarDescription.Terminals;
using NonTerminals = SqlPad.Oracle.OracleGrammarDescription.NonTerminals;
using TerminalValues = SqlPad.Oracle.OracleGrammarDescription.TerminalValues;

namespace SqlPad.Oracle.DataDictionary
{
	[DebuggerDisplay("OracleDataType (Owner={FullyQualifiedName.NormalizedOwner}; Name={FullyQualifiedName.NormalizedName}; Length={Length}; Precision={Precision}; Scale={Scale}; Unit={Unit})")]
	public class OracleDataType : OracleObject
	{
		public static readonly OracleDataType Empty = new OracleDataType { FullyQualifiedName = OracleObjectIdentifier.Create(String.Empty, String.Empty) };
		public static readonly OracleDataType NumberType = new OracleDataType { FullyQualifiedName = OracleObjectIdentifier.Create(String.Empty, TerminalValues.Number) };
		public static readonly OracleDataType DateType = new OracleDataType { FullyQualifiedName = OracleObjectIdentifier.Create(String.Empty, TerminalValues.Date) };
		public static readonly OracleDataType BlobType = new OracleDataType { FullyQualifiedName = OracleObjectIdentifier.Create(String.Empty, TerminalValues.Blob) };
		public static readonly OracleDataType ClobType = new OracleDataType { FullyQualifiedName = OracleObjectIdentifier.Create(String.Empty, TerminalValues.Clob) };
		public static readonly OracleDataType NClobType = new OracleDataType { FullyQualifiedName = OracleObjectIdentifier.Create(String.Empty, "NCLOB") };
		public static readonly OracleDataType BinaryIntegerType = new OracleDataType { FullyQualifiedName = OracleObjectIdentifier.Create(String.Empty, TerminalValues.BinaryInteger) };
		public static readonly OracleDataType BinaryFloatType = new OracleDataType { FullyQualifiedName = OracleObjectIdentifier.Create(String.Empty, TerminalValues.BinaryFloat) };
		public static readonly OracleDataType BinaryDoubleType = new OracleDataType { FullyQualifiedName = OracleObjectIdentifier.Create(String.Empty, TerminalValues.BinaryDouble) };
		public static readonly OracleDataType BinaryFileType = new OracleDataType { FullyQualifiedName = OracleObjectIdentifier.Create(String.Empty, "BFILE") };
		public static readonly OracleDataType XmlType = new OracleDataType { FullyQualifiedName = OracleObjectIdentifier.Create(OracleObjectIdentifier.SchemaSys, OracleTypeBase.TypeCodeXml) };
		public static readonly OracleDataType DynamicCollectionType = new OracleDataType { FullyQualifiedName = OracleObjectIdentifier.Create(String.Empty, "DYNAMIC"), IsDynamicCollection = true };
		public static readonly OracleDataType PlSqlBooleanType = new OracleDataType { FullyQualifiedName = OracleObjectIdentifier.Create(String.Empty, "PL/SQL BOOLEAN") };

		public bool IsDynamicCollection { get; private set; }

		public int? Length { get; set; }

		public int? Precision { get; set; }
		
		public int? Scale { get; set; }

		public DataUnit Unit { get; set; }

		public bool IsPrimitive => !FullyQualifiedName.HasOwner;

		public override string Type { get; } = OracleObjectType.Type;

		public static OracleDataType CreateTimestampDataType(int scale)
		{
			return new OracleDataType { FullyQualifiedName = OracleObjectIdentifier.Create(String.Empty, TerminalValues.Timestamp), Scale = scale };
		}

		public static OracleDataType CreateTimestampWithTimeZoneDataType(int scale)
		{
			return new OracleDataType { FullyQualifiedName = OracleObjectIdentifier.Create(String.Empty, OracleDatabaseModelBase.BuiltInDataTypeTimestampWithTimeZone), Scale = scale };
		}

		public static IReadOnlyList<OracleDataType> FromUnpivotColumnSelectorValues(IEnumerable<StatementGrammarNode> nodes)
		{
			var dataTypes = new List<OracleDataType>();

			var definitionIndex = 0;
			foreach (var node in nodes)
			{
				if (!String.Equals(node.Id, NonTerminals.NullOrStringOrNumberLiteralOrParenthesisEnclosedStringOrIntegerLiteralList))
				{
					throw new ArgumentException($"All nodes must have ID of {nameof(NonTerminals.NullOrStringOrNumberLiteralOrParenthesisEnclosedStringOrIntegerLiteralList)}", nameof(nodes));
				}

				var literals = node.GetDescendants(Terminals.StringLiteral, Terminals.NumberLiteral);

				var typeIndex = 0;
				foreach (var literal in literals)
				{
					var newDataType = String.Equals(literal.Id, Terminals.StringLiteral)
						? new OracleDataType
						{
							FullyQualifiedName = OracleObjectIdentifier.Create(null, TerminalValues.Varchar2),
							Length = literal.Token.Value.ToPlainString().Length
						}
						: NumberType;

					if (definitionIndex == 0)
					{
						dataTypes.Add(newDataType);
					}
					else
					{
						if (dataTypes.Count <= typeIndex)
						{
							return null;
						}

						var storedDataType = dataTypes[typeIndex];
						if (storedDataType.FullyQualifiedName != newDataType.FullyQualifiedName)
						{
							return null;
						}

						if (newDataType.Length > storedDataType.Length)
						{
							storedDataType.Length = newDataType.Length;
						}
					}

					typeIndex++;
				}

				if (typeIndex != dataTypes.Count)
				{
					return null;
				}

				if (typeIndex > 0)
				{
					definitionIndex++;
				}
			}

			return dataTypes.AsReadOnly();
		}

		public static string ResolveFullTypeName(OracleDataType dataType, int? characterSize = null)
		{
			if (!dataType.IsPrimitive)
			{
				return dataType.FullyQualifiedName.ToString();
			}

			var name = dataType.FullyQualifiedName.Name.Trim('"');
			var effectiveSize = String.Empty;
			switch (name)
			{
				case TerminalValues.NVarchar2:
				case TerminalValues.NVarchar:
				case TerminalValues.Varchar2:
				case TerminalValues.Varchar:
				case TerminalValues.NChar:
				case TerminalValues.Char:
					var effectiveLength = characterSize ?? dataType.Length;
					if (effectiveLength.HasValue)
					{
						var unit = dataType.Unit == DataUnit.Byte ? " BYTE" : " CHAR";
						effectiveSize = $"({effectiveLength}{(dataType.Unit == DataUnit.NotApplicable ? null : unit)})";
					}

					name = $"{name}{effectiveSize}";
					break;
				case TerminalValues.Float:
				case TerminalValues.Number:
					var decimalScale = dataType.Scale > 0 ? $", {dataType.Scale}" : null;
					if (dataType.Precision > 0 || dataType.Scale > 0)
					{
						name = $"{name}({(dataType.Precision == null ? "*" : Convert.ToString(dataType.Precision))}{decimalScale})";
					}
					
					break;
				case TerminalValues.Raw:
					name = $"{name}({dataType.Length})";
					break;
				case TerminalValues.Timestamp:
				case OracleDatabaseModelBase.BuiltInDataTypeTimestampWithTimeZone:
					if (dataType.Scale.HasValue)
					{
						name = name.Replace(TerminalValues.Timestamp, $"{TerminalValues.Timestamp}({dataType.Scale})");
					}
					
					break;
			}

			return name;
		}

		public static bool TryResolveDataTypeFromExpression(StatementGrammarNode expressionNode, OracleColumn column)
		{
			if (expressionNode == null || expressionNode.TerminalCount == 0 || !String.Equals(expressionNode.Id, NonTerminals.Expression))
			{
				return false;
			}

			expressionNode = expressionNode.UnwrapIfNonChainedExpressionWithinParentheses(out bool isChainedExpression);
			if (isChainedExpression)
			{
				return false;
			}

			var analyzedNode = expressionNode[NonTerminals.CastOrXmlCastFunction, NonTerminals.CastFunctionParameterClause, NonTerminals.AsDataType, NonTerminals.DataType];
			if (analyzedNode != null)
			{
				column.DataType = OracleReferenceBuilder.ResolveDataTypeFromNode(analyzedNode);
				column.Nullable = true;
				return true;
			}

			if (String.Equals(expressionNode.FirstTerminalNode.Id, Terminals.Collect))
			{
				column.DataType = OracleDataType.DynamicCollectionType;
				column.Nullable = true;
				return true;
			}

			var tokenValue = expressionNode.FirstTerminalNode.Token.Value;
			string literalInferredDataTypeName = null;
			var literalInferredDataType = new OracleDataType();
			var nullable = false;
			switch (expressionNode.FirstTerminalNode.Id)
			{
				case Terminals.StringLiteral:
					if (expressionNode.TerminalCount != 1)
					{
						break;
					}

					if (tokenValue[0] == 'n' || tokenValue[0] == 'N')
					{
						literalInferredDataTypeName = TerminalValues.NChar;
					}
					else
					{
						literalInferredDataTypeName = TerminalValues.Char;
					}

					literalInferredDataType.Length = tokenValue.ToPlainString().Length;
					nullable = literalInferredDataType.Length == 0;

					break;
				case Terminals.NumberLiteral:
					if (expressionNode.TerminalCount != 1)
					{
						break;
					}

					switch (tokenValue[tokenValue.Length - 1])
					{
						case 'f':
						case 'F':
							literalInferredDataTypeName = TerminalValues.BinaryFloat;
							break;
						case 'd':
						case 'D':
							literalInferredDataTypeName = TerminalValues.BinaryDouble;
							break;
						default:
							literalInferredDataTypeName = TerminalValues.Number;
							break;
					}

					/*if (includeLengthPrecisionAndScale)
					{
						literalInferredDataType.Precision = GetNumberPrecision(tokenValue);
						int? scale = null;
						if (literalInferredDataType.Precision.HasValue)
						{
							var indexDecimalDigit = tokenValue.IndexOf('.');
							if (indexDecimalDigit != -1)
							{
								scale = tokenValue.Length - indexDecimalDigit - 1;
							}
						}

						literalInferredDataType.Scale = scale;
					}*/

					break;
				case Terminals.Date:
					if (expressionNode.TerminalCount == 2)
					{
						literalInferredDataTypeName = TerminalValues.Date;
					}

					break;
				case Terminals.Timestamp:
					if (expressionNode.TerminalCount == 2)
					{
						var timestampStringValue = expressionNode.LastTerminalNode.Token.Value.ToPlainString();
						var timeZoneElement = OracleStatementValidator.TimestampValidator.Match(timestampStringValue).Groups["Timezone"];
						literalInferredDataTypeName = timeZoneElement.Success ? OracleDatabaseModelBase.BuiltInDataTypeTimestampWithTimeZone : TerminalValues.Timestamp;
						literalInferredDataType.Scale = 9;
					}

					break;
			}

			if (literalInferredDataTypeName != null)
			{
				literalInferredDataType.FullyQualifiedName = OracleObjectIdentifier.Create(null, literalInferredDataTypeName);
				column.DataType = literalInferredDataType;
				column.Nullable = nullable;
				return true;
			}

			return false;
		}

		/*private static int? GetNumberPrecision(string value)
		{
			if (value.Any(c => c.In('e', 'E')))
			{
				return null;
			}

			return value.Count(Char.IsDigit);
		}*/
	}
}

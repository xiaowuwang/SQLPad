using System;
using System.Collections.Generic;
using System.Linq;
using NonTerminals = SqlPad.Oracle.OracleGrammarDescription.NonTerminals;
using Terminals = SqlPad.Oracle.OracleGrammarDescription.Terminals;

namespace SqlPad.Oracle
{
	public class OracleStatementSemanticModel
	{
		private readonly Dictionary<StatementDescriptionNode, OracleQueryBlock> _queryBlockResults = new Dictionary<StatementDescriptionNode, OracleQueryBlock>();
		private readonly Dictionary<OracleSelectListColumn, ICollection<OracleObjectReference>> _asteriskTableReferences = new Dictionary<OracleSelectListColumn, ICollection<OracleObjectReference>>();
		private readonly List<ICollection<OracleColumnReference>> _joinClauseColumnReferences = new List<ICollection<OracleColumnReference>>();
		private readonly Dictionary<OracleQueryBlock, ICollection<StatementDescriptionNode>> _accessibleQueryBlockRoot = new Dictionary<OracleQueryBlock, ICollection<StatementDescriptionNode>>();
		private readonly Dictionary<OracleObjectReference, ICollection<KeyValuePair<StatementDescriptionNode, string>>> _objectReferenceCteRootNodes = new Dictionary<OracleObjectReference, ICollection<KeyValuePair<StatementDescriptionNode, string>>>();
		private readonly OracleDatabaseModel _databaseModel;

		public OracleStatement Statement { get; private set; }

		public ICollection<OracleQueryBlock> QueryBlocks
		{
			get { return _queryBlockResults.Values; }
		}

		public OracleQueryBlock MainQueryBlock
		{
			get { return _queryBlockResults.Values.Where(qb => qb.Type == QueryBlockType.Normal).OrderBy(qb => qb.RootNode.SourcePosition.IndexStart).FirstOrDefault(); }
		}

		public OracleStatementSemanticModel(string sqlText, OracleStatement statement, OracleDatabaseModel databaseModel)
		{
			if (statement == null)
				throw new ArgumentNullException("statement");

			if (databaseModel == null)
				throw new ArgumentNullException("databaseModel");

			_databaseModel = databaseModel;

			Statement = statement;

			if (statement.RootNode == null)
				return;

			_queryBlockResults = statement.RootNode.GetDescendants(NonTerminals.QueryBlock)
				.OrderByDescending(q => q.Level).ToDictionary(n => n, n => new OracleQueryBlock { RootNode = n, Statement = statement });

			foreach (var queryBlockNode in _queryBlockResults)
			{
				var queryBlock = queryBlockNode.Key;
				var item = queryBlockNode.Value;

				var scalarSubqueryExpression = queryBlock.GetAncestor(NonTerminals.Expression);
				if (scalarSubqueryExpression != null)
				{
					item.Type = QueryBlockType.ScalarSubquery;
				}

				var commonTableExpression = queryBlock.GetPathFilterAncestor(NodeFilters.BreakAtNestedQueryBoundary, NonTerminals.SubqueryComponent);
				if (commonTableExpression != null)
				{
					item.AliasNode = commonTableExpression.ChildNodes.First();
					item.Type = QueryBlockType.CommonTableExpression;
				}
				else
				{
					var selfTableReference = queryBlock.GetAncestor(NonTerminals.TableReference);
					if (selfTableReference != null)
					{
						item.Type = QueryBlockType.Normal;

						var nestedSubqueryAlias = selfTableReference.ChildNodes.SingleOrDefault(n => n.Id == Terminals.ObjectAlias);
						if (nestedSubqueryAlias != null)
						{
							item.AliasNode = nestedSubqueryAlias;
						}
					}
				}

				var fromClause = queryBlock.GetDescendantsWithinSameQuery(NonTerminals.FromClause).FirstOrDefault();
				var tableReferenceNonterminals = fromClause == null
					? Enumerable.Empty<StatementDescriptionNode>()
					: fromClause.GetDescendantsWithinSameQuery(NonTerminals.TableReference).ToArray();

				var cteReferences = GetCommonTableExpressionReferences(queryBlock).ToDictionary(qb => qb.Key, qb => qb.Value);
				_accessibleQueryBlockRoot.Add(item, cteReferences.Keys);

				foreach (var tableReferenceNonterminal in tableReferenceNonterminals)
				{
					var queryTableExpression = tableReferenceNonterminal.GetDescendantsWithinSameQuery(NonTerminals.QueryTableExpression).SingleOrDefault();
					if (queryTableExpression == null)
						continue;

					var tableReferenceAlias = tableReferenceNonterminal.GetDescendantsWithinSameQuery(Terminals.ObjectAlias).SingleOrDefault();
					
					var nestedQueryTableReference = queryTableExpression.GetPathFilterDescendants(f => f.Id != NonTerminals.Subquery, NonTerminals.NestedQuery).SingleOrDefault();
					if (nestedQueryTableReference != null)
					{
						var nestedQueryTableReferenceQueryBlock = nestedQueryTableReference.GetPathFilterDescendants(n => n.Id != NonTerminals.NestedQuery && n.Id != NonTerminals.SubqueryFactoringClause, NonTerminals.QueryBlock).First();

						item.ObjectReferences.Add(new OracleObjectReference
						{
							Owner = item,
							TableReferenceNode = tableReferenceNonterminal,
							ObjectNode = nestedQueryTableReferenceQueryBlock,
							Type = TableReferenceType.NestedQuery,
							AliasNode = tableReferenceAlias
						});

						continue;
					}

					var tableIdentifierNode = queryTableExpression.ChildNodes.SingleOrDefault(n => n.Id == Terminals.ObjectIdentifier);

					if (tableIdentifierNode == null)
						continue;

					var schemaPrefixNode = queryTableExpression.ChildNodes.SingleOrDefault(n => n.Id == NonTerminals.SchemaPrefix);
					if (schemaPrefixNode != null)
					{
						schemaPrefixNode = schemaPrefixNode.ChildNodes.First();
					}

					var tableName = tableIdentifierNode.Token.Value.ToQuotedIdentifier();
					var commonTableExpressions = schemaPrefixNode != null
						? (ICollection<KeyValuePair<StatementDescriptionNode, string>>)new Dictionary<StatementDescriptionNode, string>()
						: cteReferences.Where(n => n.Value == tableName).ToArray();

					var referenceType = TableReferenceType.CommonTableExpression;

					var result = SchemaObjectResult.EmptyResult;
					if (commonTableExpressions.Count == 0)
					{
						referenceType = TableReferenceType.PhysicalObject;

						var objectName = tableIdentifierNode.Token.Value;
						var owner = schemaPrefixNode == null ? null : schemaPrefixNode.Token.Value;

						// TODO: Resolve package
						result = _databaseModel.GetObject(OracleObjectIdentifier.Create(owner, objectName));
					}

					var objectReference = new OracleObjectReference
					                            {
						                            Owner = item,
						                            TableReferenceNode = tableReferenceNonterminal,
						                            OwnerNode = schemaPrefixNode,
						                            ObjectNode = tableIdentifierNode,
						                            Type = referenceType,
						                            AliasNode = tableReferenceAlias,
						                            SearchResult = result
					                            };
					
					item.ObjectReferences.Add(objectReference);

					if (commonTableExpressions.Count > 0)
					{
						_objectReferenceCteRootNodes[objectReference] = commonTableExpressions;
					}
				}

				ResolveSelectList(item);

				ResolveWhereGroupByHavingReferences(item);

				ResolveJoinColumnReferences(item);
			}

			foreach (var queryBlock in _queryBlockResults.Values)
			{
				if (queryBlock.RootNode.ParentNode.ParentNode.Id == NonTerminals.ConcatenatedSubquery)
				{
					var parentSubquery = queryBlock.RootNode.ParentNode.GetAncestor(NonTerminals.Subquery);
					if (parentSubquery != null)
					{
						var parentQueryBlockNode = parentSubquery.ChildNodes.SingleOrDefault(n => n.Id == NonTerminals.QueryBlock);
						if (parentQueryBlockNode != null)
						{
							var precedingQueryBlock = _queryBlockResults[parentQueryBlockNode];
							precedingQueryBlock.FollowingConcatenatedQueryBlock = queryBlock;
							queryBlock.PrecedingConcatenatedQueryBlock = precedingQueryBlock;
						}
					}
				}

				foreach (var nestedQueryReference in queryBlock.ObjectReferences.Where(t => t.Type != TableReferenceType.PhysicalObject))
				{
					if (nestedQueryReference.Type == TableReferenceType.NestedQuery)
					{
						nestedQueryReference.QueryBlocks.Add(_queryBlockResults[nestedQueryReference.ObjectNode]);
					}
					else if (_objectReferenceCteRootNodes.ContainsKey(nestedQueryReference))
					{
						var commonTableExpressionNode = _objectReferenceCteRootNodes[nestedQueryReference];
						foreach (var referencedQueryBlock in commonTableExpressionNode
							.Where(nodeName => OracleObjectIdentifier.Create(null, nodeName.Value) == OracleObjectIdentifier.Create(null, nestedQueryReference.ObjectNode.Token.Value)))
						{
							var cteQueryBlockNode = referencedQueryBlock.Key.GetDescendants(NonTerminals.QueryBlock).FirstOrDefault();
							if (cteQueryBlockNode != null)
							{
								nestedQueryReference.QueryBlocks.Add(_queryBlockResults[cteQueryBlockNode]);
							}
						}
					}
				}

				foreach (var accessibleQueryBlock in _accessibleQueryBlockRoot[queryBlock])
				{
					var accesibleQueryBlockRoot = accessibleQueryBlock.GetDescendants(NonTerminals.QueryBlock).FirstOrDefault();
					if (accesibleQueryBlockRoot != null)
					{
						queryBlock.AccessibleQueryBlocks.Add(_queryBlockResults[accesibleQueryBlockRoot]);
					}
				}
			}

			foreach (var asteriskTableReference in _asteriskTableReferences)
			{
				foreach (var objectReference in asteriskTableReference.Value)
				{
					IEnumerable<OracleSelectListColumn> exposedColumns;
					if (objectReference.Type == TableReferenceType.PhysicalObject)
					{
						if (objectReference.SearchResult.SchemaObject == null)
							continue;

						exposedColumns = objectReference.SearchResult.SchemaObject.Columns
							.Select(c => new OracleSelectListColumn
							        {
								        ExplicitDefinition = false,
								        IsDirectColumnReference = true,
								        ColumnDescription = c
							        });
					}
					else
					{
						exposedColumns = objectReference.QueryBlocks.SelectMany(qb => qb.Columns)
							.Where(c => !c.IsAsterisk)
							.Select(c => c.AsImplicit());
					}

					foreach (var exposedColumn in exposedColumns)
					{
						exposedColumn.Owner = asteriskTableReference.Key.Owner;
						var columnReference = CreateColumnReference(exposedColumn.Owner, exposedColumn, ColumnReferenceType.SelectList, asteriskTableReference.Key.RootNode.LastTerminalNode, null);
						columnReference.ColumnNodeObjectReferences.Add(objectReference);
						exposedColumn.ColumnReferences.Add(columnReference);

						asteriskTableReference.Key.Owner.Columns.Add(exposedColumn);
					}
				}
			}

			foreach (var queryBlock in _queryBlockResults.Values)
			{
				ResolveOrderByReferences(queryBlock);

				foreach (var selectColumm in queryBlock.Columns.Where(c => c.ExplicitDefinition))
				{
					ResolveColumnObjectReferences(selectColumm.ColumnReferences, selectColumm.FunctionReferences, queryBlock.ObjectReferences);
				}

				ResolveColumnObjectReferences(queryBlock.ColumnReferences, queryBlock.FunctionReferences, queryBlock.ObjectReferences);

				ResolveFunctionReferences(queryBlock);
			}

			foreach (var joinClauseColumnReferences in _joinClauseColumnReferences)
			{
				var queryBlock = joinClauseColumnReferences.First().Owner;
				foreach (var columnReference in joinClauseColumnReferences)
				{
					var fromClauseNode = columnReference.ColumnNode.GetAncestor(NonTerminals.FromClause);
					var relatedTableReferences = queryBlock.ObjectReferences
						.Where(t => t.TableReferenceNode.SourcePosition.IndexStart >= fromClauseNode.SourcePosition.IndexStart &&
						            t.TableReferenceNode.SourcePosition.IndexEnd <= columnReference.ColumnNode.SourcePosition.IndexStart).ToArray();

					var columnReferences = new List<OracleColumnReference> { columnReference };
					ResolveColumnObjectReferences(columnReferences, queryBlock.FunctionReferences, relatedTableReferences);
					queryBlock.ColumnReferences.AddRange(columnReferences);
				}
			}
		}

		private void ResolveFunctionReferences(OracleQueryBlock queryBlock)
		{
			foreach (var functionReference in queryBlock.AllFunctionReferences)
			{
				functionReference.Metadata = GetFunctionMetadata(functionReference);
			}
		}

		private OracleFunctionMetadata GetFunctionMetadata(OracleFunctionReference functionReference)
		{
			var functionIdentifier = OracleFunctionIdentifier.CreateFromValues(functionReference.FullyQualifiedObjectName.NormalizedOwner, functionReference.FullyQualifiedObjectName.NormalizedName, functionReference.NormalizedName);
			var parameterCount = functionReference.ParameterNodes == null ? 0 : functionReference.ParameterNodes.Count;
			var metadata = _databaseModel.AllFunctionMetadata.GetSqlFunctionMetadata(functionIdentifier, parameterCount);

			if (metadata == null && !String.IsNullOrEmpty(functionIdentifier.Package) && String.IsNullOrEmpty(functionIdentifier.Owner))
			{
				var identifier = OracleFunctionIdentifier.CreateFromValues(functionIdentifier.Package, null, functionIdentifier.Name);
				metadata = _databaseModel.AllFunctionMetadata.GetSqlFunctionMetadata(identifier, parameterCount);
			}

			return metadata;
		}

		public OracleQueryBlock GetQueryBlock(StatementDescriptionNode node)
		{
			var queryBlockNode = node.GetAncestor(NonTerminals.QueryBlock);
			return queryBlockNode == null ? null : _queryBlockResults[queryBlockNode];
		}

		private void ResolveColumnObjectReferences(ICollection<OracleColumnReference> columnReferences, ICollection<OracleFunctionReference> functionReferences, ICollection<OracleObjectReference> accessibleRowSourceReferences)
		{
			foreach (var columnReference in columnReferences.ToArray())
			{
				if (columnReference.Type == ColumnReferenceType.OrderBy)
				{
					if (columnReference.Owner.FollowingConcatenatedQueryBlock != null)
					{
						var isRecognized = true;
						var maximumReferences = 0;
						var concatenatedQueryBlocks = new List<OracleQueryBlock> { columnReference.Owner };
						concatenatedQueryBlocks.AddRange(columnReference.Owner.AllFollowingConcatenatedQueryBlocks);
						for(var i = 0; i < concatenatedQueryBlocks.Count; i++)
						{
							var queryBlockColumnAliasReferences = concatenatedQueryBlocks[i].Columns.Count(c => columnReference.ObjectNode == null && c.NormalizedName == columnReference.NormalizedName);
							isRecognized = isRecognized && (queryBlockColumnAliasReferences > 0 || i == concatenatedQueryBlocks.Count - 1);
							maximumReferences = Math.Min(maximumReferences, queryBlockColumnAliasReferences);
						}

						if (isRecognized)
						{
							columnReference.ColumnNodeObjectReferences.Add(columnReference.Owner.SelfObjectReference);
							columnReference.ColumnNodeColumnReferences += maximumReferences;
						}

						continue;
					}

					var orderByColumnAliasReferences = columnReference.Owner.Columns.Count(c => columnReference.ObjectNode == null && c.NormalizedName == columnReference.NormalizedName && (!c.IsDirectColumnReference || (c.ColumnReferences.Count > 0 && c.ColumnReferences.First().NormalizedName != c.NormalizedName)));
					columnReference.ColumnNodeColumnReferences += orderByColumnAliasReferences;
					if (orderByColumnAliasReferences > 0)
					{
						columnReference.ColumnNodeObjectReferences.Add(columnReference.Owner.SelfObjectReference);
					}
				}

				OracleColumn columnDescription = null;
				foreach (var rowSourceReference in accessibleRowSourceReferences)
				{
					if (!String.IsNullOrEmpty(columnReference.FullyQualifiedObjectName.NormalizedName) &&
						(rowSourceReference.FullyQualifiedName == columnReference.FullyQualifiedObjectName ||
						 (String.IsNullOrEmpty(columnReference.FullyQualifiedObjectName.Owner) &&
						  rowSourceReference.Type == TableReferenceType.PhysicalObject && rowSourceReference.FullyQualifiedName.NormalizedName == columnReference.FullyQualifiedObjectName.NormalizedName)))
						columnReference.ObjectNodeObjectReferences.Add(rowSourceReference);

					var columnNodeColumnReferences = GetColumnNodeObjectReferences(rowSourceReference, columnReference);

					if (columnNodeColumnReferences.Count > 0 &&
						(String.IsNullOrEmpty(columnReference.FullyQualifiedObjectName.NormalizedName) ||
						 columnReference.ObjectNodeObjectReferences.Count > 0))
					{
						columnReference.ColumnNodeColumnReferences += columnNodeColumnReferences.Count;
						columnReference.ColumnNodeObjectReferences.Add(rowSourceReference);
						columnDescription = columnNodeColumnReferences.First();
					}
				}

				if (columnDescription != null &&
					columnReference.ColumnNodeObjectReferences.Count == 1)
				{
					columnReference.ColumnDescription = columnDescription;
				}

				if (columnReference.ColumnNodeColumnReferences == 0)
				{
					var functionIdentifier = OracleFunctionIdentifier.CreateFromValues(columnReference.FullyQualifiedObjectName.NormalizedOwner, columnReference.FullyQualifiedObjectName.NormalizedName, columnReference.NormalizedName);
					var sqlFunctionMetadata = _databaseModel.BuiltInFunctionMetadata.GetSqlFunctionMetadata(functionIdentifier, 0);
					if (sqlFunctionMetadata != null)
					{
						var functionReference =
							new OracleFunctionReference
							{
								FunctionIdentifierNode = columnReference.ColumnNode,
								RootNode = columnReference.ColumnNode,
								Owner = columnReference.Owner,
								AnalyticClauseNode = null,
								ParameterListNode = null,
								ParameterNodes = null
							};

						functionReferences.Add(functionReference);
						columnReferences.Remove(columnReference);
					}
				}
			}
		}

		private ICollection<OracleColumn> GetColumnNodeObjectReferences(OracleObjectReference rowSourceReference, OracleColumnReference columnReference)
		{
			OracleColumn[] columnNodeColumnReferences;
			if (rowSourceReference.Type == TableReferenceType.PhysicalObject)
			{
				if (rowSourceReference.SearchResult.SchemaObject == null)
					return new OracleColumn[0];

				columnNodeColumnReferences = rowSourceReference.SearchResult.SchemaObject.Columns
					.Where(c => c.Name == columnReference.NormalizedName && (columnReference.ObjectNode == null || IsTableReferenceValid(columnReference, rowSourceReference)))
					.ToArray();
			}
			else
			{
				columnNodeColumnReferences = rowSourceReference.QueryBlocks.SelectMany(qb => qb.Columns)
					.Where(c => c.NormalizedName == columnReference.NormalizedName && (columnReference.ObjectNode == null || columnReference.FullyQualifiedObjectName.NormalizedName == rowSourceReference.FullyQualifiedName.NormalizedName))
					.Select(c => c.ColumnDescription)
					.ToArray();
			}

			return columnNodeColumnReferences;
		}

		private bool IsTableReferenceValid(OracleColumnReference column, OracleObjectReference schemaObject)
		{
			var objectName = column.FullyQualifiedObjectName;
			return (String.IsNullOrEmpty(objectName.NormalizedName) || objectName.NormalizedName == schemaObject.FullyQualifiedName.NormalizedName) &&
			       (String.IsNullOrEmpty(objectName.NormalizedOwner) || objectName.NormalizedOwner == schemaObject.FullyQualifiedName.NormalizedOwner);
		}

		private void ResolveJoinColumnReferences(OracleQueryBlock queryBlock)
		{
			var fromClauses = queryBlock.RootNode.GetDescendantsWithinSameQuery(NonTerminals.FromClause);
			foreach (var fromClause in fromClauses)
			{
				var joinClauses = fromClause.GetPathFilterDescendants(n => n.Id != NonTerminals.NestedQuery && n.Id != NonTerminals.FromClause, NonTerminals.JoinClause);
				foreach (var joinClause in joinClauses)
				{
					var joinCondition = joinClause.GetPathFilterDescendants(n => n.Id != NonTerminals.JoinClause, NonTerminals.JoinColumnsOrCondition).SingleOrDefault();
					if (joinCondition == null)
						continue;

					var identifiers = joinCondition.GetDescendants(Terminals.Identifier);
					var columnReferences = new List<OracleColumnReference>();
					ResolveColumnAndFunctionReferenceFromIdentifiers(queryBlock, columnReferences, queryBlock.FunctionReferences, identifiers, ColumnReferenceType.Join, null);

					if (columnReferences.Count > 0)
					{
						_joinClauseColumnReferences.Add(columnReferences);
					}
				}
			}
		}

		private void ResolveWhereGroupByHavingReferences(OracleQueryBlock queryBlock)
		{
			var identifiers = GetIdentifiersFromNodesWithinSameQuery(queryBlock, NonTerminals.WhereClause, NonTerminals.GroupByClause).ToArray();
			ResolveColumnAndFunctionReferenceFromIdentifiers(queryBlock, queryBlock.ColumnReferences, queryBlock.FunctionReferences, identifiers, ColumnReferenceType.WhereGroupHaving, null);

			var havingRootNode = queryBlock.RootNode.GetDescendantsWithinSameQuery(NonTerminals.HavingClause).FirstOrDefault();
			if (havingRootNode == null)
				return;
			
			var grammarSpecificFunctions = havingRootNode.GetDescendantsWithinSameQuery(Terminals.Count, NonTerminals.AggregateFunction, NonTerminals.AnalyticFunction).ToArray();
			CreateGrammarSpecificFunctionReferences(grammarSpecificFunctions, queryBlock, queryBlock.FunctionReferences, null);
		}

		private void ResolveOrderByReferences(OracleQueryBlock queryBlock)
		{
			if (queryBlock.PrecedingConcatenatedQueryBlock != null)
				return;

			var orderByNode = queryBlock.RootNode.ParentNode.GetPathFilterDescendants(n => n.Id != NonTerminals.QueryBlock, NonTerminals.OrderByClause).FirstOrDefault();
			if (orderByNode == null)
				return;

			var identifiers = orderByNode.GetDescendantsWithinSameQuery(Terminals.Identifier);
			ResolveColumnAndFunctionReferenceFromIdentifiers(queryBlock, queryBlock.ColumnReferences, queryBlock.FunctionReferences, identifiers, ColumnReferenceType.OrderBy, null);
		}

		private void ResolveColumnAndFunctionReferenceFromIdentifiers(OracleQueryBlock queryBlock, ICollection<OracleColumnReference> columnReferences, ICollection<OracleFunctionReference> functionReferences, IEnumerable<StatementDescriptionNode> identifiers, ColumnReferenceType type, OracleSelectListColumn selectListColumn)
		{
			foreach (var identifier in identifiers)
			{
				var prefixNonTerminal = identifier.GetPathFilterAncestor(n => n.Id != NonTerminals.Expression, NonTerminals.PrefixedColumnReference)
					.ChildNodes.SingleOrDefault(n => n.Id == NonTerminals.Prefix);

				var functionCallNodes = GetFunctionCallNodes(identifier);
				if (functionCallNodes.Length == 0)
				{
					var columnReference = CreateColumnReference(queryBlock, selectListColumn, type, identifier, prefixNonTerminal);
					columnReferences.Add(columnReference);
				}
				else
				{
					var functionReference = CreateFunctionReference(queryBlock, selectListColumn, identifier, prefixNonTerminal, functionCallNodes);
					functionReferences.Add(functionReference);
				}
			}
		}

		private IEnumerable<StatementDescriptionNode> GetIdentifiersFromNodesWithinSameQuery(OracleQueryBlock queryBlock, params string[] nonTerminalIds)
		{
			var identifiers = Enumerable.Empty<StatementDescriptionNode>();
			foreach (var nonTerminalId in nonTerminalIds)
			{
				var clauseRootNode = queryBlock.RootNode.GetDescendantsWithinSameQuery(nonTerminalId).FirstOrDefault();
				if (clauseRootNode == null)
					continue;

				var nodeIdentifiers = clauseRootNode.GetDescendantsWithinSameQuery(Terminals.Identifier);
				identifiers = identifiers.Concat(nodeIdentifiers);
			}

			return identifiers;
		}

		private void ResolveSelectList(OracleQueryBlock item)
		{
			var queryBlock = item.RootNode;

			var selectList = queryBlock.GetDescendantsWithinSameQuery(NonTerminals.SelectList).SingleOrDefault();
			if (selectList == null || selectList.FirstTerminalNode == null)
				return;

			if (selectList.FirstTerminalNode.Id == Terminals.Asterisk)
			{
				var asteriskNode = selectList.ChildNodes.Single();
				var column = new OracleSelectListColumn
				{
					RootNode = asteriskNode,
					Owner = item,
					ExplicitDefinition = true,
					IsAsterisk = true
				};

				column.ColumnReferences.Add(CreateColumnReference(item, column, ColumnReferenceType.SelectList, asteriskNode, null));

				_asteriskTableReferences[column] = new HashSet<OracleObjectReference>(item.ObjectReferences);

				item.Columns.Add(column);
			}
			else
			{
				var columnExpressions = selectList.GetDescendantsWithinSameQuery(NonTerminals.AliasedExpressionOrAllTableColumns).ToArray();
				foreach (var columnExpression in columnExpressions)
				{
					var columnAliasNode = columnExpression.GetDescendantsWithinSameQuery(Terminals.ColumnAlias).SingleOrDefault();

					var column = new OracleSelectListColumn
					{
						AliasNode = columnAliasNode,
						RootNode = columnExpression,
						Owner = item,
						ExplicitDefinition = true
					};

					_asteriskTableReferences.Add(column, new List<OracleObjectReference>());

					var asteriskNode = columnExpression.GetPathFilterDescendants(n => !n.Id.In(NonTerminals.NestedQuery, NonTerminals.AggregateFunctionCall), Terminals.Asterisk).SingleOrDefault();
					if (asteriskNode != null)
					{
						column.IsAsterisk = true;

						var prefixNonTerminal = asteriskNode.ParentNode.ChildNodes.SingleOrDefault(n => n.Id == NonTerminals.Prefix);
						var columnReference = CreateColumnReference(item, column, ColumnReferenceType.SelectList, asteriskNode, prefixNonTerminal);
						column.ColumnReferences.Add(columnReference);

						var tableReferences = item.ObjectReferences.Where(t => t.FullyQualifiedName == columnReference.FullyQualifiedObjectName || (columnReference.ObjectNode == null && t.FullyQualifiedName.NormalizedName == columnReference.FullyQualifiedObjectName.NormalizedName));
						_asteriskTableReferences[column].AddRange(tableReferences);
					}
					else
					{
						var identifiers = columnExpression.GetDescendantsWithinSameQuery(Terminals.Identifier).ToArray();
						column.IsDirectColumnReference = identifiers.Length == 1 && identifiers[0].GetAncestor(NonTerminals.Expression).ChildNodes.Count == 1;
						if (column.IsDirectColumnReference && columnAliasNode == null)
						{
							column.AliasNode = identifiers[0];
						}

						ResolveColumnAndFunctionReferenceFromIdentifiers(item, column.ColumnReferences, column.FunctionReferences, identifiers, ColumnReferenceType.SelectList, column);

						var grammarSpecificFunctions = columnExpression.GetDescendantsWithinSameQuery(Terminals.Count, NonTerminals.AggregateFunction, NonTerminals.AnalyticFunction).ToArray();
						CreateGrammarSpecificFunctionReferences(grammarSpecificFunctions, item, column.FunctionReferences, column);
					}

					item.Columns.Add(column);
				}
			}
		}

		private static void CreateGrammarSpecificFunctionReferences(IEnumerable<StatementDescriptionNode> grammarSpecificFunctions, OracleQueryBlock queryBlock, ICollection<OracleFunctionReference> functionReferences, OracleSelectListColumn selectListColumn)
		{
			foreach (var identifierNode in grammarSpecificFunctions.Select(n => n.FirstTerminalNode).Distinct())
			{
				var rootNode = identifierNode.GetAncestor(NonTerminals.AnalyticFunctionCall) ?? identifierNode.GetAncestor(NonTerminals.AggregateFunctionCall);
				var analyticClauseNode = rootNode.GetSingleDescendant(NonTerminals.AnalyticClause);

				var parameterList = rootNode.ChildNodes.SingleOrDefault(n => n.Id.In(NonTerminals.ParenthesisEnclosedExpressionListWithMandatoryExpressions, NonTerminals.CountAsteriskParameter, NonTerminals.AggregateFunctionParameter, NonTerminals.ParenthesisEnclosedExpressionListWithIgnoreNulls));
				var parameterNodes = new List<StatementDescriptionNode>();
				if (parameterList != null)
				{
					switch (parameterList.Id)
					{
						case NonTerminals.CountAsteriskParameter:
							parameterNodes.Add(parameterList.ChildNodes.SingleOrDefault(n => n.Id == Terminals.Asterisk));
							break;
						case NonTerminals.AggregateFunctionParameter:
						case NonTerminals.ParenthesisEnclosedExpressionListWithIgnoreNulls:
							var expression = parameterList.ChildNodes.SingleOrDefault(n => n.Id == NonTerminals.Expression);
							parameterNodes.Add(expression);
							goto default;
						default:
							var nodes = parameterList.GetPathFilterDescendants(n => !n.Id.In(NonTerminals.NestedQuery, NonTerminals.ParenthesisEnclosedAggregationFunctionParameters), NonTerminals.ExpressionList).Select(n => n.ChildNodes.FirstOrDefault());
							parameterNodes.AddRange(nodes);
							break;
					}
				}

				var functionReference =
					new OracleFunctionReference
					{
						FunctionIdentifierNode = identifierNode,
						RootNode = rootNode,
						Owner = queryBlock,
						AnalyticClauseNode = analyticClauseNode,
						ParameterListNode = parameterList,
						ParameterNodes = parameterNodes.AsReadOnly(),
						SelectListColumn = selectListColumn
					};

				functionReferences.Add(functionReference);
			}
		}

		private static StatementDescriptionNode[] GetFunctionCallNodes(StatementDescriptionNode identifier)
		{
			return identifier.ParentNode.ChildNodes.Where(n => n.Id.In(NonTerminals.DatabaseLink, NonTerminals.ParenthesisEnclosedAggregationFunctionParameters, NonTerminals.AnalyticClause)).ToArray();
		}

		private static OracleFunctionReference CreateFunctionReference(OracleQueryBlock queryBlock, OracleSelectListColumn selectListColumn, /*ColumnReferenceType type,*/ StatementDescriptionNode identifierNode, StatementDescriptionNode prefixNonTerminal, ICollection<StatementDescriptionNode> functionCallNodes)
		{
			var analyticClauseNode = functionCallNodes.SingleOrDefault(n => n.Id == NonTerminals.AnalyticClause);

			var parameterList = functionCallNodes.SingleOrDefault(n => n.Id == NonTerminals.ParenthesisEnclosedAggregationFunctionParameters);
			var parameterExpressionRootNodes = parameterList != null
				? parameterList
					.GetPathFilterDescendants(
						n => !n.Id.In(NonTerminals.NestedQuery, NonTerminals.ParenthesisEnclosedAggregationFunctionParameters, NonTerminals.AggregateFunctionCall, NonTerminals.AnalyticFunctionCall),
						NonTerminals.ExpressionList)
					.Select(n => n.ChildNodes.FirstOrDefault())
					.ToArray()
				: null;

			var functionReference =
				new OracleFunctionReference
				{
					FunctionIdentifierNode = identifierNode,
					RootNode = identifierNode.GetAncestor(NonTerminals.Expression),
					Owner = queryBlock,
					AnalyticClauseNode = analyticClauseNode,
					ParameterListNode = parameterList,
					ParameterNodes = parameterExpressionRootNodes,
					SelectListColumn = selectListColumn
				};

			AddPrefixNodes(functionReference, prefixNonTerminal);

			return functionReference;
		}

		private static OracleColumnReference CreateColumnReference(OracleQueryBlock queryBlock, OracleSelectListColumn selectListColumn, ColumnReferenceType type, StatementDescriptionNode identifierNode, StatementDescriptionNode prefixNonTerminal)
		{
			var columnReference =
				new OracleColumnReference
				{
					ColumnNode = identifierNode,
					Type = type,
					Owner = queryBlock,
					SelectListColumn = selectListColumn
				};

			AddPrefixNodes(columnReference, prefixNonTerminal);

			return columnReference;
		}

		private static void AddPrefixNodes(OracleReference reference, StatementDescriptionNode prefixNonTerminal)
		{
			if (prefixNonTerminal == null)
				return;
			
			var objectIdentifier = prefixNonTerminal.GetSingleDescendant(Terminals.ObjectIdentifier);
			var schemaIdentifier = prefixNonTerminal.GetSingleDescendant(Terminals.SchemaIdentifier);

			reference.OwnerNode = schemaIdentifier;
			reference.ObjectNode = objectIdentifier;
		}

		private IEnumerable<KeyValuePair<StatementDescriptionNode, string>> GetCommonTableExpressionReferences(StatementDescriptionNode node)
		{
			var queryRoot = node.GetAncestor(NonTerminals.NestedQuery);
			var subQueryCompondentNode = node.GetAncestor(NonTerminals.SubqueryComponent);
			var cteReferencesWithinSameClause = new List<KeyValuePair<StatementDescriptionNode, string>>();
			if (subQueryCompondentNode != null)
			{
				var cteNodeWithinSameClause = subQueryCompondentNode.GetAncestor(NonTerminals.SubqueryComponent);
				while (cteNodeWithinSameClause != null)
				{
					cteReferencesWithinSameClause.Add(GetCteNameNode(cteNodeWithinSameClause));
					cteNodeWithinSameClause = cteNodeWithinSameClause.GetAncestor(NonTerminals.SubqueryComponent);
				}

				if (node.Level - queryRoot.Level > node.Level - subQueryCompondentNode.Level)
				{
					queryRoot = queryRoot.GetAncestor(NonTerminals.NestedQuery);
				}
			}

			if (queryRoot == null)
				return cteReferencesWithinSameClause;

			var commonTableExpressions = queryRoot
				.GetPathFilterDescendants(n => n.Id != NonTerminals.QueryBlock, NonTerminals.SubqueryComponent)
				.Select(GetCteNameNode);
			return commonTableExpressions
				.Concat(cteReferencesWithinSameClause)
				.Concat(GetCommonTableExpressionReferences(queryRoot));
		}

		private KeyValuePair<StatementDescriptionNode, string> GetCteNameNode(StatementDescriptionNode cteNode)
		{
			var objectIdentifierNode = cteNode.ChildNodes.FirstOrDefault();
			var cteName = objectIdentifierNode == null ? null : objectIdentifierNode.Token.Value.ToQuotedIdentifier();
			return new KeyValuePair<StatementDescriptionNode, string>(cteNode, cteName);
		}
	}

	public enum TableReferenceType
	{
		PhysicalObject,
		CommonTableExpression,
		NestedQuery
	}

	public enum QueryBlockType
	{
		Normal,
		ScalarSubquery,
		CommonTableExpression
	}
}

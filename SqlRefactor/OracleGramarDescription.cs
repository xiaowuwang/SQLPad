﻿namespace SqlRefactor
{
	public static class OracleGrammarDescription
	{
		public static class NonTerminals
		{
			public const string AliasedExpression = "AliasedExpression";
			public const string AliasedExpressionOrAllTableColumns = "AliasedExpressionOrAllTableColumns";
			public const string CaseExpression = "CaseExpression";
			public const string CaseExpressionElseBranch = "CaseExpressionElseBranch";
			public const string ChainedCondition = "ChainedCondition";
			public const string ColumnAlias = "ColumnAlias";
			public const string ColumnChainedList = "ColumnChainedList";
			public const string ColumnReference = "ColumnReference";
			public const string ConcatenatedSubquery = "ConcatenatedSubquery";
			public const string Condition = "Condition";
			public const string DistinctModifier = "DistinctModifier";
			public const string EscapeClause = "EscapeClause";
			public const string Expression = "Expression";
			public const string ExpressionCommaChainedList = "ExpressionCommaChainedList";
			public const string ExpressionList = "ExpressionList";
			public const string ExpressionListOrNestedQuery = "ExpressionListOrNestedQuery";
			public const string ExpressionMathOperatorChainedList = "ExpressionMathOperatorChainedList";
			public const string FlashbackAsOfClause = "FlashbackAsOfClause";
			public const string FlashbackMaximumValue = "FlashbackMaximumValue";
			public const string FlashbackMinimumValue = "FlashbackMinimumValue";
			public const string FlashbackQueryClause = "FlashbackQueryClause";
			public const string FlashbackVersionsClause = "FlashbackVersionsClause";
			public const string ForUpdateClause = "ForUpdateClause";
			public const string ForUpdateLockingClause = "ForUpdateLockingClause";
			public const string ForUpdateWaitClause = "ForUpdateWaitClause";
			public const string FromClause = "FromClause";
			public const string FromClauseChained = "FromClauseChained";
			public const string GroupByClause = "GroupByClause";
			public const string GroupingClause = "GroupingClause";
			public const string GroupingClauseChained = "GroupingClauseChained";
			public const string GroupingExpressionList = "GroupingExpressionList";
			public const string GroupingExpressionListChained = "GroupingExpressionListChained";
			public const string GroupingExpressionListOrNestedQuery = "GroupingExpressionListOrNestedQuery";
			public const string GroupingExpressionListOrRollupCubeClause = "GroupingExpressionListOrRollupCubeClause";
			public const string GroupingExpressionListOrRollupCubeClauseChained = "GroupingExpressionListOrRollupCubeClauseChained";
			public const string GroupingSetsClause = "GroupingSetsClause";
			public const string HavingClause = "HavingClause";
			public const string HierarchicalQueryClause = "HierarchicalQueryClause";
			public const string HierarchicalQueryConnectByClause = "HierarchicalQueryConnectByClause";
			public const string HierarchicalQueryStartWithClause = "HierarchicalQueryStartWithClause";
			public const string InnerJoinClause = "InnerJoinClause";
			public const string JoinClause = "JoinClause";
			public const string JoinColumnsOrCondition = "JoinColumnsOrCondition";
			public const string LikeOperator = "LikeOperator";
			public const string LogicalOperator = "LogicalOperator";
			public const string MathOperator = "MathOperator";
			public const string NaturalOrOuterJoinType = "NaturalOrOuterJoinType";
			public const string NestedQuery = "NestedQuery";
			public const string NullNaNOrInfinite = "NullNaNOrInfinite";
			public const string NullsClause = "NullsClause";
			public const string OrderByClause = "OrderByClause";
			public const string OrderExpression = "OrderExpression";
			public const string OrderExpressionChained = "OrderExpressionChained";
			public const string OrderExpressionType = "OrderExpressionType";
			public const string OuterJoinClause = "OuterJoinClause";
			public const string OuterJoinOld = "OuterJoinOld";
			public const string OuterJoinPartitionClause = "OuterJoinPartitionClause";
			public const string OuterJoinType = "OuterJoinType";
			public const string OuterJoinTypeWithKeyword = "OuterJoinTypeWithKeyword";
			public const string ParenthesisEnclosedExpressionList = "ParenthesisEnclosedExpressionList";
			public const string ParenthesisEnclosedGroupingExpressionList = "ParenthesisEnclosedGroupingExpressionList";
			public const string PartitionOrDatabaseLink = "PartitionOrDatabaseLink";
			public const string QueryBlock = "QueryBlock";
			public const string QueryTableExpression = "QueryTableExpression";
			public const string RelationalEquiOperator = "RelationalEquiOperator";
			public const string RelationalNonEquiOperator = "RelationalNonEquiOperator";
			public const string RelationalOperator = "RelationalOperator";
			public const string RollupCubeClause = "RollupCubeClause";
			public const string SampleClause = "SampleClause";
			public const string SchemaObject = "SchemaObject";
			public const string SearchedCaseExpressionBranch = "SearchedCaseExpressionBranch";
			public const string SeedClause = "SeedClause";
			public const string SelectExpressionExpressionChainedList = "SelectExpressionExpressionChainedList";
			public const string SelectList = "SelectList";
			public const string SelectListSchemaItem = "SelectListSchemaItem";
			public const string SetOperation = "SetOperation";
			public const string SetQualifier = "SetQualifier";
			public const string SimpleCaseExpressionBranch = "SimpleCaseExpressionBranch";
			public const string SortOrder = "SortOrder";
			public const string Subquery = "Subquery";
			public const string SubqueryComponent = "SubqueryComponent";
			public const string SubqueryComponentChained = "SubqueryComponentChained";
			public const string SubqueryFactoringClause = "SubqueryFactoringClause";
			public const string SystemChangeNumberOrTimestamp = "SystemChangeNumberOrTimestamp";
			public const string TableCollectionExpression = "TableCollectionExpression";
			public const string TableReference = "TableReference";
			public const string WhereClause = "WhereClause";
		}

		public static class Terminals
		{
			public const string Alias = "Alias";
			public const string All = "All";
			public const string And = "And";
			public const string Any = "Any";
			public const string As = "As";
			public const string Asc = "Asc";
			public const string Asterisk = "Asterisk";
			public const string At = "At";
			public const string Between = "Between";
			public const string Block = "Block";
			public const string By = "By";
			public const string Case = "Case";
			public const string Check = "Check";
			public const string Comma = "Comma";
			public const string Connect = "Connect";
			public const string Constraint = "Constraint";
			public const string Cross = "Cross";
			public const string Cube = "Cube";
			public const string Date = "Date";
			public const string Desc = "Desc";
			public const string Distinct = "Distinct";
			public const string Dot = "Dot";
			public const string Else = "Else";
			public const string End = "End";
			public const string Escape = "Escape";
			public const string Exclude = "Exclude";
			public const string Exists = "Exists";
			public const string First = "First";
			public const string For = "For";
			public const string From = "From";
			public const string Full = "Full";
			public const string Group = "Group";
			public const string Grouping = "Grouping";
			public const string Having = "Having";
			public const string Identifier = "Identifier";
			public const string Ignore = "Ignore";
			public const string In = "In";
			public const string Include = "Include";
			public const string Inner = "Inner";
			public const string IntegerLiteral = "IntegerLiteral";
			public const string Intersect = "Intersect";
			public const string Is = "Is";
			public const string Join = "Join";
			public const string Last = "Last";
			public const string Left = "Left";
			public const string LeftParenthesis = "LeftParenthesis";
			public const string Like = "Like";
			public const string LikeUcs2 = "LikeUcs2";
			public const string LikeUcs4 = "LikeUcs4";
			public const string LikeUnicode = "LikeUnicode";
			public const string Locked = "Locked";
			public const string MathDivide = "MathDivide";
			public const string MathEquals = "MathEquals";
			public const string MathFactor = "MathFactor";
			public const string MathGreatherThan = "MathGreatherThan";
			public const string MathGreatherThanOrEquals = "MathGreatherThanOrEquals";
			public const string MathInfinite = "MathInfinite";
			public const string MathLessThan = "MathLessThan";
			public const string MathLessThanOrEquals = "MathLessThanOrEquals";
			public const string MathMinus = "MathMinus";
			public const string MathNotANumber = "MathNotANumber";
			public const string MathNotEqualsC = "MathNotEqualsC";
			public const string MathNotEqualsCircumflex = "MathNotEqualsCircumflex";
			public const string MathNotEqualsSql = "MathNotEqualsSql";
			public const string MathPlus = "MathPlus";
			public const string MaximumValue = "MaximumValue";
			public const string MinimumValue = "MinimumValue";
			public const string Model = "Model";
			public const string Natural = "Natural";
			public const string NoCycle = "NoCycle";
			public const string Not = "Not";
			public const string Nowait = "Nowait";
			public const string Null = "Null";
			public const string Nulls = "Nulls";
			public const string NumberLiteral = "NumberLiteral";
			public const string Of = "Of";
			public const string On = "On";
			public const string Only = "Only";
			public const string OperatorConcatenation = "OperatorConcatenation";
			public const string Option = "Option";
			public const string Or = "Or";
			public const string Order = "Order";
			public const string Outer = "Outer";
			public const string Over = "Over";
			public const string Partition = "Partition";
			public const string Pivot = "Pivot";
			public const string Read = "Read";
			public const string Right = "Right";
			public const string RightParenthesis = "RightParenthesis";
			public const string Rollup = "Rollup";
			public const string RowIdPseudoColumn = "RowIdPseudoColumn";
			public const string RowNumberPseudoColumn = "RowNumberPseudoColumn";
			public const string Sample = "Sample";
			public const string Seed = "Seed";
			public const string Select = "Select";
			public const string Semicolon = "Semicolon";
			public const string SequenceCurrentValue = "SequenceCurrentValue";
			public const string SequenceNextValue = "SequenceNextValue";
			public const string Set = "Set";
			public const string SetMinus = "SetMinus";
			public const string Sets = "Sets";
			public const string Siblings = "Siblings";
			public const string Skip = "Skip";
			public const string Some = "Some";
			public const string Space = "Space";
			public const string Start = "Start";
			public const string StringLiteral = "StringLiteral";
			public const string Subpartition = "Subpartition";
			public const string SystemChangeNumber = "SystemChangeNumber";
			public const string Table = "Table";
			public const string Then = "Then";
			public const string Timestamp = "Timestamp";
			public const string Union = "Union";
			public const string Unique = "Unique";
			public const string Unpivot = "Unpivot";
			public const string Update = "Update";
			public const string Using = "Using";
			public const string Versions = "Versions";
			public const string Wait = "Wait";
			public const string When = "When";
			public const string Where = "Where";
			public const string With = "With";
			public const string Xml = "Xml";
		}
	}
}
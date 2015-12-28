﻿using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using SqlPad.Oracle.DataDictionary;
using SqlPad.Oracle.SemanticModel;

namespace SqlPad.Oracle.Test
{
	[TestFixture]
	public class OraclePlSqlStatementSemanticModelTest
	{
		[Test]
		public void TestInitializationNullStatement()
		{
			Assert.Throws<ArgumentNullException>(() => new OraclePlSqlStatementSemanticModel(null, null, TestFixture.DatabaseModel));
		}

		[Test]
		public void TestInitializationWithNonPlSqlStatement()
		{
			const string sqlText = @"SELECT * FROM DUAL";
			var statement = (OracleStatement)OracleSqlParser.Instance.Parse(sqlText).Single();

			Assert.Throws<ArgumentException>(() => new OraclePlSqlStatementSemanticModel(sqlText, statement, TestFixture.DatabaseModel));
		}

		[Test]
		public void TestBasicInitialization()
		{
			const string plsqlText =
@"CREATE OR REPLACE FUNCTION TEST_FUNCTION(p1 IN NUMBER DEFAULT 0, p2 IN OUT VARCHAR2, p3 OUT NOCOPY CLOB) RETURN RAW
IS
	TYPE test_type1 IS RECORD (attribute1 NUMBER, attribute2 VARCHAR2(255));
	TYPE test_table_type1 IS TABLE OF test_type;
	
	test_variable1 NUMBER;
	test_variable2 VARCHAR2(100);
	test_constant1 CONSTANT VARCHAR2(30) := 'Constant 1';

	PROCEDURE TEST_INNER_PROCEDURE(p1 test_type)
	IS
	BEGIN
		NULL;
	END;

	FUNCTION TEST_INNER_FUNCTION(p1 test_table_type) RETURN NUMBER
	IS
		test_exception1 EXCEPTION;

		PROCEDURE TEST_NESTED_PROCEDURE(p1 VARCHAR2)
		IS
		BEGIN
			SELECT dummy INTO x FROM DUAL;
		END;
	BEGIN
		SELECT dummy INTO x FROM DUAL;
	END;
BEGIN
	dbms_output.put_line(item => test_constant1);
	SELECT COUNT(*) INTO test_variable1 FROM DUAL;
	SELECT NULL, 'String value' INTO test_variable1, test_variable2 FROM DUAL;
END;";

			var statement = (OracleStatement)OracleSqlParser.Instance.Parse(plsqlText).Single();
			statement.ParseStatus.ShouldBe(ParseStatus.Success);

			var expectedObjectIdentifier = OracleObjectIdentifier.Create("HUSQVIK", "TEST_FUNCTION");

			var semanticModel = new OraclePlSqlStatementSemanticModel(plsqlText, statement, TestFixture.DatabaseModel).Build(CancellationToken.None);
			semanticModel.Programs.Count.ShouldBe(1);
			var mainProgram = semanticModel.Programs[0];
			mainProgram.ObjectIdentifier.ShouldBe(expectedObjectIdentifier);
			mainProgram.Name.ShouldBe("\"TEST_FUNCTION\"");
			mainProgram.Parameters.Count.ShouldBe(3);
			mainProgram.Parameters[0].Name.ShouldBe("\"P1\"");
			mainProgram.Parameters[0].Direction.ShouldBe(ParameterDirection.Input);
			mainProgram.Parameters[1].Name.ShouldBe("\"P2\"");
			mainProgram.Parameters[1].Direction.ShouldBe(ParameterDirection.InputOutput);
			mainProgram.Parameters[2].Name.ShouldBe("\"P3\"");
			mainProgram.Parameters[2].Direction.ShouldBe(ParameterDirection.Output);
			mainProgram.ReturnParameter.ShouldNotBe(null);
			mainProgram.Variables.Count.ShouldBe(3);
			mainProgram.Variables[0].Name.ShouldBe("\"TEST_VARIABLE1\"");
			mainProgram.Variables[0].IsConstant.ShouldBe(false);
			mainProgram.Variables[0].IsException.ShouldBe(false);
			mainProgram.Variables[1].Name.ShouldBe("\"TEST_VARIABLE2\"");
			mainProgram.Variables[1].IsConstant.ShouldBe(false);
			mainProgram.Variables[1].IsException.ShouldBe(false);
			mainProgram.Variables[2].Name.ShouldBe("\"TEST_CONSTANT1\"");
			mainProgram.Variables[2].IsConstant.ShouldBe(true);
			mainProgram.Variables[2].IsException.ShouldBe(false);
			mainProgram.Types.Count.ShouldBe(2);
			mainProgram.Types[0].Name.ShouldBe("\"TEST_TYPE1\"");
			mainProgram.Types[1].Name.ShouldBe("\"TEST_TABLE_TYPE1\"");
			mainProgram.SubPrograms.Count.ShouldBe(2);
			mainProgram.ChildModels.Count.ShouldBe(2);

			mainProgram.ProgramReferences.Count.ShouldBe(1);
			var programReference = mainProgram.ProgramReferences.First();
			programReference.Name.ShouldBe("put_line");
			programReference.ObjectNode.Token.Value.ShouldBe("dbms_output");
			programReference.ParameterListNode.ShouldNotBe(null);
			programReference.ParameterReferences.Count.ShouldBe(1);
			programReference.ParameterReferences[0].OptionalIdentifierTerminal.Token.Value.ShouldBe("item");
			programReference.ParameterReferences[0].ParameterNode.LastTerminalNode.Token.Value.ShouldBe("test_constant1");

			mainProgram.SubPrograms[0].ObjectIdentifier.ShouldBe(expectedObjectIdentifier);
			mainProgram.SubPrograms[0].Name.ShouldBe("\"TEST_INNER_PROCEDURE\"");
			mainProgram.SubPrograms[0].Parameters.Count.ShouldBe(1);
			mainProgram.SubPrograms[0].Parameters[0].Name.ShouldBe("\"P1\"");
			mainProgram.SubPrograms[0].Parameters[0].Direction.ShouldBe(ParameterDirection.Input);
			mainProgram.SubPrograms[0].ReturnParameter.ShouldBe(null);
			mainProgram.SubPrograms[0].Variables.Count.ShouldBe(0);
			mainProgram.SubPrograms[0].Types.Count.ShouldBe(0);
			mainProgram.SubPrograms[0].SubPrograms.Count.ShouldBe(0);
			mainProgram.SubPrograms[0].ChildModels.Count.ShouldBe(0);

			mainProgram.SubPrograms[1].ObjectIdentifier.ShouldBe(expectedObjectIdentifier);
			mainProgram.SubPrograms[1].Name.ShouldBe("\"TEST_INNER_FUNCTION\"");
			mainProgram.SubPrograms[1].Parameters.Count.ShouldBe(1);
			mainProgram.SubPrograms[1].Parameters[0].Name.ShouldBe("\"P1\"");
			mainProgram.SubPrograms[1].Parameters[0].Direction.ShouldBe(ParameterDirection.Input);
			mainProgram.SubPrograms[1].ReturnParameter.ShouldNotBe(null);
			mainProgram.SubPrograms[1].Variables.Count.ShouldBe(1);
			mainProgram.SubPrograms[1].Variables[0].Name.ShouldBe("\"TEST_EXCEPTION1\"");
			mainProgram.SubPrograms[1].Variables[0].IsConstant.ShouldBe(false);
			mainProgram.SubPrograms[1].Variables[0].IsException.ShouldBe(true);
			mainProgram.SubPrograms[1].Types.Count.ShouldBe(0);
			mainProgram.SubPrograms[1].SubPrograms.Count.ShouldBe(1);
			mainProgram.SubPrograms[1].ChildModels.Count.ShouldBe(1);

			mainProgram.SubPrograms[1].SubPrograms[0].ObjectIdentifier.ShouldBe(expectedObjectIdentifier);
			mainProgram.SubPrograms[1].SubPrograms[0].Name.ShouldBe("\"TEST_NESTED_PROCEDURE\"");
			mainProgram.SubPrograms[1].SubPrograms[0].Parameters.Count.ShouldBe(1);
			mainProgram.SubPrograms[1].SubPrograms[0].Parameters[0].Name.ShouldBe("\"P1\"");
			mainProgram.SubPrograms[1].SubPrograms[0].Parameters[0].Direction.ShouldBe(ParameterDirection.Input);
			mainProgram.SubPrograms[1].SubPrograms[0].ReturnParameter.ShouldBe(null);
			mainProgram.SubPrograms[1].SubPrograms[0].Variables.Count.ShouldBe(0);
			mainProgram.SubPrograms[1].SubPrograms[0].Types.Count.ShouldBe(0);
			mainProgram.SubPrograms[1].SubPrograms[0].SubPrograms.Count.ShouldBe(0);
			mainProgram.SubPrograms[1].SubPrograms[0].ChildModels.Count.ShouldBe(1);
		}

		[Test]
		public void TestInitializationWithAnonymousBlock()
		{
			const string plsqlText =
@"DECLARE
	TYPE test_type1 IS RECORD (attribute1 NUMBER, attribute2 VARCHAR2(255));
	TYPE test_table_type1 IS TABLE OF test_type;
	
	test_variable1 NUMBER;
	test_variable2 VARCHAR2(100);
	test_constant1 CONSTANT VARCHAR2(30) := 'Constant 1';

	PROCEDURE TEST_INNER_PROCEDURE(p1 test_type)
	IS
	BEGIN
		NULL;
	END;

	FUNCTION TEST_INNER_FUNCTION(p1 test_table_type) RETURN NUMBER
	IS
		test_exception1 EXCEPTION;

		PROCEDURE TEST_NESTED_PROCEDURE(p1 VARCHAR2)
		IS
		BEGIN
			SELECT dummy INTO x FROM DUAL;
		END;
	BEGIN
		SELECT dummy INTO x FROM DUAL;
	END;
BEGIN
	SELECT COUNT(*) INTO test_variable1 FROM DUAL;
	SELECT NULL, 'String value' INTO test_variable1, test_variable2 FROM DUAL;
END;";

			var statement = (OracleStatement)OracleSqlParser.Instance.Parse(plsqlText).Single();
			statement.ParseStatus.ShouldBe(ParseStatus.Success);

			var semanticModel = new OraclePlSqlStatementSemanticModel(plsqlText, statement, TestFixture.DatabaseModel).Build(CancellationToken.None);
			semanticModel.Programs.Count.ShouldBe(1);
			var mainProgram = semanticModel.Programs[0];
			mainProgram.ObjectIdentifier.ShouldBe(OracleObjectIdentifier.Empty);
			mainProgram.RootNode.ShouldBe(statement.RootNode);
			mainProgram.Variables.Count.ShouldBe(3);
			mainProgram.Variables[0].Name.ShouldBe("\"TEST_VARIABLE1\"");
			mainProgram.Variables[0].IsConstant.ShouldBe(false);
			mainProgram.Variables[0].IsException.ShouldBe(false);
			mainProgram.Variables[1].Name.ShouldBe("\"TEST_VARIABLE2\"");
			mainProgram.Variables[1].IsConstant.ShouldBe(false);
			mainProgram.Variables[1].IsException.ShouldBe(false);
			mainProgram.Variables[2].Name.ShouldBe("\"TEST_CONSTANT1\"");
			mainProgram.Variables[2].IsConstant.ShouldBe(true);
			mainProgram.Variables[2].IsException.ShouldBe(false);
			mainProgram.Types.Count.ShouldBe(2);
			mainProgram.Types[0].Name.ShouldBe("\"TEST_TYPE1\"");
			mainProgram.Types[1].Name.ShouldBe("\"TEST_TABLE_TYPE1\"");
			mainProgram.SubPrograms.Count.ShouldBe(2);
			mainProgram.ChildModels.Count.ShouldBe(2);

			mainProgram.SubPrograms[0].Name.ShouldBe("\"TEST_INNER_PROCEDURE\"");
			mainProgram.SubPrograms[0].Parameters.Count.ShouldBe(1);
			mainProgram.SubPrograms[0].Parameters[0].Name.ShouldBe("\"P1\"");
			mainProgram.SubPrograms[0].Parameters[0].Direction.ShouldBe(ParameterDirection.Input);
			mainProgram.SubPrograms[0].ReturnParameter.ShouldBe(null);
			mainProgram.SubPrograms[0].Variables.Count.ShouldBe(0);
			mainProgram.SubPrograms[0].Types.Count.ShouldBe(0);
			mainProgram.SubPrograms[0].SubPrograms.Count.ShouldBe(0);
			mainProgram.SubPrograms[0].ChildModels.Count.ShouldBe(0);

			mainProgram.SubPrograms[1].Name.ShouldBe("\"TEST_INNER_FUNCTION\"");
			mainProgram.SubPrograms[1].Parameters.Count.ShouldBe(1);
			mainProgram.SubPrograms[1].Parameters[0].Name.ShouldBe("\"P1\"");
			mainProgram.SubPrograms[1].Parameters[0].Direction.ShouldBe(ParameterDirection.Input);
			mainProgram.SubPrograms[1].ReturnParameter.ShouldNotBe(null);
			mainProgram.SubPrograms[1].Variables.Count.ShouldBe(1);
			mainProgram.SubPrograms[1].Variables[0].Name.ShouldBe("\"TEST_EXCEPTION1\"");
			mainProgram.SubPrograms[1].Variables[0].IsConstant.ShouldBe(false);
			mainProgram.SubPrograms[1].Variables[0].IsException.ShouldBe(true);
			mainProgram.SubPrograms[1].Types.Count.ShouldBe(0);
			mainProgram.SubPrograms[1].SubPrograms.Count.ShouldBe(1);
			mainProgram.SubPrograms[1].ChildModels.Count.ShouldBe(1);

			mainProgram.SubPrograms[1].SubPrograms[0].Name.ShouldBe("\"TEST_NESTED_PROCEDURE\"");
			mainProgram.SubPrograms[1].SubPrograms[0].Parameters.Count.ShouldBe(1);
			mainProgram.SubPrograms[1].SubPrograms[0].Parameters[0].Name.ShouldBe("\"P1\"");
			mainProgram.SubPrograms[1].SubPrograms[0].Parameters[0].Direction.ShouldBe(ParameterDirection.Input);
			mainProgram.SubPrograms[1].SubPrograms[0].ReturnParameter.ShouldBe(null);
			mainProgram.SubPrograms[1].SubPrograms[0].Variables.Count.ShouldBe(0);
			mainProgram.SubPrograms[1].SubPrograms[0].Types.Count.ShouldBe(0);
			mainProgram.SubPrograms[1].SubPrograms[0].SubPrograms.Count.ShouldBe(0);
			mainProgram.SubPrograms[1].SubPrograms[0].ChildModels.Count.ShouldBe(1);
		}

		[Test]
		public void TestInitializationNestedAnonymousBlock()
		{
			const string plsqlText =
@"BEGIN
	DECLARE
		n NUMBER;
	BEGIN
		SELECT NULL INTO n FROM DUAL;
	END;
END;";

			var statement = (OracleStatement)OracleSqlParser.Instance.Parse(plsqlText).Single();
			statement.ParseStatus.ShouldBe(ParseStatus.Success);

			var semanticModel = new OraclePlSqlStatementSemanticModel(plsqlText, statement, TestFixture.DatabaseModel).Build(CancellationToken.None);
			semanticModel.Programs.Count.ShouldBe(1);
			var mainProgram = semanticModel.Programs[0];
			mainProgram.ObjectIdentifier.ShouldBe(OracleObjectIdentifier.Empty);
			mainProgram.RootNode.ShouldBe(statement.RootNode);
			mainProgram.Variables.Count.ShouldBe(0);
			mainProgram.SubPrograms.Count.ShouldBe(1);
			mainProgram.SubPrograms[0].ObjectIdentifier.ShouldBe(OracleObjectIdentifier.Empty);
			mainProgram.SubPrograms[0].RootNode.SourcePosition.IndexStart.ShouldBe(8);
			mainProgram.SubPrograms[0].RootNode.SourcePosition.Length.ShouldBe(68);
			mainProgram.SubPrograms[0].Variables.Count.ShouldBe(1);
			mainProgram.SubPrograms[0].Variables[0].Name.ShouldBe("\"N\"");
			mainProgram.SubPrograms[0].ChildModels.Count.ShouldBe(1);
		}

		[Test]
		public void TestSelectListRedundancy()
		{
			const string plsqlText =
@"DECLARE
	c1 NUMBER;
	c2 NUMBER;
BEGIN
	SELECT NULL, NULL INTO c1, c2 FROM DUAL;
END;";

			var statement = (OracleStatement)OracleSqlParser.Instance.Parse(plsqlText).Single();
			statement.ParseStatus.ShouldBe(ParseStatus.Success);

			var semanticModel = new OraclePlSqlStatementSemanticModel(plsqlText, statement, TestFixture.DatabaseModel).Build(CancellationToken.None);
			semanticModel.RedundantSymbolGroups.Count.ShouldBe(0);

			semanticModel.Programs.Count.ShouldBe(1);
			var mainProgram = semanticModel.Programs[0];
			mainProgram.ChildModels.Count.ShouldBe(1);
			mainProgram.ChildModels[0].RedundantSymbolGroups.Count.ShouldBe(0);
		}

		[Test]
		public void TestLocalReferences()
		{
			const string plsqlText =
@"CREATE OR REPLACE PROCEDURE test_procedure(test_parameter1 IN VARCHAR2)
IS
    test_variable1 VARCHAR2(255);
BEGIN
    test_variable1 := test_variable1 || test_parameter1 || test_parameter2;
END;";

			var statement = (OracleStatement)OracleSqlParser.Instance.Parse(plsqlText).Single();
			statement.ParseStatus.ShouldBe(ParseStatus.Success);

			var semanticModel = new OraclePlSqlStatementSemanticModel(plsqlText, statement, TestFixture.DatabaseModel).Build(CancellationToken.None);

			semanticModel.Programs.Count.ShouldBe(1);
			var mainProgram = semanticModel.Programs[0];
			mainProgram.ColumnReferences.Count.ShouldBe(0);
			mainProgram.PlSqlVariableReferences.Count.ShouldBe(4);
			var localReferences = mainProgram.PlSqlVariableReferences.OrderBy(r => r.IdentifierNode.SourcePosition.IndexStart).ToArray();
			localReferences[0].Variables.Count.ShouldBe(1);
			localReferences[1].Variables.Count.ShouldBe(1);
			localReferences[2].Variables.Count.ShouldBe(1);
			localReferences[3].Variables.Count.ShouldBe(0);
		}
	}
}

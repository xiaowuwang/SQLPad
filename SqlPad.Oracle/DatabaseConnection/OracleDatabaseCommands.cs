﻿using System;
using System.Linq;
using SqlPad.Oracle.DataDictionary;

namespace SqlPad.Oracle.DatabaseConnection
{
	internal static class OracleDatabaseCommands
	{
		public const string SelectBuiltInFunctionMetadataCommandText =
@"WITH PROCEDURES AS (
	SELECT
		ALL_PROCEDURES.OWNER,
        ALL_PROCEDURES.OBJECT_NAME PACKAGE_NAME,
        PROCEDURE_NAME FUNCTION_NAME,
		NVL(TO_NUMBER(ALL_PROCEDURES.OVERLOAD), 0) OVERLOAD,
		CASE
			WHEN PROCEDURE_NAME IN ('SYSTIMESTAMP', 'LOCALTIMESTAMP', 'CURRENT_TIMESTAMP') THEN 1
		END MAXARGS,
        AGGREGATE,
        PIPELINED,
        PARALLEL,
        DETERMINISTIC,
        AUTHID
    FROM
        ALL_PROCEDURES
        JOIN ALL_ARGUMENTS ON
        	ALL_PROCEDURES.OBJECT_ID = ALL_ARGUMENTS.OBJECT_ID AND PROCEDURE_NAME = ALL_ARGUMENTS.OBJECT_NAME AND
			POSITION = 0 AND ARGUMENT_NAME IS NULL AND DATA_LEVEL = 0 AND SEQUENCE = 1 AND NVL(ALL_ARGUMENTS.OVERLOAD, 0) = NVL(ALL_PROCEDURES.OVERLOAD, 0)
    WHERE
        ALL_PROCEDURES.OWNER = 'SYS' AND ALL_PROCEDURES.OBJECT_NAME = 'STANDARD' AND PROCEDURE_NAME NOT LIKE '%SYS$%'
),
SQL_FUNCTION_METADATA AS (
	SELECT
		FUNCTION_ID,
		FUNCTION_NAME,
		ROW_NUMBER() OVER (PARTITION BY FUNCTION_NAME ORDER BY ROWNUM) - 1 OVERLOAD,
		COUNT(*) OVER (PARTITION BY FUNCTION_NAME) TOTAL_OVERLOADS,
		ANALYTIC,
		AGGREGATE,
		OFFLOADABLE,
		MINARGS,
		MAXARGS,
		DISP_TYPE
	FROM (
		SELECT
			MAX(FUNC_ID) KEEP (DENSE_RANK LAST ORDER BY OFFLOADABLE, FUNC_ID) FUNCTION_ID,
			CASE WHEN NAME LIKE 'OPTXML%' THEN SUBSTR(NAME, 4) ELSE NAME END FUNCTION_NAME,
			ANALYTIC,
			AGGREGATE,
			MAX(OFFLOADABLE) OFFLOADABLE,
			CASE
				WHEN NAME = 'CV' THEN 0
				WHEN NAME IN ('LAG', 'LEAD') THEN 1 -- incorrect metadata
				WHEN NAME IN ('JSON_QUERY', 'JSON_EXISTS', 'JSON_VALUE') THEN 2 -- incorrect metadata
				ELSE MINARGS
			END MINARGS,
			MAXARGS,
			DISP_TYPE
		FROM
			V$SQLFN_METADATA
		WHERE
			DISP_TYPE NOT IN ('REL-OP', 'ARITHMATIC') AND
			NAME NOT IN ('TRUNC', 'TO_CHAR') AND
			(NAME LIKE 'OPTXML%' OR NAME NOT LIKE 'OPT%')
		GROUP BY
			NAME,
			ANALYTIC,
			AGGREGATE,
			MINARGS,
			MAXARGS,
			DISP_TYPE
		)
)
SELECT
	CASE WHEN PROCEDURES.FUNCTION_NAME IS NULL THEN FUNCTION_ID END FUNCTION_ID,
    OWNER,
    PACKAGE_NAME,
    NVL(SQL_FUNCTION_METADATA.FUNCTION_NAME, PROCEDURES.FUNCTION_NAME) PROGRAM_NAME,
	1 IS_FUNCTION,
	NVL(PROCEDURES.OVERLOAD, SQL_FUNCTION_METADATA.OVERLOAD) OVERLOAD,
    NVL(ANALYTIC, PROCEDURES.AGGREGATE) ANALYTIC,
    NVL(SQL_FUNCTION_METADATA.AGGREGATE, PROCEDURES.AGGREGATE) AGGREGATE,
    NVL(PIPELINED, 'NO') PIPELINED,
    NVL(OFFLOADABLE, 'NO') OFFLOADABLE,
    NVL(PARALLEL, 'NO') PARALLEL,
    NVL(DETERMINISTIC, 'NO') DETERMINISTIC,
    MINARGS,
    NVL(SQL_FUNCTION_METADATA.MAXARGS, PROCEDURES.MAXARGS) MAXARGS,
    NVL(AUTHID, 'CURRENT_USER') AUTHID,
    NVL(DISP_TYPE, 'NORMAL') DISP_TYPE
FROM
	PROCEDURES
	FULL JOIN SQL_FUNCTION_METADATA
		ON PROCEDURES.FUNCTION_NAME = SQL_FUNCTION_METADATA.FUNCTION_NAME AND (PROCEDURES.OVERLOAD = SQL_FUNCTION_METADATA.OVERLOAD OR TOTAL_OVERLOADS = 1) 
ORDER BY
    PROGRAM_NAME";

		public const string SelectBuiltInFunctionParameterMetadataCommandText =
@"SELECT
	OWNER,
    PACKAGE_NAME,
	OBJECT_NAME PROGRAM_NAME,
	NVL(OVERLOAD, 0) OVERLOAD,
	ARGUMENT_NAME,
	POSITION,
	SEQUENCE,
	DATA_LEVEL,
	DATA_TYPE,
	TYPE_OWNER,
	TYPE_NAME,
	DEFAULTED,
	IN_OUT
FROM
	ALL_ARGUMENTS
WHERE
	OWNER = 'SYS'
	AND PACKAGE_NAME = 'STANDARD'
	AND OBJECT_NAME NOT LIKE '%SYS$%'
	AND DATA_TYPE IS NOT NULL
ORDER BY
	POSITION,
    SEQUENCE";

		public const string SelectUserFunctionMetadataCommandText =
@"SELECT
	NULL FUNCTION_ID,
    OWNER,
    PACKAGE_NAME,
    PROGRAM_NAME,
    IS_FUNCTION,
	NVL(OVERLOAD, 0) OVERLOAD,
    AGGREGATE ANALYTIC,
    AGGREGATE,
    PIPELINED,
    'NO' OFFLOADABLE,
    PARALLEL,
    DETERMINISTIC,
    NULL MINARGS,
    NULL MAXARGS,
    AUTHID,
    'NORMAL' DISP_TYPE
FROM (
	SELECT DISTINCT
        OWNER,
        CASE WHEN OBJECT_TYPE = 'PACKAGE' THEN ALL_PROCEDURES.OBJECT_NAME END PACKAGE_NAME,
        CASE WHEN OBJECT_TYPE <> 'PACKAGE' THEN ALL_PROCEDURES.OBJECT_NAME ELSE PROCEDURE_NAME END PROGRAM_NAME,
        NVL2(RETURN_PARAMETERS.OBJECT_ID, 1, 0) IS_FUNCTION,
		ALL_PROCEDURES.OVERLOAD,
        AGGREGATE,
        PIPELINED,
        PARALLEL,
        DETERMINISTIC,
        AUTHID
    FROM
        ALL_PROCEDURES
        LEFT JOIN (
        	SELECT OBJECT_ID, OBJECT_NAME, NVL(OVERLOAD, 0) OVERLOAD FROM ALL_ARGUMENTS WHERE POSITION = 0 AND ARGUMENT_NAME IS NULL
        ) RETURN_PARAMETERS ON ALL_PROCEDURES.OBJECT_ID = RETURN_PARAMETERS.OBJECT_ID AND NVL(PROCEDURE_NAME, ALL_PROCEDURES.OBJECT_NAME) = RETURN_PARAMETERS.OBJECT_NAME AND NVL(ALL_PROCEDURES.OVERLOAD, 0) = RETURN_PARAMETERS.OVERLOAD
    WHERE
        NOT (OWNER = 'SYS' AND ALL_PROCEDURES.OBJECT_NAME = 'STANDARD') AND
        (OBJECT_TYPE IN ('FUNCTION', 'PROCEDURE') OR (OBJECT_TYPE = 'PACKAGE' AND PROCEDURE_NAME IS NOT NULL))
)
ORDER BY
	OWNER,
    PACKAGE_NAME,
    PROGRAM_NAME";

		public const string SelectSqlFunctionParameterMetadataCommandText = "SELECT FUNC_ID FUNCTION_ID, ARGNUM POSITION, DATATYPE FROM V$SQLFN_ARG_METADATA ORDER BY FUNC_ID, ARGNUM";

		public const string SelectUserFunctionParameterMetadataCommandText =
@"SELECT
    OWNER,
    PACKAGE_NAME,
    OBJECT_NAME PROGRAM_NAME,
    NVL(OVERLOAD, 0) OVERLOAD,
    ARGUMENT_NAME,
    POSITION,
	SEQUENCE,
	DATA_LEVEL,
    DATA_TYPE,
	TYPE_OWNER,
	TYPE_NAME,
    DEFAULTED,
    IN_OUT
FROM
    ALL_ARGUMENTS
WHERE
    DATA_LEVEL <= 1 AND DATA_TYPE <> 'PL/SQL RECORD' AND NOT (OWNER = 'SYS' AND OBJECT_NAME = 'STANDARD')
ORDER BY
    OWNER,
    PACKAGE_NAME,
	POSITION,
    SEQUENCE";

		public static readonly string SelectTypesCommandText =
		    $"SELECT OWNER, TYPE_NAME, TYPECODE, PREDEFINED, INCOMPLETE, FINAL, INSTANTIABLE, SUPERTYPE_OWNER, SUPERTYPE_NAME FROM SYS.ALL_TYPES WHERE TYPECODE IN ({ToInValueList(OracleTypeBase.TypeCodeObject, OracleTypeBase.TypeCodeCollection, OracleTypeBase.TypeCodeXml)})";

		public static readonly string SelectAllObjectsCommandText =
		    $"SELECT OWNER, OBJECT_NAME, SUBOBJECT_NAME, OBJECT_ID, DATA_OBJECT_ID, OBJECT_TYPE, CREATED, LAST_DDL_TIME, STATUS, TEMPORARY/*, EDITIONABLE, EDITION_NAME*/ FROM SYS.ALL_OBJECTS WHERE OBJECT_TYPE IN ({ToInValueList(OracleSchemaObjectType.Synonym, OracleSchemaObjectType.View, OracleSchemaObjectType.Table, OracleSchemaObjectType.Sequence, OracleSchemaObjectType.Function, OracleSchemaObjectType.Package, OracleSchemaObjectType.Type, OracleSchemaObjectType.Procedure)})";

		public const string SelectTablesCommandText =
@"SELECT OWNER, TABLE_NAME, TABLESPACE_NAME, CLUSTER_NAME, STATUS, LOGGING, NUM_ROWS, BLOCKS, AVG_ROW_LEN, DEGREE, CACHE, SAMPLE_SIZE, LAST_ANALYZED, TEMPORARY, NESTED, ROW_MOVEMENT, COMPRESS_FOR,
CASE
	WHEN TEMPORARY = 'N' AND TABLESPACE_NAME IS NULL AND PARTITIONED = 'NO' AND IOT_TYPE IS NULL AND PCT_FREE = 0 THEN 'External'
	WHEN IOT_TYPE = 'IOT' THEN 'Index'
	ELSE 'Heap'
END ORGANIZATION
FROM SYS.ALL_TABLES";

		public const string SelectAllSchemasCommandTextBase = "SELECT USERNAME, CREATED{0} FROM SYS.ALL_USERS";

		public const string SelectSynonymTargetsCommandText = "SELECT OWNER, SYNONYM_NAME, TABLE_OWNER, TABLE_NAME FROM SYS.ALL_SYNONYMS";

		public const string SelectTableColumnsCommandTextBase = "SELECT OWNER, TABLE_NAME, COLUMN_NAME, DATA_TYPE, DATA_TYPE_OWNER, DATA_LENGTH, CHAR_LENGTH, DATA_PRECISION, DATA_SCALE, CHAR_USED, VIRTUAL_COLUMN, DATA_DEFAULT, NULLABLE, COLUMN_ID, NUM_DISTINCT, LOW_VALUE, HIGH_VALUE, NUM_NULLS, NUM_BUCKETS, LAST_ANALYZED, SAMPLE_SIZE, AVG_COL_LEN, HISTOGRAM{0} FROM SYS.ALL_TAB_COLS ORDER BY OWNER, TABLE_NAME, COLUMN_ID";

		public const string SelectConstraintsCommandText = "SELECT OWNER, CONSTRAINT_NAME, CONSTRAINT_TYPE, TABLE_NAME, SEARCH_CONDITION, R_OWNER, R_CONSTRAINT_NAME, DELETE_RULE, STATUS, DEFERRABLE, VALIDATED, RELY, INDEX_OWNER, INDEX_NAME FROM SYS.ALL_CONSTRAINTS WHERE CONSTRAINT_TYPE IN ('C', 'R', 'P', 'U')";

		public const string SelectConstraintColumnsCommandText = "SELECT OWNER, CONSTRAINT_NAME, COLUMN_NAME, POSITION FROM SYS.ALL_CONS_COLUMNS ORDER BY OWNER, CONSTRAINT_NAME, POSITION";

		public const string SelectSequencesCommandText = "SELECT SEQUENCE_OWNER, SEQUENCE_NAME, MIN_VALUE, MAX_VALUE, INCREMENT_BY, CYCLE_FLAG, ORDER_FLAG, CACHE_SIZE, LAST_NUMBER FROM SYS.ALL_SEQUENCES";

		public const string SelectTypeAttributesCommandText = "SELECT OWNER, TYPE_NAME, ATTR_NAME, ATTR_TYPE_MOD, ATTR_TYPE_OWNER, ATTR_TYPE_NAME, LENGTH, PRECISION, SCALE, INHERITED, CHAR_USED FROM SYS.ALL_TYPE_ATTRS ORDER BY OWNER, TYPE_NAME, ATTR_NO";

		public const string SelectCollectionTypeAttributesCommandText = "SELECT OWNER, TYPE_NAME, COLL_TYPE, UPPER_BOUND, ELEM_TYPE_MOD, ELEM_TYPE_OWNER, ELEM_TYPE_NAME, LENGTH, PRECISION, SCALE, CHARACTER_SET_NAME, ELEM_STORAGE, NULLS_STORED, CHAR_USED FROM SYS.ALL_COLL_TYPES";

		public const string SelectObjectScriptCommandText = "SELECT SYS.DBMS_METADATA.GET_DDL(OBJECT_TYPE => :OBJECT_TYPE, NAME => :NAME, SCHEMA => :SCHEMA) SCRIPT FROM SYS.DUAL";

		public const string SelectDatabaseLinksCommandText = "SELECT OWNER, DB_LINK, USERNAME, HOST, CREATED FROM SYS.ALL_DB_LINKS";

		public const string SelectColumnStatisticsCommandText = "SELECT NUM_DISTINCT, LOW_VALUE, HIGH_VALUE, DENSITY, NUM_NULLS, NUM_BUCKETS, LAST_ANALYZED, SAMPLE_SIZE, AVG_COL_LEN, INITCAP(HISTOGRAM) HISTOGRAM FROM SYS.ALL_TAB_COL_STATISTICS WHERE OWNER = :OWNER AND TABLE_NAME = :TABLE_NAME AND COLUMN_NAME = :COLUMN_NAME";

		public const string SelectColumnCommentCommandText = "SELECT COMMENTS FROM SYS.ALL_COL_COMMENTS WHERE OWNER = :OWNER AND TABLE_NAME = :TABLE_NAME AND COLUMN_NAME = :COLUMN_NAME";

		public const string SelectColumnInMemoryDetailsCommandText = "SELECT INITCAP(INMEMORY_COMPRESSION) INMEMORY_COMPRESSION FROM V$IM_COLUMN_LEVEL WHERE OWNER = :OWNER AND TABLE_NAME = :TABLE_NAME AND COLUMN_NAME = :COLUMN_NAME";

		public const string SelectColumnHistogramCommandText = "SELECT ENDPOINT_NUMBER FROM SYS.ALL_TAB_HISTOGRAMS WHERE OWNER = :OWNER AND TABLE_NAME = :TABLE_NAME AND COLUMN_NAME = :COLUMN_NAME ORDER BY ENDPOINT_NUMBER";

		public const string SelectPartitionsCommandText = "SELECT TABLE_OWNER, TABLE_NAME, PARTITION_NAME, PARTITION_POSITION, HIGH_VALUE, TABLESPACE_NAME, LOGGING, COMPRESS_FOR, NUM_ROWS, BLOCKS, AVG_ROW_LEN, SAMPLE_SIZE, LAST_ANALYZED FROM ALL_TAB_PARTITIONS ORDER BY TABLE_OWNER, TABLE_NAME, PARTITION_POSITION";

		public const string SelectSubPartitionsCommandText = "SELECT TABLE_OWNER, TABLE_NAME, PARTITION_NAME, SUBPARTITION_NAME, SUBPARTITION_POSITION, HIGH_VALUE, TABLESPACE_NAME, LOGGING, COMPRESS_FOR, NUM_ROWS, BLOCKS, AVG_ROW_LEN, SAMPLE_SIZE, LAST_ANALYZED FROM ALL_TAB_SUBPARTITIONS ORDER BY TABLE_OWNER, TABLE_NAME, PARTITION_NAME, SUBPARTITION_POSITION";

		public const string SelectTablePartitionKeysCommandText = "SELECT OWNER TABLE_OWNER, NAME TABLE_NAME, COLUMN_NAME, COLUMN_POSITION FROM ALL_PART_KEY_COLUMNS WHERE OBJECT_TYPE = 'TABLE' ORDER BY COLUMN_POSITION";

		public const string SelectTableSubPartitionKeysCommandText = "SELECT OWNER TABLE_OWNER, NAME TABLE_NAME, COLUMN_NAME, COLUMN_POSITION FROM ALL_SUBPART_KEY_COLUMNS WHERE OBJECT_TYPE = 'TABLE' ORDER BY COLUMN_POSITION";

		public const string SelectTablePartitionDetailsCommandTextBase = "SELECT PARTITION_NAME, HIGH_VALUE, TABLESPACE_NAME, LOGGING, CASE WHEN COMPRESSION = 'ENABLED' THEN INITCAP(COMPRESS_FOR) ELSE NVL(INITCAP(COMPRESSION), 'N/A') END COMPRESSION, NUM_ROWS, BLOCKS, AVG_ROW_LEN, SAMPLE_SIZE, LAST_ANALYZED, {0} INMEMORY_COMPRESSION FROM ALL_TAB_PARTITIONS WHERE TABLE_OWNER = :TABLE_OWNER AND TABLE_NAME = :TABLE_NAME AND LNNVL(PARTITION_NAME <> :PARTITION_NAME) ORDER BY PARTITION_POSITION";

		public const string SelectTableSubPartitionsDetailsCommandTextBase = "SELECT PARTITION_NAME, SUBPARTITION_NAME, HIGH_VALUE, TABLESPACE_NAME, LOGGING, CASE WHEN COMPRESSION = 'ENABLED' THEN INITCAP(COMPRESS_FOR) ELSE NVL(INITCAP(COMPRESSION), 'N/A') END COMPRESSION, NUM_ROWS, BLOCKS, AVG_ROW_LEN, SAMPLE_SIZE, LAST_ANALYZED, {0} INMEMORY_COMPRESSION FROM ALL_TAB_SUBPARTITIONS WHERE TABLE_OWNER = :TABLE_OWNER AND TABLE_NAME = :TABLE_NAME AND LNNVL(PARTITION_NAME <> :PARTITION_NAME) AND LNNVL(SUBPARTITION_NAME <> :SUBPARTITION_NAME) ORDER BY PARTITION_NAME, SUBPARTITION_POSITION";

		public const string SelectTableCommentCommandText =
@"SELECT COMMENTS FROM SYS.ALL_MVIEW_COMMENTS WHERE COMMENTS IS NOT NULL AND OWNER = :OWNER AND MVIEW_NAME = :TABLE_NAME
UNION ALL
SELECT COMMENTS FROM SYS.ALL_TAB_COMMENTS WHERE COMMENTS IS NOT NULL AND OWNER = :OWNER AND TABLE_NAME = :TABLE_NAME";

		public const string SelectTableDetailsCommandText =
@"SELECT
	CASE 
        WHEN TABLESPACE_NAME IS NULL AND PARTITIONED = 'NO' AND IOT_TYPE IS NULL AND PCT_FREE = 0 THEN 'External'
        WHEN IOT_TYPE IS NOT NULL THEN 'Index'
        ELSE 'Heap'
    END ORGANIZATION,
	TABLESPACE_NAME,
	TEMPORARY,
	PARTITIONED,
	CLUSTER_NAME,
    BLOCKS,
    NUM_ROWS,
	SAMPLE_SIZE,
    AVG_ROW_LEN,
    CASE WHEN COMPRESSION = 'ENABLED' THEN INITCAP(COMPRESS_FOR) ELSE NVL(INITCAP(COMPRESSION), 'N/A') END COMPRESSION,
	LOGGING,
    LAST_ANALYZED,
	INITCAP(TRIM(DEGREE)) DEGREE
FROM
	SYS.ALL_TABLES
WHERE
	OWNER = :OWNER AND TABLE_NAME = :TABLE_NAME";

		public const string SelectTableAllocatedBytesCommandText =
@"SELECT
	SUM(BYTES) IN_ROW_BYTES, SUM(CASE WHEN LOBS.SEGMENT_NAME IS NOT NULL THEN BYTES END) LOB_BYTES
FROM
	SYS.DBA_SEGMENTS
	LEFT JOIN
	(
	    SELECT LOB_SEGMENT_TYPE, NVL(LOB_SEGMENT_NAME, LOB_INDEX_NAME) SEGMENT_NAME
	    FROM (
			SELECT 'TABLE' LOB_SEGMENT_TYPE, SEGMENT_NAME LOB_SEGMENT_NAME, INDEX_NAME LOB_INDEX_NAME FROM SYS.DBA_LOBS WHERE OWNER = :OWNER AND TABLE_NAME = :TABLE_NAME
	        UNION ALL
	        SELECT 'TABLE PARTITION' LOB_SEGMENT_TYPE, LOB_PARTITION_NAME LOB_SEGMENT_NAME, LOB_INDPART_NAME LOB_INDEX_NAME FROM SYS.DBA_LOB_PARTITIONS WHERE TABLE_OWNER = :OWNER AND TABLE_NAME = :TABLE_NAME
	        UNION ALL
	        SELECT 'TABLE SUBPARTITION' LOB_SEGMENT_TYPE, LOB_SUBPARTITION_NAME LOB_SEGMENT_NAME, LOB_INDSUBPART_NAME LOB_INDEX_NAME FROM SYS.DBA_LOB_SUBPARTITIONS WHERE TABLE_OWNER = :OWNER AND TABLE_NAME = :TABLE_NAME)
	)
	LOBS ON DBA_SEGMENTS.SEGMENT_NAME = LOBS.SEGMENT_NAME
WHERE
	LOBS.SEGMENT_NAME IS NOT NULL OR
	(
		DBA_SEGMENTS.SEGMENT_TYPE IN ('TABLE', 'TABLE PARTITION', 'TABLE SUBPARTITION') AND
		DBA_SEGMENTS.OWNER = :OWNER AND
		DBA_SEGMENTS.SEGMENT_NAME = :TABLE_NAME AND
		(
			:PARTITION_NAME IS NULL OR
			DBA_SEGMENTS.PARTITION_NAME = :PARTITION_NAME OR
			DBA_SEGMENTS.PARTITION_NAME IN (
				SELECT SUBPARTITION_NAME FROM ALL_TAB_SUBPARTITIONS WHERE TABLE_OWNER = :OWNER AND TABLE_NAME = :TABLE_NAME AND PARTITION_NAME = :PARTITION_NAME
			)
		)
	)";

		public const string SelectTableInMemoryAllocatedBytesCommandText = "SELECT INMEMORY_SIZE, BYTES, BYTES_NOT_POPULATED, POPULATE_STATUS, INITCAP(INMEMORY_COMPRESSION) INMEMORY_COMPRESSION FROM V$IM_SEGMENTS WHERE OWNER = :OWNER AND SEGMENT_NAME = :SEGMENT_NAME AND PARTITION_NAME IS NULL";
		public const string SelectExecutionPlanIdentifiersCommandText =
@"SELECT
	NVL(SQL_ID, PREV_SQL_ID) SQL_ID,
	NVL(SQL_CHILD_NUMBER, PREV_CHILD_NUMBER) SQL_CHILD_NUMBER,
	TRANSACTION_ID,
	NVL(TRANSACTION_ISOLATION_LEVEL, -1) TRANSACTION_ISOLATION_LEVEL
FROM
	V$SESSION
	LEFT JOIN (
		SELECT CASE BITAND(FLAG, POWER(2, 28)) WHEN 0 THEN 4096 ELSE 1048576 END TRANSACTION_ISOLATION_LEVEL, XIDUSN || '.' || XIDSLOT || '.' || XIDSQN TRANSACTION_ID FROM V$LOCK JOIN V$TRANSACTION ON V$LOCK.ADDR = V$TRANSACTION.ADDR WHERE SID = :SID AND STATUS = 'ACTIVE' AND ROWNUM = 1) TRANSACTION_DATA
	ON 1 = 1
WHERE
	SID = :SID";

		public const string FetchDatabaseOutputCommandText =
@"DECLARE
	line_count NUMBER;
	lines DBMSOUTPUT_LINESARRAY;
	line_content VARCHAR2(32767);
BEGIN
	DBMS_LOB.CREATETEMPORARY(lob_loc => :output_clob, cache => TRUE); 
    DBMS_LOB.OPEN(lob_loc => :output_clob, open_mode => DBMS_LOB.LOB_READWRITE);

	DBMS_OUTPUT.GET_LINES(lines => lines, numlines => line_count);

    FOR i IN 1..line_count LOOP
		line_content := lines(i);
		IF i < line_count THEN
			line_content := line_content || CHR(10);
		END IF;

        DBMS_LOB.WRITEAPPEND(lob_loc => :output_clob, amount => LENGTH(line_content), buffer => line_content);
    END LOOP;
END;";

		public const string SelectExecutionPlanTextCommandText = "SELECT PLAN_TABLE_OUTPUT FROM TABLE(SYS.DBMS_XPLAN.DISPLAY_CURSOR(:SQL_ID, :CHILD_NUMBER, 'ALLSTATS LAST ADVANCED'))";
		public const string SelectCursorExecutionStatisticsCommandText = "SELECT TIMESTAMP, OPERATION, OPTIONS, OBJECT_NODE, OBJECT#, OBJECT_OWNER, OBJECT_NAME, OBJECT_ALIAS, OBJECT_TYPE, OPTIMIZER, ID, PARENT_ID, DEPTH, POSITION, SEARCH_COLUMNS, COST, CARDINALITY, BYTES, OTHER_TAG, PARTITION_START, PARTITION_STOP, PARTITION_ID, OTHER, DISTRIBUTION, CPU_COST, IO_COST, TEMP_SPACE, ACCESS_PREDICATES, FILTER_PREDICATES, PROJECTION, TIME, QBLOCK_NAME, REMARKS, OTHER_XML, EXECUTIONS, LAST_STARTS, STARTS, LAST_OUTPUT_ROWS, OUTPUT_ROWS, LAST_CR_BUFFER_GETS, CR_BUFFER_GETS, LAST_CU_BUFFER_GETS, CU_BUFFER_GETS, LAST_DISK_READS, DISK_READS, LAST_DISK_WRITES, DISK_WRITES, LAST_ELAPSED_TIME, ELAPSED_TIME, POLICY, ESTIMATED_OPTIMAL_SIZE, ESTIMATED_ONEPASS_SIZE, LAST_MEMORY_USED, LAST_EXECUTION, LAST_DEGREE, TOTAL_EXECUTIONS, OPTIMAL_EXECUTIONS, ONEPASS_EXECUTIONS, MULTIPASSES_EXECUTIONS, ACTIVE_TIME, MAX_TEMPSEG_SIZE, LAST_TEMPSEG_SIZE FROM V$SQL_PLAN_STATISTICS_ALL WHERE SQL_ID = :SQL_ID AND CHILD_NUMBER = :CHILD_NUMBER ORDER BY ID";
		public const string SelectExplainPlanCommandText = "SELECT ID, PARENT_ID, DEPTH, OPERATION, OPTIONS, OBJECT_NODE, OBJECT_OWNER, OBJECT_NAME, OBJECT_ALIAS, OBJECT_INSTANCE, OBJECT_TYPE, OPTIMIZER, SEARCH_COLUMNS, POSITION, COST, CARDINALITY, BYTES, OTHER_TAG, PARTITION_START, PARTITION_STOP, PARTITION_ID, OTHER, DISTRIBUTION, CPU_COST, IO_COST, TEMP_SPACE, ACCESS_PREDICATES, FILTER_PREDICATES, PROJECTION, TIME, QBLOCK_NAME, OTHER_XML, REMARKS FROM {0} WHERE STATEMENT_ID = :STATEMENT_ID ORDER BY ID";
		public const string SelectCharacterSetsCommandText = "SELECT VALUE FROM V$NLS_VALID_VALUES WHERE parameter = 'CHARACTERSET' AND ISDEPRECATED = 'FALSE' ORDER BY VALUE";
		public const string SelectStatisticsKeysCommandText = "SELECT STATISTIC#, CLASS, DISPLAY_NAME FROM V$STATNAME";
		public const string SelectStatisticsKeysOracle11CommandText = "SELECT STATISTIC#, CLASS, NAME DISPLAY_NAME FROM V$STATNAME";
		public const string SelectSessionsStatisticsCommandText = "SELECT STATISTIC#, VALUE FROM V$SESSTAT WHERE SID = :SID";
		public const string SelectContextDataCommandText = "SELECT NAMESPACE, ATTRIBUTE FROM GLOBAL_CONTEXT UNION SELECT NAMESPACE, ATTRIBUTE FROM SESSION_CONTEXT";
		public const string SelectWeekdayNamesCommandText = "SELECT TRIM(TO_CHAR(TRUNC(SYSDATE, 'W') + LEVEL, 'Day')) WEEKDAY FROM DUAL CONNECT BY LEVEL <= 7 ORDER BY WEEKDAY";
		public const string SelectSystemParametersCommandText = "SELECT NAME, VALUE FROM V$PARAMETER WHERE NAME IN ('max_string_size')";
		public const string SelectLocalTransactionIdCommandText = "SELECT DBMS_TRANSACTION.LOCAL_TRANSACTION_ID TRANSACTION_ID FROM SYS.DUAL";
		public const string SelectCompilationErrorsCommandText = "SELECT TYPE, SEQUENCE, LINE, POSITION, TEXT, ATTRIBUTE, MESSAGE_NUMBER FROM SYS.ALL_ERRORS WHERE OWNER = :OWNER AND NAME = :NAME ORDER BY SEQUENCE";
		public const string SelectIndexDescriptionCommandText =
@"SELECT
	ALL_INDEXES.OWNER, INDEX_NAME, ALL_INDEXES.TABLESPACE_NAME, INDEX_TYPE, UNIQUENESS, COMPRESSION, PREFIX_LENGTH, LOGGING, CLUSTERING_FACTOR, STATUS, NUM_ROWS, LEAF_BLOCKS, SAMPLE_SIZE, DISTINCT_KEYS, LAST_ANALYZED, DEGREE, SEGMENT_TYPE, BLOCKS, BYTES
FROM
	SYS.ALL_INDEXES
	JOIN SYS.DBA_SEGMENTS ON ALL_INDEXES.OWNER = DBA_SEGMENTS.OWNER AND INDEX_NAME = SEGMENT_NAME
WHERE
	TABLE_OWNER = :TABLE_OWNER AND TABLE_NAME = :TABLE_NAME AND
	(:COLUMN_NAME IS NULL OR EXISTS (SELECT NULL FROM ALL_IND_COLUMNS WHERE INDEX_OWNER = ALL_INDEXES.OWNER AND INDEX_NAME = ALL_INDEXES.INDEX_NAME AND COLUMN_NAME = :COLUMN_NAME))
ORDER BY
	ALL_INDEXES.OWNER, INDEX_NAME";

		public const string SelectColumnConstraintDescriptionCommandText = "SELECT ALL_CONSTRAINTS.OWNER, ALL_CONSTRAINTS.CONSTRAINT_NAME, CONSTRAINT_TYPE, SEARCH_CONDITION, DELETE_RULE, STATUS, DEFERRABLE, DEFERRED, VALIDATED, RELY, LAST_CHANGE FROM ALL_CONS_COLUMNS JOIN ALL_CONSTRAINTS ON ALL_CONS_COLUMNS.OWNER = ALL_CONSTRAINTS.OWNER AND ALL_CONS_COLUMNS.CONSTRAINT_NAME = ALL_CONSTRAINTS.CONSTRAINT_NAME WHERE ALL_CONSTRAINTS.OWNER = :OWNER AND ALL_CONSTRAINTS.TABLE_NAME = :TABLE_NAME AND COLUMN_NAME = :COLUMN_NAME ORDER BY ALL_CONSTRAINTS.CONSTRAINT_NAME";
		public const string SelectIndexColumnDescriptionCommandText =
@"SELECT
	INDEX_OWNER, INDEX_NAME, COLUMN_NAME, COLUMN_POSITION, DESCEND
FROM
	SYS.ALL_IND_COLUMNS
WHERE
	TABLE_OWNER = :OWNER AND TABLE_NAME = :TABLE_NAME AND
	EXISTS (
		SELECT NULL FROM SYS.ALL_IND_COLUMNS C WHERE TABLE_OWNER = ALL_IND_COLUMNS.TABLE_OWNER AND TABLE_NAME = ALL_IND_COLUMNS.TABLE_NAME AND INDEX_OWNER = ALL_IND_COLUMNS.INDEX_OWNER AND INDEX_NAME = ALL_IND_COLUMNS.INDEX_NAME AND (COLUMN_NAME = :COLUMN_NAME OR :COLUMN_NAME IS NULL)
	)
ORDER BY
	INDEX_OWNER, INDEX_NAME, COLUMN_POSITION";

		public const string SelectTableConstraintDescriptionCommandText = "SELECT OWNER, CONSTRAINT_NAME, CONSTRAINT_TYPE, SEARCH_CONDITION, DELETE_RULE, STATUS, DEFERRABLE, DEFERRED, VALIDATED, RELY, LAST_CHANGE FROM SYS.ALL_CONSTRAINTS WHERE OWNER = :OWNER AND TABLE_NAME = :TABLE_NAME ORDER BY OWNER, CONSTRAINT_NAME";
		public const string SelectMaterializedViewCommandText = "SELECT OWNER, NAME, TABLE_NAME, MASTER_VIEW, MASTER_OWNER, MASTER, MASTER_LINK, CAN_USE_LOG, UPDATABLE, REFRESH_METHOD, LAST_REFRESH, ERROR, FR_OPERATIONS, CR_OPERATIONS, TYPE, NEXT, START_WITH, REFRESH_GROUP, UPDATE_TRIG, UPDATE_LOG, QUERY, MASTER_ROLLBACK_SEG, STATUS, REFRESH_MODE, PREBUILT FROM SYS.ALL_SNAPSHOTS";
		public const string SelectTraceFileFullName = "SELECT TRACEFILE FROM V$PROCESS WHERE ADDR = (SELECT PADDR FROM V$SESSION WHERE SID = SYS_CONTEXT('USERENV', 'SID'))";
		public const string SelectCurrentSessionId = "SELECT SYS_CONTEXT('USERENV', 'SID') SID FROM SYS.DUAL";
		public const string SelectUserAdditionalData = "SELECT INITCAP(ACCOUNT_STATUS) ACCOUNT_STATUS, LOCK_DATE, EXPIRY_DATE, DEFAULT_TABLESPACE, TEMPORARY_TABLESPACE, PROFILE, EDITIONS_ENABLED, INITCAP(AUTHENTICATION_TYPE) AUTHENTICATION_TYPE, LAST_LOGIN FROM DBA_USERS WHERE USERNAME = :USERNAME";

		public const string StartDebuggee = "BEGIN /*EXECUTE IMMEDIATE 'ALTER SESSION SET PLSQL_DEBUG=TRUE';*/ :debug_session_id := dbms_debug.initialize(diagnostics => 0); dbms_debug.debug_on; END;";
		public const string FinalizeDebuggee = "BEGIN dbms_debug.debug_off; END;";
		public const string AttachDebugger =
@"BEGIN
	dbms_debug.attach_session(debug_session_id => :debug_session_id, diagnostics => 0);
END;";

		public const string SynchronizeDebugger =
@"DECLARE
	run_info DBMS_DEBUG.RUNTIME_INFO; 
BEGIN
	:debug_action_status := dbms_debug.synchronize(run_info, dbms_debug.info_getlineinfo + dbms_debug.info_getbreakpoint + dbms_debug.info_getstackdepth + dbms_debug.info_getoerinfo);
	:breakpoint := run_info.breakpoint;
	:interpreterdepth := run_info.interpreterdepth;
	:line := run_info.line#;
	:oer := run_info.oer;
	:reason := run_info.reason;
	:stackdepth := run_info.stackdepth;
	:terminated := run_info.terminated;
	:dblink := run_info.program.dblink;
	:entrypointname := run_info.program.entrypointname;
	:owner := run_info.program.owner;
	:name := run_info.program.name;
	:namespace := run_info.program.namespace;
	:libunittype := run_info.program.libunittype;
END;";

		public const string ContinueDebugger =
@"DECLARE
	run_info DBMS_DEBUG.RUNTIME_INFO; 
BEGIN
	:debug_action_status := dbms_debug.continue(run_info, :break_flags, dbms_debug.info_getlineinfo + dbms_debug.info_getbreakpoint + dbms_debug.info_getstackdepth + dbms_debug.info_getoerinfo);
	:breakpoint := run_info.breakpoint;
	:interpreterdepth := run_info.interpreterdepth;
	:line := run_info.line#;
	:oer := run_info.oer;
	:reason := run_info.reason;
	:stackdepth := run_info.stackdepth;
	:terminated := run_info.terminated;
	:dblink := run_info.program.dblink;
	:entrypointname := run_info.program.entrypointname;
	:owner := run_info.program.owner;
	:name := run_info.program.name;
	:namespace := run_info.program.namespace;
	:libunittype := run_info.program.libunittype;
END;";
		public const string DetachDebugger = "BEGIN dbms_debug.detach_session; END;";
		public const string DebuggerGetValue =
@"DECLARE
    frame NUMBER := 0;
BEGIN
    :result := dbms_debug.get_value(:name, frame, :value, null);
END;";

		public const string GetDebuggerStackTrace =
@"DECLARE
	pkgs dbms_debug.backtrace_table;
	i NUMBER;
	line_content VARCHAR2(32767);
BEGIN
	dbms_debug.print_backtrace(pkgs);
	i := pkgs.first();

	DBMS_LOB.CREATETEMPORARY(lob_loc => :output_clob, cache => TRUE); 
    DBMS_LOB.OPEN(lob_loc => :output_clob, open_mode => DBMS_LOB.LOB_READWRITE);

	WHILE i IS NOT NULL LOOP
		line_content := i || ': ' || NVL(pkgs(i).name, 'Anonymous PL/SQL block') || ' (' || pkgs(i).line# ||')' || CHR(10);
		DBMS_LOB.WRITEAPPEND(lob_loc => :output_clob, amount => LENGTH(line_content), buffer => line_content);
		i := pkgs.next(i);
	END LOOP;
END;";

		public const string SetDebuggerBreakpoint =
		@"DECLARE
	program_info dbms_debug.program_info;
BEGIN
	program_info.namespace := dbms_debug.namespace_pkgspec_or_toplevel;
	program_info.owner := :owner;
	program_info.name := :name;
	--program_info.dblink := null;
	:result := dbms_debug.set_breakpoint(program => program_info, line# => :line, breakpoint# => :breakpoint_identifier, fuzzy => 1, iterations => 0);
END;";

		public const string GetDebuggerLineMap =
		@"DECLARE
	program_info dbms_debug.program_info;
BEGIN
	program_info.namespace := dbms_debug.namespace_pkgspec_or_toplevel;
	program_info.owner := :owner;
	program_info.name := :name;
	--program_info.dblink := null;
	:result := dbms_debug.get_line_map(program => program_info, maxline => :maxline, number_of_entry_points => :number_of_entry_points, linemap => :linemap);
END;";

		private static string ToInValueList(params string[] values)
		{
			return String.Join(", ", values.Select(t => $"'{t}'"));
		}
	}
}
#if NETSTANDARD2_0
using System;
using System.Runtime.InteropServices;

namespace LadybugDB.Interop;

internal static partial class Native
{
    private const CallingConvention Conv = CallingConvention.Cdecl;

    // ---- Version ---------------------------------------------------------------------------------
    [DllImport(LibraryName, EntryPoint = "lbug_get_version", CallingConvention = Conv)]
    internal static extern IntPtr GetVersionPtr();

    [DllImport(LibraryName, EntryPoint = "lbug_get_storage_version", CallingConvention = Conv)]
    internal static extern ulong GetStorageVersion();

    [DllImport(LibraryName, EntryPoint = "lbug_get_last_error", CallingConvention = Conv)]
    internal static extern IntPtr GetLastErrorPtr();

    // ---- Database --------------------------------------------------------------------------------
    [DllImport(LibraryName, EntryPoint = "lbug_default_system_config", CallingConvention = Conv)]
    internal static extern LbugSystemConfig DefaultSystemConfig();

    [DllImport(LibraryName, EntryPoint = "lbug_database_init", CallingConvention = Conv)]
    private static extern LbugState DatabaseInitRaw(byte[] databasePath, LbugSystemConfig systemConfig, out LbugDatabase outDatabase);

    internal static LbugState DatabaseInit(string databasePath, LbugSystemConfig systemConfig, out LbugDatabase outDatabase)
        => DatabaseInitRaw(ToUtf8(databasePath), systemConfig, out outDatabase);

    [DllImport(LibraryName, EntryPoint = "lbug_database_destroy", CallingConvention = Conv)]
    internal static extern void DatabaseDestroy(ref LbugDatabase database);

    // ---- Connection ------------------------------------------------------------------------------
    [DllImport(LibraryName, EntryPoint = "lbug_connection_init", CallingConvention = Conv)]
    internal static extern LbugState ConnectionInit(ref LbugDatabase database, out LbugConnection outConnection);

    [DllImport(LibraryName, EntryPoint = "lbug_connection_destroy", CallingConvention = Conv)]
    internal static extern void ConnectionDestroy(ref LbugConnection connection);

    [DllImport(LibraryName, EntryPoint = "lbug_connection_query", CallingConvention = Conv)]
    private static extern LbugState ConnectionQueryRaw(ref LbugConnection connection, byte[] query, out LbugQueryResult outQueryResult);

    internal static LbugState ConnectionQuery(ref LbugConnection connection, string query, out LbugQueryResult outQueryResult)
        => ConnectionQueryRaw(ref connection, ToUtf8(query), out outQueryResult);

    [DllImport(LibraryName, EntryPoint = "lbug_connection_prepare", CallingConvention = Conv)]
    private static extern LbugState ConnectionPrepareRaw(ref LbugConnection connection, byte[] query, out LbugPreparedStatement outPreparedStatement);

    internal static LbugState ConnectionPrepare(ref LbugConnection connection, string query, out LbugPreparedStatement outPreparedStatement)
        => ConnectionPrepareRaw(ref connection, ToUtf8(query), out outPreparedStatement);

    [DllImport(LibraryName, EntryPoint = "lbug_connection_execute", CallingConvention = Conv)]
    internal static extern LbugState ConnectionExecute(ref LbugConnection connection, ref LbugPreparedStatement preparedStatement, out LbugQueryResult outQueryResult);

    [DllImport(LibraryName, EntryPoint = "lbug_connection_get_pushed_sql", CallingConvention = Conv)]
    private static extern LbugState ConnectionGetPushedSqlRaw(ref LbugConnection connection, byte[] cypherQuery, out IntPtr outSql);

    internal static LbugState ConnectionGetPushedSql(ref LbugConnection connection, string cypherQuery, out IntPtr outSql)
        => ConnectionGetPushedSqlRaw(ref connection, ToUtf8(cypherQuery), out outSql);

    // ---- PreparedStatement -----------------------------------------------------------------------
    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_destroy", CallingConvention = Conv)]
    internal static extern void PreparedStatementDestroy(ref LbugPreparedStatement preparedStatement);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_is_success", CallingConvention = Conv)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool PreparedStatementIsSuccess(ref LbugPreparedStatement preparedStatement);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_is_read_only", CallingConvention = Conv)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool PreparedStatementIsReadOnly(ref LbugPreparedStatement preparedStatement);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_get_error_message", CallingConvention = Conv)]
    internal static extern IntPtr PreparedStatementGetErrorMessage(ref LbugPreparedStatement preparedStatement);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_bool", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindBoolRaw(ref LbugPreparedStatement preparedStatement, byte[] paramName, [MarshalAs(UnmanagedType.U1)] bool value);

    internal static LbugState PreparedStatementBindBool(ref LbugPreparedStatement preparedStatement, string paramName, bool value)
        => PreparedStatementBindBoolRaw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_int64", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindInt64Raw(ref LbugPreparedStatement preparedStatement, byte[] paramName, long value);

    internal static LbugState PreparedStatementBindInt64(ref LbugPreparedStatement preparedStatement, string paramName, long value)
        => PreparedStatementBindInt64Raw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_int32", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindInt32Raw(ref LbugPreparedStatement preparedStatement, byte[] paramName, int value);

    internal static LbugState PreparedStatementBindInt32(ref LbugPreparedStatement preparedStatement, string paramName, int value)
        => PreparedStatementBindInt32Raw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_int16", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindInt16Raw(ref LbugPreparedStatement preparedStatement, byte[] paramName, short value);

    internal static LbugState PreparedStatementBindInt16(ref LbugPreparedStatement preparedStatement, string paramName, short value)
        => PreparedStatementBindInt16Raw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_int8", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindInt8Raw(ref LbugPreparedStatement preparedStatement, byte[] paramName, sbyte value);

    internal static LbugState PreparedStatementBindInt8(ref LbugPreparedStatement preparedStatement, string paramName, sbyte value)
        => PreparedStatementBindInt8Raw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_uint64", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindUInt64Raw(ref LbugPreparedStatement preparedStatement, byte[] paramName, ulong value);

    internal static LbugState PreparedStatementBindUInt64(ref LbugPreparedStatement preparedStatement, string paramName, ulong value)
        => PreparedStatementBindUInt64Raw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_uint32", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindUInt32Raw(ref LbugPreparedStatement preparedStatement, byte[] paramName, uint value);

    internal static LbugState PreparedStatementBindUInt32(ref LbugPreparedStatement preparedStatement, string paramName, uint value)
        => PreparedStatementBindUInt32Raw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_uint16", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindUInt16Raw(ref LbugPreparedStatement preparedStatement, byte[] paramName, ushort value);

    internal static LbugState PreparedStatementBindUInt16(ref LbugPreparedStatement preparedStatement, string paramName, ushort value)
        => PreparedStatementBindUInt16Raw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_uint8", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindUInt8Raw(ref LbugPreparedStatement preparedStatement, byte[] paramName, byte value);

    internal static LbugState PreparedStatementBindUInt8(ref LbugPreparedStatement preparedStatement, string paramName, byte value)
        => PreparedStatementBindUInt8Raw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_double", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindDoubleRaw(ref LbugPreparedStatement preparedStatement, byte[] paramName, double value);

    internal static LbugState PreparedStatementBindDouble(ref LbugPreparedStatement preparedStatement, string paramName, double value)
        => PreparedStatementBindDoubleRaw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_float", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindFloatRaw(ref LbugPreparedStatement preparedStatement, byte[] paramName, float value);

    internal static LbugState PreparedStatementBindFloat(ref LbugPreparedStatement preparedStatement, string paramName, float value)
        => PreparedStatementBindFloatRaw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_string", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindStringRaw(ref LbugPreparedStatement preparedStatement, byte[] paramName, byte[] value);

    internal static LbugState PreparedStatementBindString(ref LbugPreparedStatement preparedStatement, string paramName, string value)
        => PreparedStatementBindStringRaw(ref preparedStatement, ToUtf8(paramName), ToUtf8(value));

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_date", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindDateRaw(ref LbugPreparedStatement preparedStatement, byte[] paramName, LbugDate value);

    internal static LbugState PreparedStatementBindDate(ref LbugPreparedStatement preparedStatement, string paramName, LbugDate value)
        => PreparedStatementBindDateRaw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_timestamp", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindTimestampRaw(ref LbugPreparedStatement preparedStatement, byte[] paramName, LbugTimestamp value);

    internal static LbugState PreparedStatementBindTimestamp(ref LbugPreparedStatement preparedStatement, string paramName, LbugTimestamp value)
        => PreparedStatementBindTimestampRaw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_timestamp_ms", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindTimestampMsRaw(ref LbugPreparedStatement preparedStatement, byte[] paramName, LbugTimestamp value);

    internal static LbugState PreparedStatementBindTimestampMs(ref LbugPreparedStatement preparedStatement, string paramName, LbugTimestamp value)
        => PreparedStatementBindTimestampMsRaw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_timestamp_sec", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindTimestampSecRaw(ref LbugPreparedStatement preparedStatement, byte[] paramName, LbugTimestamp value);

    internal static LbugState PreparedStatementBindTimestampSec(ref LbugPreparedStatement preparedStatement, string paramName, LbugTimestamp value)
        => PreparedStatementBindTimestampSecRaw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_timestamp_ns", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindTimestampNsRaw(ref LbugPreparedStatement preparedStatement, byte[] paramName, LbugTimestamp value);

    internal static LbugState PreparedStatementBindTimestampNs(ref LbugPreparedStatement preparedStatement, string paramName, LbugTimestamp value)
        => PreparedStatementBindTimestampNsRaw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_timestamp_tz", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindTimestampTzRaw(ref LbugPreparedStatement preparedStatement, byte[] paramName, LbugTimestamp value);

    internal static LbugState PreparedStatementBindTimestampTz(ref LbugPreparedStatement preparedStatement, string paramName, LbugTimestamp value)
        => PreparedStatementBindTimestampTzRaw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_interval", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindIntervalRaw(ref LbugPreparedStatement preparedStatement, byte[] paramName, LbugInterval value);

    internal static LbugState PreparedStatementBindInterval(ref LbugPreparedStatement preparedStatement, string paramName, LbugInterval value)
        => PreparedStatementBindIntervalRaw(ref preparedStatement, ToUtf8(paramName), value);

    [DllImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_value", CallingConvention = Conv)]
    private static extern LbugState PreparedStatementBindValueRaw(ref LbugPreparedStatement preparedStatement, byte[] paramName, IntPtr value);

    internal static LbugState PreparedStatementBindValue(ref LbugPreparedStatement preparedStatement, string paramName, IntPtr value)
        => PreparedStatementBindValueRaw(ref preparedStatement, ToUtf8(paramName), value);

    // ---- QueryResult -----------------------------------------------------------------------------
    [DllImport(LibraryName, EntryPoint = "lbug_query_result_destroy", CallingConvention = Conv)]
    internal static extern void QueryResultDestroy(ref LbugQueryResult queryResult);

    [DllImport(LibraryName, EntryPoint = "lbug_query_result_is_success", CallingConvention = Conv)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool QueryResultIsSuccess(ref LbugQueryResult queryResult);

    [DllImport(LibraryName, EntryPoint = "lbug_query_result_get_error_message", CallingConvention = Conv)]
    internal static extern IntPtr QueryResultGetErrorMessage(ref LbugQueryResult queryResult);

    [DllImport(LibraryName, EntryPoint = "lbug_query_result_get_num_columns", CallingConvention = Conv)]
    internal static extern ulong QueryResultGetNumColumns(ref LbugQueryResult queryResult);

    [DllImport(LibraryName, EntryPoint = "lbug_query_result_get_column_name", CallingConvention = Conv)]
    internal static extern LbugState QueryResultGetColumnName(ref LbugQueryResult queryResult, ulong index, out IntPtr outColumnName);

    [DllImport(LibraryName, EntryPoint = "lbug_query_result_get_num_tuples", CallingConvention = Conv)]
    internal static extern ulong QueryResultGetNumTuples(ref LbugQueryResult queryResult);

    [DllImport(LibraryName, EntryPoint = "lbug_query_result_has_next", CallingConvention = Conv)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool QueryResultHasNext(ref LbugQueryResult queryResult);

    [DllImport(LibraryName, EntryPoint = "lbug_query_result_get_next", CallingConvention = Conv)]
    internal static extern LbugState QueryResultGetNext(ref LbugQueryResult queryResult, out LbugFlatTuple outFlatTuple);

    [DllImport(LibraryName, EntryPoint = "lbug_query_result_to_string", CallingConvention = Conv)]
    internal static extern IntPtr QueryResultToString(ref LbugQueryResult queryResult);

    // ---- FlatTuple -------------------------------------------------------------------------------
    [DllImport(LibraryName, EntryPoint = "lbug_flat_tuple_destroy", CallingConvention = Conv)]
    internal static extern void FlatTupleDestroy(ref LbugFlatTuple flatTuple);

    [DllImport(LibraryName, EntryPoint = "lbug_flat_tuple_get_value", CallingConvention = Conv)]
    internal static extern LbugState FlatTupleGetValue(ref LbugFlatTuple flatTuple, ulong index, out LbugValue outValue);

    [DllImport(LibraryName, EntryPoint = "lbug_flat_tuple_to_string", CallingConvention = Conv)]
    internal static extern IntPtr FlatTupleToString(ref LbugFlatTuple flatTuple);

    // ---- DataType --------------------------------------------------------------------------------
    [DllImport(LibraryName, EntryPoint = "lbug_data_type_get_id", CallingConvention = Conv)]
    internal static extern LbugDataTypeId DataTypeGetId(ref LbugLogicalType dataType);

    [DllImport(LibraryName, EntryPoint = "lbug_data_type_create", CallingConvention = Conv)]
    internal static extern void DataTypeCreate(LbugDataTypeId id, IntPtr childType, ulong numElementsInArray, out LbugLogicalType outType);

    [DllImport(LibraryName, EntryPoint = "lbug_data_type_create", CallingConvention = Conv)]
    internal static extern void DataTypeCreateWithChild(LbugDataTypeId id, ref LbugLogicalType childType, ulong numElementsInArray, out LbugLogicalType outType);

    [DllImport(LibraryName, EntryPoint = "lbug_data_type_destroy", CallingConvention = Conv)]
    internal static extern void DataTypeDestroy(ref LbugLogicalType dataType);

    // ---- Value -----------------------------------------------------------------------------------
    [DllImport(LibraryName, EntryPoint = "lbug_value_destroy", CallingConvention = Conv)]
    internal static extern void ValueDestroy(ref LbugValue value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_destroy", CallingConvention = Conv)]
    internal static extern void ValueDestroy(IntPtr value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_null", CallingConvention = Conv)]
    internal static extern IntPtr ValueCreateNull();

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_default", CallingConvention = Conv)]
    internal static extern IntPtr ValueCreateDefault(ref LbugLogicalType dataType);

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_bool", CallingConvention = Conv)]
    internal static extern IntPtr ValueCreateBool([MarshalAs(UnmanagedType.U1)] bool value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_int8", CallingConvention = Conv)]
    internal static extern IntPtr ValueCreateInt8(sbyte value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_int16", CallingConvention = Conv)]
    internal static extern IntPtr ValueCreateInt16(short value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_int32", CallingConvention = Conv)]
    internal static extern IntPtr ValueCreateInt32(int value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_int64", CallingConvention = Conv)]
    internal static extern IntPtr ValueCreateInt64(long value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_uint8", CallingConvention = Conv)]
    internal static extern IntPtr ValueCreateUInt8(byte value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_uint16", CallingConvention = Conv)]
    internal static extern IntPtr ValueCreateUInt16(ushort value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_uint32", CallingConvention = Conv)]
    internal static extern IntPtr ValueCreateUInt32(uint value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_uint64", CallingConvention = Conv)]
    internal static extern IntPtr ValueCreateUInt64(ulong value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_float", CallingConvention = Conv)]
    internal static extern IntPtr ValueCreateFloat(float value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_double", CallingConvention = Conv)]
    internal static extern IntPtr ValueCreateDouble(double value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_string", CallingConvention = Conv)]
    private static extern IntPtr ValueCreateStringRaw(byte[] value);

    internal static IntPtr ValueCreateString(string value)
        => ValueCreateStringRaw(ToUtf8(value));

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_date", CallingConvention = Conv)]
    internal static extern IntPtr ValueCreateDate(LbugDate value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_timestamp", CallingConvention = Conv)]
    internal static extern IntPtr ValueCreateTimestamp(LbugTimestamp value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_timestamp_tz", CallingConvention = Conv)]
    internal static extern IntPtr ValueCreateTimestampTz(LbugTimestamp value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_interval", CallingConvention = Conv)]
    internal static extern IntPtr ValueCreateInterval(LbugInterval value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_create_list", CallingConvention = Conv)]
    internal static extern LbugState ValueCreateList(ulong numElements, [In] IntPtr[] elements, out IntPtr outValue);

    [DllImport(LibraryName, EntryPoint = "lbug_value_is_null", CallingConvention = Conv)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool ValueIsNull(ref LbugValue value);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_data_type", CallingConvention = Conv)]
    internal static extern void ValueGetDataType(ref LbugValue value, out LbugLogicalType outType);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_bool", CallingConvention = Conv)]
    internal static extern LbugState ValueGetBool(ref LbugValue value, out byte outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_int8", CallingConvention = Conv)]
    internal static extern LbugState ValueGetInt8(ref LbugValue value, out sbyte outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_int16", CallingConvention = Conv)]
    internal static extern LbugState ValueGetInt16(ref LbugValue value, out short outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_int32", CallingConvention = Conv)]
    internal static extern LbugState ValueGetInt32(ref LbugValue value, out int outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_int64", CallingConvention = Conv)]
    internal static extern LbugState ValueGetInt64(ref LbugValue value, out long outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_uint8", CallingConvention = Conv)]
    internal static extern LbugState ValueGetUInt8(ref LbugValue value, out byte outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_uint16", CallingConvention = Conv)]
    internal static extern LbugState ValueGetUInt16(ref LbugValue value, out ushort outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_uint32", CallingConvention = Conv)]
    internal static extern LbugState ValueGetUInt32(ref LbugValue value, out uint outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_uint64", CallingConvention = Conv)]
    internal static extern LbugState ValueGetUInt64(ref LbugValue value, out ulong outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_float", CallingConvention = Conv)]
    internal static extern LbugState ValueGetFloat(ref LbugValue value, out float outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_double", CallingConvention = Conv)]
    internal static extern LbugState ValueGetDouble(ref LbugValue value, out double outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_string", CallingConvention = Conv)]
    internal static extern LbugState ValueGetString(ref LbugValue value, out IntPtr outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_to_string", CallingConvention = Conv)]
    internal static extern IntPtr ValueToString(ref LbugValue value);

    // ---- Numeric / temporal ----------------------------------------------------------------------
    [DllImport(LibraryName, EntryPoint = "lbug_value_get_int128", CallingConvention = Conv)]
    internal static extern LbugState ValueGetInt128(ref LbugValue value, out LbugInt128 outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_internal_id", CallingConvention = Conv)]
    internal static extern LbugState ValueGetInternalId(ref LbugValue value, out LbugInternalId outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_date", CallingConvention = Conv)]
    internal static extern LbugState ValueGetDate(ref LbugValue value, out LbugDate outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_timestamp", CallingConvention = Conv)]
    internal static extern LbugState ValueGetTimestamp(ref LbugValue value, out LbugTimestamp outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_timestamp_ns", CallingConvention = Conv)]
    internal static extern LbugState ValueGetTimestampNs(ref LbugValue value, out LbugTimestamp outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_timestamp_ms", CallingConvention = Conv)]
    internal static extern LbugState ValueGetTimestampMs(ref LbugValue value, out LbugTimestamp outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_timestamp_sec", CallingConvention = Conv)]
    internal static extern LbugState ValueGetTimestampSec(ref LbugValue value, out LbugTimestamp outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_timestamp_tz", CallingConvention = Conv)]
    internal static extern LbugState ValueGetTimestampTz(ref LbugValue value, out LbugTimestamp outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_interval", CallingConvention = Conv)]
    internal static extern LbugState ValueGetInterval(ref LbugValue value, out LbugInterval outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_decimal_as_string", CallingConvention = Conv)]
    internal static extern LbugState ValueGetDecimalAsString(ref LbugValue value, out IntPtr outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_uuid", CallingConvention = Conv)]
    internal static extern LbugState ValueGetUuid(ref LbugValue value, out IntPtr outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_blob", CallingConvention = Conv)]
    internal static extern LbugState ValueGetBlob(ref LbugValue value, out IntPtr outResult, out ulong outLength);

    // ---- Nested (list / array / struct / map) ----------------------------------------------------
    [DllImport(LibraryName, EntryPoint = "lbug_value_get_list_size", CallingConvention = Conv)]
    internal static extern LbugState ValueGetListSize(ref LbugValue value, out ulong outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_list_element", CallingConvention = Conv)]
    internal static extern LbugState ValueGetListElement(ref LbugValue value, ulong index, out LbugValue outValue);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_struct_num_fields", CallingConvention = Conv)]
    internal static extern LbugState ValueGetStructNumFields(ref LbugValue value, out ulong outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_struct_field_name", CallingConvention = Conv)]
    internal static extern LbugState ValueGetStructFieldName(ref LbugValue value, ulong index, out IntPtr outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_struct_field_value", CallingConvention = Conv)]
    internal static extern LbugState ValueGetStructFieldValue(ref LbugValue value, ulong index, out LbugValue outValue);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_map_size", CallingConvention = Conv)]
    internal static extern LbugState ValueGetMapSize(ref LbugValue value, out ulong outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_map_key", CallingConvention = Conv)]
    internal static extern LbugState ValueGetMapKey(ref LbugValue value, ulong index, out LbugValue outKey);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_map_value", CallingConvention = Conv)]
    internal static extern LbugState ValueGetMapValue(ref LbugValue value, ulong index, out LbugValue outValue);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_recursive_rel_node_list", CallingConvention = Conv)]
    internal static extern LbugState ValueGetRecursiveRelNodeList(ref LbugValue value, out LbugValue outValue);

    [DllImport(LibraryName, EntryPoint = "lbug_value_get_recursive_rel_rel_list", CallingConvention = Conv)]
    internal static extern LbugState ValueGetRecursiveRelRelList(ref LbugValue value, out LbugValue outValue);

    // ---- Node ------------------------------------------------------------------------------------
    [DllImport(LibraryName, EntryPoint = "lbug_node_val_get_id_val", CallingConvention = Conv)]
    internal static extern LbugState NodeValGetIdVal(ref LbugValue nodeVal, out LbugValue outValue);

    [DllImport(LibraryName, EntryPoint = "lbug_node_val_get_label_val", CallingConvention = Conv)]
    internal static extern LbugState NodeValGetLabelVal(ref LbugValue nodeVal, out LbugValue outValue);

    [DllImport(LibraryName, EntryPoint = "lbug_node_val_get_property_size", CallingConvention = Conv)]
    internal static extern LbugState NodeValGetPropertySize(ref LbugValue nodeVal, out ulong outValue);

    [DllImport(LibraryName, EntryPoint = "lbug_node_val_get_property_name_at", CallingConvention = Conv)]
    internal static extern LbugState NodeValGetPropertyNameAt(ref LbugValue nodeVal, ulong index, out IntPtr outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_node_val_get_property_value_at", CallingConvention = Conv)]
    internal static extern LbugState NodeValGetPropertyValueAt(ref LbugValue nodeVal, ulong index, out LbugValue outValue);

    // ---- Rel -------------------------------------------------------------------------------------
    [DllImport(LibraryName, EntryPoint = "lbug_rel_val_get_id_val", CallingConvention = Conv)]
    internal static extern LbugState RelValGetIdVal(ref LbugValue relVal, out LbugValue outValue);

    [DllImport(LibraryName, EntryPoint = "lbug_rel_val_get_src_id_val", CallingConvention = Conv)]
    internal static extern LbugState RelValGetSrcIdVal(ref LbugValue relVal, out LbugValue outValue);

    [DllImport(LibraryName, EntryPoint = "lbug_rel_val_get_dst_id_val", CallingConvention = Conv)]
    internal static extern LbugState RelValGetDstIdVal(ref LbugValue relVal, out LbugValue outValue);

    [DllImport(LibraryName, EntryPoint = "lbug_rel_val_get_label_val", CallingConvention = Conv)]
    internal static extern LbugState RelValGetLabelVal(ref LbugValue relVal, out LbugValue outValue);

    [DllImport(LibraryName, EntryPoint = "lbug_rel_val_get_property_size", CallingConvention = Conv)]
    internal static extern LbugState RelValGetPropertySize(ref LbugValue relVal, out ulong outValue);

    [DllImport(LibraryName, EntryPoint = "lbug_rel_val_get_property_name_at", CallingConvention = Conv)]
    internal static extern LbugState RelValGetPropertyNameAt(ref LbugValue relVal, ulong index, out IntPtr outResult);

    [DllImport(LibraryName, EntryPoint = "lbug_rel_val_get_property_value_at", CallingConvention = Conv)]
    internal static extern LbugState RelValGetPropertyValueAt(ref LbugValue relVal, ulong index, out LbugValue outValue);

    // ---- Memory ----------------------------------------------------------------------------------
    [DllImport(LibraryName, EntryPoint = "lbug_destroy_string", CallingConvention = Conv)]
    internal static extern void DestroyString(IntPtr str);

    [DllImport(LibraryName, EntryPoint = "lbug_destroy_blob", CallingConvention = Conv)]
    internal static extern void DestroyBlob(IntPtr blob);
}
#endif

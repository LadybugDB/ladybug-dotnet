#if NET7_0_OR_GREATER
using System;
using System.Runtime.InteropServices;

namespace LadybugDB.Interop;

internal static partial class Native
{
    // ---- Version ---------------------------------------------------------------------------------
    [LibraryImport(LibraryName, EntryPoint = "lbug_get_version")]
    internal static partial IntPtr GetVersionPtr();

    [LibraryImport(LibraryName, EntryPoint = "lbug_get_storage_version")]
    internal static partial ulong GetStorageVersion();

    [LibraryImport(LibraryName, EntryPoint = "lbug_get_last_error")]
    internal static partial IntPtr GetLastErrorPtr();

    // ---- Database --------------------------------------------------------------------------------
    [LibraryImport(LibraryName, EntryPoint = "lbug_default_system_config")]
    internal static partial LbugSystemConfig DefaultSystemConfig();

    [LibraryImport(LibraryName, EntryPoint = "lbug_database_init", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState DatabaseInit(string databasePath, LbugSystemConfig systemConfig, out LbugDatabase outDatabase);

    [LibraryImport(LibraryName, EntryPoint = "lbug_database_destroy")]
    internal static partial void DatabaseDestroy(ref LbugDatabase database);

    // ---- Connection ------------------------------------------------------------------------------
    [LibraryImport(LibraryName, EntryPoint = "lbug_connection_init")]
    internal static partial LbugState ConnectionInit(ref LbugDatabase database, out LbugConnection outConnection);

    [LibraryImport(LibraryName, EntryPoint = "lbug_connection_destroy")]
    internal static partial void ConnectionDestroy(ref LbugConnection connection);

    [LibraryImport(LibraryName, EntryPoint = "lbug_connection_query", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState ConnectionQuery(ref LbugConnection connection, string query, out LbugQueryResult outQueryResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_connection_prepare", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState ConnectionPrepare(ref LbugConnection connection, string query, out LbugPreparedStatement outPreparedStatement);

    [LibraryImport(LibraryName, EntryPoint = "lbug_connection_execute")]
    internal static partial LbugState ConnectionExecute(ref LbugConnection connection, ref LbugPreparedStatement preparedStatement, out LbugQueryResult outQueryResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_connection_get_pushed_sql", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState ConnectionGetPushedSql(ref LbugConnection connection, string cypherQuery, out IntPtr outSql);

    // ---- PreparedStatement -----------------------------------------------------------------------
    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_destroy")]
    internal static partial void PreparedStatementDestroy(ref LbugPreparedStatement preparedStatement);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_is_success")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool PreparedStatementIsSuccess(ref LbugPreparedStatement preparedStatement);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_is_read_only")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool PreparedStatementIsReadOnly(ref LbugPreparedStatement preparedStatement);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_get_error_message")]
    internal static partial IntPtr PreparedStatementGetErrorMessage(ref LbugPreparedStatement preparedStatement);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_bool", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindBool(ref LbugPreparedStatement preparedStatement, string paramName, [MarshalAs(UnmanagedType.U1)] bool value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_int64", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindInt64(ref LbugPreparedStatement preparedStatement, string paramName, long value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_int32", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindInt32(ref LbugPreparedStatement preparedStatement, string paramName, int value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_int16", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindInt16(ref LbugPreparedStatement preparedStatement, string paramName, short value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_int8", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindInt8(ref LbugPreparedStatement preparedStatement, string paramName, sbyte value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_uint64", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindUInt64(ref LbugPreparedStatement preparedStatement, string paramName, ulong value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_uint32", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindUInt32(ref LbugPreparedStatement preparedStatement, string paramName, uint value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_uint16", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindUInt16(ref LbugPreparedStatement preparedStatement, string paramName, ushort value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_uint8", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindUInt8(ref LbugPreparedStatement preparedStatement, string paramName, byte value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_double", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindDouble(ref LbugPreparedStatement preparedStatement, string paramName, double value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_float", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindFloat(ref LbugPreparedStatement preparedStatement, string paramName, float value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_string", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindString(ref LbugPreparedStatement preparedStatement, string paramName, string value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_date", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindDate(ref LbugPreparedStatement preparedStatement, string paramName, LbugDate value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_timestamp", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindTimestamp(ref LbugPreparedStatement preparedStatement, string paramName, LbugTimestamp value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_timestamp_ms", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindTimestampMs(ref LbugPreparedStatement preparedStatement, string paramName, LbugTimestamp value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_timestamp_sec", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindTimestampSec(ref LbugPreparedStatement preparedStatement, string paramName, LbugTimestamp value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_timestamp_ns", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindTimestampNs(ref LbugPreparedStatement preparedStatement, string paramName, LbugTimestamp value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_timestamp_tz", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindTimestampTz(ref LbugPreparedStatement preparedStatement, string paramName, LbugTimestamp value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_interval", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindInterval(ref LbugPreparedStatement preparedStatement, string paramName, LbugInterval value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_prepared_statement_bind_value", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial LbugState PreparedStatementBindValue(ref LbugPreparedStatement preparedStatement, string paramName, IntPtr value);

    // ---- QueryResult -----------------------------------------------------------------------------
    [LibraryImport(LibraryName, EntryPoint = "lbug_query_result_destroy")]
    internal static partial void QueryResultDestroy(ref LbugQueryResult queryResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_query_result_is_success")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool QueryResultIsSuccess(ref LbugQueryResult queryResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_query_result_get_error_message")]
    internal static partial IntPtr QueryResultGetErrorMessage(ref LbugQueryResult queryResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_query_result_get_num_columns")]
    internal static partial ulong QueryResultGetNumColumns(ref LbugQueryResult queryResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_query_result_get_column_name")]
    internal static partial LbugState QueryResultGetColumnName(ref LbugQueryResult queryResult, ulong index, out IntPtr outColumnName);

    [LibraryImport(LibraryName, EntryPoint = "lbug_query_result_get_num_tuples")]
    internal static partial ulong QueryResultGetNumTuples(ref LbugQueryResult queryResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_query_result_has_next")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool QueryResultHasNext(ref LbugQueryResult queryResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_query_result_get_next")]
    internal static partial LbugState QueryResultGetNext(ref LbugQueryResult queryResult, out LbugFlatTuple outFlatTuple);

    [LibraryImport(LibraryName, EntryPoint = "lbug_query_result_to_string")]
    internal static partial IntPtr QueryResultToString(ref LbugQueryResult queryResult);

    // ---- FlatTuple -------------------------------------------------------------------------------
    [LibraryImport(LibraryName, EntryPoint = "lbug_flat_tuple_destroy")]
    internal static partial void FlatTupleDestroy(ref LbugFlatTuple flatTuple);

    [LibraryImport(LibraryName, EntryPoint = "lbug_flat_tuple_get_value")]
    internal static partial LbugState FlatTupleGetValue(ref LbugFlatTuple flatTuple, ulong index, out LbugValue outValue);

    [LibraryImport(LibraryName, EntryPoint = "lbug_flat_tuple_to_string")]
    internal static partial IntPtr FlatTupleToString(ref LbugFlatTuple flatTuple);

    // ---- DataType --------------------------------------------------------------------------------
    [LibraryImport(LibraryName, EntryPoint = "lbug_data_type_get_id")]
    internal static partial LbugDataTypeId DataTypeGetId(ref LbugLogicalType dataType);

    [LibraryImport(LibraryName, EntryPoint = "lbug_data_type_create")]
    internal static partial void DataTypeCreate(LbugDataTypeId id, IntPtr childType, ulong numElementsInArray, out LbugLogicalType outType);

    [LibraryImport(LibraryName, EntryPoint = "lbug_data_type_create")]
    internal static partial void DataTypeCreateWithChild(LbugDataTypeId id, ref LbugLogicalType childType, ulong numElementsInArray, out LbugLogicalType outType);

    [LibraryImport(LibraryName, EntryPoint = "lbug_data_type_destroy")]
    internal static partial void DataTypeDestroy(ref LbugLogicalType dataType);

    // ---- Value -----------------------------------------------------------------------------------
    [LibraryImport(LibraryName, EntryPoint = "lbug_value_destroy")]
    internal static partial void ValueDestroy(ref LbugValue value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_destroy")]
    internal static partial void ValueDestroy(IntPtr value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_null")]
    internal static partial IntPtr ValueCreateNull();

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_default")]
    internal static partial IntPtr ValueCreateDefault(ref LbugLogicalType dataType);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_bool")]
    internal static partial IntPtr ValueCreateBool([MarshalAs(UnmanagedType.U1)] bool value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_int8")]
    internal static partial IntPtr ValueCreateInt8(sbyte value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_int16")]
    internal static partial IntPtr ValueCreateInt16(short value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_int32")]
    internal static partial IntPtr ValueCreateInt32(int value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_int64")]
    internal static partial IntPtr ValueCreateInt64(long value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_uint8")]
    internal static partial IntPtr ValueCreateUInt8(byte value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_uint16")]
    internal static partial IntPtr ValueCreateUInt16(ushort value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_uint32")]
    internal static partial IntPtr ValueCreateUInt32(uint value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_uint64")]
    internal static partial IntPtr ValueCreateUInt64(ulong value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_float")]
    internal static partial IntPtr ValueCreateFloat(float value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_double")]
    internal static partial IntPtr ValueCreateDouble(double value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_string", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr ValueCreateString(string value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_date")]
    internal static partial IntPtr ValueCreateDate(LbugDate value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_timestamp")]
    internal static partial IntPtr ValueCreateTimestamp(LbugTimestamp value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_timestamp_tz")]
    internal static partial IntPtr ValueCreateTimestampTz(LbugTimestamp value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_interval")]
    internal static partial IntPtr ValueCreateInterval(LbugInterval value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_create_list")]
    internal static partial LbugState ValueCreateList(ulong numElements, [In] IntPtr[] elements, out IntPtr outValue);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_is_null")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool ValueIsNull(ref LbugValue value);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_data_type")]
    internal static partial void ValueGetDataType(ref LbugValue value, out LbugLogicalType outType);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_bool")]
    internal static partial LbugState ValueGetBool(ref LbugValue value, out byte outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_int8")]
    internal static partial LbugState ValueGetInt8(ref LbugValue value, out sbyte outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_int16")]
    internal static partial LbugState ValueGetInt16(ref LbugValue value, out short outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_int32")]
    internal static partial LbugState ValueGetInt32(ref LbugValue value, out int outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_int64")]
    internal static partial LbugState ValueGetInt64(ref LbugValue value, out long outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_uint8")]
    internal static partial LbugState ValueGetUInt8(ref LbugValue value, out byte outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_uint16")]
    internal static partial LbugState ValueGetUInt16(ref LbugValue value, out ushort outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_uint32")]
    internal static partial LbugState ValueGetUInt32(ref LbugValue value, out uint outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_uint64")]
    internal static partial LbugState ValueGetUInt64(ref LbugValue value, out ulong outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_float")]
    internal static partial LbugState ValueGetFloat(ref LbugValue value, out float outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_double")]
    internal static partial LbugState ValueGetDouble(ref LbugValue value, out double outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_string")]
    internal static partial LbugState ValueGetString(ref LbugValue value, out IntPtr outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_to_string")]
    internal static partial IntPtr ValueToString(ref LbugValue value);

    // ---- Numeric / temporal ----------------------------------------------------------------------
    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_int128")]
    internal static partial LbugState ValueGetInt128(ref LbugValue value, out LbugInt128 outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_internal_id")]
    internal static partial LbugState ValueGetInternalId(ref LbugValue value, out LbugInternalId outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_date")]
    internal static partial LbugState ValueGetDate(ref LbugValue value, out LbugDate outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_timestamp")]
    internal static partial LbugState ValueGetTimestamp(ref LbugValue value, out LbugTimestamp outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_timestamp_ns")]
    internal static partial LbugState ValueGetTimestampNs(ref LbugValue value, out LbugTimestamp outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_timestamp_ms")]
    internal static partial LbugState ValueGetTimestampMs(ref LbugValue value, out LbugTimestamp outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_timestamp_sec")]
    internal static partial LbugState ValueGetTimestampSec(ref LbugValue value, out LbugTimestamp outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_timestamp_tz")]
    internal static partial LbugState ValueGetTimestampTz(ref LbugValue value, out LbugTimestamp outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_interval")]
    internal static partial LbugState ValueGetInterval(ref LbugValue value, out LbugInterval outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_decimal_as_string")]
    internal static partial LbugState ValueGetDecimalAsString(ref LbugValue value, out IntPtr outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_uuid")]
    internal static partial LbugState ValueGetUuid(ref LbugValue value, out IntPtr outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_blob")]
    internal static partial LbugState ValueGetBlob(ref LbugValue value, out IntPtr outResult, out ulong outLength);

    // ---- Nested (list / array / struct / map) ----------------------------------------------------
    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_list_size")]
    internal static partial LbugState ValueGetListSize(ref LbugValue value, out ulong outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_list_element")]
    internal static partial LbugState ValueGetListElement(ref LbugValue value, ulong index, out LbugValue outValue);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_struct_num_fields")]
    internal static partial LbugState ValueGetStructNumFields(ref LbugValue value, out ulong outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_struct_field_name")]
    internal static partial LbugState ValueGetStructFieldName(ref LbugValue value, ulong index, out IntPtr outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_struct_field_value")]
    internal static partial LbugState ValueGetStructFieldValue(ref LbugValue value, ulong index, out LbugValue outValue);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_map_size")]
    internal static partial LbugState ValueGetMapSize(ref LbugValue value, out ulong outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_map_key")]
    internal static partial LbugState ValueGetMapKey(ref LbugValue value, ulong index, out LbugValue outKey);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_map_value")]
    internal static partial LbugState ValueGetMapValue(ref LbugValue value, ulong index, out LbugValue outValue);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_recursive_rel_node_list")]
    internal static partial LbugState ValueGetRecursiveRelNodeList(ref LbugValue value, out LbugValue outValue);

    [LibraryImport(LibraryName, EntryPoint = "lbug_value_get_recursive_rel_rel_list")]
    internal static partial LbugState ValueGetRecursiveRelRelList(ref LbugValue value, out LbugValue outValue);

    // ---- Node ------------------------------------------------------------------------------------
    [LibraryImport(LibraryName, EntryPoint = "lbug_node_val_get_id_val")]
    internal static partial LbugState NodeValGetIdVal(ref LbugValue nodeVal, out LbugValue outValue);

    [LibraryImport(LibraryName, EntryPoint = "lbug_node_val_get_label_val")]
    internal static partial LbugState NodeValGetLabelVal(ref LbugValue nodeVal, out LbugValue outValue);

    [LibraryImport(LibraryName, EntryPoint = "lbug_node_val_get_property_size")]
    internal static partial LbugState NodeValGetPropertySize(ref LbugValue nodeVal, out ulong outValue);

    [LibraryImport(LibraryName, EntryPoint = "lbug_node_val_get_property_name_at")]
    internal static partial LbugState NodeValGetPropertyNameAt(ref LbugValue nodeVal, ulong index, out IntPtr outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_node_val_get_property_value_at")]
    internal static partial LbugState NodeValGetPropertyValueAt(ref LbugValue nodeVal, ulong index, out LbugValue outValue);

    // ---- Rel -------------------------------------------------------------------------------------
    [LibraryImport(LibraryName, EntryPoint = "lbug_rel_val_get_id_val")]
    internal static partial LbugState RelValGetIdVal(ref LbugValue relVal, out LbugValue outValue);

    [LibraryImport(LibraryName, EntryPoint = "lbug_rel_val_get_src_id_val")]
    internal static partial LbugState RelValGetSrcIdVal(ref LbugValue relVal, out LbugValue outValue);

    [LibraryImport(LibraryName, EntryPoint = "lbug_rel_val_get_dst_id_val")]
    internal static partial LbugState RelValGetDstIdVal(ref LbugValue relVal, out LbugValue outValue);

    [LibraryImport(LibraryName, EntryPoint = "lbug_rel_val_get_label_val")]
    internal static partial LbugState RelValGetLabelVal(ref LbugValue relVal, out LbugValue outValue);

    [LibraryImport(LibraryName, EntryPoint = "lbug_rel_val_get_property_size")]
    internal static partial LbugState RelValGetPropertySize(ref LbugValue relVal, out ulong outValue);

    [LibraryImport(LibraryName, EntryPoint = "lbug_rel_val_get_property_name_at")]
    internal static partial LbugState RelValGetPropertyNameAt(ref LbugValue relVal, ulong index, out IntPtr outResult);

    [LibraryImport(LibraryName, EntryPoint = "lbug_rel_val_get_property_value_at")]
    internal static partial LbugState RelValGetPropertyValueAt(ref LbugValue relVal, ulong index, out LbugValue outValue);

    // ---- Memory ----------------------------------------------------------------------------------
    [LibraryImport(LibraryName, EntryPoint = "lbug_destroy_string")]
    internal static partial void DestroyString(IntPtr str);

    [LibraryImport(LibraryName, EntryPoint = "lbug_destroy_blob")]
    internal static partial void DestroyBlob(IntPtr blob);
}
#endif

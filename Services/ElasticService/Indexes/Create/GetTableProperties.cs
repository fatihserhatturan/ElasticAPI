using Dapper;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using ElasticAPI.Models.CreationModels;

namespace ElasticAPI.Services.ElasticService.Indexes.Create
{
    public class GetTableProperties
    {
        public async Task<bool> CheckTableHasDataAsync(string connectionString, string tableName)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        IF EXISTS (SELECT 1 
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_NAME = @TableName)
                        BEGIN
                            SELECT COUNT(1) 
                            FROM [" + tableName + @"]
                        END
                        ELSE
                            SELECT 0";

                    var parameters = new { TableName = tableName };
                    var count = await connection.QueryFirstOrDefaultAsync<int>(sql, parameters);
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{tableName} tablosu kontrolü sırasında hata oluştu", ex);
            }
        }

        public async Task<(IEnumerable<dynamic> Data, IEnumerable<ColumnSchema> Schema)> GetTableDataWithSchemaAsync(
            string connectionString,
            string tableName,
            string? whereClause = null,
            string? orderByClause = null)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var schemaQuery = @"
                        SELECT 
                            COLUMN_NAME as ColumnName,
                            DATA_TYPE as DataType,
                            CHARACTER_MAXIMUM_LENGTH as MaxLength,
                            IS_NULLABLE as IsNullable
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = @TableName
                        ORDER BY ORDINAL_POSITION";

                    var schema = await connection.QueryAsync<ColumnSchema>(
                        schemaQuery,
                        new { TableName = tableName }
                    );

                    var dataQuery = $"SELECT * FROM [{tableName}]";
                    if (!string.IsNullOrWhiteSpace(whereClause))
                    {
                        dataQuery += $" WHERE {whereClause}";
                    }
                    if (!string.IsNullOrWhiteSpace(orderByClause))
                    {
                        dataQuery += $" ORDER BY {orderByClause}";
                    }

                    var data = await connection.QueryAsync(dataQuery);
                    return (data, schema);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{tableName} tablosu verilerini alırken hata oluştu", ex);
            }
        }

        public static string SanitizeTableName(string tableName)
        {
            return tableName.Replace("'", "''")
                          .Replace("];", "")
                          .Replace("--", "");
        }
    }
}
using CovidTrackUS_Core.Enums;
using CovidTrackUS_Core.Interfaces;
using CovidTrackUS_Core.Models;
using CovidTrackUS_Core.Models.Data.TypeHandlers;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidTrackUS_Core.Services
{
    public class DataService : IDataService
    {
        readonly Settings _settings;
        public DataService(IOptions<Settings> settings)
        {
            _settings = settings.Value;
            SqlMapper.AddTypeHandler(typeof(HandleType), new HandleTypeHandler());
        }
        public async Task<List<T>> FindAsync<T>(string qry, DynamicParameters parameters = null)
        {
            using (var connection = new SqlConnection(_settings.DataConnectionString))
            {
                connection.Open();
                return (await connection.QueryAsync<T>(qry, parameters)).AsList();
            }
        }


        public async Task<T> QueryFirstOrDefaultAsync<T>(string qry, DynamicParameters parameters = null)
        {
            using (var connection = new SqlConnection(_settings.DataConnectionString))
            {
                connection.Open();
                return await connection.QueryFirstOrDefaultAsync<T>(qry, parameters);
            }
        }

        /// <summary>
        /// Executes a non-query, returns rows effects
        /// </summary>
        /// <param name="qry">The Sql to execute</param>
        /// <param name="parameters">Parameters for the sql</param>
        /// <returns>Number of rows effected by the non-query</returns>
        public async Task<int> ExecuteNonQueryAsync(string qry, DynamicParameters parameters = null)
        {
            using (var connection = new SqlConnection(_settings.DataConnectionString))
            {
                connection.Open();
                return await connection.ExecuteAsync(qry, parameters);
            }
        }

        /// <summary>
        /// Executes a scaler-query, returns a scaler result
        /// </summary>
        /// <param name="qry">The Sql to execute</param>
        /// <param name="parameters">Parameters for the sql</param>
        /// <returns>The scaler result of the scaler-query</returns>
        public async Task<object> ExecuteScalarAsync(string qry, DynamicParameters parameters = null)
        {
            using (var connection = new SqlConnection(_settings.DataConnectionString))
            {
                connection.Open();
                return await connection.ExecuteScalarAsync(qry, parameters);
            }
        }


        /// <summary>
        /// Executes an update of a <see cref="CovidTrackDO "/> object
        /// </summary>
        /// <param name="objToUpdate">The <see cref="CovidTrackDO "/> object to update in DB</param>
        /// <returns>Whether any records were updated</returns>
        public async Task<bool> ExecuteUpdateAsync<T>(T objToUpdate) where T : CovidTrackDO
        {
            using (var connection = new SqlConnection(_settings.DataConnectionString))
            {
                connection.Open();
                return await connection.UpdateAsync(objToUpdate);
            }
        }

        /// <summary>
        /// Executes an update of a collection of <see cref="CovidTrackDO "/> objects
        /// </summary>
        /// <param name="objsToUpdate">The IEnumerable <see cref="CovidTrackDO "/> objects to update in DB</param>
        /// <returns>Whether any records were updated</returns>
        public async Task<bool> ExecuteUpdateAsync<T>(IEnumerable<T> objsToUpdate) where T : CovidTrackDO
        {
            using (var connection = new SqlConnection(_settings.DataConnectionString))
            {
                connection.Open();
                return await connection.UpdateAsync(objsToUpdate);
            }
        }

        /// <summary>
        /// Executes an insert of a <see cref="CovidTrackDO "/> object
        /// </summary>
        /// <param name="objToInsert">The <see cref="CovidTrackDO "/> object to insert in DB</param>
        /// <returns>The ID of the new object</returns>
        public async Task<int> ExecuteInsertAsync<T>(T objToInsert) where T : CovidTrackDO
        {
            using (var connection = new SqlConnection(_settings.DataConnectionString))
            {
                connection.Open();
                return await connection.InsertAsync(objToInsert);
            }
        }

        /// <summary>
        /// Executes an insert of a collection of <see cref="CovidTrackDO "/> objects
        /// </summary>
        /// <param name="objsToInsert">The IEnumerable <see cref="CovidTrackDO "/> objects to insert into DB</param>
        /// <returns>Whether any records were inserted</returns>
        public async Task<int> ExecuteInsertAsync<T>(IEnumerable<T> objsToInsert) where T : CovidTrackDO
        {
            using (var connection = new SqlConnection(_settings.DataConnectionString))
            {
                connection.Open();
                return await connection.InsertAsync(objsToInsert);
            }
        }

        /// <summary>
        /// Executes multiple queries in one call in the order in which they are provided
        /// </summary>
        /// <param name="qry">The Sql to execute</param>
        /// <param name="mapItems">A List of <see cref="DataItemMap"/> objects that define each result set from this multi-statement query</param>
        /// <param name="parameters">Parameters for the sql</param>
        /// <returns>An <see cref="ExpandoObject"/> for the result set.  Needs to be cast at the source of call.</returns>
        public async Task<dynamic> QueryMultipleAsync(string qry, IEnumerable<DataItemMap> mapItems = null, DynamicParameters parameters = null)
        {
            var data = new ExpandoObject();
            using (var connection = new SqlConnection(_settings.DataConnectionString))
            {
                connection.Open();
                var multi = await connection.QueryMultipleAsync(qry, parameters);
                if (mapItems == null)
                {
                    return data;
                }
                foreach (var item in mapItems)
                {
                    if (item.DataRetrieveType == DataRetrieveType.FirstOrDefault)
                    {
                        var singleItem = multi.Read(item.Type).FirstOrDefault();
                        ((IDictionary<string, object>)data).Add(item.PropertyName, singleItem);
                    }

                    if (item.DataRetrieveType == DataRetrieveType.List)
                    {
                        var listItem = multi.Read(item.Type).ToList();
                        ((IDictionary<string, object>)data).Add(item.PropertyName, listItem);
                    }
                }
                return data;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CovidTrackUS_Core.Models;
using Dapper;

namespace CovidTrackUS_Core.Interfaces
{
    public interface IDataService
    {
        Task<int> ExecuteNonQueryAsync(string qry, DynamicParameters parameters = null);
        Task<object> ExecuteScalarAsync(string qry, DynamicParameters parameters = null);
        Task<List<T>> FindAsync<T>(string qry, DynamicParameters parameters = null);
        Task<T> QueryFirstOrDefaultAsync<T>(string qry, DynamicParameters parameters = null);
        Task<bool> ExecuteUpdateAsync<T>(T objToUpdate) where T : CovidTrackDO;
        Task<bool> ExecuteUpdateAsync<T>(IEnumerable<T> objsToUpdate) where T : CovidTrackDO;
        Task<int> ExecuteInsertAsync<T>(T objToInsert) where T : CovidTrackDO;
        Task<int> ExecuteInsertAsync<T>(IEnumerable<T> objsToInsert) where T : CovidTrackDO;
        Task<dynamic> QueryMultipleAsync(string qry, IEnumerable<DataItemMap> mapItems = null, DynamicParameters parameters = null);
        string HashText(string text, byte[] salt);
    }
}

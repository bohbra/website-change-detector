using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace WebsiteChangeDetector.Services
{
    public class BalboaTennisService : IBalboaTennisService
    {
        private readonly string _connectionString;

        public BalboaTennisService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<BlackoutDate>> GetAllBlackoutDatesAsync()
        {
            await using var connection = new SqlConnection(_connectionString);
            const string sql = "SELECT * FROM [BalboaTennisClub].[BlackoutDates]";
            var blackoutDates = await connection.QueryAsync<BlackoutDate>(sql);
            return blackoutDates;
        }

        public async Task<int> AddBlackoutDateAsync(BlackoutDate entity)
        {
            await using var connection = new SqlConnection(_connectionString);
            const string sql = "INSERT INTO [BalboaTennisClub].[BlackoutDates] (BlackoutDateTime, Reservation) VALUES (@BlackoutDateTime, @Reservation)";
            var parameters = new DynamicParameters();
            parameters.Add("BlackoutDateTime", entity.BlackoutDateTime, DbType.DateTime2);
            parameters.Add("Reservation", entity.Reservation, DbType.Boolean);
            return await connection.ExecuteAsync(sql, parameters);
        }
    }
}

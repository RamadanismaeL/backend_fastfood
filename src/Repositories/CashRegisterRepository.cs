using Dapper;
using Npgsql;
using unipos_basic_backend.src.Constants;
using unipos_basic_backend.src.Data;
using unipos_basic_backend.src.DTOs;
using unipos_basic_backend.src.Interfaces;

namespace unipos_basic_backend.src.Repositories
{
    public sealed class CashRegisterRepository(PostgresDb db, ILogger<CashRegisterRepository> logger) : ICashRegisterRepository
    {
        private readonly PostgresDb _db = db;
        private readonly ILogger<CashRegisterRepository> _logger = logger;

        public async Task<IEnumerable<CashRegisterListDTO>> GetAllAsync()
        {
            const string sql = @"
                SELECT
                    cr.id AS Id,
                    cr.status AS Status,
                    cr.opened_at AS OpenedAt,
                    cr.opening_balance AS OpeningBalance,  
                    cr.closing_balance AS ClosingBalance,  
                    cr.closed_at AS ClosedAt,    
                    u.username AS Operator
                FROM tbCashRegister cr
                JOIN tbUsers u ON cr.user_id = u.id
                ORDER BY cr.date_time DESC";

            await using var conn = _db.CreateConnection();
            return (await conn.QueryAsync<CashRegisterListDTO>(sql)).AsList();
        }

        public async Task<ResponseDTO> OpenRegisterAsync(CashRegisterOpenDTO cashRegister)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                const string sql = @"INSERT INTO tbCashRegister (opening_balance, user_id, date_time) VALUES (@OpeningBalance, @UserId, @DateTime)";

                var parameters = new
                {
                    cashRegister.OpeningBalance,
                    cashRegister.UserId,
                    DateTime = DateTime.UtcNow
                };

                var result = await conn.ExecuteAsync(sql, parameters);

                if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);

                return ResponseDTO.Success(MessagesConstant.CashOpened);
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "23505")
            {
                return ResponseDTO.Failure(MessagesConstant.CashOpenedError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " - Failed to open cash register.");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }

        public async Task<ResponseDTO> CloseRegisterAsync(CashRegisterCloseDTO cashRegister)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                const string sqlExist = @"SELECT 1 FROM tbCashRegister WHERE id = @Id";
                var exists = await conn.QueryFirstOrDefaultAsync<int>(sqlExist, new { cashRegister.Id });

                if (exists != 1) return ResponseDTO.Failure(MessagesConstant.NotFound);

                var sql = @"UPDATE tbCashRegister SET status = FALSE, closing_balance = @ClosingBalance, closed_at = NOW(), date_time = NOW() WHERE id = @Id";

                var parameters = new
                {
                    cashRegister.Id,
                    cashRegister.ClosingBalance
                };

                var result = await conn.ExecuteAsync(sql, parameters);

                if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);

                return ResponseDTO.Success(MessagesConstant.CashClosed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " - Failed to close cash register.");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }

        public async Task<ResponseDTO> DeleteAsync(Guid id)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                const string sql = @"DELETE FROM tbCashRegister WHERE id = @Id";
                var affectedRows = await conn.ExecuteAsync(sql, new { Id = id });

                if (affectedRows == 0) return ResponseDTO.Failure(MessagesConstant.NotFound);

                return ResponseDTO.Success(MessagesConstant.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting cash register");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }        
    }
}
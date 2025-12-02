using Dapper;
using Npgsql;
using unipos_basic_backend.src.Constants;
using unipos_basic_backend.src.Services;
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
                    cr.id,
                    u.username AS Operator,
                    cr.is_opened AS Status,
                    COALESCE(MAX(crd.amount) FILTER (
                        WHERE crd.cash_name = 'opened' AND crd.is_confirmed = TRUE
                    ), 0.00) AS TotalOpened,
                    COALESCE(MAX(crd.amount) FILTER (
                        WHERE crd.cash_name = 'closed' AND crd.is_confirmed = TRUE
                    ), 0.00) AS TotalClosed,
                    MAX(crd.created_at) FILTER (
                        WHERE crd.cash_name = 'opened' AND crd.is_confirmed = TRUE
                    ) AS OpenedAt,    
                    MAX(crd.created_at) FILTER (
                        WHERE crd.cash_name = 'closed' AND crd.is_confirmed = TRUE
                    ) AS ClosedAt
                FROM tbCashRegister cr
                JOIN tbUsers u ON cr.user_id = u.id
                LEFT JOIN tbCashRegisterDetails crd ON cr.id = crd.cash_register_id
                GROUP BY
                    cr.id,
                    u.username,
                    cr.is_opened
                ORDER BY MAX(crd.date_time) DESC NULLS LAST";

            await using var conn = _db.CreateConnection();
            return (await conn.QueryAsync<CashRegisterListDTO>(sql)).AsList();
        }

        public async Task<ResponseDTO> OpenRegisterAsync(CashRegisterOpenDTO cashRegister)
        {
            await using var conn = _db.CreateConnection();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                const string sqlInsertCash = @"INSERT INTO tbCashRegister (user_id) 
                VALUES (@UserId)
                RETURNING id";

                var cashId = await conn.ExecuteScalarAsync<Guid>
                (
                    sqlInsertCash,
                    new { cashRegister.UserId },
                    tx
                );                

                const string sqlInsertCashDetail = @"INSERT INTO tbCashRegisterDetails (cash_register_id, cash_name, amount, description, date_time) 
                VALUES (@CashId, @CashName::cash_name_enum, @Amount, @Description, NOW())";

                var parameters = new
                {
                    CashId = cashId,
                    CashName = "opened",
                    cashRegister.Amount,
                    Description = "Caixa Aberto"
                };

                var result = await conn.ExecuteAsync(sqlInsertCashDetail, parameters, tx);

                if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);

                await tx.CommitAsync();
                return ResponseDTO.Success(MessagesConstant.CashOpened);
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "23505")
            {
                await tx.RollbackAsync();
                return ResponseDTO.Failure(MessagesConstant.CashOpenedError);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, " - Failed to open cash register.");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }

        public async Task<ResponseDTO> CloseRegisterAsync(CashRegisterCloseDTO cashRegister)
        {
            await using var conn = _db.CreateConnection();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                const string sqlExist = @"SELECT 1 FROM tbCashRegister WHERE id = @Id";

                const string sqlInsertCashDetail = @"INSERT INTO tbCashRegisterDetails (cash_register_id, cash_name, amount, description, date_time) 
                VALUES (@CashRegisterId, @CashName::cash_name_enum, @Amount, @Description, NOW())";

                var sqlUpdateCash = @"UPDATE tbCashRegister SET is_opened = FALSE WHERE id = @Id";

                var exists = await conn.QueryFirstOrDefaultAsync<int>(sqlExist, new { Id = cashRegister.CashRegisterId });                

                var parameters = new
                {
                    cashRegister.CashRegisterId,
                    CashName = "closed",
                    cashRegister.Amount,
                    Description = "Caixa Fechado"
                };

                await conn.ExecuteAsync(sqlInsertCashDetail, parameters, tx);

                var result = await conn.ExecuteAsync(
                    sqlUpdateCash,
                    new { Id = cashRegister.CashRegisterId },
                     tx
                );

                if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);

                await tx.CommitAsync();
                return ResponseDTO.Success(MessagesConstant.CashClosed);
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "23505")
            {
                await tx.RollbackAsync();
                return ResponseDTO.Failure("Erro ao fechar caixa");
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

        public async Task<IEnumerable<CashRegisterSelectUserDTO>> GetSelectUserToOpenCash()
        {
            const string sql = @"
                SELECT DISTINCT ON (u.id)
                    u.id,
                    u.username
                FROM tbUsers u
                LEFT JOIN (
                    SELECT DISTINCT ON (cr.user_id)
                        cr.user_id,
                        cr.is_opened
                    FROM tbCashRegister cr
                    JOIN tbCashRegisterDetails crd 
                        ON cr.id = crd.cash_register_id
                    ORDER BY cr.user_id, crd.date_time DESC
                ) latest 
                    ON latest.user_id = u.id
                WHERE 
                    latest.is_opened = FALSE
                    OR latest.user_id IS NULL
                ORDER BY 
                    u.id,
                    u.username ASC";

            await using var conn = _db.CreateConnection();
            return (await conn.QueryAsync<CashRegisterSelectUserDTO>(sql)).AsList();
        } 

        public async Task<IEnumerable<CashRegisterSelectUserDTO>> GetSelectUserToCloseCash()
        {
            const string sql = @"
                SELECT *
                FROM (
                    SELECT DISTINCT ON (cr.user_id)
                        cr.id AS Id,
                        u.username AS Username
                    FROM tbCashRegister cr
                    JOIN tbUsers u ON cr.user_id = u.id
                    WHERE cr.is_opened = TRUE
                    ORDER BY cr.user_id DESC
                ) sub
                ORDER BY Username ASC";

            await using var conn = _db.CreateConnection();
            return (await conn.QueryAsync<CashRegisterSelectUserDTO>(sql)).AsList();
        }   

        public async Task<IEnumerable<CashRegisterDetailListDTO>> GetAllDetails(Guid cashRegisterId)
        {
            const string sql = @"
                SELECT
                    crd.id As Id,
                    crd.cash_name AS CashName,
                    crd.amount AS Amount,
                    crd.description AS Description,
                    crd.is_confirmed AS Status,
                    crd.created_at AS CreatedAt,
                    crd.date_time AS UpdatedAt
                FROM tbCashRegister cr
                JOIN tbCashRegisterDetails crd ON cr.id = crd.cash_register_id
                WHERE cr.id = @Id
                ORDER BY crd.date_time DESC";

            await using var conn = _db.CreateConnection();
            return (await conn.QueryAsync<CashRegisterDetailListDTO>(sql, new { Id = cashRegisterId })).AsList();
        }   

        public async Task<ResponseDTO> CreateCashDetails(CashRegisterDetailCreateDTO cashRegister)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                const string sql = @"INSERT INTO tbCashRegisterDetails (cash_register_id, cash_name, amount, description, date_time) VALUES (@CashRegisterId, @CashName::cash_name_enum, @Amount, @Description, NOW())";

                var parameters = new
                {
                    cashRegister.CashRegisterId,
                    cashRegister.CashName,
                    cashRegister.Amount,
                    cashRegister.Description
                };

                var result = await conn.ExecuteAsync(sql, parameters);

                if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);

                return ResponseDTO.Success(MessagesConstant.Created);
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "23505")
            {
                return ResponseDTO.Failure(MessagesConstant.CashOpenedError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " - Failed to create cash register details.");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }

        public async Task<ResponseDTO> UpdateCashDetails(CashRegisterDetailUpdateDTO cashRegister)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                const string sqlExist = @"SELECT 1 FROM tbCashRegisterDetails WHERE id = @Id";
                var exists = await conn.QueryFirstOrDefaultAsync<int>(sqlExist, new { cashRegister.Id });

                if (exists != 1) return ResponseDTO.Failure(MessagesConstant.NotFound);

                var sql = @"UPDATE tbCashRegisterDetails SET is_confirmed = @Status, date_time = NOW() WHERE id = @Id";

                var parameters = new
                {
                    cashRegister.Id,
                    cashRegister.Status
                };

                var result = await conn.ExecuteAsync(sql, parameters);

                if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);

                return ResponseDTO.Success(MessagesConstant.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " - Failed to update cash register detail.");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }
    }
}
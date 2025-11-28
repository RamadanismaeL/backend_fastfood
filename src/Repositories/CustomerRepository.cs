using Dapper;
using unipos_basic_backend.src.Constants;
using unipos_basic_backend.src.DTOs;
using unipos_basic_backend.src.Interfaces;
using unipos_basic_backend.src.Services;

namespace unipos_basic_backend.src.Repositories
{
    public sealed class CustomerRepository(PostgresDb db, ILogger<CustomerRepository> logger) : ICustomerRepository
    {
        private readonly PostgresDb _db = db;
        private readonly ILogger<CustomerRepository> _logger = logger;

        public async Task<IEnumerable<CustomerListDTO>> GetAllAsync()
        {
            const string sql = @"
                SELECT id, fullname AS FullName, phone_number AS phoneNumber, order_qty AS OrderQty, created_at AS createdAt
                FROM tbCustomers
                ORDER BY created_at DESC";

            await using var conn = _db.CreateConnection();
            return (await conn.QueryAsync<CustomerListDTO>(sql)).AsList();
        }

        public async Task<ResponseDTO> CreateAsync(CustomerCreateDTO customer)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                const string sqlExist = @"SELECT 1 FROM tbCustomers WHERE fullname = @FullName";
                var exists = await conn.QueryFirstOrDefaultAsync<int>(sqlExist, new {customer.FullName});

                if (exists == 1) return ResponseDTO.Failure(MessagesConstant.AlreadyExists);

                const string sqlInsert = @"INSERT INTO tbCustomers (fullname, phone_number, order_qty)
                VALUES (@FullName, @PhoneNumber, @OrderQty)";

                var parameters = new
                {
                    customer.FullName,
                    customer.PhoneNumber,
                    customer.OrderQty
                };

                var result = await conn.ExecuteAsync(sqlInsert, parameters);

                if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);

                return ResponseDTO.Success(MessagesConstant.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " - Failed to create customer.");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }

        public async Task<ResponseDTO> UpdateAsync(CustomerUpdateDTO customer)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                // 1. Check if user exists
                const string checkSql = @"SELECT 1 FROM tbCustomers WHERE id = @Id";
                var exists = await conn.QueryFirstOrDefaultAsync<int>(checkSql, new { customer.Id });

                if (exists != 1)
                    return ResponseDTO.Failure(MessagesConstant.NotFound);

                var updates = new List<string>();
                var parameters = new DynamicParameters();
                parameters.Add("@Id", customer.Id);

                if (!string.IsNullOrWhiteSpace(customer.FullName))
                {
                    updates.Add("fullname = @FullName");
                    parameters.Add("@FullName", customer.FullName);
                }

                if (!string.IsNullOrWhiteSpace(customer.PhoneNumber))
                {
                    updates.Add("phone_number = @PhoneNumber");
                    parameters.Add("@PhoneNumber", customer.PhoneNumber);
                }

                updates.Add("order_qty = @OrderQty");
                parameters.Add("@OrderQty", customer.OrderQty);

                updates.Add("updated_at = NOW()");

                if (updates.Count == 1)
                {
                    return ResponseDTO.Failure(MessagesConstant.NoChanges);
                }

                // 3. Build final SQL
                var sql = $"UPDATE tbCustomers SET {string.Join(", ", updates)} WHERE id = @Id";

                var result = await conn.ExecuteAsync(sql, parameters);

                if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);

                return ResponseDTO.Success(MessagesConstant.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to patch this customer");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }

        public async Task<ResponseDTO> DeleteAsync(Guid id)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                const string sql = @"DELETE FROM tbCustomers WHERE id = @Id";
                var affectedRows = await conn.ExecuteAsync(sql, new { Id = id });

                if (affectedRows == 0) return ResponseDTO.Failure(MessagesConstant.NotFound);

                return ResponseDTO.Success(MessagesConstant.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer.");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }
    }    
}
using Dapper;
using unipos_basic_backend.src.Constants;
using unipos_basic_backend.src.Data;
using unipos_basic_backend.src.DTOs;
using unipos_basic_backend.src.Interfaces;

namespace unipos_basic_backend.src.Repositories
{
    public sealed class IngredientsRepository(PostgresDb db, ILogger<IngredientsRepository> logger) : IIngredientsRepository
    {
        private readonly PostgresDb _db = db;
        private readonly ILogger<IngredientsRepository> _logger = logger;

        public async Task<IEnumerable<IngredientsListDTO>> GetAllAsync()
        {
            const string sql = @"SELECT id, item_name AS ItemName, batch_number AS BatchNumber, package_size AS PackageSize, unit_of_measure AS UnitOfMeasure, quantity, unit_cost_price AS UnitCostPrice, total_cost_price AS TotalCostPrice, expiration_at AS ExpirationAt, expiration_status AS ExpirationStatus, is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt FROM tbIngredients ORDER BY ItemName ASC";

            await using var conn = _db.CreateConnection();
            return (await conn.QueryAsync<IngredientsListDTO>(sql)).AsList();
        }

        public async Task<ResponseDTO> CreateAsync(IngredientsCreateDTO ingredient)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                const string sqlExist = @"SELECT 1 FROM tbIngredients WHERE item_name = @ItemName";
                var exists = await conn.QueryFirstOrDefaultAsync<int>(sqlExist, new {ingredient.ItemName});

                if (exists == 1) return ResponseDTO.Failure(MessagesConstant.AlreadyExists);

                const string sqlInsert = @"INSERT INTO tbIngredients (id, item_name, batch_number, package_size, unit_of_measure, quantity, unit_cost_price, expiration_at, expiration_status)
                VALUES (@Id, @ItemName, @BatchNumber, @PackageSize, @UnitOfMeasure, @Quantity, @UnitCostPrice, @ExpirationAt, @ExpirationStatus)";

                var parameters = new
                {
                    Id = Guid.NewGuid(),
                    ingredient.ItemName,
                    ingredient.BatchNumber,
                    ingredient.PackageSize,
                    ingredient.UnitOfMeasure,
                    ingredient.Quantity,
                    ingredient.UnitCostPrice,
                    ingredient.ExpirationAt,
                    ExpirationStatus = GetExpirationStatus(ingredient.ExpirationAt)
                };

                var result = await conn.ExecuteAsync(sqlInsert, parameters);

                if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);

                return ResponseDTO.Success(MessagesConstant.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " - Failed to create ingredient.");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }

        public async Task<ResponseDTO> UpdateAsync(IngredientsUpdateDTO ingredient)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                const string sqlExist = @"SELECT 1 FROM tbIngredients WHERE id = @Id";
                var exists = await conn.QueryFirstOrDefaultAsync<int>(sqlExist, new { ingredient.Id });

                if (exists != 1) return ResponseDTO.Failure(MessagesConstant.NotFound);

                var updates = new List<string>();
                var parameters = new DynamicParameters();

                parameters.Add("@Id", ingredient.Id);

                if (!string.IsNullOrWhiteSpace(ingredient.ItemName))
                {
                    updates.Add("item_name = @ItemName");
                    parameters.Add("@ItemName", ingredient.ItemName);
                }
                
                updates.Add("batch_number = @BatchNumber");
                parameters.Add("@BatchNumber", ingredient.BatchNumber);

                updates.Add("package_size = @PackageSize");
                parameters.Add("@PackageSize", ingredient.PackageSize);

                if (!string.IsNullOrWhiteSpace(ingredient.UnitOfMeasure))
                {
                    updates.Add("unit_of_measure = @UnitOfMeasure");
                    parameters.Add("@UnitOfMeasure", ingredient.UnitOfMeasure);
                }

                updates.Add("quantity = @Quantity");
                parameters.Add("@Quantity", ingredient.Quantity);

                updates.Add("unit_cost_price = @UnitCostPrice");
                parameters.Add("@UnitCostPrice", ingredient.UnitCostPrice);

                updates.Add("expiration_at = @ExpirationAt");
                parameters.Add("@ExpirationAt", ingredient.ExpirationAt);

                updates.Add("expiration_status = @ExpirationStatus");
                parameters.Add("@ExpirationStatus", GetExpirationStatus(ingredient.ExpirationAt));

                updates.Add("is_active = @IsActive");
                parameters.Add("@IsActive", ingredient.IsActive);

                updates.Add("updated_at = NOW()");

                if (updates.Count == 1) return ResponseDTO.Failure(MessagesConstant.NoChanges);

                var sql = $@"UPDATE tbIngredients SET {string.Join(",", updates)} WHERE id = @Id";

                var result = await conn.ExecuteAsync(sql, parameters);

                return ResponseDTO.Success(MessagesConstant.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " - Failed to update ingredient.");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }

        public async Task<ResponseDTO> DeleteAsync(Guid id)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                const string sql = @"DELETE FROM tbIngredients WHERE id = @Id";
                var affectedRows = await conn.ExecuteAsync(sql, new { Id = id });

                if (affectedRows == 0) return ResponseDTO.Failure(MessagesConstant.NotFound);

                return ResponseDTO.Success(MessagesConstant.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting ingredient with ID: {id}");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }        

        private static string GetExpirationStatus(DateTime? expirationAt)
        {
            if (expirationAt is null) return null!;
            const int NearExpiryDays = 30;
            var utcNow = DateTime.UtcNow;

            if (expirationAt < utcNow)
                return ExpirationStatusConstant.Expired;

            if (expirationAt <= utcNow.AddDays(NearExpiryDays))
                return ExpirationStatusConstant.NearExpiry;

            return ExpirationStatusConstant.Valid;
        }
    }
}
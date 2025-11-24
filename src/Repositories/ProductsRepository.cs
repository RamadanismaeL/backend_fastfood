using Dapper;
using unipos_basic_backend.src.Configs;
using unipos_basic_backend.src.Constants;
using unipos_basic_backend.src.Data;
using unipos_basic_backend.src.DTOs;
using unipos_basic_backend.src.Interfaces;

namespace unipos_basic_backend.src.Repositories
{
    public sealed class ProductsRepository(PostgresDb db, IHttpContextAccessor httpContextAcc, ILogger<ProductsRepository> logger) : IProductsRepository
    {
        private readonly PostgresDb _db = db;
        private readonly IHttpContextAccessor _httpContextAcc = httpContextAcc;
        private readonly ILogger<ProductsRepository> _logger = logger;

        public async Task<IEnumerable<ProductsListDTO>> GetAllAsync()
        {
            const string sql = @"SELECT id AS Id, item_name AS ItemName, image_url AS ImageUrl, price AS Price, category AS Category, is_active AS IsActive, created_at AS CreatedAt FROM tbProducts ORDER BY ItemName ASC";

            await using var conn = _db.CreateConnection();
            return (await conn.QueryAsync<ProductsListDTO>(sql)).AsList();
        }

        public async Task<ResponseDTO> CreateAsync(ProductsCreateDTO products)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                const string sqlExist = @"SELECT 1 FROM tbProducts WHERE item_name = @ItemName";
                var exists = await conn.QueryFirstOrDefaultAsync<int>(sqlExist, new {products.ItemName});

                if (exists == 1) return ResponseDTO.Failure(MessagesConstant.AlreadyExists);

                var imageUrl = string.Empty;
                if (products.ImageUrl is not null && products.ImageUrl.Length > 0)
                {
                    var fileName = await FileUploadConfig.UploadFile(products.ImageUrl);
                    var request = _httpContextAcc.HttpContext!.Request;
                    imageUrl = $"{request.Scheme}://{request.Host}/images/{fileName}";
                }

                const string sqlInsert = @"INSERT INTO tbProducts (item_name, image_url, price, category) VALUES (@ItemName, @ImageUrl, @Price, @Category)";

                var parameters = new
                {
                    products.ItemName,
                    ImageUrl = imageUrl,
                    products.Price,
                    products.Category
                };

                var result = await conn.ExecuteAsync(sqlInsert, parameters);

                if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);

                return ResponseDTO.Success(MessagesConstant.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " - Failed to create product");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }

        public async Task<ResponseDTO> UpdateAsync(ProductsUpdateDTO products)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                const string sqlExist = @"SELECT 1 FROM tbProducts WHERE id = @Id";
                var exists = await conn.QueryFirstOrDefaultAsync<int>(sqlExist, new { products.Id });

                if (exists != 1) return ResponseDTO.Failure(MessagesConstant.NotFound);

                var updates = new List<string>();
                var parameters = new DynamicParameters();

                parameters.Add("@Id", products.Id);

                if (!string.IsNullOrWhiteSpace(products.ItemName))
                {
                    updates.Add("item_name = @ItemName");
                    parameters.Add("@ItemName", products.ItemName);
                }                

                const string getImageSql = @"SELECT image_url FROM tbProducts WHERE id = @Id";
                var imagePath = await conn.QueryFirstOrDefaultAsync<string>(getImageSql, new { products.Id });

                if (!string.IsNullOrEmpty(imagePath))
                {
                    try
                    {
                        var oldImageName = Path.GetFileName(new Uri(imagePath).LocalPath);
                        if (!string.IsNullOrEmpty(oldImageName))
                        {
                            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

                            var oldImagePath = Path.Combine(uploadsFolder, oldImageName);

                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting image file.");
                    }
                }       

                var imageUrl = string.Empty;
                if (products.ImageUrl is not null && products.ImageUrl.Length > 0)
                {
                    var fileName = await FileUploadConfig.UploadFile(products.ImageUrl);
                    var request = _httpContextAcc.HttpContext!.Request;
                    imageUrl = $"{request.Scheme}://{request.Host}/images/{fileName}";
                }  

                updates.Add("image_url = @ImageUrl");
                parameters.Add("@ImageUrl", imageUrl);

                updates.Add("price = @Price");
                parameters.Add("@Price", products.Price);

                updates.Add("category = @Category");
                parameters.Add("@Category", products.Category);

                updates.Add("is_active = @IsActive");
                parameters.Add("@IsActive", products.IsActive);

                if (updates.Count == 1) return ResponseDTO.Failure(MessagesConstant.NoChanges);

                var sql = $@"UPDATE tbProducts SET {string.Join(",", updates)} WHERE id = @Id";

                var result = await conn.ExecuteAsync(sql, parameters);

                if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);

                return ResponseDTO.Success(MessagesConstant.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " - Failed to update product.");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }

        public async Task<ResponseDTO> DeleteAsync(Guid id)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                const string getImageSql = @"SELECT image_url FROM tbProducts WHERE id = @Id";
                var imagePath = await conn.QueryFirstOrDefaultAsync<string>(getImageSql, new { Id = id });

                const string deleteSql = @"DELETE FROM tbProducts WHERE id = @Id";                
                var affectedRows = await conn.ExecuteAsync(deleteSql, new { Id = id });

                if (affectedRows == 0) return ResponseDTO.Failure(MessagesConstant.NotFound);

                if (!string.IsNullOrEmpty(imagePath))
                {
                    try
                    {
                        var oldImageName = Path.GetFileName(new Uri(imagePath).LocalPath);
                        if (!string.IsNullOrEmpty(oldImageName))
                        {
                            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

                            var oldImagePath = Path.Combine(uploadsFolder, oldImageName);

                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting image file.");
                    }
                }

                return ResponseDTO.Success(MessagesConstant.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting product with ID: {id}");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }
    }
}
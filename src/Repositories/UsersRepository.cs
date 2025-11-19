using Dapper;
using unipos_basic_backend.src.Configs;
using unipos_basic_backend.src.Data;
using unipos_basic_backend.src.DTOs;
using unipos_basic_backend.src.Interfaces;

namespace unipos_basic_backend.src.Repositories
{
    public sealed class UsersRepository (PostgresDb db, IHttpContextAccessor httpContextAcc, ILogger<UsersRepository> logger) : IUsersRepository
    {
        private readonly PostgresDb _db = db;
        private readonly IHttpContextAccessor _httpContextAcc = httpContextAcc;
        private readonly ILogger<UsersRepository> _logger = logger;

        public async Task<IEnumerable<UsersListDTO>> GetAllAsync()
        {
            const string sql = @"
            SELECT id, username, phone_number AS phoneNumber, roles, images AS image, is_active AS is_Active, created_at AS createdAt, updated_at as updatedAt
            FROM tbUsers
            ORDER BY created_at DESC";

            await using var conn = _db.CreateConnection();
            return (await conn.QueryAsync<UsersListDTO>(sql)).AsList();
        }

        public async Task<ResponseDTO> CreateAsync(UsersCreateDTO user)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                const string checkSql = @"SELECT 1 FROM tbUsers WHERE username = @Username";
                var userExists = await conn.QueryFirstOrDefaultAsync<int>(checkSql, new { user.Username });

                if (userExists == 1) return new ResponseDTO { IsSuccess = false, Message = "Username already exists." };

                var imageUrl = string.Empty;
                if (user.Image is not null && user.Image.Length > 0)
                {
                    var fileName = await FileUploadConfig.UploadFile(user.Image);
                    var request = _httpContextAcc.HttpContext!.Request;
                    imageUrl = $"{request.Scheme}://{request.Host}/images/{fileName}";
                }


                const string insertSql = @"INSERT INTO tbUsers (id, username, phone_number, roles, password_hash, images) VALUES (@Id, @Username, @PhoneNumber, @Roles, @Password, @Image)";

                var parameters = new
                {
                    Id = Guid.NewGuid(),
                    user.Username,
                    user.PhoneNumber,
                    user.Roles,
                    Password = BCrypt.Net.BCrypt.HashPassword(user.Password),
                    Image = imageUrl
                };

                var result = await conn.ExecuteAsync(insertSql, parameters);

                if (result == 0) return new ResponseDTO { IsSuccess = false, Message = "Failed to create user. Please try again." };

                return new ResponseDTO
                {
                    IsSuccess = true,
                    Message = "User created successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " - Failed to create user. Please try again.");
                return new ResponseDTO
                {
                    IsSuccess = false,
                    Message = "Failed to create user. Please try again."
                };
            }
        }

        public async Task<ResponseDTO> CreateDeftsAsync(UsersCreateDeftsDTO user)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                const string checkSql = @"SELECT 1 FROM tbUsers WHERE username = @Username";
                var userExists = await conn.QueryFirstOrDefaultAsync<int>(checkSql, new { user.Username });

                if (userExists == 1) return new ResponseDTO { IsSuccess = false, Message = "Username already exists." };

                const string insertSql = @"INSERT INTO tbUsers (id, username, phone_number, roles, password_hash, images) VALUES (@Id, @Username, @PhoneNumber, @Roles, @Password, @Image)";

                const string passwordDefault = "123456";

                var parameters = new
                {
                    Id = Guid.NewGuid(),
                    user.Username,
                    user.PhoneNumber,
                    Roles = "user",
                    Password = BCrypt.Net.BCrypt.HashPassword(passwordDefault),
                    Image = ""
                };

                var result = await conn.ExecuteAsync(insertSql, parameters);

                if (result == 0) return new ResponseDTO { IsSuccess = false, Message = "Failed to create user. Please try again." };

                return new ResponseDTO
                {
                    IsSuccess = true,
                    Message = "User created successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " - Failed to create user. Please try again.");
                return new ResponseDTO
                {
                    IsSuccess = false,
                    Message = "Failed to create user. Please try again."
                };
            }
        }

        public async Task<ResponseDTO> UpdateAsync(UsersUpdateDTO user)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                // 1. Check if user exists
                const string checkSql = @"SELECT 1 FROM tbUsers WHERE id = @Id";
                var exists = await conn.QueryFirstOrDefaultAsync<int>(checkSql, new { Id = user.Id });

                if (exists != 1)
                    return new ResponseDTO { IsSuccess = false, Message = "User not found." };

                var updates = new List<string>();
                var parameters = new DynamicParameters();
                parameters.Add("@Id", user.Id);

                if (!string.IsNullOrWhiteSpace(user.Username))
                {
                    updates.Add("username = @Username");
                    parameters.Add("@Username", user.Username);
                }

                if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    updates.Add("phone_number = @PhoneNumber");
                    parameters.Add("@PhoneNumber", user.PhoneNumber);
                }

                if (!string.IsNullOrEmpty(user.Roles))
                {
                    updates.Add("roles = @Roles");
                    parameters.Add("@Roles", user.Roles);
                }

                updates.Add("is_active = @Is_Active");
                parameters.Add("@Is_Active", user.Is_Active);

                if (!string.IsNullOrWhiteSpace(user.Password))
                {
                    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);
                    updates.Add("password_hash = @PasswordHash");
                    parameters.Add("@PasswordHash", hashedPassword);
                }

                updates.Add("updated_at = NOW()");

                if (updates.Count == 1)
                {
                    return new ResponseDTO { IsSuccess = true, Message = "No changes detected." };
                }

                // 3. Build final SQL
                var sql = $"UPDATE tbUsers SET {string.Join(", ", updates)} WHERE id = @Id";

                var result = await conn.ExecuteAsync(sql, parameters);

                return new ResponseDTO
                {
                    IsSuccess = true,
                    Message = "User updated successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to patch user {Username}:", user.Username);
                return new ResponseDTO
                {
                    IsSuccess = false,
                    Message = "Failed to update user. Please try again."
                };
            }
        }

        public async Task<ResponseDTO> DeleteAsync(Guid id)
        {
            try
            {
                await using var conn = _db.CreateConnection();

                const string getImageSql = @"SELECT images FROM tbUsers WHERE id = @Id";
                var imagePath = await conn.QueryFirstOrDefaultAsync<string>(getImageSql, new { Id = id });

                const string deleteSql = @"DELETE FROM tbUsers WHERE id = @id";                
                var affectedRows = await conn.ExecuteAsync(deleteSql, new { Id = id });

                if (affectedRows == 0) return new ResponseDTO { IsSuccess = false, Message = $"No user found with ID: {id}" };

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

                return new ResponseDTO
                {
                    IsSuccess = true,
                    Message = $"User deleted successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user with ID: {id}");
                return new ResponseDTO
                {
                    IsSuccess = false,
                    Message = $"Error deleting user with ID: {id}"
                };
            }
        }
    }
}
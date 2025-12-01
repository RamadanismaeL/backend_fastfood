using Dapper;
using unipos_basic_backend.src.Constants;
using unipos_basic_backend.src.DTOs;
using unipos_basic_backend.src.Interfaces;
using unipos_basic_backend.src.Services;

namespace unipos_basic_backend.src.Repositories
{
    public sealed class OrdersRepository(PostgresDb db, ILogger<OrdersRepository> logger) : IOrdersRepository
    {
        private readonly PostgresDb _db = db;
        private readonly ILogger<OrdersRepository> _logger = logger;

        public async Task<IEnumerable<OrdersListDTO>> GetAllAsync()
        {
            const string sql = @"
                SELECT
                    c.id AS Id,
                    c.fullName AS FullName,
                    c.phone_number AS PhoneNumber,
                    
                    STRING_AGG(DISTINCT p.item_name, '  ••  ') 
                        FILTER (WHERE o.status IN (1, 2)) 
                        AS Description,

                    COALESCE(SUM(o.quantity) FILTER (WHERE o.status IN (1, 2)), 0)     AS TotalQty,

                    COALESCE(SUM(o.total_to_pay) FILTER (WHERE o.status IN (1, 2)), 0) AS TotalPay,

                    MAX(o.created_at) FILTER (WHERE o.status IN (1, 2))               AS CreatedAt

                FROM tbCustomers c
                LEFT JOIN tbOrders o 
                    ON o.customer_id = c.id 
                AND o.status IN (1, 2)
                LEFT JOIN tbProducts p 
                    ON p.id = o.product_id

                GROUP BY 
                    c.id, 
                    c.fullName, 
                    c.phone_number

                ORDER BY 
                    CreatedAt DESC NULLS LAST";

            await using var conn = _db.CreateConnection();
            return (await conn.QueryAsync<OrdersListDTO>(sql)).AsList();
        }

        public async Task<ResponseDTO> CreateAsync(OrdersCreateDTO order)
        {
            await using var conn = _db.CreateConnection();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Insert customer and retrieve generated ID
                const string sqlInsertCustomer = """
                    INSERT INTO tbCustomers (fullName, phone_number)
                    VALUES (@CustomerFullName, @CustomerPhoneNumber)
                    RETURNING id;
                    """;

                var customerId = await conn.ExecuteScalarAsync<Guid>(
                    sqlInsertCustomer,
                    new { order.CustomerFullName, order.CustomerPhoneNumber },
                    tx);

                // Reusable queries
                const string sqlGetPrice = """
                    SELECT price FROM tbProducts WHERE id = @ProductId LIMIT 1;
                    """;

                const string sqlGetIngredients = """
                    SELECT
                        ip.ingredient_id,
                        ip.quantity,
                        i.item_name || ' ' || i.package_size || '' || i.unit_of_measure AS ItemName
                    FROM tbIngredientsProducts ip
                    JOIN tbIngredients i ON i.id = ip.ingredient_id
                    WHERE product_id = @ProductId;
                    """;

                const string sqlCheckStock = """
                    SELECT quantity FROM tbIngredients WHERE id = @IngredientId FOR UPDATE;
                    """;

                const string sqlConsumeStock = """
                    UPDATE tbIngredients 
                    SET quantity = quantity - @QtyUsed, updated_at = NOW() 
                    WHERE id = @IngredientId;
                    """;

                const string sqlInsertOrderItem = """
                    INSERT INTO tbOrders (customer_id, product_id, quantity, price)
                    VALUES (@CustomerId, @ProductId, @Quantity, @Price);
                    """;

                if (order.OrderItems?.Any() != true)
                    return ResponseDTO.Failure(MessagesConstant.OperationFailed);

                foreach (var item in order.OrderItems)
                {
                    // 1. Get unit price
                    var unitPrice = await conn.ExecuteScalarAsync<decimal>(
                        sqlGetPrice, new { item.ProductId }, tx);

                    if (unitPrice <= 0)
                        return ResponseDTO.Failure(MessagesConstant.NotFound);

                    // 2. Load required ingredients
                    var ingredients = await conn.QueryAsync<(Guid IngredientId, decimal RequiredQty, string ItemName)>(
                        sqlGetIngredients, new { item.ProductId }, tx);

                    // 3. Check & lock stock + consume atomically
                    foreach (var ing in ingredients)
                    {
                        var qtyNeeded = ing.RequiredQty * item.Quantity;

                        var currentStock = await conn.ExecuteScalarAsync<decimal>(
                            sqlCheckStock, new { IngredientId = ing.IngredientId }, tx);

                        if (currentStock < qtyNeeded)
                            return ResponseDTO.Failure($"Insufficient stock for ingredient {ing.ItemName}. Required: {qtyNeeded}, Available: {currentStock}");

                        await conn.ExecuteAsync(
                            sqlConsumeStock,
                            new { IngredientId = ing.IngredientId, QtyUsed = qtyNeeded },
                            tx);
                    }

                    // 4. Persist order item
                    await conn.ExecuteAsync(
                        sqlInsertOrderItem,
                        new
                        {
                            CustomerId = customerId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            Price = unitPrice * item.Quantity
                        },
                        tx);
                }

                await tx.CommitAsync();
                return ResponseDTO.Success(MessagesConstant.Created);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to create order for customer {FullName} ({Phone})", 
                    order.CustomerFullName, order.CustomerPhoneNumber);
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }
    }
}
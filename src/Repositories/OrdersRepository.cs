using Dapper;
using Npgsql;
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
                        AS Description,

                    COALESCE(SUM(o.quantity) FILTER (WHERE o.status IN ('pending', 'paid')), 0)     AS TotalQty,

                    COALESCE(SUM(o.total_to_pay) FILTER (WHERE o.status IN ('pending', 'paid')), 0) AS TotalPay,
                    
                    c.total_paid AS TotalPaid,
                    c.total_change AS TotalChange,
                    o.status,

                    MAX(o.created_at) FILTER (WHERE o.status IN ('pending', 'paid'))               AS CreatedAt

                FROM tbCustomers c
                LEFT JOIN tbOrders o 
                    ON o.customer_id = c.id 
                LEFT JOIN tbProducts p 
                    ON p.id = o.product_id

                GROUP BY 
                    c.id, 
                    c.fullName, 
                    c.phone_number,
                    o.status

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
                    tx
                );

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
                    INSERT INTO tbOrders (customer_id, product_id, quantity, unit_price)
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
                            new { ing.IngredientId, QtyUsed = qtyNeeded },
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
                            Price = unitPrice
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

        public async Task<ResponseDTO> CreatePayNow(OrdersCreatePayNowDTO orderPayNow)
        {
            await using var conn = _db.CreateConnection();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Insert customer and retrieve generated ID
                const string sqlInsertCustomer = """
                    INSERT INTO tbCustomers (total_paid, total_change)
                    VALUES (@TotalPaid, @TotalChange)
                    RETURNING id;
                    """;

                var customerId = await conn.ExecuteScalarAsync<Guid>( 
                    sqlInsertCustomer,
                    new { orderPayNow.TotalPaid, orderPayNow.TotalChange },
                    tx
                );

                const string sqlExistCustomer = @"SELECT 1 FROM tbCustomers WHERE id = @Id";
                var checkIfExist = await conn.QueryFirstOrDefaultAsync<int>(sqlExistCustomer, new { Id = customerId });
                if (checkIfExist != 1) return ResponseDTO.Failure(MessagesConstant.NotFound);

                const string sqlInsertPymt = @"INSERT INTO tbPaymentOrders (customer_id, method, amount) VALUES (@CustomerId, @Method::pymt_method, @Amount)";

                if (orderPayNow.Method!.Cash is not null && orderPayNow.Method!.Cash != 0)
                {
                    var parameters = new
                    {
                        CustomerId = customerId,
                        Method = "cash",
                        Amount = orderPayNow.Method.Cash
                    };

                    var result = await conn.ExecuteAsync(sqlInsertPymt, parameters, tx);

                    if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);
                }


                if (orderPayNow.Method!.EMola is not null && orderPayNow.Method!.EMola != 0)
                {
                    var parameters = new
                    {
                        CustomerId = customerId,
                        Method = "eMola",
                        Amount = orderPayNow.Method.EMola
                    };

                    var result = await conn.ExecuteAsync(sqlInsertPymt, parameters, tx);

                    if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);
                }


                if (orderPayNow.Method!.MPesa is not null && orderPayNow.Method!.MPesa != 0)
                {
                    var parameters = new
                    {
                        CustomerId = customerId,
                        Method = "mPesa",
                        Amount = orderPayNow.Method.MPesa
                    };

                    var result = await conn.ExecuteAsync(sqlInsertPymt, parameters, tx);

                    if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);
                }

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

                const string sqlInsertOrderItem = @"
                    INSERT INTO tbOrders (customer_id, product_id, quantity, unit_price, status)
                    VALUES (@CustomerId, @ProductId, @Quantity, @Price, @Status::order_status);
                    ";

                if (orderPayNow.OrderItems?.Any() != true)
                    return ResponseDTO.Failure(MessagesConstant.OperationFailed);

                foreach (var item in orderPayNow.OrderItems)
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
                            sqlCheckStock, new { ing.IngredientId }, tx);

                        if (currentStock < qtyNeeded)
                            return ResponseDTO.Failure($"Insufficient stock for ingredient {ing.ItemName}. Required: {qtyNeeded}, Available: {currentStock}");

                        await conn.ExecuteAsync(
                            sqlConsumeStock,
                            new { ing.IngredientId, QtyUsed = qtyNeeded },
                            tx);
                    }

                    // 4. Persist order item
                    await conn.ExecuteAsync(
                        sqlInsertOrderItem,
                        new
                        {
                            CustomerId = customerId,
                            item.ProductId,
                            item.Quantity,
                            Price = unitPrice,
                            Status = "paid"
                        },
                        tx
                    );
                }

                await tx.CommitAsync();
                return ResponseDTO.Success(MessagesConstant.Created);
            }
            catch (PostgresException pex)
            {
                await tx.RollbackAsync();
                _logger.LogError(pex, "Database error during CreatePayNow for customer");
                return ResponseDTO.Failure("Database error: " + pex.MessageText);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to create order-pay-now for customer)");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }

        public async Task<string> GetReceiptNumber()
        {
            try
            {
                await using var conn = _db.CreateConnection();

                const string sql = @"SELECT COUNT(*) AS total FROM tbCustomers";

                var result = await conn.QueryFirstOrDefaultAsync<int>(sql);

                return $"{result + 1:D7}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate Receipt Number.");
                throw new InvalidOperationException("Failed to generate Receipt Number");
            }
        }

        public async Task<ResponseDTO> UpdateAsync(OrdersUpdatePayNowDTO order)
        {
            await using var conn = _db.CreateConnection();

            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                const string sqlExist = @"SELECT 1 FROM tbCustomers WHERE id = @Id";
                var exists = await conn.QueryFirstOrDefaultAsync<int>(sqlExist, new { order.Id });

                if (exists != 1) return ResponseDTO.Failure(MessagesConstant.NotFound);

                const string sqlUpdateCustomer = @"UPDATE tbCustomers SET total_paid = @TotalPaid, total_change = @TotalChange WHERE id = @CustomerId";
                
                var result = await conn.ExecuteAsync(
                    sqlUpdateCustomer,
                    new {  order.TotalPaid, order.TotalChange, CustomerId = order.Id },
                    tx
                );

                if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);

                const string sqlGetOrdersId = @"SELECT id FROM tbOrders WHERE customer_id = @CustomerId";

                const string sqlUpdateOrders = @"UPDATE tbOrders SET status = @Status::order_status WHERE id = @OrderId";

                var ordersIDs = await conn.QueryAsync<Guid>(sqlGetOrdersId, new { CustomerId = order.Id });

                foreach (var id in ordersIDs)
                {
                    await conn.ExecuteAsync(
                        sqlUpdateOrders,
                        new { Status = "paid", OrderId = id },
                        tx
                    );
                }

                const string sqlInsertPymt = @"INSERT INTO tbPaymentOrders (customer_id, method, amount) VALUES (@CustomerId, @Method::pymt_method, @Amount)";

                if (order.Method!.Cash is not null && order.Method!.Cash != 0)
                {
                    var parameters = new
                    {
                        CustomerId = order.Id,
                        Method = "cash",
                        Amount = order.Method.Cash
                    };

                    var results = await conn.ExecuteAsync(sqlInsertPymt, parameters, tx);

                    if (results == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);
                }


                if (order.Method!.EMola is not null && order.Method!.EMola != 0)
                {
                    var parameters = new
                    {
                        CustomerId = order.Id,
                        Method = "eMola",
                        Amount = order.Method.EMola
                    };

                    var results = await conn.ExecuteAsync(sqlInsertPymt, parameters, tx);

                    if (results == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);
                }


                if (order.Method!.MPesa is not null && order.Method!.MPesa != 0)
                {
                    var parameters = new
                    {
                        CustomerId = order.Id,
                        Method = "mPesa",
                        Amount = order.Method.MPesa
                    };

                    var results = await conn.ExecuteAsync(sqlInsertPymt, parameters, tx);

                    if (results == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);
                }                
                
                await tx.CommitAsync();
                return ResponseDTO.Success(MessagesConstant.Updated);
            }
            catch (PostgresException pex)
            {
                await tx.RollbackAsync();
                _logger.LogError(pex, "Database error during UpdatePayNow for customer");
                return ResponseDTO.Failure("Database error: " + pex.MessageText);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to update order-pay-now for customer)");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }
    }
}
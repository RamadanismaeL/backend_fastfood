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

        public async Task<OrdersCheckPosDTO?> CheckPos(Guid userId)
        {
            const string sql = @"
                SELECT
                    cr.id AS CashRegisterId,
                    cr.is_opened AS Status
                FROM tbCashRegister cr
                INNER JOIN tbCashRegisterDetails crd ON crd.cash_register_id = cr.id
                WHERE (crd.cash_name = 'opened' OR crd.cash_name = 'closed') AND cr.user_id = @UserId
                ORDER BY crd.created_at DESC
                LIMIT 1;";

            await using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<OrdersCheckPosDTO?>(sql, new { UserId = userId });
        }

        public async Task<IEnumerable<OrdersListDTO>> GetAllAsync(Guid registerId)
        {
            const string sql = @"
                SELECT
                    s.id AS Id,
                    MAX(c.fullName) AS CustomerName,
                    MAX(c.phone_number) AS CustomerPhone,

                    COALESCE(produtos.descricao, 'Sem itens') AS Description,

                    COALESCE(items.total_qty, 0)           AS TotalQty,
                    COALESCE(items.total_to_pay, 0.00)     AS TotalPay,
                    COALESCE(payments.total_paid, 0.00)    AS TotalPaid,
                    GREATEST(COALESCE(payments.total_paid, 0) - COALESCE(items.total_to_pay, 0), 0) AS TotalChange,

                    MAX(o.status) AS Status,
                    MAX(o.created_at) AS CreatedAt

                FROM tbSales s
                INNER JOIN tbCashRegister cr ON cr.id = s.cash_register_id
                INNER JOIN tbCashRegisterDetails crd ON crd.cash_register_id = cr.id
                INNER JOIN tbCustomers c ON c.sales_id = s.id

                -- Totais dos itens
                CROSS JOIN LATERAL (
                    SELECT
                        SUM(o.quantity)     AS total_qty,
                        SUM(o.total_to_pay) AS total_to_pay
                    FROM tbOrders o
                    WHERE o.sales_id = s.id 
                        AND o.status IN ('pending', 'paid')
                        AND o.created_at::DATE = CURRENT_DATE
                ) items

                -- Total pago
                LEFT JOIN LATERAL (
                    SELECT SUM(total_paid) AS total_paid
                    FROM tbPaymentSales ps
                    WHERE ps.sales_id = s.id 
                        AND ps.is_paid = TRUE
                ) payments ON TRUE

                -- Descrição dos itens
                LEFT JOIN LATERAL (
                    SELECT STRING_AGG(qtd_nome, ' • ' ORDER BY qtd_nome) AS descricao
                    FROM (
                        SELECT DISTINCT
                            o.quantity || ' × ' || p.item_name AS qtd_nome
                        FROM tbOrders o
                        JOIN tbProducts p ON p.id = o.product_id
                        WHERE o.sales_id = s.id
                        AND o.status IN ('pending', 'paid')
                    ) sub
                ) produtos ON TRUE

                LEFT JOIN tbOrders o 
                    ON o.sales_id = s.id 
                AND o.status IN ('pending', 'paid')

                WHERE cr.is_opened = TRUE
                AND cr.id = @CashRegisterId
                AND crd.date_time::DATE = CURRENT_DATE
                GROUP BY 
                    s.id,
                    items.total_qty,
                    items.total_to_pay,
                    payments.total_paid,
                    produtos.descricao

                ORDER BY CreatedAt DESC NULLS LAST;";

            await using var conn = _db.CreateConnection();
            return (await conn.QueryAsync<OrdersListDTO>(sql, new { CashRegisterId = registerId })).AsList();
        }

        public async Task<IEnumerable<OrdersListDTO>> GetAllAsync()
        {
            const string sql = @"
                SELECT
                    s.id AS Id,
                    MAX(c.fullName) AS CustomerName,
                    MAX(c.phone_number) AS CustomerPhone,

                    COALESCE(produtos.descricao, 'Sem itens') AS Description,

                    COALESCE(items.total_qty, 0)        AS TotalQty,
                    COALESCE(items.total_to_pay, 0.00)    AS TotalPay,
                    COALESCE(payments.total_paid, 0.00)   AS TotalPaid,
                    GREATEST(COALESCE(payments.total_paid, 0) - COALESCE(items.total_to_pay, 0), 0) AS TotalChange,

                    MAX(o.status) AS Status,
                    MAX(u.username) AS Operator,
                    MAX(o.created_at) AS CreatedAt

                FROM tbSales s
                INNER JOIN tbCashRegister cr ON cr.id = s.cash_register_id
                INNER JOIN tbUsers u ON u.id = cr.user_id
                INNER JOIN tbCustomers c ON c.sales_id = s.id

                CROSS JOIN LATERAL (
                    SELECT
                        SUM(quantity)     AS total_qty,
                        SUM(total_to_pay) AS total_to_pay
                    FROM tbOrders
                    WHERE sales_id = s.id AND status IN ('pending', 'paid')
                ) items

                LEFT JOIN LATERAL (
                    SELECT SUM(total_paid) AS total_paid
                    FROM tbPaymentSales
                    WHERE sales_id = s.id AND is_paid = TRUE
                ) payments ON TRUE

                LEFT JOIN LATERAL (
                    SELECT STRING_AGG(qtd_nome, ' • ' ORDER BY qtd_nome) AS descricao
                    FROM (
                        SELECT DISTINCT
                            o.quantity || ' × ' || p.item_name AS qtd_nome
                        FROM tbOrders o
                        JOIN tbProducts p ON p.id = o.product_id
                        WHERE o.sales_id = s.id
                        AND o.status IN ('pending', 'paid')
                    ) sub
                ) produtos ON TRUE

                LEFT JOIN tbOrders o ON o.sales_id = s.id AND o.status IN ('pending', 'paid')

                GROUP BY 
                    s.id,
                    items.total_qty,
                    items.total_to_pay,
                    payments.total_paid,
                    produtos.descricao

                ORDER BY CreatedAt DESC NULLS LAST;";

            await using var conn = _db.CreateConnection();
            return (await conn.QueryAsync<OrdersListDTO>(sql)).AsList();
        }

        public async Task<ResponseDTO> CreateAsync(OrdersCreateDTO order)
        {
            await using var conn = _db.CreateConnection();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                const string sqlInsertSale = @"INSERT INTO tbSales (cash_register_id) VALUES(@CashRegisterId) RETURNING id;";
                
                const string sqlInsertCustomer = @"INSERT INTO tbCustomers (sales_id, fullName, phone_number) VALUES (@SaleId, @FullName, @PhoneNumber);";

                const string sqlInsertOrder = @"INSERT INTO tbOrders (sales_id, product_id, quantity, unit_price) VALUES (@SaleId, @ProductId, @Quantity, @UnitPrice);";

                var saleId = await conn.ExecuteScalarAsync<Guid>(
                    sqlInsertSale,
                    new { order.CashRegisterId },
                    tx
                );
                
                await conn.ExecuteAsync(
                    sqlInsertCustomer,
                    new 
                    { 
                        SaleId = saleId,
                        FullName = order.CustomerName,
                        PhoneNumber = order.CustomerPhone
                    },
                    tx
                );

                // Reusable queries
                const string sqlGetUnitPrice = @"SELECT price FROM tbProducts WHERE id = @ProductId LIMIT 1;";

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

                

                if (order.OrderItems?.Any() != true)
                    return ResponseDTO.Failure(MessagesConstant.OperationFailed);

                foreach (var item in order.OrderItems)
                {
                    // 1. Get unit price
                    var unitPrice = await conn.ExecuteScalarAsync<decimal>(
                        sqlGetUnitPrice, new { item.ProductId }, tx);

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

                    await conn.ExecuteAsync(
                        sqlInsertOrder,
                        new
                        {
                            SaleId = saleId,
                            item.ProductId,
                            item.Quantity,
                            UnitPrice = unitPrice
                        },
                        tx
                    );
                }

                await tx.CommitAsync();
                return ResponseDTO.Success(MessagesConstant.Created);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to create order for customer)");
                return ResponseDTO.Failure(MessagesConstant.ServerError);
            }
        }

        public async Task<ResponseDTO> CreatePayNow(OrdersCreatePayNowDTO order)
        {
            await using var conn = _db.CreateConnection();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                const string sqlInsertSale = @"INSERT INTO tbSales (cash_register_id) VALUES(@CashRegisterId) RETURNING id;";
                
                const string sqlInsertCustomer = @"INSERT INTO tbCustomers (sales_id) VALUES (@SaleId);";

                const string sqlInsertOrderItem = @"INSERT INTO tbOrders (sales_id, product_id, quantity, unit_price, status) 
                VALUES (@SaleId, @ProductId, @Quantity, @UnitPrice, @Status::order_status);";

                const string sqlInsertPymt = @"INSERT INTO tbPaymentSales (sales_id, method, total_paid)
                VALUES (@SaleId, @Method::pymt_method, @TotalPaid)";

                var saleId = await conn.ExecuteScalarAsync<Guid>(
                    sqlInsertSale,
                    new { order.CashRegisterId },
                    tx
                );

                await conn.ExecuteAsync(
                    sqlInsertCustomer,
                    new { SaleId = saleId },
                    tx
                );

                if (order.Method!.Cash is not null && order.Method!.Cash > 0)
                {
                    var parameters = new
                    {
                        SaleId = saleId,
                        Method = "cash",
                        TotalPaid = order.Method.Cash               
                    };

                    var result = await conn.ExecuteAsync(sqlInsertPymt, parameters, tx);

                    if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);
                }


                if (order.Method!.EMola is not null && order.Method!.EMola > 0)
                {
                    var parameters = new
                    {
                        SaleId = saleId,
                        Method = "eMola",
                        TotalPaid = order.Method.EMola
                    };

                    var result = await conn.ExecuteAsync(sqlInsertPymt, parameters, tx);

                    if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);
                }


                if (order.Method!.MPesa is not null && order.Method!.MPesa > 0)
                {
                    var parameters = new
                    {
                        SaleId = saleId,
                        Method = "mPesa",
                        TotalPaid = order.Method.MPesa
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
                            sqlCheckStock, new { ing.IngredientId }, tx);

                        if (currentStock < qtyNeeded)
                            return ResponseDTO.Failure($"Insufficient stock for ingredient {ing.ItemName}. Required: {qtyNeeded}, Available: {currentStock}");

                        await conn.ExecuteAsync(
                            sqlConsumeStock,
                            new { ing.IngredientId, QtyUsed = qtyNeeded },
                            tx);
                    }

                    await conn.ExecuteAsync(
                        sqlInsertOrderItem,
                        new
                        {
                            SaleId = saleId,
                            item.ProductId,
                            item.Quantity,
                            UnitPrice = unitPrice,
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

                const string sql = @"SELECT COUNT(*) AS Total FROM tbSales";

                var result = await conn.QueryFirstOrDefaultAsync<int>(sql);

                return $"{result + 1:D7}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate Receipt Number.");
                throw new InvalidOperationException("Failed to generate Receipt Number");
            }
        }

        public async Task<ResponseDTO> UpdatePayNow(OrdersUpdatePayNowDTO order)
        {
            await using var conn = _db.CreateConnection();

            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                const string sqlExist = @"SELECT 1 FROM tbSales WHERE id = @Id";

                const string sqlGetOrdersId = @"SELECT id FROM tbOrders WHERE sales_id = @SaleId";

                const string sqlUpdateOrders = @"UPDATE tbOrders SET status = @Status::order_status WHERE id = @OrderId";

                const string sqlInsertPymt = @"INSERT INTO tbPaymentSales (sales_id, method, total_paid)
                VALUES (@SaleId, @Method::pymt_method, @TotalPaid)";

                var exists = await conn.QueryFirstOrDefaultAsync<int>(sqlExist, new { Id = order.SaleId });

                if (exists != 1) return ResponseDTO.Failure(MessagesConstant.NotFound);
                
                var ordersID = await conn.QueryAsync<Guid>(sqlGetOrdersId, new { order.SaleId });

                foreach (var id in ordersID)
                {
                    await conn.ExecuteAsync(
                        sqlUpdateOrders,
                        new { Status = "paid", OrderId = id },
                        tx
                    );
                }

                if (order.Method!.Cash is not null && order.Method!.Cash > 0)
                {
                    var parameters = new
                    {
                        order.SaleId,
                        Method = "cash",
                        TotalPaid = order.Method.Cash
                    };

                    var result = await conn.ExecuteAsync(sqlInsertPymt, parameters, tx);

                    if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);
                }


                if (order.Method!.EMola is not null && order.Method!.EMola > 0)
                {
                    var parameters = new
                    {
                        order.SaleId,
                        Method = "eMola",
                        TotalPaid = order.Method.EMola
                    };

                    var result = await conn.ExecuteAsync(sqlInsertPymt, parameters, tx);

                    if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);
                }


                if (order.Method!.MPesa is not null && order.Method!.MPesa > 0)
                {
                    var parameters = new
                    {
                        order.SaleId,
                        Method = "mPesa",
                        TotalPaid = order.Method.MPesa
                    };

                    var result = await conn.ExecuteAsync(sqlInsertPymt, parameters, tx);

                    if (result == 0) return ResponseDTO.Failure(MessagesConstant.OperationFailed);
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
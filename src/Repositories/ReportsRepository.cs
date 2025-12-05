using Dapper;
using unipos_basic_backend.src.DTOs;
using unipos_basic_backend.src.Interfaces;
using unipos_basic_backend.src.Services;

namespace unipos_basic_backend.src.Repositories
{
    public sealed class ReportsRepository(PostgresDb db) : IReportsRepository
    {
        private readonly PostgresDb _db = db;

        public async Task<IEnumerable<ReportsCashMovementsDTO>> GetCashMovementsAsync(DateDTO date)
        {
            const string sql = @"
                SELECT
                    u.username AS Operator,
                    crd.cash_name AS CashName,
                    crd.amount AS Amount,
                    crd.description AS Description,
                    crd.is_confirmed AS Status,
                    crd.date_time AS UpdatedAt
                FROM tbCashRegisterDetails crd
                INNER JOIN tbCashRegister cr ON cr.id = crd.cash_register_id
                INNER JOIN tbUsers u ON u.id = cr.user_id
                WHERE crd.date_time::DATE = @Date::DATE
                ORDER BY crd.date_time DESC;";

            await using var conn = _db.CreateConnection();
            return (await conn.QueryAsync<ReportsCashMovementsDTO>(sql, new { date.Date })).AsList();
        }

        public async Task<IEnumerable<OrdersListDTO>> GetOrdersDetailAsync(DateDTO date)
        {
            const string sql = @"
                SELECT
                    s.id AS Id,
                    MAX(u.Username) AS Operator,
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
                INNER JOIN tbUsers u ON u.id = cr.user_id
                INNER JOIN tbCustomers c ON c.sales_id = s.id

                -- Totais dos itens
                CROSS JOIN LATERAL (
                    SELECT
                        SUM(o.quantity)     AS total_qty,
                        SUM(o.total_to_pay) AS total_to_pay
                    FROM tbOrders o
                    WHERE o.sales_id = s.id 
                        AND o.status IN ('pending', 'paid')
                        AND o.created_at::DATE = @Date::DATE
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
                AND crd.date_time::DATE = @Date::DATE
                GROUP BY 
                    s.id,
                    items.total_qty,
                    items.total_to_pay,
                    payments.total_paid,
                    produtos.descricao

                ORDER BY CreatedAt DESC NULLS LAST;";

            await using var conn = _db.CreateConnection();
            return (await conn.QueryAsync<OrdersListDTO>(sql, new { date.Date })).AsList();
        }
    }
}
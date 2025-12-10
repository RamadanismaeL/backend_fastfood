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

                CROSS JOIN LATERAL (
                    SELECT
                        SUM(o.quantity)     AS total_qty,
                        SUM(o.total_to_pay) AS total_to_pay
                    FROM tbOrders o
                    WHERE o.sales_id = s.id 
                        AND o.status IN ('pending', 'paid')
                        AND o.created_at::DATE = @Date::DATE
                ) items

                LEFT JOIN LATERAL (
                    SELECT SUM(total_paid) AS total_paid
                    FROM tbPaymentSales ps
                    WHERE ps.sales_id = s.id 
                        AND ps.is_paid = TRUE
                ) payments ON TRUE

                LEFT JOIN LATERAL (
                    SELECT STRING_AGG(qtd_nome, ' + ' ORDER BY qtd_nome) AS descricao
                    FROM (
                        SELECT DISTINCT
                            o.quantity || 'X ' || p.item_name AS qtd_nome
                        FROM tbOrders o
                        JOIN tbProducts p ON p.id = o.product_id
                        WHERE o.sales_id = s.id
                        AND o.status IN ('pending', 'paid')
                    ) sub
                ) produtos ON TRUE

                LEFT JOIN tbOrders o 
                    ON o.sales_id = s.id 
                AND o.status IN ('pending', 'paid')

                WHERE crd.date_time::DATE = @Date::DATE
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

        public async Task<IEnumerable<ReportsCardDTO>> GetInitialBalance(DateDTO date)
        {
            await using var conn = _db.CreateConnection();

            const string sql = @"
                WITH params AS (
                    SELECT
                        @PreviousDate::date AS prev_date,
                        @CurrentDate::date AS curr_date
                ),
                data AS (
                    SELECT
                        date_time::date AS trx_date,
                        date_time       AS trx_time,
                        amount
                    FROM tbCashRegisterDetails
                    WHERE date_time::date IN (SELECT prev_date FROM params UNION ALL SELECT curr_date FROM params)
                    AND cash_name IN ('opened', 'cash in')
                    AND is_confirmed = true
                ),
                daily AS (
                    SELECT
                        trx_date,
                        SUM(amount)      AS total_amount,
                        MAX(trx_time)    AS last_update
                    FROM data
                    GROUP BY trx_date
                ),
                pivot AS (
                    SELECT
                        COALESCE(MAX(CASE WHEN trx_date = (SELECT prev_date FROM params) THEN total_amount END), 0) AS prev_total,
                        COALESCE(MAX(CASE WHEN trx_date = (SELECT curr_date FROM params) THEN total_amount END), 0) AS curr_total,
                        MAX(CASE WHEN trx_date = (SELECT prev_date FROM params) THEN last_update END)          AS prev_update,
                        MAX(CASE WHEN trx_date = (SELECT curr_date FROM params) THEN last_update END)          AS curr_update
                    FROM daily
                )
                SELECT
                    COALESCE(p.curr_update, p.prev_update)                                      AS LastUpdated,
                    ARRAY[COALESCE(p.prev_total, 0), COALESCE(p.curr_total, 0)] AS TotalAmount,
                    ARRAY[
                        TO_CHAR((SELECT prev_date FROM params), 'yyyy-MM-dd'),
                        TO_CHAR((SELECT curr_date FROM params), 'yyyy-MM-dd')
                    ]                                                                           AS Date
                FROM pivot p;";

            var result = await conn.QuerySingleOrDefaultAsync<ReportsCardResponseDTO>(sql, new
            { date.PreviousDate, date.CurrentDate });

            if (result == null || result.TotalAmount == null || result.TotalAmount.Length < 2)
                return [];

            decimal prevAmount = result.TotalAmount[0];
            decimal currAmount = result.TotalAmount[1];
            int trendPercentage = 0;

            if (prevAmount > currAmount && prevAmount != 0m)
            {
                trendPercentage = (int)Math.Round(currAmount * 100m / prevAmount) - 100;
            }
            else if (prevAmount < currAmount && currAmount != 0m)
            {
                trendPercentage = 100 - (int)Math.Round(prevAmount * 100m / currAmount);
            }
            else
            {
                trendPercentage = 0;
            }

            var parameters = new ReportsCardDTO
            {
                TotalAmount     = currAmount,
                TrendPercentage = trendPercentage,
                LastUpdated     = result.LastUpdated,
                ChartAmount     = result.TotalAmount ?? [],
                ChartDate       = result.Date       ?? []
            };

            return [parameters];
        }

        public async Task<IEnumerable<ReportsCardDTO>> GetInFlows(DateDTO date)
        {
            await using var conn = _db.CreateConnection();

            const string sql = @"
                WITH params AS
                (
                    SELECT
                        @PreviousDate::date AS prev_date,
                        @CurrentDate::date AS curr_date
                ),
                data AS
                (
                    SELECT
                        created_at::date AS trx_date,
                        created_at AS trx_time,
                        total_to_pay
                    FROM tbOrders
                    WHERE created_at::date IN (SELECT prev_date FROM params UNION ALL SELECT curr_date FROM params)
                        AND is_available = TRUE
                        AND status = 'paid'
                ),
                daily AS
                (
                    SELECT
                        trx_date,
                        SUM(total_to_pay) AS total_amount,
                        MAX(trx_time) AS last_updated
                    FROM data
                    GROUP BY trx_date
                ),
                pivot AS
                (
                    SELECT
                        COALESCE(MAX(CASE WHEN trx_date = (SELECT prev_date FROM params) THEN total_amount END), 0) AS prev_total,
                        COALESCE(MAX(CASE WHEN trx_date = (SELECT curr_date FROM params) THEN total_amount END), 0) AS curr_total,
                        MAX(CASE WHEN trx_date = (SELECT prev_date FROM params) THEN last_updated END) AS prev_updated,
                        MAX(CASE WHEN trx_date = (SELECT curr_date FROM params) THEN last_updated END) AS curr_updated
                    FROM daily
                )
                SELECT
                    COALESCE(p.curr_updated, p.prev_updated) AS LastUpdated,
                    ARRAY[COALESCE(p.prev_total, 0), COALESCE(p.curr_total, 0)] AS TotalAmount,
                    ARRAY
                    [
                        TO_CHAR((SELECT prev_date FROM params), 'yyyy-MM-dd'),
                        TO_CHAR((SELECT curr_date FROM params), 'yyyy-MM-dd')
                    ] AS Date
                FROM pivot p;";

            var result = await conn.QuerySingleOrDefaultAsync<ReportsCardResponseDTO>(sql, new
            { date.PreviousDate, date.CurrentDate });

            if (result == null || result.TotalAmount == null || result.TotalAmount.Length < 2)
                return [];

            decimal prevAmount = result.TotalAmount[0];
            decimal currAmount = result.TotalAmount[1];
            int trendPercentage = 0;

            if (prevAmount > currAmount && prevAmount != 0m)
            {
                trendPercentage = (int)Math.Round(currAmount * 100m / prevAmount) - 100;
            }
            else if (prevAmount < currAmount && currAmount != 0m)
            {
                trendPercentage = 100 - (int)Math.Round(prevAmount * 100m / currAmount);
            }
            else
            {
                trendPercentage = 0;
            }

            var parameters = new ReportsCardDTO
            {
                TotalAmount     = currAmount,
                TrendPercentage = trendPercentage,
                LastUpdated     = result.LastUpdated,
                ChartAmount     = result.TotalAmount ?? [],
                ChartDate       = result.Date       ?? []
            };

            return [parameters];
        }

        public async Task<IEnumerable<ReportsCardDTO>> GetOutFlows(DateDTO date)
        {
            await using var conn = _db.CreateConnection();

            const string sql = @"
                WITH params AS
                (
                    SELECT
                        @PreviousDate::date AS prev_date,
                        @CurrentDate::date AS curr_date
                ),
                data AS 
                (
                    SELECT
                        date_time::date AS trx_date,
                        date_time AS trx_time,
                        amount
                    FROM tbCashRegisterDetails
                    WHERE date_time::date IN (SELECT prev_date FROM params UNION ALL SELECT curr_date FROM params)
                        AND cash_name = 'cash out'
                        AND is_confirmed = true
                ),
                daily AS 
                (
                    SELECT
                        trx_date,
                        SUM(amount) AS total_amount,
                        MAX(trx_time) AS last_update
                    FROM data
                    GROUP BY trx_date
                ),
                pivot AS 
                (
                    SELECT
                        COALESCE(MAX(CASE WHEN trx_date = (SELECT prev_date FROM params) THEN total_amount END), 0) AS prev_total,
                        COALESCE(MAX(CASE WHEN trx_date = (SELECT curr_date FROM params) THEN total_amount END), 0) AS curr_total,
                        MAX(CASE WHEN trx_date = (SELECT prev_date FROM params) THEN last_update END) AS prev_update,
                        MAX(CASE WHEN trx_date = (SELECT curr_date FROM params) THEN last_update END) AS curr_update
                    FROM daily
                )
                SELECT
                    COALESCE(p.curr_update, p.prev_update) AS LastUpdated,
                        ARRAY[COALESCE(p.prev_total, 0), COALESCE(p.curr_total, 0)] AS TotalAmount,
                        ARRAY[
                            TO_CHAR((SELECT prev_date FROM params), 'yyyy-MM-dd'),
                            TO_CHAR((SELECT curr_date FROM params), 'yyyy-MM-dd')
                        ] AS Date
                FROM pivot p;";

            var result = await conn.QuerySingleOrDefaultAsync<ReportsCardResponseDTO>(sql, new
            { date.PreviousDate, date.CurrentDate });

            if (result == null || result.TotalAmount == null || result.TotalAmount.Length < 2)
                return [];

            decimal prevAmount = result.TotalAmount[0];
            decimal currAmount = result.TotalAmount[1];
            int trendPercentage = 0;

            if (prevAmount > currAmount && prevAmount != 0m)
            {
                trendPercentage = (int)Math.Round(currAmount * 100m / prevAmount) - 100;
            }
            else if (prevAmount < currAmount && currAmount != 0m)
            {
                trendPercentage = 100 - (int)Math.Round(prevAmount * 100m / currAmount);
            }
            else
            {
                trendPercentage = 0;
            }

            var parameters = new ReportsCardDTO
            {
                TotalAmount     = currAmount,
                TrendPercentage = trendPercentage,
                LastUpdated     = result.LastUpdated,
                ChartAmount     = result.TotalAmount ?? [],
                ChartDate       = result.Date       ?? []
            };

            return [parameters];
        }

        public async Task<IEnumerable<ReportsCardDTO>> GetClosingBalance(DateDTO date)
        {
            await using var conn = _db.CreateConnection();

            const string sql = @"
                WITH params AS
                (
                    SELECT
                        @PreviousDate::date AS prev_date,
                        @CurrentDate::date AS curr_date
                ),
                data AS 
                (
                    SELECT
                        date_time::date AS trx_date,
                        date_time AS trx_time,
                        amount
                    FROM tbCashRegisterDetails
                    WHERE date_time::date IN (SELECT prev_date FROM params UNION ALL SELECT curr_date FROM params)
                        AND cash_name = 'closed'
                        AND is_confirmed = true
                ),
                daily AS 
                (
                    SELECT
                        trx_date,
                        SUM(amount) AS total_amount,
                        MAX(trx_time) AS last_update
                    FROM data
                    GROUP BY trx_date
                ),
                pivot AS 
                (
                    SELECT
                        COALESCE(MAX(CASE WHEN trx_date = (SELECT prev_date FROM params) THEN total_amount END), 0) AS prev_total,
                        COALESCE(MAX(CASE WHEN trx_date = (SELECT curr_date FROM params) THEN total_amount END), 0) AS curr_total,
                        MAX(CASE WHEN trx_date = (SELECT prev_date FROM params) THEN last_update END) AS prev_update,
                        MAX(CASE WHEN trx_date = (SELECT curr_date FROM params) THEN last_update END) AS curr_update
                    FROM daily
                )
                SELECT
                    COALESCE(p.curr_update, p.prev_update) AS LastUpdated,
                        ARRAY[COALESCE(p.prev_total, 0), COALESCE(p.curr_total, 0)] AS TotalAmount,
                        ARRAY[
                            TO_CHAR((SELECT prev_date FROM params), 'yyyy-MM-dd'),
                            TO_CHAR((SELECT curr_date FROM params), 'yyyy-MM-dd')
                        ] AS Date
                FROM pivot p;";

            var result = await conn.QuerySingleOrDefaultAsync<ReportsCardResponseDTO>(sql, new
            { date.PreviousDate, date.CurrentDate });

            if (result == null || result.TotalAmount == null || result.TotalAmount.Length < 2)
                return [];

            decimal prevAmount = result.TotalAmount[0];
            decimal currAmount = result.TotalAmount[1];
            int trendPercentage = 0;

            if (prevAmount > currAmount && prevAmount != 0m)
            {
                trendPercentage = (int)Math.Round(currAmount * 100m / prevAmount) - 100;
            }
            else if (prevAmount < currAmount && currAmount != 0m)
            {
                trendPercentage = 100 - (int)Math.Round(prevAmount * 100m / currAmount);
            }
            else
            {
                trendPercentage = 0;
            }

            var parameters = new ReportsCardDTO
            {
                TotalAmount     = currAmount,
                TrendPercentage = trendPercentage,
                LastUpdated     = result.LastUpdated,
                ChartAmount     = result.TotalAmount ?? [],
                ChartDate       = result.Date       ?? []
            };

            return [parameters];
        }

        public async Task<IEnumerable<ReportsCardNumDTO>> GetNumOfSales(DateDTO date)
        {
            await using var conn = _db.CreateConnection();

            const string sql = @"
                WITH params AS
                (
                    SELECT
                        @PreviousDate::date AS prev_date,
                        @CurrentDate::date AS curr_date
                ),
                data AS
                (
                    SELECT
                        created_at::date AS trx_date,
                        created_at AS trx_time
                    FROM tbOrders
                    WHERE created_at::date IN (SELECT prev_date FROM params UNION ALL SELECT curr_date FROM params)
                    AND is_available = TRUE
                    AND status = 'paid'
                ),
                daily AS
                (
                    SELECT
                        trx_date,
                        COUNT(*) AS total_sales,
                        MAX(trx_time) AS last_updated
                    FROM data
                    GROUP BY trx_date
                ),
                pivot AS
                (
                    SELECT
                        COALESCE(MAX(CASE WHEN trx_date = (SELECT prev_date FROM params) THEN total_sales END), 0) AS prev_total,
                        COALESCE(MAX(CASE WHEN trx_date = (SELECT curr_date FROM params) THEN total_sales END), 0) AS curr_total,
                        MAX(CASE WHEN trx_date = (SELECT prev_date FROM params) THEN last_updated END) AS prev_updated,
                        MAX(CASE WHEN trx_date = (SELECT curr_date FROM params) THEN last_updated END) AS curr_updated
                    FROM daily
                )
                SELECT
                    COALESCE(p.curr_updated, p.prev_updated) AS LastUpdated,
                    ARRAY[COALESCE(p.prev_total, 0), COALESCE(p.curr_total, 0)] AS TotalAmount,
                    ARRAY[
                        TO_CHAR((SELECT prev_date FROM params), 'YYYY-MM-DD'),
                        TO_CHAR((SELECT curr_date FROM params), 'YYYY-MM-DD')
                    ] AS Date
                FROM pivot p;";

            var result = await conn.QuerySingleOrDefaultAsync<ReportsCardResponseNumDTO>(sql, new
            { date.PreviousDate, date.CurrentDate });

            if (result == null || result.TotalAmount == null || result.TotalAmount.Length < 2)
                return [];

            long prevAmount = result.TotalAmount[0];
            long currAmount = result.TotalAmount[1];
            long trendPercentage = 0;

            if (prevAmount > currAmount && prevAmount != 0m)
            {
                trendPercentage = (long)Math.Round(currAmount * 100m / prevAmount) - 100;
            }
            else if (prevAmount < currAmount && currAmount != 0m)
            {
                trendPercentage = 100 - (long)Math.Round(prevAmount * 100m / currAmount);
            }
            else
            {
                trendPercentage = 0;
            }

            var parameters = new ReportsCardNumDTO
            {
                TotalAmount     = currAmount,
                TrendPercentage = trendPercentage,
                LastUpdated     = result.LastUpdated,
                ChartAmount     = result.TotalAmount ?? [],
                ChartDate       = result.Date       ?? []
            };

            return [parameters];
        }

        public async Task<IEnumerable<ReportsCardDTO>> GetExpectedBalance(DateDTO date)
        {
            await using var conn = _db.CreateConnection();

            const string sql = @"
                WITH params AS (
                    SELECT
                        @PreviousDate::date AS prev_date,
                        @CurrentDate::date AS curr_date
                ),
                inflows AS (
                    SELECT
                        created_at::date AS trx_date,
                        created_at AS trx_time,
                        total_to_pay AS amount
                    FROM tbOrders
                    WHERE created_at::date IN (SELECT prev_date FROM params UNION ALL SELECT curr_date FROM params)
                    AND is_available = TRUE
                    AND status = 'paid'
                ),
                inflows_daily AS (
                    SELECT
                        trx_date,
                        COALESCE(SUM(amount), 0) AS total_in,
                        MAX(trx_time) AS last_in
                    FROM inflows
                    GROUP BY trx_date
                ),
                outflows AS (
                    SELECT
                        date_time::date AS trx_date,
                        date_time AS trx_time,
                        amount
                    FROM tbCashRegisterDetails
                    WHERE date_time::date IN (SELECT prev_date FROM params UNION ALL SELECT curr_date FROM params)
                    AND cash_name = 'cash out'
                    AND is_confirmed = true
                ),
                outflows_daily AS (
                    SELECT
                        trx_date,
                        COALESCE(SUM(amount), 0) AS total_out,
                        MAX(trx_time) AS last_out
                    FROM outflows
                    GROUP BY trx_date
                ),
                combined AS (
                    SELECT
                        d.trx_date,
                        COALESCE(i.total_in, 0) AS inflows,
                        COALESCE(o.total_out, 0) AS outflows,
                        GREATEST(COALESCE(i.last_in, '1970-01-01'::timestamp),
                                COALESCE(o.last_out, '1970-01-01'::timestamp)) AS last_updated
                    FROM (
                        SELECT prev_date AS trx_date FROM params
                        UNION ALL
                        SELECT curr_date AS trx_date FROM params
                    ) d
                    LEFT JOIN inflows_daily i ON i.trx_date = d.trx_date
                    LEFT JOIN outflows_daily o ON o.trx_date = d.trx_date
                ),
                final AS (
                    SELECT
                        MAX(last_updated) AS LastUpdated,
                        ARRAY[
                            MAX(CASE WHEN trx_date = (SELECT prev_date FROM params)
                                    THEN inflows - outflows END),
                            MAX(CASE WHEN trx_date = (SELECT curr_date FROM params)
                                    THEN inflows - outflows END)
                        ] AS totalamount,
                        ARRAY[
                            TO_CHAR((SELECT prev_date FROM params), 'YYYY-MM-DD'),
                            TO_CHAR((SELECT curr_date FROM params), 'YYYY-MM-DD')
                        ] AS date
                    FROM combined
                )
                SELECT 
                    LastUpdated,
                    totalamount,
                    date
                FROM final;";

            var result = await conn.QuerySingleOrDefaultAsync<ReportsCardResponseDTO>(sql, new
            { date.PreviousDate, date.CurrentDate });

            if (result == null || result.TotalAmount == null || result.TotalAmount.Length < 2)
                return [];

            decimal prevAmount = result.TotalAmount[0];
            decimal currAmount = result.TotalAmount[1];
            int trendPercentage = 0;

            if (prevAmount > currAmount && prevAmount != 0m)
            {
                trendPercentage = (int)Math.Round(currAmount * 100m / prevAmount) - 100;
            }
            else if (prevAmount < currAmount && currAmount != 0m)
            {
                trendPercentage = 100 - (int)Math.Round(prevAmount * 100m / currAmount);
            }
            else
            {
                trendPercentage = 0;
            }

            var parameters = new ReportsCardDTO
            {
                TotalAmount     = currAmount,
                TrendPercentage = trendPercentage,
                LastUpdated     = result.LastUpdated,
                ChartAmount     = result.TotalAmount ?? [],
                ChartDate       = result.Date       ?? []
            };

            return [parameters];
        }

        public async Task<IEnumerable<ReportsCardDTO>> GetAverageTicket(DateDTO date)
        {
            await using var conn = _db.CreateConnection();

            const string sql = @"
                WITH params AS
                (
                    SELECT
                        @PreviousDate::date AS prev_date,
                        @CurrentDate::date AS curr_date
                ),
                inflows_raw AS
                (
                    SELECT
                        created_at::date AS trx_date,
                        created_at AS trx_time,
                        total_to_pay
                    FROM tbOrders
                    WHERE created_at::date IN (SELECT prev_date FROM params UNION ALL SELECT curr_date FROM params)
                    AND is_available = TRUE
                    AND status = 'paid'
                ),
                inflows_daily AS
                (
                    SELECT
                        trx_date,
                        SUM(total_to_pay) AS inflow_amount,
                        MAX(trx_time) AS inflow_last
                    FROM inflows_raw
                    GROUP BY trx_date
                ),
                sales_raw AS
                (
                    SELECT
                        created_at::date AS trx_date,
                        created_at AS trx_time,
                        1 AS sale
                    FROM tbOrders
                    WHERE created_at::date IN (SELECT prev_date FROM params UNION ALL SELECT curr_date FROM params)
                    AND is_available = TRUE
                    AND status = 'paid'
                ),
                sales_daily AS
                (
                    SELECT
                        trx_date,
                        COUNT(*) AS total_sales,
                        MAX(trx_time) AS sales_last
                    FROM sales_raw
                    GROUP BY trx_date
                ),
                combined AS
                (
                    SELECT
                        d.trx_date,
                        COALESCE(i.inflow_amount, 0) AS inflows,
                        COALESCE(s.total_sales, 0) AS sales,
                        GREATEST(COALESCE(i.inflow_last, '1970-01-01'::timestamp),
                                COALESCE(s.sales_last, '1970-01-01'::timestamp)) AS last_updated
                    FROM
                    (
                        SELECT prev_date AS trx_date FROM params
                        UNION ALL
                        SELECT curr_date AS trx_date FROM params
                    ) d
                    LEFT JOIN inflows_daily i ON i.trx_date = d.trx_date
                    LEFT JOIN sales_daily s ON s.trx_date = d.trx_date
                ),
                ticket AS
                (
                    SELECT
                        ROUND
                        (
                            COALESCE(MAX(CASE WHEN trx_date = (SELECT prev_date FROM params) THEN inflows END), 0) /
                            NULLIF(MAX(CASE WHEN trx_date = (SELECT prev_date FROM params) THEN sales END), 0)
                            ,2
                        ) AS prev_ticket,

                        ROUND
                        (
                            COALESCE(MAX(CASE WHEN trx_date = (SELECT curr_date FROM params) THEN inflows END), 0) /
                            NULLIF(MAX(CASE WHEN trx_date = (SELECT curr_date FROM params) THEN sales END), 0)
                            , 2
                        ) AS curr_ticket,

                        MAX(last_updated) AS last_updated
                    FROM combined
                )
                SELECT
                    ticket.last_updated AS LastUpdated,
                    ARRAY[COALESCE(ticket.prev_ticket, 0), COALESCE(ticket.curr_ticket, 0)] AS TotalAmount,
                    ARRAY
                    [
                        TO_CHAR((SELECT prev_date FROM params), 'YYYY-MM-DD'),
                        TO_CHAR((SELECT curr_date FROM params), 'YYYY-MM-DD')
                    ] AS Date
                FROM ticket;";

            var result = await conn.QuerySingleOrDefaultAsync<ReportsCardResponseDTO>(sql, new
            { date.PreviousDate, date.CurrentDate });

            if (result == null || result.TotalAmount == null || result.TotalAmount.Length < 2)
                return [];

            decimal prevAmount = result.TotalAmount[0];
            decimal currAmount = result.TotalAmount[1];
            int trendPercentage = 0;

            if (prevAmount > currAmount && prevAmount != 0m)
            {
                trendPercentage = (int)Math.Round(currAmount * 100m / prevAmount) - 100;
            }
            else if (prevAmount < currAmount && currAmount != 0m)
            {
                trendPercentage = 100 - (int)Math.Round(prevAmount * 100m / currAmount);
            }
            else
            {
                trendPercentage = 0;
            }

            var parameters = new ReportsCardDTO
            {
                TotalAmount     = currAmount,
                TrendPercentage = trendPercentage,
                LastUpdated     = result.LastUpdated,
                ChartAmount     = result.TotalAmount ?? [],
                ChartDate       = result.Date       ?? []
            };

            return [parameters];
        }

        public async Task<IEnumerable<CarouselPymtMethodDTO>> GetPymtMethod(DateDTO date)
        {
            const string sql = @"
                SELECT ARRAY
                [
                    COALESCE(SUM(o.total_to_pay) FILTER(WHERE ps.method = 'cash'), 0),
                    COALESCE(SUM(o.total_to_pay) FILTER(WHERE ps.method = 'eMola'), 0),
                    COALESCE(SUM(o.total_to_pay) FILTER(WHERE ps.method = 'mPesa'), 0)
                ] AS Amounts
                FROM tbOrders o
                INNER JOIN tbPaymentSales ps
                    ON ps.sales_id = o.sales_id
                WHERE o.is_available = TRUE
                    AND o.status = 'paid'
                    AND ps.is_paid = TRUE
                    AND ps.created_at::DATE = @Date::DATE";

            await using var conn = _db.CreateConnection();
            var result = await conn.QuerySingleOrDefaultAsync<CarouselPymtMethodDTO>(sql, new { date.Date });

            if (result is null) return [];

            var parameters = new CarouselPymtMethodDTO
            {
              Amounts = result.Amounts ?? []  
            };

            return [parameters];
        }

        public async Task<IEnumerable<ChartAreaReportDTO>> GetChartSalesPerHour(DateDTO date)
        {
            const string sql = @"
                SELECT
                    ARRAY_AGG(total_to_pay ORDER BY created_at) AS Amounts,
                    ARRAY_AGG(
                        to_char(created_at, 'HH24:MI')
                        ORDER BY created_at
                    ) AS Date
                FROM tbOrders
                WHERE is_available = TRUE
                AND status = 'paid'
                AND created_at::DATE = @Date::DATE";

            await using var conn = _db.CreateConnection();
            var result = await conn.QuerySingleOrDefaultAsync<ChartAreaReportDTO>(sql, new { date.Date });

            return result is null ? [] : new [] { result };
        }

        public async Task<IEnumerable<ReportsCardRecentSaleDTO>> GetRecentSale(DateDTO date)
        {
            const string sql = @"
                SELECT
                    s.id AS Id,
                    order_info.order_number AS OrderNumber,
                    payments.methods        AS Methods,
                    COALESCE(items.total_to_pay, 0.00)     AS TotalPay,
                    COALESCE(produtos.descricao, 'Sem itens') AS Description,
                    c.fullName AS CustomerName,
                    payments.last_payment_at AS Time

                FROM tbSales s
                INNER JOIN tbCashRegister cr         ON cr.id = s.cash_register_id
                INNER JOIN tbCashRegisterDetails crd ON crd.cash_register_id = cr.id
                INNER JOIN tbCustomers c             ON c.sales_id = s.id

                LEFT JOIN LATERAL (
                    SELECT 
                        o.order_number
                    FROM tbOrders o
                    WHERE o.sales_id = s.id
                    AND o.status = 'paid'
                    ORDER BY o.created_at DESC
                    LIMIT 1
                ) order_info ON TRUE

                LEFT JOIN LATERAL (
                    SELECT SUM(o.total_to_pay) AS total_to_pay
                    FROM tbOrders o
                    WHERE o.sales_id = s.id
                    AND o.status = 'paid'
                ) items ON TRUE

                LEFT JOIN LATERAL (
                    SELECT 
                        ARRAY_AGG(ps.method ORDER BY ps.created_at) AS methods,
                        TO_CHAR(MAX(ps.created_at), 'HH24:MI') AS last_payment_at
                    FROM tbPaymentSales ps
                    WHERE ps.sales_id = s.id
                    AND ps.is_paid = TRUE
                    AND ps.created_at::DATE = @Date::DATE
                ) payments ON TRUE

                LEFT JOIN LATERAL (
                    SELECT STRING_AGG(qtd_nome, ' + ' ORDER BY qtd_nome) AS descricao
                    FROM (
                        SELECT DISTINCT
                            o.quantity || 'X ' || p.item_name AS qtd_nome
                        FROM tbOrders o
                        JOIN tbProducts p ON p.id = o.product_id
                        WHERE o.sales_id = s.id
                        AND o.status = 'paid'
                    ) sub
                ) produtos ON TRUE

                WHERE payments.last_payment_at IS NOT NULL

                GROUP BY 
                    s.id,
                    order_info.order_number,
                    items.total_to_pay,
                    payments.methods,
                    payments.last_payment_at,
                    produtos.descricao,
                    c.fullName

                ORDER BY order_info.order_number DESC NULLS LAST;";

            await using var conn = _db.CreateConnection();
            return (await conn.QueryAsync<ReportsCardRecentSaleDTO>(sql, new { date.Date })).AsList();
        }
    }
}
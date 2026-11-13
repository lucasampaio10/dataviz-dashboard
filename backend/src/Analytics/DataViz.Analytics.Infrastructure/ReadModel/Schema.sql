-- Read model of the dashboard. It holds nothing that cannot be rebuilt by replaying the events,
-- so the script is idempotent and the tables are denormalized for the questions the screen asks.

-- Events already applied: makes every projection idempotent.
CREATE TABLE IF NOT EXISTS processed_events (
    event_id     uuid        PRIMARY KEY,
    processed_at timestamptz NOT NULL DEFAULT now()
);

-- One row per sale: feeds the "latest sales" list and tells a refund which totals to adjust.
CREATE TABLE IF NOT EXISTS sales (
    sale_id     uuid           PRIMARY KEY,
    sale_date   date           NOT NULL,
    store_id    uuid           NOT NULL,
    store_name  text           NOT NULL,
    category    text           NOT NULL,
    channel     text           NOT NULL,
    amount      numeric(18, 2) NOT NULL,
    occurred_at timestamptz    NOT NULL,
    refunded_at timestamptz    NULL
);

CREATE INDEX IF NOT EXISTS ix_sales_occurred_at ON sales (occurred_at DESC);

-- Pre-aggregated totals: every chart is a GROUP BY over a few hundred rows, however many sales exist.
CREATE TABLE IF NOT EXISTS daily_sales (
    sale_date        date           NOT NULL,
    store_id         uuid           NOT NULL,
    store_name       text           NOT NULL,
    category         text           NOT NULL,
    channel          text           NOT NULL,
    gross_revenue    numeric(18, 2) NOT NULL DEFAULT 0,
    refunded_revenue numeric(18, 2) NOT NULL DEFAULT 0,
    orders           integer        NOT NULL DEFAULT 0,
    refunds          integer        NOT NULL DEFAULT 0,
    PRIMARY KEY (sale_date, store_id, category, channel)
);

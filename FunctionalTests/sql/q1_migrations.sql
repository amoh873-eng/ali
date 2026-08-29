SELECT COUNT(*) AS MigrationCount FROM __EFMigrationsHistory;
SELECT name FROM sys.tables WHERE name IN ('Notifications','StockBatches','HeldSales');
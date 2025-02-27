using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows;

public static class DatabaseHelper
{
    private static readonly string DatabasePath = "Database/bar.db"; // Оставляем только одну БД

    private static SQLiteConnection _transactionConnection = null;
    private static SQLiteTransaction _transaction = null;

    private static void LogAction(string action)
    {
        string logsDirectory = "Logs";
        if (!Directory.Exists(logsDirectory))
            Directory.CreateDirectory(logsDirectory);

        string logFilePath = Path.Combine(logsDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.log");
        string logEntry = $"{DateTime.Now:HH:mm:ss} - {action}";
        File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
    }

    public static long ExecuteNonQuery(string sql, params SQLiteParameter[] parameters)
    {
        LogAction($"ExecuteNonQuery - SQL: {sql}");

        if (_transactionConnection != null)
        {
            return ExecuteWithinTransaction(sql, parameters);
        }

        if (!File.Exists(DatabasePath)) return -1; // Проверяем, существует ли БД

        try
        {
            using (var connection = new SQLiteConnection($"Data Source={DatabasePath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);

                    int affectedRows = command.ExecuteNonQuery();
                    LogAction($"ExecuteNonQuery - {affectedRows} rows affected.");

                    long lastInsertedId = connection.LastInsertRowId;
                    LogAction($"Last Inserted ID: {lastInsertedId}");

                    return lastInsertedId;
                }
            }
        }
        catch (Exception ex)
        {
            LogAction($"Ошибка выполнения SQL-команды: {ex.Message}");
            MessageBox.Show($"Ошибка выполнения SQL-команды: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return -1;
        }
    }

    public static void ExecuteQuery(string sql, Action<SQLiteDataReader> processRow, params SQLiteParameter[] parameters)
    {
        LogAction($"ExecuteQuery - SQL: {sql}");

        if (!File.Exists(DatabasePath)) return;

        try
        {
            using (var connection = new SQLiteConnection($"Data Source={DatabasePath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);

                    using (var reader = command.ExecuteReader())
                    {
                        processRow(reader);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogAction($"Ошибка выполнения SQL-запроса: {ex.Message}");
            MessageBox.Show($"Ошибка выполнения SQL-запроса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public static DataTable ExecuteQueryToDataTable(string sql, params SQLiteParameter[] parameters)
    {
        LogAction($"ExecuteQueryToDataTable - SQL: {sql}");

        if (!File.Exists(DatabasePath)) return null;

        DataTable dt = new DataTable();

        try
        {
            using (var connection = new SQLiteConnection($"Data Source={DatabasePath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);

                    using (var reader = command.ExecuteReader())
                    {
                        dt.Load(reader);
                        LogAction($"ExecuteQueryToDataTable - Rows loaded: {dt.Rows.Count}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogAction($"Ошибка выполнения SQL-запроса: {ex.Message}");
            MessageBox.Show($"Ошибка выполнения SQL-запроса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return dt;
    }

    public static void BeginTransaction()
    {
        LogAction("BeginTransaction");

        if (_transactionConnection != null)
        {
            MessageBox.Show("Транзакция уже открыта!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!File.Exists(DatabasePath)) return;

        try
        {
            _transactionConnection = new SQLiteConnection($"Data Source={DatabasePath};Version=3;");
            _transactionConnection.Open();
            _transaction = _transactionConnection.BeginTransaction();
        }
        catch (Exception ex)
        {
            LogAction($"Ошибка при открытии транзакции: {ex.Message}");
        }
    }

    public static void CommitTransaction()
    {
        LogAction("CommitTransaction");

        if (_transaction == null)
        {
            MessageBox.Show("Нет активной транзакции для завершения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            _transaction.Commit();
        }
        catch (Exception ex)
        {
            LogAction($"Ошибка при завершении транзакции: {ex.Message}");
        }
        finally
        {
            CloseTransaction();
        }
    }

    public static void RollbackTransaction()
    {
        LogAction("RollbackTransaction");

        if (_transaction == null)
        {
            MessageBox.Show("Нет активной транзакции для отката!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            _transaction.Rollback();
        }
        catch (Exception ex)
        {
            LogAction($"Ошибка при откате транзакции: {ex.Message}");
        }
        finally
        {
            CloseTransaction();
        }
    }

    private static long ExecuteWithinTransaction(string sql, SQLiteParameter[] parameters)
    {
        LogAction($"ExecuteWithinTransaction - SQL: {sql}");

        try
        {
            using (var command = new SQLiteCommand(sql, _transactionConnection, _transaction))
            {
                if (parameters != null)
                    command.Parameters.AddRange(parameters);

                int affectedRows = command.ExecuteNonQuery();
                LogAction($"ExecuteWithinTransaction - {affectedRows} rows affected.");

                long lastInsertedId = _transactionConnection.LastInsertRowId;
                LogAction($"Last Inserted ID in Transaction: {lastInsertedId}");

                return lastInsertedId;
            }
        }
        catch (Exception ex)
        {
            LogAction($"Ошибка выполнения SQL-команды в транзакции: {ex.Message}");
            return -1;
        }
    }

    private static void CloseTransaction()
    {
        LogAction("CloseTransaction");

        try
        {
            _transaction.Dispose();
            _transaction = null;
            _transactionConnection.Close();
            _transactionConnection.Dispose();
            _transactionConnection = null;
        }
        catch (Exception ex)
        {
            LogAction($"Ошибка при закрытии транзакции: {ex.Message}");
        }
    }
}

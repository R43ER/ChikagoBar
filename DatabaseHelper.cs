using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows;

public static class DatabaseHelper
{
    private static readonly string LogsDirectory = "logs";
    private static readonly string LogFilePath = Path.Combine(LogsDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.log");

    static DatabaseHelper()
    {
        if (!Directory.Exists(LogsDirectory))
            Directory.CreateDirectory(LogsDirectory);
    }

    private static readonly Dictionary<DatabaseType, string> DatabaseFiles = new Dictionary<DatabaseType, string>
    {
        { DatabaseType.Data, "database/data.db" },
        { DatabaseType.Bar, "database/bar.db" },
        { DatabaseType.Release, "database/release.db" }
    };

    private static SQLiteConnection _transactionConnection = null;
    private static SQLiteTransaction _transaction = null;

    private static void Log(string message)
    {
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
        File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
    }

    public static void ExecuteNonQuery(DatabaseType dbType, string sql, params SQLiteParameter[] parameters)
    {
        Log($"ExecuteNonQuery - SQL: {sql}");

        if (_transactionConnection != null)
        {
            ExecuteWithinTransaction(sql, parameters);
            return;
        }

        string databasePath = GetDatabasePath(dbType);
        if (databasePath == null) return;

        try
        {
            using (var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);
                    int affectedRows = command.ExecuteNonQuery();
                    Log($"ExecuteNonQuery - {affectedRows} rows affected.");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Ошибка выполнения SQL-команды: {ex.Message}");
            MessageBox.Show($"Ошибка выполнения SQL-команды: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public static void ExecuteQuery(DatabaseType dbType, string sql, Action<SQLiteDataReader> processRow, params SQLiteParameter[] parameters)
    {
        Log($"ExecuteQuery - SQL: {sql}");

        string databasePath = GetDatabasePath(dbType);
        if (databasePath == null) return;

        try
        {
            using (var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);

                    using (var reader = command.ExecuteReader())
                    {
                        processRow(reader); // Просто передаем reader в вызванный метод
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Ошибка выполнения SQL-запроса: {ex.Message}");
            MessageBox.Show($"Ошибка выполнения SQL-запроса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }



    public static DataTable ExecuteQueryToDataTable(DatabaseType dbType, string sql, params SQLiteParameter[] parameters)
    {
        Log($"ExecuteQueryToDataTable - SQL: {sql}");

        string databasePath = GetDatabasePath(dbType);
        if (databasePath == null)
            return null;

        DataTable dt = new DataTable();

        try
        {
            using (var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);

                    using (var reader = command.ExecuteReader())
                    {
                        dt.Load(reader);
                        Log($"ExecuteQueryToDataTable - Rows loaded: {dt.Rows.Count}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Ошибка выполнения SQL-запроса: {ex.Message}");
            MessageBox.Show($"Ошибка выполнения SQL-запроса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return dt;
    }

    public static void BeginTransaction(DatabaseType dbType)
    {
        Log("BeginTransaction");

        if (_transactionConnection != null)
        {
            MessageBox.Show("Транзакция уже открыта!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        string databasePath = GetDatabasePath(dbType);
        if (databasePath == null) return;

        try
        {
            _transactionConnection = new SQLiteConnection($"Data Source={databasePath};Version=3;");
            _transactionConnection.Open();
            _transaction = _transactionConnection.BeginTransaction();
        }
        catch (Exception ex)
        {
            Log($"Ошибка при открытии транзакции: {ex.Message}");
        }
    }

    public static void CommitTransaction()
    {
        Log("CommitTransaction");

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
            Log($"Ошибка при завершении транзакции: {ex.Message}");
        }
        finally
        {
            CloseTransaction();
        }
    }

    public static void RollbackTransaction()
    {
        Log("RollbackTransaction");

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
            Log($"Ошибка при откате транзакции: {ex.Message}");
        }
        finally
        {
            CloseTransaction();
        }
    }

    private static void ExecuteWithinTransaction(string sql, SQLiteParameter[] parameters)
    {
        Log($"ExecuteWithinTransaction - SQL: {sql}");

        try
        {
            using (var command = new SQLiteCommand(sql, _transactionConnection, _transaction))
            {
                if (parameters != null)
                    command.Parameters.AddRange(parameters);
                int affectedRows = command.ExecuteNonQuery();
                Log($"ExecuteWithinTransaction - {affectedRows} rows affected.");
            }
        }
        catch (Exception ex)
        {
            Log($"Ошибка выполнения SQL-команды в транзакции: {ex.Message}");
        }
    }

    private static void CloseTransaction()
    {
        Log("CloseTransaction");

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
            Log($"Ошибка при закрытии транзакции: {ex.Message}");
        }
    }

    private static string GetDatabasePath(DatabaseType dbType)
    {
        if (!DatabaseFiles.TryGetValue(dbType, out string databasePath) || !File.Exists(databasePath))
        {
            Log($"Файл базы данных {databasePath} не найден.");
            return null;
        }
        return databasePath;
    }
}

public enum DatabaseType
{
    Data,
    Bar,
    Release
}

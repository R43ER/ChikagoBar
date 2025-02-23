using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Windows;

public static class DatabaseHelper
{
    private static readonly Dictionary<DatabaseType, string> DatabaseFiles = new Dictionary<DatabaseType, string>
    {
        { DatabaseType.Data, "database/data.db" },
        { DatabaseType.Bar, "database/bar.db" },
        { DatabaseType.Release, "database/release.db" }
    };

    private static SQLiteConnection _transactionConnection = null;
    private static SQLiteTransaction _transaction = null;

    /// <summary>
    /// Выполняет SQL-команду, не возвращающую результат (INSERT, UPDATE, DELETE).
    /// </summary>
    public static void ExecuteNonQuery(DatabaseType dbType, string sql, params SQLiteParameter[] parameters)
    {
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
                    command.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка выполнения SQL-команды: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Выполняет SQL-запрос и обрабатывает результаты через переданный метод.
    /// </summary>
    public static void ExecuteQuery(DatabaseType dbType, string sql, Action<SQLiteDataReader> processRow, params SQLiteParameter[] parameters)
    {
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
                        while (reader.Read())
                        {
                            processRow(reader);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка выполнения SQL-запроса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Открывает транзакцию для указанной базы данных.
    /// </summary>
    public static void BeginTransaction(DatabaseType dbType)
    {
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
            MessageBox.Show($"Ошибка при открытии транзакции: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Завершает транзакцию (COMMIT).
    /// </summary>
    public static void CommitTransaction()
    {
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
            MessageBox.Show($"Ошибка при завершении транзакции: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            CloseTransaction();
        }
    }

    /// <summary>
    /// Откатывает транзакцию (ROLLBACK).
    /// </summary>
    public static void RollbackTransaction()
    {
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
            MessageBox.Show($"Ошибка при откате транзакции: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            CloseTransaction();
        }
    }

    /// <summary>
    /// Выполняет SQL-команду внутри активной транзакции.
    /// </summary>
    private static void ExecuteWithinTransaction(string sql, SQLiteParameter[] parameters)
    {
        try
        {
            using (var command = new SQLiteCommand(sql, _transactionConnection, _transaction))
            {
                if (parameters != null)
                    command.Parameters.AddRange(parameters);
                command.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка выполнения SQL-команды в транзакции: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Закрывает текущее соединение с БД после завершения транзакции.
    /// </summary>
    private static void CloseTransaction()
    {
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
            MessageBox.Show($"Ошибка при закрытии транзакции: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Возвращает путь к базе данных по переданному типу.
    /// </summary>
    private static string GetDatabasePath(DatabaseType dbType)
    {
        if (!DatabaseFiles.TryGetValue(dbType, out string databasePath) || !File.Exists(databasePath))
        {
            MessageBox.Show($"Файл базы данных {databasePath} не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
        return databasePath;
    }
}

/// <summary>
/// Доступные базы данных.
/// </summary>
public enum DatabaseType
{
    Data,
    Bar,
    Release
}

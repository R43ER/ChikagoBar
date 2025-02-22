using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ChikagoBar
{
    public partial class OrderWindow : Window
    {
        private DispatcherTimer timer;
        private readonly string logFilePath;
        public ICommand ExitCommand { get; }
        List<GrpProdItem> grpProdList = new List<GrpProdItem>();
        List<AsortItem> asortList = new List<AsortItem>();

        public OrderWindow(bool discount, string discountCard)
        {
            InitializeComponent();
            SetDateTime();
            StartClock();
            LoadGrpProd();
        }

        private void LoadGrpProd()
        {
            ExecuteQuery("data.db", "SELECT * FROM GrpProd;", reader =>
            {
                while (reader.Read())
                {
                    grpProdList.Add(new GrpProdItem
                    {
                        ID = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    });
                }
            });
            grpProdDataGrid.ItemsSource = grpProdList; // Привязываем данные к DataGrid
        }


        private void SetDateTime()
        {
            // Устанавливаем текущую дату и день недели
            DateTime now = DateTime.Now;
            CultureInfo culture = new CultureInfo("ru-RU");

            curDate.Text = now.ToString("d MMMM yyyy г.", culture); // Пример: 22 февраля 2025 г.
            curDay.Text = now.ToString("dddd", culture); // Пример: Суббота
        }

        private void StartClock()
        {
            // Создаём таймер, который будет обновлять время каждую секунду
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Обновляем текущее время каждую секунду
            curTime.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private string GetSelectedRadioButtonValue()
        {
            foreach (var child in myStackPanel.Children)
            {
                if (child is RadioButton rb && rb.IsChecked == true)
                {
                    return rb.Content.ToString();
                }
            }
            return null;
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LogAction(string action)
        {
            string logEntry = $"{DateTime.Now:HH:mm:ss} - {action}";
            File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
        }

        private void ExecuteNonQuery(string databaseFileName, string sql, params SQLiteParameter[] parameters)
        {
            string databasePath = Path.Combine("database", databaseFileName);
            if (!File.Exists(databasePath))
            {
                MessageBox.Show($"Файл базы данных {databaseFileName} не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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

        private void ExecuteQuery(string databaseFileName, string sql, Action<SQLiteDataReader> processRow, params SQLiteParameter[] parameters)
        {
            string databasePath = Path.Combine("database", databaseFileName);
            if (!File.Exists(databasePath))
            {
                MessageBox.Show($"Файл базы данных {databaseFileName} не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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

        public class RelayCommand : ICommand
        {
            private readonly Action<object> execute;
            private readonly Predicate<object> canExecute;

            public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
            {
                this.execute = execute;
                this.canExecute = canExecute;
            }

            public bool CanExecute(object parameter) => canExecute?.Invoke(parameter) ?? true;

            public void Execute(object parameter) => execute(parameter);

            public event EventHandler CanExecuteChanged;
        }

        private void grpProdDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (grpProdDataGrid.SelectedItem is GrpProdItem selectedItem)
            {
                asortList.Clear();
                asortDataGrid.ItemsSource = null; // Очистка DataGrid

                // Анонимный метод вместо лямбда-выражения
                ExecuteQuery("data.db", $"SELECT * FROM Asort WHERE Actual = true AND G{selectedItem.ID} = true;", delegate (SQLiteDataReader reader)
                {
                    List<AsortItem> tempList = new List<AsortItem>();

                    while (reader.Read())
                    {
                        tempList.Add(new AsortItem
                        {
                            ID = reader.GetInt32(0),
                            Name = reader.GetString(3)
                        });
                    }

                    asortList.Clear();
                    asortList.AddRange(tempList);
                    tempList.Clear();
                    asortDataGrid.ItemsSource = asortList; // Привязываем данные к DataGrid
                });
            }
        }
    }

    public class GrpProdItem
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    public class AsortItem
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

}

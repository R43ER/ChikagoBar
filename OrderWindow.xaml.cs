using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
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
        List<VimirItem> vimirList = new List<VimirItem>();
        List<AsortItem> basketList = new List<AsortItem>();

        private bool discount;
        private string discountCard;

        public OrderWindow(bool discount, string discountCard)
        {
            InitializeComponent();
            this.discount = discount;
            this.discountCard = discountCard;
            string logsDirectory = "logs";
            if (!Directory.Exists(logsDirectory))
                Directory.CreateDirectory(logsDirectory);
            logFilePath = Path.Combine(logsDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.log");
            ExitCommand = new RelayCommand(_ => btnExit_Click(this, null));
            curOrderNo.Text = Properties.Resources.curOrderNo;
            SetDateTime();
            StartClock();
            LoadData();
        }

        private void LoadData()
        {
            LogAction("Начало загрузки данных из БД.");
            DatabaseHelper.ExecuteQuery(DatabaseType.Data, "SELECT * FROM Vimir", reader =>
            {
                while (reader.Read())
                {

                    vimirList.Add(new VimirItem
                    {
                        VimirNo = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        NotFractal = Convert.ToBoolean(reader.GetValue(2))
                    });
                }
            });
            LogAction($"Загружено единиц измерения: {vimirList.Count}");
            DatabaseHelper.ExecuteQuery(DatabaseType.Data, "SELECT * FROM GrpProd;", reader =>
            {
                while (reader.Read())
                {
                    grpProdList.Add(new GrpProdItem
                    {
                        ID = reader.GetInt32(0),
                        Name = reader.GetString(1),
                    });
                }
            });
            LogAction($"Загружено групп товаров: {grpProdList.Count}");
            grpProdDataGrid.ItemsSource = grpProdList;
            LogAction("Завершена загрузка данных из БД.");
        }

        private void SetDateTime()
        {
            DateTime now = DateTime.Now;
            CultureInfo culture = new CultureInfo("ru-RU");
            curDate.Text = now.ToString("d MMMM yyyy г.", culture);
            curDay.Text = now.ToString("dddd", culture);
        }

        private void StartClock()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
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
            LogAction("Нажата кнопка выхода (btnExit). Окно будет закрыто.");
            this.Close();
        }

        private void LogAction(string action)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {action}";
            File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
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
                LogAction($"Выбрана группа товаров: ID={selectedItem.ID}, Наименование='{selectedItem.Name}'");
                asortList.Clear();
                asortDataGrid.ItemsSource = null;
                DatabaseHelper.ExecuteQuery(DatabaseType.Data, $"SELECT * FROM Asort WHERE Actual = true AND G{selectedItem.ID} = true;", delegate (SQLiteDataReader reader)
                {
                    List<AsortItem> tempList = new List<AsortItem>();
                    while (reader.Read())
                    {
                        tempList.Add(new AsortItem
                        {
                            ID = reader.GetInt32(0),
                            Name = reader.GetString(3),
                            VimirNo = reader.GetInt32(4),
                            Price = reader.GetFloat(5)
                        });
                    }
                    asortList.Clear();
                    asortList.AddRange(tempList);
                    tempList.Clear();
                    asortDataGrid.ItemsSource = asortList;
                });
            }
        }

        private void asortDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (asortDataGrid.SelectedItem is AsortItem selectedItem)
            {
                LogAction($"Выбран товар: ID={selectedItem.ID}, Наименование='{selectedItem.Name}'");
                VimirItem vimir = vimirList.FirstOrDefault(v => v.VimirNo == selectedItem.VimirNo);
                if (vimir != null)
                {
                    selectedItem.Vimir = vimir.Name;
                    asortName.Text = selectedItem.Name;
                    vimirName.Text = selectedItem.Vimir;
                    priceText.Text = selectedItem.Price.ToString();
                    SelectFirstRadioButton();
                    manualQuantityInput.Text = "";
                }
            }
        }

        private void btnExit_Click_1(object sender, RoutedEventArgs e)
        {
            LogAction("Нажата кнопка выхода (btnExit_1). Окно будет закрыто.");
            this.Close();
        }

        private void btnAddToBasket_Click(object sender, RoutedEventArgs e)
        {
            if (asortDataGrid.SelectedItem is AsortItem selectedItem)
            {
                float quantity = 0;
                VimirItem measurementUnit = vimirList.FirstOrDefault(v => v.VimirNo == selectedItem.VimirNo);

                if (measurementUnit != null)
                {
                    AsortItem asortItem = new AsortItem
                    {
                        ID = selectedItem.ID,
                        Name = selectedItem.Name,
                        Price = selectedItem.Price,
                        VimirNo = selectedItem.VimirNo,
                        Vimir = measurementUnit.Name
                    };

                    string inputText = manualQuantityInput.Text.Trim(); // Убираем пробелы

                    // 🟢 Попытка парсинга из поля ввода
                    bool isInputValid = float.TryParse(
                        inputText.Replace(',', '.'),  // Заменяем `,` на `.` (универсальный формат)
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out quantity);

                    // Если поле ввода пустое или содержит некорректное значение → берем из RadioButton
                    if (!isInputValid)
                    {
                        string radioValue = GetSelectedRadioButtonValue();
                        float.TryParse(
                            radioValue.Replace(',', '.'),  // Аналогично обрабатываем `.`
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out quantity);
                    }

                    // Обновляем количество и сумму
                    asortItem.Quant = quantity;
                    asortItem.Summ = asortItem.Price * asortItem.Quant;

                    // Добавляем в корзину
                    basketList.Add(asortItem);

                    // Пересчёт общей суммы корзины
                    float totalBasketSum = basketList.Sum(item => item.Summ);

                    // Обновляем UI
                    basketDataGrid.ItemsSource = null;
                    basketDataGrid.ItemsSource = basketList;
                    basketSumm.Text = totalBasketSum.ToString("F2");

                    LogAction($"Добавлен товар в корзину: ID={asortItem.ID}, '{asortItem.Name}', кол-во={asortItem.Quant}, сумма={asortItem.Summ:F2}");
                }
            }
        }



        private void btnRemoveFromBasket_Click(object sender, RoutedEventArgs e)
        {
            int oldIndex = basketDataGrid.SelectedIndex;
            if (basketDataGrid.SelectedItem is AsortItem selectedItem)
            {
                LogAction($"Запрошено удаление товара из корзины: ID={selectedItem.ID}, '{selectedItem.Name}'");
                basketList.Remove(selectedItem);
                RecalcBasketSum();
                basketDataGrid.ItemsSource = null;
                basketDataGrid.ItemsSource = basketList;
                if (basketList.Count > 0)
                {
                    if (oldIndex >= basketList.Count)
                    {
                        basketDataGrid.SelectedIndex = basketList.Count - 1;
                    }
                    else
                    {
                        basketDataGrid.SelectedIndex = oldIndex;
                    }
                }
                LogAction($"Товар удалён из корзины: ID={selectedItem.ID}, '{selectedItem.Name}'");
            }
        }

        private void btnPayForBasket_Click(object sender, RoutedEventArgs e)
        {
            LogAction("Запрошена оплата корзины.");
            if (basketList.Count == 0)
            {
                LogAction("Попытка оплатить пустую корзину. Действие отменено.");
                MessageBox.Show("Корзина пуста. Добавьте товары перед оплатой.");
                return;
            }
            LogAction($"Корзина содержит {basketList.Count} позиций. Сумма к оплате: {basketSumm.Text}");
            MessageBox.Show($"Оплата прошла успешно! Итоговая сумма: {basketSumm.Text} руб.", "Оплата", MessageBoxButton.OK, MessageBoxImage.Information);
            basketList.Clear();
            basketDataGrid.ItemsSource = null;
            basketSumm.Text = "0.00";
            LogAction("Корзина очищена после оплаты (заглушка логики).");
        }

        private void RecalcBasketSum()
        {
            float total = basketList.Sum(x => x.Summ);
            basketSumm.Text = total.ToString("F2");
        }

        private void SelectFirstRadioButton()
        {
            foreach (var child in myStackPanel.Children)
            {
                if (child is RadioButton radioButton)
                {
                    radioButton.IsChecked = true;
                    break;
                }
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
        public int VimirNo { get; set; }
        public string Vimir { get; set; }
        public float Price { get; set; }
        public float Quant { get; set; }
        public float Summ { get; set; }
    }

    public class VimirItem
    {
        public int VimirNo { get; set; }
        public string Name { get; set; }
        public bool NotFractal { get; set; }
    }
}

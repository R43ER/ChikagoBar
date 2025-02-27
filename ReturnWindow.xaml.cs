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
    public partial class ReturnWindow : Window
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
        private int discountVolume = 0;
        private float totalDiscountSumm = 0;

        public ReturnWindow(bool discount, string discountCard)
        {
            InitializeComponent();
            this.discount = discount;
            this.discountCard = discountCard;
            if (discount)
            {
                this.discountVolume = 10;
            }
            string logsDirectory = "Logs";
            logFilePath = Path.Combine(logsDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.log");
            ExitCommand = new RelayCommand(_ => btnExit_Click(this, null));
            SetDateTime();
            StartClock();
            LoadData();
        }

        private void LoadData()
        {
            LogAction("Начало загрузки данных из БД.");
            DatabaseHelper.ExecuteQuery("SELECT * FROM Vimir", reader =>
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
            DatabaseHelper.ExecuteQuery("SELECT * FROM GrpProd;", reader =>
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
                DatabaseHelper.ExecuteQuery($"SELECT * FROM Asort WHERE Actual = true AND G{selectedItem.ID} = true;", delegate (SQLiteDataReader reader)
                {
                    List<AsortItem> tempList = new List<AsortItem>();
                    while (reader.Read())
                    {
                        tempList.Add(new AsortItem
                        {
                            ID = reader.GetInt32(0),
                            AsortCode = reader.GetString(1),
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
                        AsortCode = selectedItem.AsortCode,
                        Name = selectedItem.Name,
                        Price = selectedItem.Price,
                        VimirNo = selectedItem.VimirNo,
                        Vimir = measurementUnit.Name
                    };

                    string inputText = manualQuantityInput.Text.Trim();

                    // 🟢 Попытка парсинга из поля ввода
                    bool isInputValid = float.TryParse(
                        inputText.Replace(',', '.'),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out quantity);

                    // Если поле ввода пустое или содержит некорректное значение → берем из RadioButton
                    if (!isInputValid)
                    {
                        string radioValue = GetSelectedRadioButtonValue();
                        float.TryParse(
                            radioValue.Replace(',', '.'),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out quantity);
                    }

                    // Обновляем количество
                    asortItem.Quant = quantity * -1;
                    asortItem.Summ = asortItem.Price * asortItem.Quant;

                    // 🟢 Рассчитываем скидку
                    float discountAmount = 0;
                    if (discount) // Если скидка активна
                    {
                        discountAmount = (asortItem.Summ * discountVolume) / 100; // Считаем сумму скидки
                        totalDiscountSumm += discountAmount; // Обновляем общую сумму скидки
                        asortItem.Summ -= discountAmount; // Применяем скидку к товару
                    }

                    // Добавляем товар в корзину
                    basketList.Add(asortItem);

                    // Пересчёт общей суммы корзины
                    RecalcBasketSum();

                    // Обновляем UI
                    basketDataGrid.ItemsSource = null;
                    basketDataGrid.ItemsSource = basketList;

                    LogAction($"Добавлен товар в корзину: ID={asortItem.ID}, '{asortItem.Name}', кол-во={asortItem.Quant}, сумма={asortItem.Summ:F2}, скидка={discountAmount:F2}");
                }
            }
        }




        private void btnRemoveFromBasket_Click(object sender, RoutedEventArgs e)
        {
            int oldIndex = basketDataGrid.SelectedIndex;
            if (basketDataGrid.SelectedItem is AsortItem selectedItem)
            {
                LogAction($"Запрошено удаление товара из корзины: ID={selectedItem.ID}, '{selectedItem.Name}'");

                // 🟢 Вычитаем скидку этого товара из общей суммы скидки
                float discountAmount = (selectedItem.Price * selectedItem.Quant * discountVolume) / 100;
                totalDiscountSumm -= discountAmount;

                basketList.Remove(selectedItem);
                RecalcBasketSum();

                // Обновляем UI
                basketDataGrid.ItemsSource = null;
                basketDataGrid.ItemsSource = basketList;

                if (basketList.Count > 0)
                {
                    basketDataGrid.SelectedIndex = oldIndex >= basketList.Count ? basketList.Count - 1 : oldIndex;
                }

                LogAction($"Товар удалён из корзины: ID={selectedItem.ID}, '{selectedItem.Name}', удалённая скидка={discountAmount:F2}");
            }
        }

        private void btnReturnForBasket_Click(object sender, RoutedEventArgs e)
        {
            LogAction("Запрошен возврат корзины.");
            if (basketList.Count == 0)
            {
                MessageBox.Show("Корзина пуста. Добавьте товары перед возвратом.");
                return;
            }

            decimal totalAmount = decimal.Parse(basketSumm.Text);
            LogAction($"Корзина содержит {basketList.Count} позиций.");

            MessageBoxResult result = MessageBox.Show("Оформить возврат?", "Возврат", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var zakazDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                DatabaseHelper.BeginTransaction();
                foreach (AsortItem item in basketList)
                {
                    DatabaseHelper.ExecuteNonQuery("INSERT INTO Zakaz (Date, ZakazNo, AsortNo, AsortCode, Quantity, Amount, CashNo, OperNo, Release, PrintCheck, Discount, DiscountType, CardNo) VALUES (@Date, @ZakazNo, @AsortNo, @AsortCode, @Quantity, @Amount, @CashNo, @OperNo, @Release, @PrintCheck, @Discount, @DiscountType, @CardNo)",
                        new SQLiteParameter("@Date", zakazDate),
                        new SQLiteParameter("@ZakazNo", null),
                        new SQLiteParameter("@AsortNo", item.ID),
                        new SQLiteParameter("@AsortCode", item.AsortCode),
                        new SQLiteParameter("@Quantity", item.Quant),
                        new SQLiteParameter("@Amount", item.Summ),
                        new SQLiteParameter("@CashNo", 1),
                        new SQLiteParameter("@OperNo", 1),
                        new SQLiteParameter("@Release", 1),
                        new SQLiteParameter("@PrintCheck", false),
                        new SQLiteParameter("@Discount", discount),
                        new SQLiteParameter("@DiscountType", discountVolume),
                        new SQLiteParameter("@CardNo", discountCard));
                }
                DatabaseHelper.ExecuteNonQuery("UPDATE Cash SET Rest = Rest + @totalAmount, DateFinish = @DateFinish WHERE CashNo = 1;",
                    new SQLiteParameter("@totalAmount", totalAmount),
                    new SQLiteParameter("@DateFinish", zakazDate));
                DatabaseHelper.CommitTransaction();
            }

            basketList.Clear();
            basketDataGrid.ItemsSource = null;
            basketSumm.Text = "0.00";
            LogAction($"Корзина очищена после возврата. Оплачено: руб.");
        }


        private void btnPayForBasket_Click(object sender, RoutedEventArgs e)
        {


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
}

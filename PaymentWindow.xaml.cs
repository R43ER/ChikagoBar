using System;
using System.Windows;
using System.Windows.Input;

namespace ChikagoBar
{
    public partial class PaymentWindow : Window
    {
        public decimal ReceivedAmount { get; private set; }
        private decimal totalAmount;

        public PaymentWindow(decimal amountToPay, int orderNum, float totalDiscountSumm)
        {
            InitializeComponent();
            totalAmount = amountToPay;
            orderNumText.Text = "Заказ №" + orderNum.ToString();
            needPaySummText.Text = amountToPay.ToString("F2");
            discountSumm.Text = totalDiscountSumm.ToString("F2");

            // Подписываемся на события клавиатуры
            this.PreviewKeyDown += PaymentWindow_PreviewKeyDown;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            changeSummText.Focus();
        }

        private void txtReceived_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (decimal.TryParse(receivedSummInput.Text, out decimal received))
            {
                decimal change = received - totalAmount;
                changeSummText.Text = change >= 0 ? change.ToString("F2") : "0,00";
            }
            else
            {
                changeSummText.Text = "0,00";
            }
        }

        private void PaymentWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // Закрытие окна без возврата результата
                this.DialogResult = false;
                this.Close();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                ConfirmPayment();
                e.Handled = true;
            }
        }

        private void ConfirmPayment()
        {
            if (decimal.TryParse(receivedSummInput.Text, out decimal received) && received >= totalAmount)
            {
                var result = MessageBox.Show("Заказ оплачен и его можно закрывать?", "Подтверждение",
                                             MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    ReceivedAmount = received;
                    this.DialogResult = true; // Устанавливаем корректное значение перед закрытием
                    this.Close();
                }
            }
            else
            {
                MessageBox.Show("Недостаточно средств!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private void payAllSumRectangle_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (decimal.TryParse(receivedSummInput.Text, out decimal received) && received >= totalAmount)
            {
                ConfirmPayment();
            }
            else
            {
                receivedSummInput.Text = totalAmount.ToString("F2");
            }
        }
    }
}
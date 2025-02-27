using System;
using System.Windows;
using System.Windows.Input;

namespace ChikagoBar
{
    public partial class InputBox : Window
    {
        public string InputText { get; private set; }

        public InputBox(string message, string title)
        {
            InitializeComponent();
            Title = title;
            lblMessage.Text = message;
            PreviewKeyDown += InputBox_PreviewKeyDown;
        }

        private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
            else if (e.Key == Key.Enter)
            {
                InputText = txtInput.Text;
                DialogResult = true;
                e.Handled = true;
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            txtInput.Focus();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            InputText = txtInput.Text;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

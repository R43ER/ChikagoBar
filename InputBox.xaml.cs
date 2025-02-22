using System;
using System.Windows;

namespace ChikagoBar
{
    public partial class InputBox : Window
    {
        public string InputText { get; private set; }

        public InputBox(string message, string title)
        {
            InitializeComponent();
            this.Title = title;
            lblMessage.Text = message;
        }
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            txtInput.Focus();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            InputText = txtInput.Text;
            this.DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

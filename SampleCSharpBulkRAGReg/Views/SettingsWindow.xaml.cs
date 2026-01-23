using SampleCSharpBulkRAGReg.ViewModels;
using System;
using System.Windows;

namespace SampleCSharpBulkRAGReg.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsViewModel ViewModel { get; } = new SettingsViewModel();

        public SettingsWindow(MainWindow window)
        {
            this.Owner = window;
            InitializeComponent();

            this.ViewModel.Messaged += (s, e) =>
            {
                this.ViewModel.IsBusy = true;
                try
                {
                    if (e.Message == "")
                    {
                        // Saveボタン押下時
                        App.MainVM.IsSettings = App.MainVM.IsSettings; // 設定状態を更新
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show(this.Owner, e.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
                catch (Exception ex)
                {
                    // ViewModelの処理で例外が発生した場合はここでキャッチしてメッセージ表示
                    MessageBox.Show(this.Owner, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                this.ViewModel.IsBusy = false;
            };
        }
    }
}

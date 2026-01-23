using SampleCSharpBulkRAGReg.ViewModels;
using System.Windows;

namespace SampleCSharpBulkRAGReg.Views
{
    /// <summary>
    /// AboutWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutViewModel ViewModel { get; } = new AboutViewModel();

        public AboutWindow(MainWindow window)
        {
            this.Owner = window;
            InitializeComponent();

            App.MainVM.IsBusy = true;
            try
            {
                this.ViewModel.GetVersion();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(this.Owner, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            App.MainVM.IsBusy = false;
        }

        /// <summary>
        /// ハイパーリンクのクリック時に外部ブラウザでURIを開く
        /// </summary>
        /// <param name="sender">クリックされたハイパーリンク</param>
        /// <param name="e">ナビゲーション要求イベント引数</param>
        private void RequestNavigation(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            // ハイパーリンクのNavigateUriを取得
            string navigateUri = ((System.Windows.Documents.Hyperlink)sender).NavigateUri.ToString();
            // 既定のブラウザでURIを開く
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = navigateUri, UseShellExecute = true });
            // イベントを処理済みにする
            e.Handled = true;
        }
    }
}

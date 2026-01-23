using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;

namespace SampleCSharpBulkRAGReg.Views
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private SynchronizationContext Context { get; set; } = SynchronizationContext.Current;

        public ViewModels.MainViewModel ViewModel { get; } = App.MainVM;
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += async (s, e) =>
            {
                this.ViewModel.IsBusy = true;
                try
                {
                    // 初期接続
                    await App.MainVM.ConnectAsync();
                }
                catch (Exception ex)
                {
                    // ViewModelの処理で例外が発生した場合はここでキャッチしてメッセージ表示
                    App.MainVM.OnMessaged(ex.Message);
                }
                this.ViewModel.IsBusy = false;
            };

            // サブ画面表示
            App.MainVM.Messaged += async (s, e) =>
            {
                this.ViewModel.IsBusy = true;
                try
                {
                    switch (e.Message)
                    {
                        case "Connect":
                            {
                                await App.MainVM.ConnectAsync();
                            }
                            break;

                        case "Disconnect":
                            {
                                await App.MainVM.DisconnectAsync();
                            }
                            break;

                        case "Settings":
                            {
                                this.ShowDialog(new SettingsWindow(this) { Owner = this });
                            }
                            break;

                        case "About":
                            {
                                this.ShowDialog(new AboutWindow(this) { Owner = this });
                            }
                            break;

                        case "SelectFolder":
                            {
                                using (var dlg = new System.Windows.Forms.FolderBrowserDialog()
                                {
                                    Description = Properties.Resources.SelectFolder,
                                    SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                    ShowNewFolderButton = true,
                                })
                                {
                                    // ShowDialog() はアプリケーションモーダルで表示されます
                                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedPath))
                                    {
                                        App.MainVM.FolderName = dlg.SelectedPath;
                                        Commons.LogHelper.WriteLog($"Selected folder: {dlg.SelectedPath}");
                                    }
                                }
                            }
                            break;

                        case "ConfirmationRemove":
                            {
                                // 削除ボタン押下
                                var result = System.Windows.MessageBox.Show(this, "削除します。よろしいですか。", this.Title, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                                if (result == MessageBoxResult.No)
                                {
                                    // いいえが選択された場合
                                }
                                else
                                {
                                    this.ViewModel.DeleteRetrieverAsync();
                                }
                            }
                            break;

                        default:
                            {
                                // ViewModelの処理で例外が発生した場合はここでキャッチしてメッセージ表示
                                try
                                {
                                    Commons.LogHelper.WriteLog(e.Message, MessageBoxIcon.Error);
                                }
                                catch (Exception) { }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        Commons.LogHelper.WriteLog(ex.Message, MessageBoxIcon.Error);
                    }
                    catch (Exception) { }
                }
                this.ViewModel.IsBusy = false;
            };

            App.MainVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Messages_Item")
                {
                    // 最新メッセージが追加されたときに最下部までスクロールする
                    var border = VisualTreeHelper.GetChild(this.listBoxMessage, 0) as Border;
                    if (border != null)
                    {
                        var listBoxScroll = border.Child as ScrollViewer;
                        if (listBoxScroll != null)
                        {
                            // スクロールバーを末尾に移動 
                            listBoxScroll.ScrollToEnd();
                        }
                    }
                }
            };
        }

        // メイン画面を使用不可にして疑似的なDialogとする（サブスクリーン含め移動などは可能）
        private void ShowDialog(Window target)
        {
            target.Closed += (_, __) =>
            {
                this.IsEnabled = true;
                this.Activate();
            };
            this.IsEnabled = false;
            target.Show();
        }
    }
}

using SampleCSharpBulkRAGReg.Commons;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SampleCSharpBulkRAGReg.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private Models.ChatModel Model = new Models.ChatModel();
        private Models.RetrieverModel RagModel = new Models.RetrieverModel();

        /// <summary>
        /// IDトークン
        /// </summary>
        public string IdToken { get { return this.Model.IdToken; } }

        /// <summary>
        /// GAP接続済みかどうかを示すプロパティ
        /// </summary>
        public bool IsLogin
        {
            get { return this.Model.IsLogin; }
            set { OnPropertyChanged(); }
        }

        /// <summary>
        /// 初期設定済みかどうかを示すプロパティ
        /// </summary>
        public bool IsSettings
        {
            get { return this.Model.IsSettings; }
            set { OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets the username associated with the current user.
        /// </summary>
        public string UserName
        {
            get { return this.Model.UserName; }
        }

        /// <summary>
        /// Represents the unique identifier (ID) of the current user.
        /// </summary>
        public string UserId
        {
            get { return this.Model.UserId; }
        }

        /// <summary>
        /// Busy表示用
        /// </summary>
        private bool _IsBusy = false;
        public bool IsBusy
        {
            get { return _IsBusy; }
            set
            {
                _IsBusy = value;
                OnPropertyChanged();
                if (value)
                {
                    App.Current.MainWindow.Cursor = System.Windows.Input.Cursors.Wait;
                }
                else
                {
                    App.Current.MainWindow.Cursor = null;
                }
            }
        }

        /// <summary>
        /// リトリーバ一覧を表すコレクション
        /// </summary>
        public ObservableCollection<Models.TDataRetriever> Retrievers
        {
            get { return this.RagModel.Retrievers; }
            set { this.RagModel.Retrievers = value; }
        }

        /// <summary>
        /// 対象リトリーバ一
        /// </summary>
        private Models.TDataRetriever _Retriever;
        public Models.TDataRetriever Retriever
        {
            get { return this._Retriever; }
            set
            {
                this._Retriever = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 処理対象フォルダ
        /// </summary>
        private string _FolderName = string.Empty;
        public string FolderName
        {
            get { return _FolderName; }
            set
            {
                if (_FolderName != value)
                {
                    _FolderName = value;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// 処理対象フォルダ
        /// </summary>
        private string _LogText = string.Empty;
        public string LogText
        {
            get { return _LogText; }
            set
            {
                if (_LogText != value)
                {
                    _LogText = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 共有RAGにするかどうかのフラグ（起動時パラメタで設定）
        /// </summary>
        public bool IsPublic
        {
            get
            {
                var isPublic = false;
                try
                {
                    // 起動時パラメタの確認: "Public" 指定で IsPublic = true に設定
                    var args = Environment.GetCommandLineArgs().Skip(1); // 0 は実行ファイル名
                    isPublic = args.Any(a =>
                        string.Equals(a, "Public", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(a, "/public", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(a, "-public", StringComparison.OrdinalIgnoreCase)
                        || a.StartsWith("--public", StringComparison.OrdinalIgnoreCase)
                    );
                }
                catch
                {
                    // 安全のため例外は無視（IsPublic は既定で false）
                }
                return isPublic;
                ;
            }
        }

        /// <summary>
        /// コンストラクター
        /// </summary>
        public MainViewModel()
        {
            // ModelのイベントをViewModelのイベントに転送する
            this.Model.PropertyChanged += async (s, e) =>
            {
                OnPropertyChanged(e.PropertyName);
            };
            this.RagModel.PropertyChanged += async (s, e) =>
            {
                OnPropertyChanged(e.PropertyName);
            };
        }

        /// <summary>
        /// 接続処理を非同期で実行
        /// </summary>
        /// <returns></returns>
        internal async Task ConnectAsync()
        {
            // 接続情報設定済の時は、ログイン処理を実行
            if (this.Model.IsSettings)
            {
                await this.Model.ConnectAsync();
                if (this.Model.IsLogin)
                {
                    await this.RagModel.GetRetrieversAsync(false);
                }
            }
            else
            {
                OnMessaged("Settings");
            }
        }

        /// <summary>
        /// 切断処理を非同期で実行
        /// </summary>
        /// <returns></returns>
        internal async Task DisconnectAsync()
        {
            // 接続情報設定済の時は、ログイン処理を実行
            if (this.Model.IsSettings)
            {
                await this.Model.DisconnectAsync();
            }
        }

        /// <summary>
        /// サブ画面表示
        /// </summary>
        RelayCommand<string> _ShowDialogCommand;
        public RelayCommand<string> ShowDialogCommand
        {
            get
            {
                if (_ShowDialogCommand == null)
                {
                    _ShowDialogCommand = new RelayCommand<string>((target) =>
                    {
                        this.IsBusy = true;
                        try
                        {
                            OnMessaged(target);
                        }
                        catch (Exception ex)
                        {
                            // Command内で例外が発生した場合はここでキャッチしてメッセージ表示
                            OnMessaged(ex.Message);
                        }
                        this.IsBusy = false;
                    });
                }
                return _ShowDialogCommand;
            }
            set
            {
                _ShowDialogCommand = value;
            }
        }


        /// <summary>
        /// 登録
        /// </summary>
        /// <remarks>
        /// フォルダ名をRAG名として、フォルダ内のファイルをそのRAGに登録する、処理はAPIの終了ではなく処理の終了を判定しシーケンシャルに実施する
        /// </remarks>
        RelayCommand _SaveCommand;
        public RelayCommand SaveCommand
        {
            get
            {
                if (_SaveCommand == null)
                {
                    _SaveCommand = new RelayCommand(async () =>
                    {
                        this.IsBusy = true;
                        try
                        {
                            Commons.LogHelper.WriteLog($"Start Create Retriever from Folder. FolderName: {FolderName}");
                            await this.RagModel.CreateRetrieverFromFolder(FolderName, true);
                            Commons.LogHelper.WriteLog($"Completed Create Retriever from Folder. FolderName: {FolderName}");
                        }
                        catch (Exception ex)
                        {
                            OnMessaged(ex.Message);
                        }
                        finally
                        {
                            this.IsBusy = false;
                        }
                    });
                }
                return _SaveCommand;
            }
            set
            {
                _SaveCommand = value;
            }
        }

        /// <summary>
        /// リトリーバー削除(確認ダイアログあり)
        /// </summary>
        RelayCommand<Models.TDataRetriever> _DeleteRetrieverCommand;
        public RelayCommand<Models.TDataRetriever> DeleteRetrieverCommand
        {
            get
            {
                if (_DeleteRetrieverCommand == null)
                {
                    _DeleteRetrieverCommand = new RelayCommand<Models.TDataRetriever>((target) =>
                    {
                        this.IsBusy = true;
                        try
                        {
                            // 削除
                            this.Retriever = target;
                            OnMessaged("ConfirmationRemove");
                        }
                        catch (Exception ex)
                        {
                            OnMessaged(ex.Message);
                        }
                        finally
                        {
                            this.IsBusy = false;
                        }
                    });
                }
                return _DeleteRetrieverCommand;
            }
            set
            {
                _DeleteRetrieverCommand = value;
            }
        }

        /// <summary>
        /// リトリーバー削除
        /// </summary>
        internal async void DeleteRetrieverAsync()
        {
            this.IsBusy = true;
            try
            {
                // 削除
                await this.RagModel.DeleteRetrieverAsync(this.Retriever?.ID);

                // 再取得
                await this.RagModel.GetRetrieversAsync(false);
            }
            catch (Exception ex)
            {
                OnMessaged(ex.Message);
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        /// <summary>
        /// ダイアログ表示用イベント
        /// </summary>
        public event MessagedEventHandler Messaged;
        internal virtual void OnMessaged(String message = "")
        {
            this.Messaged?.Invoke(this, new MessageEventArgs(message));
        }

        // プロパティが変更されたときに通知するイベント
        public event PropertyChangedEventHandler PropertyChanged;

        // プロパティ変更通知を発行するメソッド
        protected virtual void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

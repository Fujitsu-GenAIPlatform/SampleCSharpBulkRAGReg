using SampleCSharpBulkRAGReg.Commons;
using SampleCSharpBulkRAGReg.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SampleCSharpBulkRAGReg.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private SynchronizationContext Context = SynchronizationContext.Current;
        private SettingsModel Model = new SettingsModel();

        public string TenantName
        {
            get { return this.Model.TenantName; }
            set { this.Model.TenantName = value; }
        }
        public string ClientId
        {
            get { return this.Model.ClientId; }
            set { this.Model.ClientId = value; }
        }

        public bool IsUseOSWebView
        {
            get { return this.Model.IsUseOSWebView; }
            set { this.Model.IsUseOSWebView = value; }
        }   

        public bool IsPromptAuthentication
        {
            get { return this.Model.IsPromptAuthentication; }
            set { this.Model.IsPromptAuthentication = value; }
        }

        public string ClientSecret
        {
            get { return this.Model.ClientSecret; }
            set { this.Model.ClientSecret = value; }
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
                OnPropertyChanged("IsBusy");
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
        /// コンストラクター
        /// </summary>
        public SettingsViewModel()
        {
        }

        /// <summary>
        /// 設定保存
        /// </summary>
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
                            await this.Model.SaveAsync();
                            OnMessaged("");
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

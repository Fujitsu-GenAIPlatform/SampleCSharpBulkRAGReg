using SampleCSharpBulkRAGReg.Commons;
using SampleCSharpBulkRAGReg.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SampleCSharpBulkRAGReg.Models
{
    internal class ChatModel : INotifyPropertyChanged
    {
        private SynchronizationContext Context { get; set; } = SynchronizationContext.Current;

        public string IdToken { get; private set; } = string.Empty;

        #region "認証関連"
        private TimeSpan ExpireInterval;
        private DispatcherTimer ExpireTimer = new DispatcherTimer();

        /// <summary>
        /// GAP接続済
        /// </summary>
        private bool _IsLogin = false;
        public bool IsLogin
        {
            get { return _IsLogin; }
            private set
            {
                if (_IsLogin != value)
                {
                    _IsLogin = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Represents the username associated with the current user.
        /// </summary>
        private string _UserName = string.Empty;
        public string UserName
        {
            get { return _UserName; }
            private set
            {
                if (_UserName != value)
                {
                    _UserName = value;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// Represents the unique identifier (ID) of the current user.
        /// </summary>
        private string _UserId = string.Empty;
        public string UserId
        {
            get { return _UserId; }
            private set
            {
                if (_UserId != value)
                {
                    _UserId = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 初期設定済
        /// </summary>
        public bool IsSettings { get { return !string.IsNullOrEmpty(Config.ClientId) && !string.IsNullOrEmpty(Config.TenantName); } }

        /// <summary>
        /// GAPと接続
        /// </summary>
        /// <returns></returns>
        internal async Task ConnectAsync()
        {
            this.ExpireTimer.Stop();

            // 接続処理
            this.IdToken = string.Empty;
            this.UserName = string.Empty;
            this.IsLogin = !string.IsNullOrEmpty(this.IdToken);
            using (var id = new MicrosoftIdentityModel())
            {
                if (Config.IsPromptAuthentication)
                {
                    await id.LoginAsync(Config.ClientId, Config.TenantName);
                }
                else
                {
                    await id.LoginAsync(Config.ClientId, Config.TenantName, Config.ClientSecret);
                }
                this.IdToken = id.IdToken;
                this.UserName = id.UserName;
                this.UserId = id.UserId;
                this.ExpireInterval = id.ExpireInterval;
                this.ExpireTimer.Interval = this.ExpireInterval;
            }
            if (!string.IsNullOrEmpty(this.IdToken))
            {
                this.ExpireTimer.Start();
            }
            this.IsLogin = !string.IsNullOrEmpty(this.IdToken);

            // ログ出力
            if (this.IsLogin)
            {
                Commons.LogHelper.WriteLog($"Login Success. UserName: {this.UserName}");
            }
            else
            {
                Commons.LogHelper.WriteLog($"Login Failed.", System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// GAPとの切断
        /// </summary>
        /// <returns></returns>
        internal async Task DisconnectAsync()
        {
            this.ExpireTimer.Stop();
            this.IdToken = string.Empty;
            this.UserName = string.Empty;
            this.IsLogin = !string.IsNullOrEmpty(this.IdToken);
            using (var id = new MicrosoftIdentityModel())
            {
                await id.Logout();
            }

            // ログ出力
            Commons.LogHelper.EndLog();
        }
        #endregion

        /// <summary>
        /// コンストラクター
        /// </summary>
        public ChatModel()
        {
            this.ExpireTimer.Stop();

            // 認証キー期限切れ対応
            this.ExpireTimer.Tick += async (s, e) =>
            {
                this.ExpireTimer.Stop();
                try
                {
                    using (var id = new MicrosoftIdentityModel())
                    {
                        if (Config.IsPromptAuthentication)
                        {
                            await id.LoginAsync(Config.ClientId, Config.TenantName);
                        }
                        else
                        {
                            await id.LoginAsync(Config.ClientId, Config.TenantName, Config.ClientSecret);
                        }
                        this.IdToken = id.IdToken;
                        this.UserName = id.UserName;
                        this.ExpireInterval = id.ExpireInterval;
                        this.ExpireTimer.Interval = this.ExpireInterval;
                    }
                    if (!string.IsNullOrEmpty(this.IdToken))
                    {
                        this.ExpireTimer.Start();
                    }
                }
                catch
                {
                    this.ExpireTimer.Start();
                }
            };
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

    /// <summary>
    /// チャットルーム
    /// </summary>
    public class TDataChatRoom
    {
        public string ID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ChatTemplateId { get; set; } = string.Empty;
        public string[] RetrieverIDs { get; set; } = null;
    }

    /// <summary>
    /// リトリーバー
    /// </summary>
    public class TDataRetriever
    {
        public string ID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsOwner { get; set; } = false;
        public bool IsPublic { get; set; } = false;
        public string EmbeddingModel { get; set; } = string.Empty;
        public string[] OriginIDs { get; set; } = null;
        public long Created { get; set; } = 0;
    }
}
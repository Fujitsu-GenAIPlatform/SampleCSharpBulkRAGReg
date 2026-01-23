using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SampleCSharpBulkRAGReg.Models
{
    internal class AboutModel : INotifyPropertyChanged
    {

        // 会社名を取得するプロパティ
        private string _Company = null;
        public string Company
        {
            get { return _Company; }
            private set
            {
                if (_Company != value)
                {
                    _Company = value;
                    OnPropertyChanged();
                }
            }
        }

        // アプリ情報を取得するプロパティ
        private string _AppInfo = null;
        public string AppInfo
        {
            get { return _AppInfo; }
            private set
            {
                if (_AppInfo != value)
                {
                    _AppInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// バージョン番号設定
        /// </summary>
        public void GetVersion()
        {
            var mainAssembly = Assembly.GetEntryAssembly();

            //自分自身のAssemblyを取得
            var asm = System.Reflection.Assembly.GetExecutingAssembly();

            //バージョンの取得
            var version = asm.GetName().Version;

            // コピーライト情報を取得
            var CopyrightArray = mainAssembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            if ((CopyrightArray != null) && (CopyrightArray.Length > 0))
            {
                this.Company = ((AssemblyCopyrightAttribute)CopyrightArray[0]).Copyright;
            }
            this.AppInfo = $"{mainAssembly?.GetName().Name ?? "UnknownApp"} V{version.Major}.{version.Minor}.{version.Build} {(IntPtr.Size == 8 ? "(x64)" : "(x86)")}"; //ver.ToString();
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

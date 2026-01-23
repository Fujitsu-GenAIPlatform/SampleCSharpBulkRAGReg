using SampleCSharpBulkRAGReg.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SampleCSharpBulkRAGReg.ViewModels
{
    public class AboutViewModel : INotifyPropertyChanged
    {
        private AboutModel Model = new AboutModel();

        public string Company { get { return this.Model.Company; } }
        public string AppInfo { get { return this.Model.AppInfo; } }

        /// <summary>
        /// コンストラクター
        /// </summary>
        public AboutViewModel()
        {
            this.Model.PropertyChanged += (s, e) => { OnPropertyChanged(e.PropertyName); };
        }

        /// <summary>
        ///  バージョン情報取得
        /// </summary>
        internal void GetVersion()
        {
            this.Model.GetVersion();
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

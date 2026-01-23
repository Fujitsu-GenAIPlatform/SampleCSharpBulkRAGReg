using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SampleCSharpBulkRAGReg
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        // MainViewModel (シングルトン)
        private static ViewModels.MainViewModel _MainVM = null;
        public static ViewModels.MainViewModel MainVM
        {
            get
            {
                if (_MainVM == null)
                {
                    _MainVM = new ViewModels.MainViewModel();
                }
                return _MainVM;
            }
        }

        // MSAL認証
        private static Microsoft.Identity.Client.IPublicClientApplication _clientApp;
        public static Microsoft.Identity.Client.IPublicClientApplication PublicClientApp
        {
            get { return _clientApp; }
            set { _clientApp = value; }
        }

    }
}

using System;
using System.Security.Cryptography;
using System.Text;

namespace SampleCSharpBulkRAGReg.Commons
{
    internal static class CryptographyHelper
    {
        /// <summary>
        /// 指定されたプレーンテキスト文字列を暗号化し、暗号化されたデータを Base64 形式の文字列として返却
        /// </summary>
        /// <remarks>
        /// 暗号化はローカル マシン スコープを使用するため、暗号化されたデータは暗号化を行った同じマシン上でのみ復号できます。
        /// このメソッドは、異なるコンピューター間で共有する必要のない機密データを保護するのに適しています。
        /// <param name="plainText"></param>
        /// <returns></returns>
        public static string EncryptString(string plainText)
        {
            var data = Encoding.UTF8.GetBytes(plainText);
            var scope = DataProtectionScope.LocalMachine;
            var protectedBytes = ProtectedData.Protect(data, optionalEntropy: null, scope);
            return Convert.ToBase64String(protectedBytes);
        }

        /// <summary>
        /// Base64 でエンコードされた、マシン保護された文字列を復号し、元のプレーンテキスト値を返却
        /// </summary>
        /// <remarks>
        /// このメソッドは復号にローカル マシン スコープを使用するため、同じコンピューター上で実行されているアプリケーションのみがデータを復号できます。
        /// このメソッドは、対応する暗号化ルーチンと同じスコープで保護された機密情報を取得する場合に使用してください。
        /// <param name="protectedText"></param>
        /// <returns></returns>
        public static string DecryptStringe(string protectedText)
        {
            var protectedBytes = Convert.FromBase64String(protectedText);
            var scope = DataProtectionScope.LocalMachine;

            var data = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, scope);
            return Encoding.UTF8.GetString(data);
        }
    }
}

using Microsoft.Identity.Client;
using SampleCSharpBulkRAGReg.Commons;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SampleCSharpBulkRAGReg.Models
{
    internal class MicrosoftIdentityModel : IDisposable
    {
        // 要求するスコープ（OpenID認証とオフラインアクセス）
        private readonly string[] Scopes = new string[] { "openid", "offline_access" };

        // アクセストークンの有効期限間隔（50分）
        internal TimeSpan ExpireInterval { get; private set; } = new TimeSpan(0, 0, 50, 0, 0);

        // 取得したアクセストークン
        internal string IdToken { get; private set; } = string.Empty;

        // ユーザー名
        internal string UserName { get; private set; } = string.Empty;
        internal string UserId { get; private set; } = string.Empty;

        // コンストラクター
        public MicrosoftIdentityModel()
        {
        }

        /// <summary>
        /// MSALログイン（対話認証）
        /// </summary>
        /// <returns></returns>
        internal async Task<bool> LoginAsync(string clientId, string tenantName)
        {
            var isLogin = false;
            var redirectUri = "http://localhost";
            var idToken = string.Empty;
            AuthenticationResult authResult = null;

            if (App.PublicClientApp == null)
            {
                var signInPolicy = "B2C_1_fjcloud_genai_susi";
                var authorityBase = $"https://{tenantName}.b2clogin.com/tfp/{tenantName}.onmicrosoft.com/";
                var builder = PublicClientApplicationBuilder
                    .Create(clientId)
                    .WithRedirectUri(redirectUri)
                    .WithB2CAuthority($"{authorityBase}{signInPolicy}")
                    .WithAuthority(AzureCloudInstance.AzurePublic, "common");
                App.PublicClientApp = builder.Build();
            }

            // ログインする
            try
            {
                // ログイン済みの認証情報を使ってサイレント更新を試みる
                var accounts = await App.PublicClientApp.GetAccountsAsync().ConfigureAwait(false);
                authResult = await App.PublicClientApp
                    .AcquireTokenSilent(this.Scopes, accounts.FirstOrDefault())
                    .ExecuteAsync();
            }
            catch (MsalException ex)
            {
                // UIからログインする
                if (ex.ErrorCode != "authentication_canceled")
                {
                    authResult = await AcquireTokenInteractiveAsync();
                }
            }
            catch (Exception ex)
            {
                this.IdToken = idToken;
                throw ex;
            }
            if (authResult != null)
            {
                idToken = authResult.IdToken;
                if (!string.IsNullOrEmpty(idToken))
                {
                    var handler = new JwtSecurityTokenHandler();
                    var data = handler.ReadJwtToken(idToken);//JwtSecurityTokenHandlerを使ってトークンをデータ化する
                    if (data != null)
                    {
                        var expClaim = data.Claims.FirstOrDefault(x => x.Type.Equals("exp"))?.Value;
                        if (!string.IsNullOrEmpty(expClaim))
                        {
                            var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim));
                            this.ExpireInterval = dateTimeOffset.UtcDateTime.AddMinutes(-3).Subtract(DateTime.UtcNow);
                        }
                        this.UserName = data.Claims.FirstOrDefault(x => x.Type.Equals("name"))?.Value;
                        this.UserId = data.Claims.FirstOrDefault(x => x.Type.Equals("sub"))?.Value;
                    }
                    isLogin = true;
                }
            }

            this.IdToken = idToken;
            return isLogin;
        }

        // 対話ログイン
        private async Task<AuthenticationResult> AcquireTokenInteractiveAsync()
        {
            var auth = App.PublicClientApp
                        .AcquireTokenInteractive(this.Scopes)
                        .WithUseEmbeddedWebView(!Config.IsUseOSWebView)
                        .WithAccount(null)
                        .WithPrompt(Prompt.SelectAccount);
            return await auth.ExecuteAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// MSALログイン（非対話認証）
        /// </summary>
        /// <returns></returns>
        internal async Task<bool> LoginAsync(string clientId, string tenantName, string clientSecret)
        {
            var isLogin = false;
            var redirectUri = "http://localhost";
            var idToken = string.Empty;

            var signInPolicy = "B2C_1_fjcloud_genai_susi";
            var authorityBase = $"https://{tenantName}.b2clogin.com/tfp/{tenantName}.onmicrosoft.com/";
            var scopes = new string[] { $"https://{tenantName}.onmicrosoft.com/{clientId}/.default" };
            var builder = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithRedirectUri(redirectUri)
                .WithB2CAuthority($"{authorityBase}{signInPolicy}")
                .WithClientSecret(clientSecret);
            var app = builder.Build();
            try
            {
                var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                idToken = result.AccessToken;
                if (!string.IsNullOrEmpty(idToken))
                {
                    var handler = new JwtSecurityTokenHandler();
                    var data = handler.ReadJwtToken(idToken);//JwtSecurityTokenHandlerを使ってトークンをデータ化する
                    if (data != null)
                    {
                        var expClaim = data.Claims.FirstOrDefault(x => x.Type.Equals("exp"))?.Value;
                        if (!string.IsNullOrEmpty(expClaim))
                        {
                            var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim));
                            this.ExpireInterval = dateTimeOffset.UtcDateTime.AddMinutes(-3).Subtract(DateTime.UtcNow);
                        }
                        this.UserName = data.Claims.FirstOrDefault(x => x.Type.Equals("name"))?.Value ?? "SYSTEM";
                        this.UserId = data.Claims.FirstOrDefault(x => x.Type.Equals("sub"))?.Value;
                    }
                    isLogin = true;
                }
            }
            catch (Exception ex)
            {
                this.IdToken = idToken;
                throw ex;
            }

            this.IdToken = idToken;
            return isLogin;
        }

        /// <summary>
        /// MSALログアウト
        /// </summary>
        /// <returns></returns>
        internal async Task Logout()
        {
            if (App.PublicClientApp != null)
            {
                try
                {
                    var accounts = await App.PublicClientApp.GetAccountsAsync();
                    foreach (var account in accounts)
                    {
                        await App.PublicClientApp.RemoveAsync(account);
                    }
                }
                catch { }
            }
            App.PublicClientApp = null;
            this.IdToken = string.Empty;
        }

        public void Dispose()
        {
        }
    }
}
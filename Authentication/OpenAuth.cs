using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Browser;
using Newtonsoft.Json.Bson;
using Serilog;

namespace CommissioningChecklistGenerator.Authentication
{
    internal static class OpenAuth
    {
        private const string Prefix = "[OpenAuth]";

        private const int RedirectPort = 7900;

        private static readonly string RedirectUri = $"http://localhost:{RedirectPort}/";

        private static readonly string LogoutUri = $"http://localhost:{RedirectPort}/";

        private static OidcClientOptions OpenAuthConfiguration = new OidcClientOptions();

        private static OidcClient? OpenAuthClient;

        public static HttpClient? TokenRefreshClient { get; private set; }

        private static string IdentityToken = "";

        public static bool IsAuthenticated { get; internal set; } = false;

        public static bool IsAuthenticated { get; private set; } = false;

        public static bool IsInitialized { get { return (OpenAuthClient != null); } }

        public static async Task Initialize(string auth, string id)
        {
            Log.Debug($"{Prefix} initializing with authority -> {auth} client id -> {id}");
            try { 
                OpenAuthConfiguration.Authority = auth;
                OpenAuthConfiguration.ClientId = id;
                OpenAuthConfiguration.Scope = "openid profile email offline_access";
                OpenAuthConfiguration.RedirectUri = RedirectUri;
                OpenAuthConfiguration.PostLogoutRedirectUri = LogoutUri;
                OpenAuthConfiguration.Browser = new SystemBrowser();
                OpenAuthConfiguration.DisablePushedAuthorization = true;

                OpenAuthClient = new OidcClient(OpenAuthConfiguration);

                await Login();
            }
            catch(Exception e) { Log.Fatal(e, $"{Prefix}");  }
        }

        private static async Task Login()
        {
            if (IsInitialized && OpenAuthClient != null) {
                try
                {
                    LoginResult result = await OpenAuthClient.LoginAsync(new LoginRequest());

                    if (result.IsError) {
                        IsAuthenticated = false;
                        Log.Warning($"{Prefix} login error -> {(result.Error != String.Empty ? result.Error.ToLower() : "success")} : {(result.ErrorDescription != String.Empty ? result.ErrorDescription : "")}"); 
                    }
                    else
                    {
                        IsAuthenticated = true;

                        Log.Information($"{Prefix} login successful -> token expires: {result.AccessTokenExpiration.ToLocalTime().ToString()}");
                        Log.Debug($"{Prefix} application authenticated with user -> {result.User?.Identity?.Name}");

                        IdentityToken = result.IdentityToken;
                        Log.Debug($"{Prefix} storing identity token for logout requests");

                        DelegatingHandler refreshHandler = result.RefreshTokenHandler;
                        AuthenticationStateUpdateHandler authenticationHandler = new AuthenticationStateUpdateHandler();

                        refreshHandler.InnerHandler = authenticationHandler;
                        Log.Debug($"{Prefix} assigned refresh token -> auth handler");
                        authenticationHandler.InnerHandler = new HttpClientHandler();
                        Log.Debug($"{Prefix} assigned auth handler -> inner handler");
                        TokenRefreshClient = new HttpClient(refreshHandler, disposeHandler: true);
                        Log.Information($"{Prefix} token refresh client created/updated");                
                    }
                }
                catch (InvalidOperationException e) { Log.Fatal(e, $"{Prefix} login failed"); }
            }
            else { Log.Fatal($"{Prefix} open auth not initialized, cannot login"); }
        }
    
        public static async Task Logout()
        {
            if (OpenAuthClient != null && IsInitialized && IsAuthenticated)
            {
                LogoutResult result = await OpenAuthClient.LogoutAsync(new LogoutRequest() {  IdTokenHint = IdentityToken});

                if (result.IsError) { Log.Warning($"{Prefix} logout error -> {(result.Error != String.Empty ? result.Error : "success")} {(result.ErrorDescription != String.Empty ? result.ErrorDescription : "")}"); }
                else
                { 
                    Log.Information($"{Prefix} logout successful");
                    IdentityToken = "";
                }

                IsAuthenticated = false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Browser;
using Newtonsoft.Json.Bson;
using Serilog;

namespace CommissioningChecklistGenerator.Authentication
{
    internal static class OpenAuth
    {
        private const string Prefix = "[OpenAuth]";

        private const string RedirectUri = "http://localhost/callback";
        private static OidcClientOptions OpenAuthConfiguration = new OidcClientOptions();
        private static OidcClient OpenAuthClient = new OidcClient(OpenAuthConfiguration);

        public static bool IsAuthenticated { get; private set; } = false;
        public static bool IsInitialized { get; private set; } = false;

        private static string Token = String.Empty;

        public static void Initialize(string auth, string id)
        {
            OpenAuthConfiguration.Authority = auth;
            OpenAuthConfiguration.ClientId = id;
            OpenAuthConfiguration.Scope = "openid profile email";
            OpenAuthConfiguration.RedirectUri = RedirectUri;
            OpenAuthConfiguration.Browser = new SystemBrowser();
            IsInitialized = true;
        }

        private static async Task Login()
        {
            if (IsInitialized) {
                LoginResult result = await OpenAuthClient.LoginAsync();
                Log.Debug($"{Prefix} login result -> {result}");
            }
            else { Log.Fatal($"{Prefix} open auth not initialized, cannot login"); }
        }
    }
}

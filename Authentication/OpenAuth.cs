using System;
using System.Collections.Generic;
using System.Linq;
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
        private static OidcClientOptions OpenAuthConfiguration = new OidcClientOptions();
        private static OidcClient? OpenAuthClient;

        public static bool IsAuthenticated { get; private set; } = false;

        public static bool IsInitialized 
        { 
            get { return (OpenAuthClient != null); }
        }

        private static string Token = String.Empty;

        public static async Task Initialize(string auth, string id)
        {
            try { 
                OpenAuthConfiguration.Authority = auth;
                OpenAuthConfiguration.ClientId = id;
                OpenAuthConfiguration.Scope = "openid profile email";
                OpenAuthConfiguration.RedirectUri = RedirectUri;
                OpenAuthConfiguration.Browser = new SystemBrowser();
                OpenAuthConfiguration.DisablePushedAuthorization = true;

                OpenAuthClient = new OidcClient(OpenAuthConfiguration);

                await Login();
            }
            catch(Exception e) { Log.Fatal(e, $"{Prefix}");  }
        }

        private static async Task Login()
        {
            if (IsInitialized) {
                LoginResult result = await OpenAuthClient.LoginAsync(new LoginRequest());



                Log.Warning($"{Prefix} login result -> {result.Error} {result.ErrorDescription}");
            }
            else { Log.Fatal($"{Prefix} open auth not initialized, cannot login"); }
        }
    }
}

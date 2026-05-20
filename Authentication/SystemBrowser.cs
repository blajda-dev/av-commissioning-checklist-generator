using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Browser;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommissioningChecklistGenerator.Authentication
{
    internal class SystemBrowser : IBrowser
    {
        public bool IsRunning { get; private set; } = false;
        public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            BrowserResult result = new BrowserResult
            {
                ResultType = BrowserResultType.UnknownError
            };

            if (!IsRunning)
            {
                HttpListener callbackListener = new HttpListener();

                IsRunning = true;

                Process.Start(new ProcessStartInfo
                {
                    FileName = options.StartUrl,
                    UseShellExecute = true
                });

                HttpListenerContext context = await callbackListener.GetContextAsync();

                result.ResultType = BrowserResultType.Success;
            }
            else 
            { 
                Log.Warning($"[{nameof(SystemBrowser)}] is already running, cannot invoke again until previous instance is closed"); 
                result.Error = "Browser is already running, cannot invoke again until previous instance is closed";
            }

            return result;
        }
    }
}

using DocumentFormat.OpenXml.Wordprocessing;
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Browser;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CommissioningChecklistGenerator.Authentication
{
    internal class SystemBrowser : IBrowser
    {
        private const string Prefix = "[SystemBrowser]";

        private bool IsSuccessResponseCode(int code)
        {
            return (code >= 200 && code < 300);
        }

        private bool IsAuthenticated(NameValueCollection? parameters)
        {
            if (parameters != null)
            {
                return ((parameters["error"] == String.Empty) || (parameters["error"] == null));
            }
            return false;
        }

        private FileStream HandleRequest(Uri? url)
        {
            FileStream html = new FileStream("D:\\PERSONAL\\_ryan_root\\_projects\\_programming\\C#\\CommissioningChecklistGenerator\\Authentication\\failure.html", FileMode.Open, FileAccess.Read);

            if (url != null)
            { 
                NameValueCollection parameters = HttpUtility.ParseQueryString(url?.Query ?? String.Empty);
                
                if (IsAuthenticated(parameters)) {
                    html = new FileStream("D:\\PERSONAL\\_ryan_root\\_projects\\_programming\\C#\\CommissioningChecklistGenerator\\Authentication\\success.html", FileMode.Open, FileAccess.Read);
                }
                else { Log.Error($"{Prefix} {parameters["error"]} {parameters["error_description"]}"); }
            }

            return html;
        }

        public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            BrowserResult result = new BrowserResult { ResultType = BrowserResultType.UnknownError };
            HttpListener? callbackListener = null;

            try
            {
                callbackListener = new HttpListener();
                callbackListener.Prefixes.Add(options.EndUrl);
                callbackListener.Start();
            }
            catch (Exception e)
            {
                Log.Fatal(e, $"{Prefix} creating http listener for server response callback");
            }

            if (callbackListener != null)
            {
                if (callbackListener.IsListening)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = options.StartUrl,
                        UseShellExecute = true
                    });

                    HttpListenerContext context = await callbackListener.GetContextAsync();

                    FileStream reader = HandleRequest(context.Request.Url);

                    context.Response.ContentType = "text/html; charset=utf-8";
                    context.Response.ContentLength64 = reader.Length;

                    await reader.CopyToAsync(context.Response.OutputStream);
                    await context.Response.OutputStream.FlushAsync();
                    context.Response.Close();

                    result.Response = context.Request.Url?.AbsoluteUri ?? String.Empty;
                    result.ResultType = IsAuthenticated(HttpUtility.ParseQueryString(context.Request.Url?.Query ?? String.Empty)) ? BrowserResultType.Success : BrowserResultType.HttpError;
                }
                else
                {
                    Log.Warning($"{Prefix} http callback not listening, could not handle server response");
                    result.Error = "http callback not listening, could not handle server response";
                }

                callbackListener.Stop();
                callbackListener.Close();
            }

            return result;
        }
    }
}

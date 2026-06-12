using CommissioningChecklistGenerator.Settings;
using DocumentFormat.OpenXml.Office2016.Drawing.Command;
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

        private AuthorizationResponse ParseResponse(NameValueCollection? parameters)
        {
            AuthorizationResponse response = AuthorizationResponse.Unknown;

            if (parameters?["code"] != null) { response = AuthorizationResponse.Success; }
            else if (parameters?["error"] != null) { response = AuthorizationResponse.Failure; }
            else if (parameters?["code"] == null && parameters?["error"] == null) { response = AuthorizationResponse.Logout; }
            else { }

            return response;
        }

        private Stream HandleRequest(Uri? url)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            Stream html = assembly.GetManifestResourceStream($"{Constants.ApplicationName}.Authentication.Resources.sso-failure.html") ?? Stream.Null;

            if (url != null)
            { 
                NameValueCollection parameters = HttpUtility.ParseQueryString(url?.Query ?? String.Empty);
                AuthorizationResponse response = ParseResponse(parameters);
                Log.Debug($"{Prefix} authorization response -> {response}");

                switch (response)
                {
                    case AuthorizationResponse.Success:
                        html = assembly.GetManifestResourceStream($"{Constants.ApplicationName}.Authentication.Resources.sso-login-success.html") ?? Stream.Null;
                        break;
                    case AuthorizationResponse.Logout:
                        html = assembly.GetManifestResourceStream($"{Constants.ApplicationName}.Authentication.Resources.sso-logout-success.html") ?? Stream.Null;
                        break;
                    default:
                        break;
                }
            }

            return html;
        }

        public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            BrowserResult result = new BrowserResult { ResultType = BrowserResultType.UnknownError };
            HttpListener? callbackListener = null;
            CancellationTokenSource timeoutToken = new CancellationTokenSource(TimeSpan.FromMinutes(1));

            try
            {
                Log.Debug($"{Prefix} create http listener to listen for responses from {Settings.Configuration.ApplicationConfiguration.AuthenticationURL} -> {options.EndUrl}");
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
                    try { 
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = options.StartUrl,
                            UseShellExecute = true
                        });

                        bool pageLoaded = false;

                        while (callbackListener.IsListening && !pageLoaded)
                        {
                            HttpListenerContext? context = null;
                            try { context = await callbackListener.GetContextAsync().WaitAsync(timeoutToken.Token); }
                            catch (TaskCanceledException e) {
                                if(callbackListener != null && callbackListener.IsListening)
                                {
                                    callbackListener.Stop();
                                    callbackListener.Close();
                                    result.Error = "TimeoutError";
                                    result.ErrorDescription = "timed out waiting for server response callback";
                                }
                                Log.Warning(e, $"{Prefix} timed out waiting for server response callback"); 
                            }
                            finally {
                                if (context != null)
                                {
                                    if (context.Request.Url?.LocalPath == "/loaded")
                                    {
                                        Log.Debug($"{Prefix} response page loaded");
                                        pageLoaded = true;
                                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                                        context.Response.Close();
                                    }
                                    else if (context.Request.Url?.LocalPath == "/")
                                    {
                                        Stream reader = HandleRequest(context.Request.Url);

                                        context.Response.ContentType = "text/html; charset=utf-8";
                                        context.Response.ContentLength64 = reader.Length;

                                        await reader.CopyToAsync(context.Response.OutputStream);
                                        await context.Response.OutputStream.FlushAsync();

                                        result.Response = context.Request.Url?.AbsoluteUri ?? String.Empty;

                                        AuthorizationResponse response = ParseResponse(HttpUtility.ParseQueryString(context.Request.Url?.Query ?? String.Empty));

                                        result.ResultType = response.HasFlag(AuthorizationResponse.Success) || response.HasFlag(AuthorizationResponse.Logout) ? BrowserResultType.Success : BrowserResultType.HttpError;
                                    }
                                }
                            }
                        }
                    }
                    catch(Exception e) { Log.Error(e, $"{Prefix} handling server response callback"); }
                    finally
                    {
                        if (callbackListener != null && callbackListener.IsListening)
                        {
                            callbackListener.Stop();
                            callbackListener.Close();
                        }
                    }
                }
                else
                {
                    Log.Warning($"{Prefix} http callback not listening, could not handle server response");
                    result.Error = "http callback not listening, could not handle server response";
                }
            }

            return result;
        }
    }
}

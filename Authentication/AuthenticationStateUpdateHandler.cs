using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommissioningChecklistGenerator.Authentication
{
    internal class AuthenticationStateUpdateHandler : DelegatingHandler
    {
        private const string Prefix = "[AuthenticationStateUpdateHandler]";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            //send the request
            HttpResponseMessage response = await base.SendAsync(request, token);
            Log.Information($"{Prefix} server responded with {(int)response.StatusCode}: {response.StatusCode}");
            //check the response
            if (response.StatusCode == HttpStatusCode.Unauthorized) { Authentication.OpenAuth.IsAuthenticated = false; }
            else if (response.IsSuccessStatusCode) { Authentication.OpenAuth.IsAuthenticated = true; }

            return response;
        }
    }
}

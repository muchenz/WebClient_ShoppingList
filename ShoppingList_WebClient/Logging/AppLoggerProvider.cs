using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShoppingList_WebClient.Logging
{
    public class AppLoggerProvider : ILoggerProvider
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authenticationStateProvider;

        public AppLoggerProvider(HttpClient httpClient, AuthenticationStateProvider authenticationStateProvider)
        {
            _httpClient = httpClient;
            _authenticationStateProvider = authenticationStateProvider;
        }
        public ILogger CreateLogger(string categoryName)
        {
            return new DbLogger(_httpClient, _authenticationStateProvider);
        }

        public void Dispose()
        {

        }
    }
}

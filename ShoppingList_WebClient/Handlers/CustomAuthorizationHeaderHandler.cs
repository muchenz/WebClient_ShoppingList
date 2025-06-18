using Blazored.LocalStorage;
using ShoppingList_WebClient.Services;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingList_WebClient.Handlers
{
    public class CustomAuthorizationHeaderHandler : DelegatingHandler
    {
        private readonly StateService _stateService;
        private readonly ILocalStorageService _localStorageService;

        public CustomAuthorizationHeaderHandler(StateService userInfoService, ILocalStorageService localStorageService)
        {
            _stateService = userInfoService;
            _localStorageService = localStorageService;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            //var accessToken = await _localStorageService.GetItemAsync<string>("accessToken");

            var accessToken = _stateService.StateInfo.Token;

              Console.WriteLine("________________CustomAuthorizationHeaderHandler  user service " + _stateService.StateInfo.Token);
              Console.WriteLine("________________CustomAuthorizationHeaderHandler  local storage" + accessToken);

            if (accessToken != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
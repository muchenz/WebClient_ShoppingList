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
        private readonly UserInfoService _userInfoService;
        private readonly ILocalStorageService _localStorageService;

        public CustomAuthorizationHeaderHandler(UserInfoService userInfoService, ILocalStorageService localStorageService)
        {
            _userInfoService = userInfoService;
            _localStorageService = localStorageService;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            //var accessToken = await _localStorageService.GetItemAsync<string>("accessToken");

            var accessToken = _userInfoService.Token;

              Console.WriteLine("________________CustomAuthorizationHeaderHandler  user service " + _userInfoService.Token);
              Console.WriteLine("________________CustomAuthorizationHeaderHandler  local storage" + accessToken);

            if (accessToken != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
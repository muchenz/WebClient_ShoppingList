using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using ShoppingList_WebClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShoppingList_WebClient.Data
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly StateService _stateService;

        public ILocalStorageService _localStorageService { get; }
        public UserService _userService { get; set; }

        public CustomAuthenticationStateProvider(ILocalStorageService localStorageService,
            UserService userService, StateService userInfoService)
        {
            //throw new Exception("CustomAuthenticationStateProviderException");
            _localStorageService = localStorageService;
            _userService = userService;
            _stateService = userInfoService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var isAccessToken = await _localStorageService.ContainKeyAsync("accessToken");

            ClaimsIdentity identity;


            if (isAccessToken == false)
            {

                identity = new ClaimsIdentity();
            }
            else
            {
                var accessToken = await _localStorageService.GetItemAsync<string>("accessToken");
                try
                {
                    identity = GetClaimsIdentity(accessToken);
                    _stateService.StateInfo.Token = accessToken;
                }
                catch
                {
                    identity = new ClaimsIdentity();
                    _stateService.StateInfo.Token = null;
                }
            }



            var claimsPrincipal = new ClaimsPrincipal(identity);

            return await Task.FromResult(new AuthenticationState(claimsPrincipal));
        }

        public async void MarkUserAsAuthenticated(string token)
        {
            await _localStorageService.SetItemAsync("accessToken", token);
            //await _localStorageService.SetItemAsync("refreshToken", user.RefreshToken);
            _stateService.StateInfo.Token = token;

            var identity = GetClaimsIdentity(token);

            var claimsPrincipal = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        }

        public void MarkUserAsLoggedOut()
        {
            // _localStorageService.RemoveItemAsync("refreshToken");
            _localStorageService.RemoveItemAsync("accessToken");
            _stateService.StateInfo.Token = null;

            var identity = new ClaimsIdentity();

            var user = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        private ClaimsIdentity GetClaimsIdentity(string token)
        {

            var claim = ParseClaimsFromJwt(token);


            var claimsIdentity = new ClaimsIdentity(claim, "apiauth_type");
            return claimsIdentity;
        }



        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            keyValuePairs.TryGetValue(ClaimTypes.Role, out object roles);

            if (roles != null)
            {
                if (roles.ToString().Trim().StartsWith("["))
                {
                    var parsedRoles = JsonSerializer.Deserialize<string[]>(roles.ToString());

                    foreach (var parsedRole in parsedRoles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, parsedRole));
                    }
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, roles.ToString()));
                }

                keyValuePairs.Remove(ClaimTypes.Role);
            }

            claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString())));

            // claims.Add(new Claim(ClaimTypes.Role, "admin"));

            return claims;
        }
        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}

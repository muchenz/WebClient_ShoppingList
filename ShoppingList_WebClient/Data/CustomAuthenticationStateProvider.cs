using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
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
        private readonly TokenClientService _tokenClientService;
        private readonly NavigationManager _navigationManager;

        public ILocalStorageService _localStorageService { get; }
        public UserService _userService { get; set; }

        public CustomAuthenticationStateProvider(ILocalStorageService localStorageService,
            UserService userService, StateService userInfoService,
            TokenClientService tokenClientService, NavigationManager navigationManager)
        {
            //throw new Exception("CustomAuthenticationStateProviderException");
            _localStorageService = localStorageService;
            _userService = userService;
            _stateService = userInfoService;
            _tokenClientService = tokenClientService;
            _navigationManager = navigationManager;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var isAccessToken = await _localStorageService.ContainKeyAsync("accessToken");
            var isRefreshToken = await _localStorageService.ContainKeyAsync("refreshToken");
            var isExpectedVersion = await _localStorageService.ContainKeyAsync("expectedVersion");
            var isGid = await _localStorageService.ContainKeyAsync("Gid");

            ClaimsIdentity identity;

            if (isAccessToken == false || isRefreshToken == false || isExpectedVersion == false)
            {

                identity = new ClaimsIdentity();
                _stateService.StateInfo.Token = null;
                _stateService.StateInfo.RefreshToken = null;

            }
            else
            {
                _tokenClientService.LogInstance("CustomAuthenticationStateProvider");
                while (_tokenClientService.IsTokenRefresing)
                {
                    await Task.Delay(100);
                }
                try
                {
                    var accessToken = await _localStorageService.GetItemAsync<string>("accessToken");
                    var refreshToken = await _localStorageService.GetItemAsync<string>("refreshToken");
                    var expectedVersion = await _localStorageService.GetItemAsync<int>("expectedVersion");


                    identity = GetClaimsIdentity(accessToken);

                    //var isTokensOK = await _userService.VerifyAcceessRefreshTokens(accessToken, refreshToken);
                    var actualVersion = identity.Claims.First(a => a.Type == ClaimTypes.Version).Value;


                    if (int.Parse(actualVersion) != expectedVersion)
                    {
                        await CleanAndLogout();


                        var nullClaimPrincipal = new ClaimsPrincipal(new ClaimsIdentity());

                        //NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(nullClaimPrincipal)));
                        _navigationManager.NavigateTo("/login", forceLoad: true);

                        return await Task.FromResult(new AuthenticationState(nullClaimPrincipal));
                    }




                    _stateService.StateInfo.UserName = identity.Claims.Where(a => a.Type == ClaimTypes.Name).First().Value;

                    _stateService.StateInfo.Token = accessToken;
                    _stateService.StateInfo.RefreshToken = refreshToken;

                }
                catch (Exception ex)
                {
                    identity = new ClaimsIdentity();
                    _stateService.StateInfo.Token = null;
                    _stateService.StateInfo.RefreshToken = null;

                }
            }



            var claimsPrincipal = new ClaimsPrincipal(identity);

            return await Task.FromResult(new AuthenticationState(claimsPrincipal));
        }

        public async Task MarkUserAsAuthenticatedAsync(string token, string refreshToken)
        {

            await _localStorageService.SetItemAsync("accessToken", token);
            await _localStorageService.SetItemAsync("refreshToken", refreshToken);
            await _localStorageService.SetItemAsync("expectedVersion", 1);
            //await _localStorageService.SetItemAsync("refreshToken", user.RefreshToken);
            _stateService.StateInfo.Token = token;
            _stateService.StateInfo.RefreshToken = refreshToken;
            var identity = GetClaimsIdentity(token);

            var claimsPrincipal = new ClaimsPrincipal(identity);


            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        }

        public async Task MarkUserAsLoggedOut()
        {
            await CleanAndLogout();

            var identity = new ClaimsIdentity();

            var user = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        private async Task CleanAndLogout()
        {
            if (_stateService.StateInfo.Token is not null)
            {
                await _userService.LogOutAsync();
            }
            await _localStorageService.RemoveItemAsync("accessToken");
            await _localStorageService.RemoveItemAsync("refreshToken");
            await _localStorageService.RemoveItemAsync("expectedVersion");
            _stateService.StateInfo.Token = null;
            _stateService.StateInfo.RefreshToken = null;
            _stateService.StateInfo.UserName = null;
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

using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using ShoppingList_WebClient.Models.Response;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingList_WebClient.Services;

public class TokenClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILocalStorageService _localStorage;
    private readonly IConfiguration _configuration;
    private readonly StateService _stateService;
    private readonly string _apiAddress;

    public CancellationTokenSource _cts = new();

    public TokenClientService(IHttpClientFactory httpClientFactory, ILocalStorageService localStorage, IConfiguration configuration, StateService stateService
        )
    {
        _httpClientFactory = httpClientFactory;
        _localStorage = localStorage;
        _configuration = configuration;
        _stateService = stateService;

         _apiAddress = _configuration.GetSection("AppSettings")["ShoppingWebAPIBaseAddress"];
        //_httpClient.BaseAddress = new Uri(_apiAddress);

    }
    private readonly Guid _instanceId = Guid.NewGuid();

    public void LogInstance(string from)
    {
        Console.WriteLine($"######## TokenClientService instance: {_instanceId}");
        Console.WriteLine($"######## ###########################:  {from}");
    }

    public async Task<bool> RefreshTokensAsync()
    {
        var refreshToken = _stateService.StateInfo.RefreshToken;
        var accessToken = _stateService.StateInfo.Token;
        var expectedVersion = int.Parse(ParseClaimsFromJwt(accessToken).First(a => a.Type == ClaimTypes.Version).Value) + 1;
        await _localStorage.SetItemAsync("expectedVersion", expectedVersion);
        return await RefreshTokensAsync(accessToken, refreshToken);
    }
    private async Task<bool> RefreshTokensAsync(string accessToken, string refreshToken)
    {
        var httpClient = _httpClientFactory.CreateClient("api");
        httpClient.BaseAddress = new Uri(_apiAddress);

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "User/GetNewToken");

        requestMessage.Headers.Authorization
                    = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", accessToken);
        requestMessage.Headers.Add("refresh_token", refreshToken);
        requestMessage.Headers.Add("deviceid", await _localStorage.GetItemAsync<string>("gid"));

        HttpResponseMessage response = null;
        response = await httpClient.SendAsync(requestMessage, _cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }


        var tokensResponse = await response.Content.ReadFromJsonAsync<UserNameAndTokensResponse>();

        await SetTokens(tokensResponse.Token, tokensResponse.RefreshToken);
        return true;
    }

    SemaphoreSlim semSlim = new SemaphoreSlim(1);
    public bool IsTokenRefresing { get; set; } = false;
    public async Task CheckAndSetNewTokens()
    {
        if (!IsTokenExpired())
        {
            return;
        }
        try
        {
            await semSlim.WaitAsync();
            if (IsTokenExpired())
            {
                //_stateService.StateInfo.IsTokenRefresing = true;
                IsTokenRefresing = true;
                if (await RefreshTokensAsync() == false)
                {
                    throw new UnauthorizedAccessException("Could not refresh token");
                }
            }
        }
        finally
        {
            LogInstance("CheckAndSetNewTokens");
            //_stateService.StateInfo.IsTokenRefresing = false;
            IsTokenRefresing = false;
            semSlim.Release();

        }
    }


    private async Task SetTokens(string accessToken, string refreshToken)
    {
        await _localStorage.SetItemAsync("accessToken", accessToken);
        await _localStorage.SetItemAsync("refreshToken", refreshToken);

        _stateService.StateInfo.Token = accessToken;
        _stateService.StateInfo.RefreshToken = refreshToken;
    }

    public async Task<(string accessToken, string refreshToken)> GetTokens(string accessToken, string refreshToken)
    {
        await _localStorage.SetItemAsync("accessToken", accessToken);
        await _localStorage.SetItemAsync("refreshToken", refreshToken);

        _stateService.StateInfo.Token = accessToken;
        _stateService.StateInfo.RefreshToken = refreshToken;
        return (accessToken, refreshToken);
    }

    public bool IsTokenExpired()
    {
        var token = _stateService.StateInfo.Token;
        if (token is null)
        {
            return false;
        }
        var claims = ParseClaimsFromJwt(token);

        var claimsList = claims.ToList();

        var expiration = claims.Where(a => a.Type == "exp_datetime").First().Value;
        var timeExpiration = DateTimeOffset.Parse(expiration);
        var isExpiered = timeExpiration < DateTime.UtcNow;
        return isExpiered;
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


        if (keyValuePairs.TryGetValue("exp", out var expValue))
        {

            var exp = long.Parse(((JsonElement)expValue).GetString());
            var expDate = DateTimeOffset.FromUnixTimeSeconds(exp);
            claims.Add(new Claim("exp_datetime", expDate.ToUniversalTime().ToString("o")));
        }

        return claims;
    }
    private byte[] ParseBase64WithoutPadding(string base64)
    {
        //base64 = base64.Replace('-', '+').Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
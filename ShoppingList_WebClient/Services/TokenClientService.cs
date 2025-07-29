using BlazorClient.Models.Response;
using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using ShoppingList_WebClient.Models.Response;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShoppingList_WebClient.Services;

public class TokenClientService
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly StateService _stateService;

    public TokenClientService(ILocalStorageService localStorage, HttpClient httpClient, IConfiguration configuration, StateService stateService)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
        _configuration = configuration;
        _stateService = stateService;
        _httpClient.BaseAddress = new Uri(configuration.GetSection("AppSettings")["ShoppingWebAPIBaseAddress"]);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "BlazorServer");
    }


    public async Task<bool> RefreshTokensAsync()
    {
        var refreshToken = _stateService.StateInfo.RefreshToken;
        var accessToken = _stateService.StateInfo.Token;
         

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "User/GetNewToken");

        requestMessage.Headers.Authorization
                    = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", accessToken);
        requestMessage.Headers.Add("refresh_token", refreshToken);

        HttpResponseMessage response = null;
            response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }


        var tokensResponse = await response.Content.ReadFromJsonAsync<UserNameAndTokensResponse>();

        await SetTokens(tokensResponse.Token, tokensResponse.RefreshToken);
        return true;
    }



    private async Task SetTokens(string accessToken, string refreshToken)
    {
        await _localStorage.SetItemAsync("accessToken", accessToken);
        await _localStorage.SetItemAsync("refreshToken", refreshToken);

        _stateService.StateInfo.Token =accessToken;
        _stateService.StateInfo.RefreshToken = refreshToken;
    }
    public bool IsTokenExpired()
    {
        var token = _stateService.StateInfo.Token;
        var claims = ParseClaimsFromJwt(token);

        var claimsList = claims.ToList();   

        var expiration = claims.Where(a=>a.Type == "exp_datetime").First().Value;

        // UTC czas, więc trzeba porównać z DateTime.UtcNow
        return DateTime.Parse( expiration )< DateTime.UtcNow;
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

            var exp = long.Parse(((JsonElement) expValue).GetString());
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
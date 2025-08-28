using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShoppingList_WebClient.Models;
using ShoppingList_WebClient.Services;
using System;
using System.Data.Common;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

//namespace BlazorClient.Data.example_httpclent;

public class SignalRService
{
    private readonly ILocalStorageService _localStorage;
    private readonly IConfiguration _configuration;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly StateService _stateService;
    private readonly TokenClientService _tokenClientService;
    private HubConnection? _hubConnection;
    private int _userId;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public SignalRService(ILocalStorageService localStorage, 
        IConfiguration configuration, 
        AuthenticationStateProvider authenticationStateProvider,
        IServiceProvider serviceProvider,
        StateService stateService,
        TokenClientService tokenClientService)
    {
        _localStorage = localStorage;
        _configuration = configuration;
        _authenticationStateProvider = authenticationStateProvider;
        _stateService = stateService;
        _tokenClientService = tokenClientService;
    }

    public async Task StartConnectionAsync()
    {
        var auth = await _authenticationStateProvider.GetAuthenticationStateAsync();

        if (auth.User.Identity.IsAuthenticated is not true)
        {
            return;
        }

       
        var accessToken = _stateService.StateInfo.Token;

        _userId = int.Parse(auth.User.Claims.First(a => a.Type == ClaimTypes.NameIdentifier).Value);

        //if (!isAccessToken) return;

        //var accessToken = await _localStorage.GetItemAsync<string>("accessToken");


        _hubConnection = new HubConnectionBuilder().WithUrl(_configuration.GetSection("AppSettings")["SignlRAddress"], (opts) =>
        {
            //opts.Headers.Add("Access_Token", accessToken);

            opts.AccessTokenProvider = async () =>
            {
                await _tokenClientService.CheckAndSetNewTokens();

                return _stateService.StateInfo.Token;
            };

            //opts.Headers.Add("Authorization", $"Bearer {accessToken}"); //for normal authorization in HUB

            //opts.HttpMessageHandlerFactory = (message) =>
            //{
            //    if (message is HttpClientHandler clientHandler)
            //        // bypass SSL certificate
            //        clientHandler.ServerCertificateCustomValidationCallback +=
            //                                                  (sender, certificate, chain, sslPolicyErrors) => { return true; };
            //    return message;
            //};
        }).WithAutomaticReconnect().Build();

        try
        {
            await _hubConnection.StartAsync();
            Console.WriteLine("SignalR connected.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR connection failed: {ex.Message}");
        }

        _stateService.StateInfo.ClientSignalRID= _hubConnection.ConnectionId;
        await CallHuBReadyAsync();

        _hubConnection.Reconnected += (connectionId) =>
        {
            _stateService.StateInfo.ClientSignalRID = connectionId;
            return Task.CompletedTask;
        };

        _hubConnection.Closed += async (exception) =>
        {

        };

    }

    public async Task StopConnectionAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    event Action HuBReady;
    event Func<Task> HuBReadyAsync;

    public async Task CallHuBReadyAsync()
    {
        

        foreach (var handler in HuBReadyAsync?.GetInvocationList() ?? Array.Empty<Delegate>())
        {
            if (handler is Func<Task> callback)
            {
                try
                {
                    await callback();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"error in signar registration: {ex.Message}");
                }

                HuBReadyAsync -= callback;
            }
        }


        foreach (var handler in HuBReady?.GetInvocationList() ?? Array.Empty<Delegate>())
        {
            if (handler is Action callback)
            {
                try
                {
                    callback();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"error in signar registration: {ex.Message}");

                }

                HuBReady -= callback;
            }
        }
    }

    public void JoinToHub(Action func)
    {

        if (_hubConnection == null)
        {
            HuBReady += func;
        }
        else
            func();
    }

    public void JoinToHub(Func<Task> func)
    {

        if (_hubConnection == null)
        {
            HuBReadyAsync += func;
        }
        else
            func();
    }

    public IDisposable RegisterInvitationAreChanedHandlers(Func<Task> func)
    {
        return _hubConnection?.On("InvitationAreChaned_" + _userId, async () =>
        {
            await func();
        });
    }
    //-----------------------------------------------

    public IDisposable RegisterDataAreChangedHandlers(Func<Task> func)
    {
        return _hubConnection?.On("DataAreChanged_" + _userId, async () =>
        {
            await func();
        });
    }

    public IDisposable RegisterListItemAreChangedHandlers(Func<SignaREnvelope, Task> func)
    {
        return _hubConnection?.On("ListItemAreChanged_" + _userId, async (string signaREnvelope) =>
        {
            var envelope = JsonSerializer.Deserialize<SignaREnvelope>(signaREnvelope);

            await func(envelope);
        });
    }
}

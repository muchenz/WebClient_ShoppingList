using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using ShoppingList_WebClient.Data;
using ShoppingList_WebClient.Models;
using ShoppingList_WebClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShoppingList_WebClient.Data
{
    public class SignalRHelper
    {

        static HubConnection _hubConnection;

        public static async Task SignalRInitAsync(IConfiguration configuration, StateInfoService userInfoService)
        {
            _hubConnection = new HubConnectionBuilder().WithUrl(configuration.GetSection("AppSettings")["SignlRAddress"]
                , (opts) =>
            {
                opts.Headers.Add("Access_Token", userInfoService.Token);

                //Console.WriteLine($"tttttttttttoooooooooooooken_________: { userInfoService.Token}");

                //    opts.HttpMessageHandlerFactory = (message) =>
                //    {
                //        if (message is HttpClientHandler clientHandler)
                //            // bypass SSL certificate
                //            clientHandler.ServerCertificateCustomValidationCallback +=
                //                      (sender, certificate, chain, sslPolicyErrors) => { return true; };
                //        return message;
                //    };
            }
            ).WithAutomaticReconnect().Build();

            await _hubConnection.StartAsync();

            //Console.WriteLine($"tttttttttttoooooooooooooken: { userInfoService.Token}");

            userInfoService.HubState.CallHuBReady(_hubConnection);

            userInfoService.ClientSignalRID = _hubConnection.ConnectionId;

            _hubConnection.Reconnected += (id) =>
            {
                userInfoService.ClientSignalRID = id;
                return Task.CompletedTask;
            };

            Console.WriteLine($"SingnalR state: {_hubConnection.State}");
        }




        public static async Task<List<IDisposable>> SignalRInvitationInitAsync(int userId,
            AuthenticationStateProvider authenticationStateProvider,
            UserService userService,
             Action<List<Invitation>> SetInvitation,
             Action<int> SetInvitationCount,
             Action StateHasChanged


            )
        {

            IDisposable dataAreChanged = null;

            try
            {
                var authProvider = await authenticationStateProvider.GetAuthenticationStateAsync();

                dataAreChanged = _hubConnection.On("InvitationAreChanged_" + userId, async () =>
                {


                    var userName = authProvider.User.Identity.Name;

                    var invitationsList = await userService.GetInvitationsListAsync(userName);

                    int count = invitationsList?.Count ?? 0; // == null ? 0 : invitationsList.Count;

                    SetInvitation(invitationsList);
                    SetInvitationCount(count);
                    StateHasChanged();

                });

            }
            catch
            {

            }

            return new List<IDisposable> { dataAreChanged };
        }

        public static List<IDisposable> SignalRShoppingListInitAsync(
            User data,
            Action<User> SetData,
            Action<ListAggregator> SetListAggregatorChoosed,
            Action<List> SetListChoosed,

            AuthenticationStateProvider authenticationStateProvider, UserService userService,
            NavigationManager navigationManager, Action StateHasChanged,
            ShoppingListService shoppingListService,
            ILocalStorageService localStorage

            )
        {

            List<IDisposable> disposables = new List<IDisposable>();

            ////_hubConnection = new HubConnectionBuilder().WithUrl("https://94.251.148.92:5013/chatHub", (opts) =>
            ////{
            ////_hubConnection = new HubConnectionBuilder().WithUrl("https://192.168.8.222:91/chatHub", (opts) =>
            ////{
            //HubConnection _hubConnection = new HubConnectionBuilder().WithUrl(configuration.GetSection("AppSettings")["SignlRAddress"], (opts) =>
            //{
            //    opts.HttpMessageHandlerFactory = (message) =>
            //    {
            //        if (message is HttpClientHandler clientHandler)
            //            // bypass SSL certificate
            //            clientHandler.ServerCertificateCustomValidationCallback +=
            //                       (sender, certificate, chain, sslPolicyErrors) => { return true; };
            //        return message;
            //    };
            //}).WithAutomaticReconnect().Build();


            var dataAreChanged = _hubConnection.On("DataAreChanged_" + data.UserId, async () =>
            {

                try
                {

                    data = await userService.GetUserDataTreeAsync();

                    SetData(data);

                }

                catch (Exception ex)
                {

                    ((CustomAuthenticationStateProvider)authenticationStateProvider).MarkUserAsLoggedOut();

                    navigationManager.NavigateTo("login");

                    return;
                }

                (var listAggregatorChoosed, var listChoosed) = await LoadSaveOrderHelper.LoadChoosedList(data, localStorage);

                SetListAggregatorChoosed(listAggregatorChoosed);
                SetListChoosed(listChoosed);

                await LoadSaveOrderHelper.LoadListAggregatorsOrder(localStorage, data, authenticationStateProvider);

                StateHasChanged();

                return;


            });

            var listAreChaned = _hubConnection.On("ListItemAreChanged_" + data.UserId, async  (string signaREnvelope) =>
            {

                var envelope = JsonSerializer.Deserialize<SignaREnvelope>(signaREnvelope);
                var eventName = envelope.SiglREventName;
                var signaREventSerialized = envelope.SerializedEvent;

                ListItemSignalREvent signaREvent = GetDeserializedSinglaREvent(signaREventSerialized);


                var listAggregationId = signaREvent.ListAggregationId;
                var listItemId = signaREvent.ListItemId;

                switch (eventName)
                {
                    case SiganalREventName.ListItemEdited:
                        {
                            var item = await shoppingListService.GetItem<ListItem>(signaREvent.ListItemId, signaREvent.ListAggregationId);

                            var lists = data.ListAggregators.Where(a => a.ListAggregatorId == listAggregationId).FirstOrDefault();

                            ListItem foundListItem = null;
                            foreach (var listItem in lists.Lists)
                            {
                                foundListItem = listItem.ListItems.FirstOrDefault(a => a.Id == listItemId);
                                if (foundListItem != null) break;
                            }
                            if (foundListItem == null) return;
                            foundListItem.ListItemName = item.ListItemName;
                            foundListItem.State = item.State;
                            StateHasChanged();

                            break;
                        }
                    case SiganalREventName.ListItemAdded:
                        {
                            var addSignaREvent = signaREvent as ListItemAddedSignalREvent;
                            var item = await shoppingListService.GetItem<ListItem>(signaREvent.ListItemId, signaREvent.ListAggregationId);


                            var tempList = data.ListAggregators.Where(a => a.ListAggregatorId == listAggregationId).FirstOrDefault().
                            Lists.Where(a => a.ListId == addSignaREvent.ListId).FirstOrDefault();

                            if (!tempList.ListItems.Any(a => a.ListItemId == item.ListItemId))
                            {
                                tempList.ListItems.Insert(0, item);
                            }

                            StateHasChanged();
                            break;
                        }
                    case SiganalREventName.ListItemDeleted:
                        {

                            var lists = data.ListAggregators.Where(a => a.ListAggregatorId == listAggregationId).FirstOrDefault();

                            ListItem foundListItem = null;
                            List founfList = null;

                            foreach (var listItem in lists.Lists)
                            {
                                founfList = listItem;
                                foundListItem = listItem.ListItems.FirstOrDefault(a => a.Id == listItemId);
                                if (foundListItem != null) break;
                            }
                            if (foundListItem == null) return;

                            founfList.ListItems.Remove(foundListItem);

                            StateHasChanged();

                            break;
                        }
                    default:
                        break;
                }

                ListItemSignalREvent GetDeserializedSinglaREvent(string signaREventSerialized)
                {
                   
                    return signaREvent = eventName switch
                    {
                        SiganalREventName.ListItemAdded => JsonSerializer.Deserialize<ListItemAddedSignalREvent>(signaREventSerialized),
                        SiganalREventName.ListItemEdited => JsonSerializer.Deserialize<ListItemEditedSignalREvent>(signaREventSerialized),
                        SiganalREventName.ListItemDeleted => JsonSerializer.Deserialize<ListItemDeletedSignalREvent>(signaREventSerialized),
                        _ => throw new ArgumentException("Unknown signaREvent")
                    };
                }
            });



            disposables.Add(dataAreChanged);
            disposables.Add(listAreChaned);

            return disposables;
        }
    }
}

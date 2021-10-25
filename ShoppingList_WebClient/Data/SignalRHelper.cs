using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using ShoppingList_WebClient.Data;
using ShoppingList_WebClient.Models;
using ShoppingList_WebClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShoppingList_WebClient.Data
{
    public class SignalRHelper
    {

        static HubConnection _hubConnection;

        public static async Task SignalRInitAsync(IConfiguration configuration, UserInfoService userInfoService)
        {
            //_hubConnection = new HubConnectionBuilder().WithUrl("https://94.251.148.92:5013/chatHub", (opts) =>
            //{
            //_hubConnection = new HubConnectionBuilder().WithUrl("https://192.168.8.222:91/chatHub", (opts) =>
            //{
            _hubConnection = new HubConnectionBuilder().WithUrl(configuration.GetSection("AppSettings")["SignlRAddress"]
            //    ,(opts) =>
            //{
            //    opts.HttpMessageHandlerFactory = (message) =>
            //    {
            //        if (message is HttpClientHandler clientHandler)
            //            // bypass SSL certificate
            //            clientHandler.ServerCertificateCustomValidationCallback +=
            //                      (sender, certificate, chain, sslPolicyErrors) => { return true; };
            //        return message;
            //    };
            //}
            ).WithAutomaticReconnect().Build();

            await _hubConnection.StartAsync();

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

                dataAreChanged = _hubConnection.On("NewInvitation_" + userId, async () =>
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

                    var identity = await authenticationStateProvider.GetAuthenticationStateAsync();

                    var nameUser = identity.User.Identity.Name;

                    data = await userService.GetUserDataTreeObjectsgAsync(nameUser);

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

            var listAreChaned = _hubConnection.On("ListItemAreChanged_" + data.UserId, async (string command, int? id1, int? listAggregationId, int? parentId) =>
            {


                if (command.EndsWith("ListItem"))
                {
                    var item = await shoppingListService.GetItem<ListItem>((int)id1, (int)listAggregationId);

                    if (command == "Edit/Save_ListItem")
                    {
                        var lists = data.ListAggregators.Where(a => a.ListAggregatorId == listAggregationId).FirstOrDefault();

                        ListItem foundListItem = null;
                        foreach (var listItem in lists.Lists)
                        {
                            foundListItem = listItem.ListItems.FirstOrDefault(a => a.Id == id1);
                            if (foundListItem != null) break;
                        }
                        if (foundListItem == null) return;
                        foundListItem.ListItemName = item.ListItemName;
                        foundListItem.State = item.State;
                        StateHasChanged();


                    }
                    else
                         if (command == "Add_ListItem")
                    {


                        data.ListAggregators.Where(a => a.ListAggregatorId == listAggregationId).FirstOrDefault().
                       Lists.Where(a => a.ListId == parentId).FirstOrDefault().ListItems.Add(item);

                        StateHasChanged();
                    }
                    else
                             if (command == "Delete_ListItem")
                    {

                        var lists = data.ListAggregators.Where(a => a.ListAggregatorId == listAggregationId).FirstOrDefault();

                        ListItem foundListItem = null;
                        List founfList = null;

                        foreach (var listItem in lists.Lists)
                        {
                            founfList = listItem;
                            foundListItem = listItem.ListItems.FirstOrDefault(a => a.Id == id1);
                            if (foundListItem != null) break;
                        }
                        if (foundListItem == null) return;

                        founfList.ListItems.Remove(foundListItem);

                        StateHasChanged();

                    }
                }
            });



            disposables.Add(dataAreChanged);
            disposables.Add(listAreChaned);

            return disposables;
        }
    }
}

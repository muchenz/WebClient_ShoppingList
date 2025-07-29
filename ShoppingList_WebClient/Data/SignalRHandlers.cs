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
    public class SignalRHandlers
    {


        public static async Task SignalRInvitationInitAsync(
            UserService userService,
             Action<List<Invitation>> SetInvitation,
             Action<int> SetInvitationCount,
             Action StateHasChanged


            )
        {

            try
            {

                var invitationsList = await userService.GetInvitationsListAsync();

                int count = invitationsList?.Count ?? 0; // == null ? 0 : invitationsList.Count;

                SetInvitation(invitationsList);
                SetInvitationCount(count);
                StateHasChanged();

            }
            catch
            {

            }
        }

        public static async Task SignalRGetUserDataTreeAsync(
            User data,
            Action<User> SetData,
            Action<ListAggregator> SetListAggregatorChoosed,
            Action<List> SetListChoosed,

            AuthenticationStateProvider authenticationStateProvider,
            UserService userService,
            NavigationManager navigationManager,
            Action StateHasChanged,
            ShoppingListService shoppingListService,
            ILocalStorageService localStorage

            )
        {

            try
            {

                data = await userService.GetUserDataTreeAsync();

                SetData(data);

            }

            catch (Exception ex)
            {

                await ((CustomAuthenticationStateProvider)authenticationStateProvider).MarkUserAsLoggedOut();

                navigationManager.NavigateTo("login");

                return;
            }

            (var listAggregatorChoosed, var listChoosed) = await LoadSaveOrderHelper.LoadChoosedList(data, localStorage);

            SetListAggregatorChoosed(listAggregatorChoosed);
            SetListChoosed(listChoosed);

            await LoadSaveOrderHelper.LoadListAggregatorsOrder(localStorage, data, authenticationStateProvider);

            StateHasChanged();



        }

        public static async Task SignalRListItemAreChangedAsync(
                        SignaREnvelope envelope,
                        User data,
                        Action<User> SetData,
                        Action<ListAggregator> SetListAggregatorChoosed,
                        Action<List> SetListChoosed,
                        AuthenticationStateProvider authenticationStateProvider,
                        UserService userService,
                        NavigationManager navigationManager,
                        Action StateHasChanged,
                        ShoppingListService shoppingListService,
                        ILocalStorageService localStorage
                            )
        {

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
        }



    }
}


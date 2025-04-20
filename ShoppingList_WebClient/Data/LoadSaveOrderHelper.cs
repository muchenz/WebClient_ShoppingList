using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using ShoppingList_WebClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingList_WebClient.Data
{
    public class LoadSaveOrderHelper
    {
        public static async Task<(ListAggregator, List)> LoadChoosedList(User data, ILocalStorageService localStorage)
        {
            ListAggregator listAggregatorChoosed = null;
            List listChoosed = null;

            var listAggregatorId = await localStorage.GetItemAsync<int>("ListAggregatorId");
            var listId = await localStorage.GetItemAsync<int>("ListId");


            listAggregatorChoosed = data.ListAggregators.Where(a => a.ListAggregatorId == listAggregatorId).FirstOrDefault();


            if (listAggregatorId <= 0 || listAggregatorChoosed == null)
            {
                listAggregatorChoosed = data.ListAggregators.FirstOrDefault();
            }

            if (listAggregatorChoosed != null)
                listAggregatorChoosed.Lists = listAggregatorChoosed.Lists.OrderByDescending(a => a.Order).ToList();


            listChoosed = listAggregatorChoosed?.Lists.Where(a => a.ListId == listId).FirstOrDefault();



            if (listId < 0 || listChoosed == null)
            {
                listChoosed = listAggregatorChoosed?.Lists.FirstOrDefault();
            }

            if (listChoosed != null)
                listChoosed.ListItems = listChoosed.ListItems.OrderByDescending(a => a.Order).ToList();

            return (listAggregatorChoosed, listChoosed);
        }
        public static async Task LoadListAggregatorsOrder(ILocalStorageService localStorage, User data, AuthenticationStateProvider authenticationStateProvider)
        {
            var user = await authenticationStateProvider.GetAuthenticationStateAsync();

            if (data == null || data.ListAggregators == null || !data.ListAggregators.Any()) return;


            SetEntryOrder2(data.ListAggregators);


            var tempListFromFile = await localStorage.GetItemAsync<List<OrderListAggrItem>>(user.User.Identity.Name);


            // if (tempListFromFile == null) return;  //????????????????? nie


            foreach (var listAggr in data.ListAggregators)
            {

                var itemAggrFromFile = tempListFromFile?.Where(a => a.Id == listAggr.ListAggregatorId).FirstOrDefault();

                if (itemAggrFromFile != null)
                {

                    listAggr.Order = itemAggrFromFile.Order;

                }


                ////////////////////
                SetEntryOrder2(listAggr.Lists);


                foreach (var listList in listAggr.Lists)
                {

                    var itemListFromFile = itemAggrFromFile?.List.Where(a => a.Id == listList.ListId).FirstOrDefault();

                    if (itemListFromFile != null)
                    {

                        listList.Order = itemListFromFile.Order;

                    }


                    SetEntryOrder2(listList.ListItems);


                    foreach (var listItem in listList.ListItems)
                    {

                        var itemItemFromFile = itemListFromFile?.List.Where(a => a.Id == listItem.ListItemId).FirstOrDefault();

                        if (itemItemFromFile != null)
                        {

                            listItem.Order = itemItemFromFile.Order;

                        }

                    }

                    ResolveDoubleOrderValue(listList.ListItems);

                    listList.ListItems = listList.ListItems.OrderByDescending(a => a.Order).ToList();
                }

                ResolveDoubleOrderValue(listAggr.Lists);

                listAggr.Lists = listAggr.Lists.OrderByDescending(a => a.Order).ToList();
                ////////////////////////////


            }


            ResolveDoubleOrderValue(data.ListAggregators);


            data.ListAggregators = data.ListAggregators.OrderByDescending(a => a.Order).ToList();

        }
        static void SetEntryOrder(IEnumerable<IModelItemOrder> list)
        {
            int i = 1;
            foreach (var item in list)
            {
                //if (item.Order == 0)
                //{
                //    item.Order = list.Max(a => a.Order) + 1;

                //}

                item.Order = i++;
            }
        }

        static void SetEntryOrder2(IEnumerable<IModelItemOrder> list)
        {
            int i = 1;
            foreach (var item in list)
            {

                item.Order = item.Id;
            }
        }


            static void ResolveDoubleOrderValue(IEnumerable<IModelItemOrder> list)
        {
            foreach (var item in list)
            {
                foreach (var item2 in list)
                {
                    if (item.Id != item2.Id)
                        if (item.Order == item2.Order)
                            item2.Order = list.Max(a => a.Order) + 1;
                }
            }

        }
        public static async Task<IList<T>> ChangeOrderItemsInList<T>(T dropItem, T dragItem,
       IList<T> choosedList, Func<List<T>, Task> function) where T : IModelItem
        {

            List<T> listOfListItemsToChange = null;
            int tempOrder = dropItem.Order;

            if (dropItem.Order > dragItem.Order)
            {

                listOfListItemsToChange = choosedList.Where(a => a.Order > dragItem.Order && a.Order <= dropItem.Order).ToList();
                listOfListItemsToChange.ForEach(a => a.Order -= 1);

            }
            else if (dropItem.Order < dragItem.Order)
            {

                listOfListItemsToChange = choosedList.Where(a => a.Order < dragItem.Order && a.Order >= dropItem.Order).ToList();
                listOfListItemsToChange.ForEach(a => a.Order += 1);

            }

            if (listOfListItemsToChange != null)
            {
                dragItem.Order = tempOrder;

                listOfListItemsToChange.Add(dragItem);



                switch (dropItem)
                {
                    case ListItem i:
                        //  await function.Invoke(listOfListItemsToChange);
                        break;
                    case List i:
                        //     await function.Invoke(listOfListItemsToChange);
                        break;
                    case ListAggregator i:
                        //    await function.Invoke(listOfListItemsToChange);
                        break;

                }

                choosedList = choosedList.OrderByDescending(a => a.Order).ToList();

            }

            return choosedList;
        }
    }
}

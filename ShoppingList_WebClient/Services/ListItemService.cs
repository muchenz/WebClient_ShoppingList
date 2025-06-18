using Blazored.LocalStorage;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingList_WebClient.Services
{
    public class ShoppingListService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILocalStorageService _localStorage;
        private readonly StateService _stateService;

        public ShoppingListService(HttpClient httpClient, IConfiguration configuration, ILocalStorageService localStorage
            , StateService stateService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _localStorage = localStorage;
            _stateService = stateService;
            _httpClient.BaseAddress = new Uri(_configuration.GetSection("AppSettings")["ShoppingWebAPIBaseAddress"]);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BlazorServer");

        }

        string token;
        private async Task SetRequestBearerAuthorizationHeader(HttpRequestMessage httpRequestMessage)
        {

            token = await _localStorage.GetItemAsync<string>("accessToken");
            //token += "a";
            if (token != null)
            {

                httpRequestMessage.Headers.Authorization
                    = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", token);
                // _httpClient.DefaultRequestHeaders.Add("Authorization", $"bearer  {token}");
            }

            httpRequestMessage.Headers.Add("SignalRId", _stateService.StateInfo.ClientSignalRID);

        }

        void SetRequestAuthorizationLevelHeader(HttpRequestMessage httpRequestMessage, int listAggregationId)
        {

            if (token != null)
            {
                httpRequestMessage.Headers.Add("listAggregationId", listAggregationId.ToString());

                using SHA256 mySHA256 = SHA256.Create();

                var bytes = Encoding.ASCII.GetBytes(token + listAggregationId.ToString());

                var hashBytes = mySHA256.ComputeHash(bytes);

                var hashString = Convert.ToBase64String(hashBytes);

                httpRequestMessage.Headers.Add("Hash", hashString);

            }



        }


        public async Task<T> AddItem<T>(int parentId, T item, int listAggregationId)
        {

            var querry = new QueryBuilder();
            querry.Add("parentId", parentId.ToString());
            querry.Add("listAggregationId", listAggregationId.ToString());
            // querry.Add("password", password);

            string serializedUser = JsonConvert.SerializeObject(item);

            string typeName = typeof(T).ToString().Split(".").LastOrDefault();

            // var requestMessage = new HttpRequestMessage(HttpMethod.Post, "ListItem/AddItemListItemToList"+querry.ToString());
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, typeName + "/Add" + typeName + querry.ToString());


            requestMessage.Content = new StringContent(serializedUser);

            requestMessage.Content.Headers.ContentType
              = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");


            await SetRequestBearerAuthorizationHeader(requestMessage);
            SetRequestAuthorizationLevelHeader(requestMessage, listAggregationId);

            var response = await _httpClient.SendAsync(requestMessage);

            var responseStatusCode = response.StatusCode;

            var responseBody = await response.Content.ReadAsStringAsync();

            var returnedUser = JsonConvert.DeserializeObject<T>(responseBody);


            return await Task.FromResult(returnedUser);
        }

        public async Task<int> Delete<T>(int ItemId, int listAggregationId) where T : new()
        {

            var querry = new QueryBuilder();
            querry.Add("ItemId", ItemId.ToString());
            querry.Add("listAggregationId", listAggregationId.ToString());
            // querry.Add("password", password);

            string typeName = typeof(T).ToString().Split(".").LastOrDefault();

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, typeName + "/Delete" + typeName + querry.ToString());

            //var requestMessage = new HttpRequestMessage(HttpMethod.Post, "ListItem/DeleteListItem" + querry.ToString());                                  


            await SetRequestBearerAuthorizationHeader(requestMessage);
            SetRequestAuthorizationLevelHeader(requestMessage, listAggregationId);


            var response = await _httpClient.SendAsync(requestMessage);

            var responseStatusCode = response.StatusCode;

            var responseBody = await response.Content.ReadAsStringAsync();

            var returnedUser = JsonConvert.DeserializeObject<int>(responseBody);

            return await Task.FromResult(returnedUser);
        }

        public async Task<T> EditItem<T>(T item, int listAggregationId)
        {
            var querry = new QueryBuilder();
            querry.Add("listAggregationId", listAggregationId.ToString());
            querry.Add("lvl", 2.ToString());

            string serializedUser = JsonConvert.SerializeObject(item);

            string typeName = typeof(T).ToString().Split(".").LastOrDefault();

            // var requestMessage = new HttpRequestMessage(HttpMethod.Post, "ListItem/AddItemListItemToList"+querry.ToString());
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, typeName + "/Edit" + typeName + querry.ToString());


            requestMessage.Content = new StringContent(serializedUser);

            requestMessage.Content.Headers.ContentType
              = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");


            await SetRequestBearerAuthorizationHeader(requestMessage);
            SetRequestAuthorizationLevelHeader(requestMessage, listAggregationId);


            var response = await _httpClient.SendAsync(requestMessage);

            var responseStatusCode = response.StatusCode;

            var responseBody = await response.Content.ReadAsStringAsync();

            var returnedUser = JsonConvert.DeserializeObject<T>(responseBody);


            return await Task.FromResult(returnedUser);
        }


        public async Task ChangeOrder<T>(IEnumerable<T> items)
        {


            string serializedUser = JsonConvert.SerializeObject(items);

            string typeName = typeof(T).ToString().Split(".").LastOrDefault();

            // var requestMessage = new HttpRequestMessage(HttpMethod.Post, "ListItem/AddItemListItemToList"+querry.ToString());
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, typeName + "/ChangeOrder" + typeName);


            requestMessage.Content = new StringContent(serializedUser);

            requestMessage.Content.Headers.ContentType
              = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");


            await SetRequestBearerAuthorizationHeader(requestMessage);


            var response = await _httpClient.SendAsync(requestMessage);

            var responseStatusCode = response.StatusCode;

            var responseBody = await response.Content.ReadAsStringAsync();

            //var returnedUser = JsonConvert.DeserializeObject<T>(responseBody);


            //return await Task.FromResult(returnedUser);
        }


        public async Task<T> SaveItemProperty<T>(T item, string propertyName, int listAggregationId)
        {
            var querry = new QueryBuilder();
            querry.Add("listAggregationId", listAggregationId.ToString());
            querry.Add("propertyName", propertyName);

            string serializedUser = JsonConvert.SerializeObject(item);

            string typeName = typeof(T).ToString().Split(".").LastOrDefault();

            // var requestMessage = new HttpRequestMessage(HttpMethod.Post, "ListItem/AddItemListItemToList"+querry.ToString());
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, typeName + "/SaveProperty" + querry);


            requestMessage.Content = new StringContent(serializedUser);

            requestMessage.Content.Headers.ContentType
              = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");


            await SetRequestBearerAuthorizationHeader(requestMessage);
            SetRequestAuthorizationLevelHeader(requestMessage, listAggregationId);


            var response = await _httpClient.SendAsync(requestMessage);

            var responseStatusCode = response.StatusCode;

            var responseBody = await response.Content.ReadAsStringAsync();

            var returnedUser = JsonConvert.DeserializeObject<T>(responseBody);


            return await Task.FromResult(returnedUser);
        }

        public async Task<T> GetItem<T>(int itemId, int listAggregationId)
        {
            var querry = new QueryBuilder();
            querry.Add("listAggregationId", listAggregationId.ToString());
            querry.Add("itemId", itemId.ToString());

            //string serializedUser = JsonConvert.SerializeObject(item);

            string typeName = typeof(T).ToString().Split('.').LastOrDefault();

            // var requestMessage = new HttpRequestMessage(HttpMethod.Post, "ListItem/AddItemListItemToList"+querry.ToString());
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, typeName + "/GetItem" + typeName + querry);


            requestMessage.Content = new StringContent("");

            //requestMessage.Content.Headers.ContentType
            //  = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");


            await SetRequestBearerAuthorizationHeader(requestMessage);
            SetRequestAuthorizationLevelHeader(requestMessage, listAggregationId);


            var response = await _httpClient.SendAsync(requestMessage);

            var responseStatusCode = response.StatusCode;

            var responseBody = await response.Content.ReadAsStringAsync();

            var returnedItem = JsonConvert.DeserializeObject<T>(responseBody);


            return await Task.FromResult(returnedItem);
        }
    }
}


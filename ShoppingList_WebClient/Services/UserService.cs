using Blazored.LocalStorage;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ShoppingList_WebClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http.Json;

namespace ShoppingList_WebClient.Services
{
    public class UserService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILocalStorageService _localStorage;
        private readonly StateInfoService _userInfoService;

        public UserService(HttpClient httpClient, IConfiguration configuration, ILocalStorageService localStorage
            , StateInfoService userInfoService)
        {
            _httpClient = httpClient;
            _configuration = configuration;// new ConfigMock();
            _localStorage = localStorage;
            _userInfoService = userInfoService;
            var apiAddress = _configuration.GetSection("AppSettings")["ShoppingWebAPIBaseAddress"];

            _httpClient.BaseAddress = new Uri(apiAddress);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BlazorServer");

        }

        string token;
        private async Task SetRequestBearerAuthorizationHeader(HttpRequestMessage httpRequestMessage)
        {
            token = await _localStorage.GetItemAsync<string>("accessToken");

            if (token != null)
            {

                httpRequestMessage.Headers.Authorization
                    = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", token);
                // _httpClient.DefaultRequestHeaders.Add("Authorization", $"bearer  {token}");
            }


            httpRequestMessage.Headers.Add("SignalRId", _userInfoService.ClientSignalRID);

            await Task.CompletedTask;
        }


        public async Task<string> RegisterAsync(RegistrationModel model)
        {

            var querry = new QueryBuilder();
            querry.Add("userName", model.UserName);
            querry.Add("password", model.Password);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "User/Register" + querry.ToString());

            // await SetRequestBearerAuthorizationHeader(requestMessage);

            requestMessage.Content = new StringContent("");

            requestMessage.Content.Headers.ContentType
                = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");


            var response = await _httpClient.SendAsync(requestMessage);

            var token = await response.Content.ReadAsStringAsync();

            var message = JsonConvert.DeserializeObject<MessageAndStatus>(token);


            return await Task.FromResult(message.Message);

        }


        public async Task<MessageAndStatus> LoginAsync(string userName, string password)
        {

            var querry = new QueryBuilder();
            querry.Add("userName", userName);
            querry.Add("password", password);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "User/Login" + querry.ToString());

            // await SetRequestBearerAuthorizationHeader(requestMessage);

            requestMessage.Content = new StringContent("");

            requestMessage.Content.Headers.ContentType
                = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            MessageAndStatus message =null;

            try
            {

                var response = await _httpClient.SendAsync(requestMessage);

                var token = await response.Content.ReadAsStringAsync();

                 message = JsonConvert.DeserializeObject<MessageAndStatus>(token);
            }
            catch (Exception ex) { 
            
            
            }


            return await Task.FromResult(message);

        }

        public async Task<string> GetUserDataTreeStringAsync(string userName)
        {

            var querry = new QueryBuilder();
            querry.Add("userName", userName);
            // querry.Add("password", password);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "User/GetUserDataTree" + querry.ToString());


            await SetRequestBearerAuthorizationHeader(requestMessage);
            Console.WriteLine("!_______header ");

            Console.WriteLine("!_______header " + requestMessage.Headers.Authorization.ToString());

            var response = await _httpClient.SendAsync(requestMessage);
             // var response =  await  _httpClient.PostAsync("User/GetUserDataTree" + querry.ToString(),
             //     new StringContent("", Encoding.UTF8, "application/json"));
          //var response = await _httpClient.PostAsJsonAsync<string>("User/GetUserDataTree" + querry.ToString(), "");

            var data = await response.Content.ReadAsStringAsync();

            var message = JsonConvert.DeserializeObject<MessageAndStatus>(data);


            return await Task.FromResult(message.Message);
        }

        public async Task<List<ListAggregationForPermission>> GetListAggregationForPermissionAsync(string userName)
        {

            var querry = new QueryBuilder();
            querry.Add("userName", userName);
            // querry.Add("password", password);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "Permissions/GetListAggregationForPermission" + querry.ToString());


            await SetRequestBearerAuthorizationHeader(requestMessage);


            var response = await _httpClient.SendAsync(requestMessage);

            var data = await response.Content.ReadAsStringAsync();

            var message = JsonConvert.DeserializeObject<MessageAndStatus>(data);


            //  var dataObjects = JsonConvert.DeserializeObject<List<ListAggregationForPermissionTransferClass>>(data);
            var dataObjects = JsonConvert.DeserializeObject<List<ListAggregationForPermission>>(message.Message);


            return await Task.FromResult(dataObjects);
        }




        public async Task<User> GetUserDataTreeObjectsgAsync(string userName)
        {

            var dataString = await GetUserDataTreeStringAsync(userName);



            var dataObjects = JsonConvert.DeserializeObject<User>(dataString);

            return dataObjects;
        }


        public async Task<string> AddUserPermission(UserPermissionToListAggregation userPermissionToList, int listAggregationId)
        {
            return await UniversalUserPermission(userPermissionToList, listAggregationId, "AddUserPermission");
        }

        public async Task<string> ChangeUserPermission(UserPermissionToListAggregation userPermissionToList, int listAggregationId)
        {

            return await UniversalUserPermission(userPermissionToList, listAggregationId, "ChangeUserPermission");

        }


        public async Task<string> DeleteUserPermission(UserPermissionToListAggregation userPermissionToList, int listAggregationId)
        {
            return await UniversalUserPermission(userPermissionToList, listAggregationId, "DeleteUserPermission");
        }

        public async Task<string> InviteUserPermission(UserPermissionToListAggregation userPermissionToList, int listAggregationId)
        {
            return await UniversalUserPermission(userPermissionToList, listAggregationId, "InviteUserPermission");
        }

        private async Task<string> UniversalUserPermission(UserPermissionToListAggregation userPermissionToList, int listAggregationId,
            string actionName)
        {
            var querry = new QueryBuilder();

            querry.Add("listAggregationId", listAggregationId.ToString());
            // querry.Add("password", password);

            string serializedUser = JsonConvert.SerializeObject(userPermissionToList);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "Permissions/" + actionName + querry.ToString());


            requestMessage.Content = new StringContent(serializedUser);

            requestMessage.Content.Headers.ContentType
              = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");


            await SetRequestBearerAuthorizationHeader(requestMessage);
            SetRequestAuthorizationLevelHeader(requestMessage, listAggregationId);

            var response = await _httpClient.SendAsync(requestMessage);

            var responseStatusCode = response.StatusCode;

            var responseBody = await response.Content.ReadAsStringAsync();

            var message = JsonConvert.DeserializeObject<MessageAndStatus>(responseBody);

            return await Task.FromResult(message.Message);
        }


        public async Task<List<Invitation>> GetInvitationsListAsync(string userName)
        {
            var querry = new QueryBuilder();
            querry.Add("userName", userName);
            // querry.Add("password", password);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "Invitation/GetInvitationsList" + querry.ToString());


            await SetRequestBearerAuthorizationHeader(requestMessage);


            var response = await _httpClient.SendAsync(requestMessage);

            var data = await response.Content.ReadAsStringAsync();

            var message = JsonConvert.DeserializeObject<MessageAndStatus>(data);

            var dataObjects = JsonConvert.DeserializeObject<List<Invitation>>(message.Message);


            return await Task.FromResult(dataObjects);
        }



        public async Task<string> AcceptInvitationAsync(Invitation invitation)
        {
            return await UniversalInvitationAction(invitation, "AcceptInvitation");

        }
        public async Task<string> RejectInvitaionAsync(Invitation invitation)
        {

            return await UniversalInvitationAction(invitation, "RejectInvitaion");

        }

        async Task<string> UniversalInvitationAction(Invitation invitation, string actionName)
        {
            string serialized = JsonConvert.SerializeObject(invitation);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "Invitation/" + actionName);


            requestMessage.Content = new StringContent(serialized);

            requestMessage.Content.Headers.ContentType
              = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");


            await SetRequestBearerAuthorizationHeader(requestMessage);

            var response = await _httpClient.SendAsync(requestMessage);

            var responseStatusCode = response.StatusCode;

            var responseBody = await response.Content.ReadAsStringAsync();

            var message = JsonConvert.DeserializeObject<MessageAndStatus>(responseBody);


            return await Task.FromResult(message.Message);
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
    }
}

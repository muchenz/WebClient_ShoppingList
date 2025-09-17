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
using ShoppingList_WebClient.Models.Requests;
using ShoppingList_WebClient.Models.Response;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace ShoppingList_WebClient.Services
{
    public class UserService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILocalStorageService _localStorage;
        private readonly StateService _stateService;
        private readonly TokenHttpClient _tokenHttpClient;

        public UserService(HttpClient httpClient, IConfiguration configuration, ILocalStorageService localStorage
            , StateService stateService,  TokenHttpClient tokenHttpClient)
        {
            _httpClient = httpClient;
            _configuration = configuration;// new ConfigMock();
            _localStorage = localStorage;
            _stateService = stateService;
            _tokenHttpClient = tokenHttpClient;
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


            httpRequestMessage.Headers.Add("SignalRId", _stateService.StateInfo.ClientSignalRID);

            await Task.CompletedTask;
        }


        public async Task<MessageAndStatusAndData<UserNameAndTokensResponse>> RegisterAsync(RegistrationModel model)
        {
            var gid = await _localStorage.GetItemAsync<string>("gid");
            var loginRequest = new RegistrationRequest
            {
                UserName = model.UserName,
                Password = model.Password,
                DeviceId = gid,
            };

            var json = JsonConvert.SerializeObject(loginRequest);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "User/Register")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                var tokens = await response.Content.ReadFromJsonAsync<UserNameAndTokensResponse>();

                return MessageAndStatusAndData<UserNameAndTokensResponse>.Ok(tokens);
            }

            return response switch
            {
                { StatusCode: System.Net.HttpStatusCode.Conflict } =>
                     MessageAndStatusAndData<UserNameAndTokensResponse>.Fail("User exists."),
                _ =>
                    MessageAndStatusAndData<UserNameAndTokensResponse>.Fail("Server error."),
            };

        }


        public async Task<MessageAndStatusAndData<UserNameAndTokensResponse>> LoginAsync(string userName, string password)
        {
            var gid = await _localStorage.GetItemAsync<string>("gid");
            var loginRequest = new LoginRequest
            {
                UserName = userName,
                Password = password,
                DeviceId = gid
            };

            var json = JsonConvert.SerializeObject(loginRequest);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "User/Login")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            requestMessage.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            var response = await _httpClient.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                return MessageAndStatusAndData<UserNameAndTokensResponse>.Fail("Invalid username or password.");
            }

            var content = await response.Content.ReadAsStringAsync();

            var tokenAndUsername = JsonConvert.DeserializeObject<UserNameAndTokensResponse>(content);


            return MessageAndStatusAndData<UserNameAndTokensResponse>.Ok(tokenAndUsername);

        }

        public async Task LogOutAsync()
        {

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "User/LogOut");

            //await SetRequestBearerAuthorizationHeader(requestMessage);

            var response = await _tokenHttpClient.SendAsync(requestMessage);
        }

        public async Task<List<ListAggregationWithUsersPermission>> GetListAggrWithUsersPermAsync()
        {


            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "Permissions/ListAggregationWithUsersPermission");


            //await SetRequestBearerAuthorizationHeader(requestMessage);


            var response = await _tokenHttpClient.SendAsync(requestMessage);

            var data = await response.Content.ReadAsStringAsync();

            var lists = JsonConvert.DeserializeObject<List<ListAggregationWithUsersPermission>>(data);


            return lists;
        }




        public async Task<User> GetUserDataTreeAsync()
        {


            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "User/UserDataTree");


            //await SetRequestBearerAuthorizationHeader(requestMessage);
            //Console.WriteLine("!_______header ");

            //Console.WriteLine("!_______header " + requestMessage.Headers.Authorization.ToString());
            HttpResponseMessage response = null;
            try
            {
                 response = await _tokenHttpClient.SendAsync(requestMessage);
            }
            catch (Exception ex)
            {
                var m = ex.Message;
                throw;
            }
            // var response =  await  _httpClient.PostAsync("User/GetUserDataTree" + querry.ToString(),
            //     new StringContent("", Encoding.UTF8, "application/json"));
            //var response = await _httpClient.PostAsJsonAsync<string>("User/GetUserDataTree" + querry.ToString(), "");

            var data = await response.Content.ReadAsStringAsync();

            var user = JsonConvert.DeserializeObject<User>(data);


            return user;
        }

        public async Task<MessageAndStatusAndData<GetAccessTokenResponse>> GetAccessTokenFromIdAsync(string id)
        {
            var querry = new QueryBuilder();

            querry.Add("id", id);
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "User/GetAccessToken" + querry.ToString());


            var response = await _httpClient.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                return MessageAndStatusAndData<GetAccessTokenResponse>.Fail("Try log again.");
            }

            var data = await response.Content.ReadAsStringAsync();

            var token = JsonConvert.DeserializeObject<GetAccessTokenResponse>(data);


            return await Task.FromResult(MessageAndStatusAndData<GetAccessTokenResponse>.Ok(token));
        }
        public async Task<MessageAndStatus> AddUserPermission(InviteUserRequest userPermissionToList, int listAggregationId)
        {
            return await UniversalUserPermission(userPermissionToList, listAggregationId, "AddUserPermission");
        }

        public async Task<MessageAndStatus> ChangeUserPermission(UserPermissionToListAggregation userPermissionToList, int listAggregationId)
        {

            return await UniversalUserPermission(userPermissionToList, listAggregationId, "ChangeUserPermission");

        }


        public async Task<MessageAndStatus> DeleteUserPermission(UserPermissionToListAggregation userPermissionToList, int listAggregationId)
        {
            return await UniversalUserPermission(userPermissionToList, listAggregationId, "DeleteUserPermission");
        }

        public async Task<MessageAndStatus> InviteUserPermission(InviteUserRequest userPermissionToList, int listAggregationId)
        {
            return await UniversalUserPermission(userPermissionToList, listAggregationId, "InviteUserPermission");
        }

        private async Task<MessageAndStatus> UniversalUserPermission(object userPermissionToList, int listAggregationId,
            string actionName)
        {
            var querry = new QueryBuilder();

            querry.Add("listAggregationId", listAggregationId.ToString());

            //var httpMethod = actionName == "DeleteUserPermission" ? HttpMethod.Delete : HttpMethod.Post;

            string serializedUser = JsonConvert.SerializeObject(userPermissionToList);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "Permissions/" + actionName + querry.ToString());


            requestMessage.Content = new StringContent(serializedUser);

            requestMessage.Content.Headers.ContentType
              = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");


            //await SetRequestBearerAuthorizationHeader(requestMessage);
            //SetRequestAuthorizationLevelHeader(requestMessage, listAggregationId);

            var response = await _tokenHttpClient.SendAsync(requestMessage,listAggregationId);

            var responseStatusCode = response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();

                var problem = JsonConvert.DeserializeObject<ProblemDetails>(responseBody);

                return MessageAndStatus.Fail(problem.Title);
            }

            var message = actionName switch
            {
                "AddUserPermission" => "User was added.",
                "ChangeUserPermission" => "Permission has changed.",
                "InviteUserPermission" => "Ivitation was added.",
                "DeleteUserPermission" => "User permission was deleted.",
                _ => throw new ArgumentException("Bad action name.")
            };

            return MessageAndStatus.Ok(message);
        }


        public async Task<List<Invitation>> GetInvitationsListAsync()
        {

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "Invitation/InvitationsList");


            //await SetRequestBearerAuthorizationHeader(requestMessage);


            var response = await _tokenHttpClient.SendAsync(requestMessage);

            var data = await response.Content.ReadAsStringAsync();

            var invitations = JsonConvert.DeserializeObject<List<Invitation>>(data);

            return await Task.FromResult(invitations);
        }



        public async Task<MessageAndStatus> AcceptInvitationAsync(Invitation invitation)
        {
            return await UniversalInvitationAction(invitation, "AcceptInvitation");

        }
        public async Task<MessageAndStatus> RejectInvitaionAsync(Invitation invitation)
        {

            return await UniversalInvitationAction(invitation, "RejectInvitaion");

        }

        async Task<MessageAndStatus> UniversalInvitationAction(Invitation invitation, string actionName)
        {
            string serialized = JsonConvert.SerializeObject(invitation);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "Invitation/" + actionName);


            requestMessage.Content = new StringContent(serialized);

            requestMessage.Content.Headers.ContentType
              = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");


           // await SetRequestBearerAuthorizationHeader(requestMessage);

            var response = await _tokenHttpClient.SendAsync(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                return MessageAndStatus.Ok();
            }
            return MessageAndStatus.Fail();
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

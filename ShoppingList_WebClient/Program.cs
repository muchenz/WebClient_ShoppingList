using Blazored.LocalStorage;
using Blazored.Modal;
using Blazored.Toast;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShoppingList_WebClient.Data;
using ShoppingList_WebClient.Handlers;
using ShoppingList_WebClient.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingList_WebClient
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");


            builder.Services.AddOptions();
            builder.Services.AddAuthorizationCore();


            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
            //builder.Services.AddScoped<CustomAuthorizationHeaderHandler>(); 
            builder.Services.AddSingleton(new UserInfoService());


            // services.AddHttpClient<UserService>();
            // services.AddHttpClient<ShoppingListService>();

            builder.Services.AddHttpClient<UserService>(client => {
                // code to configure headers etc..
            }).ConfigurePrimaryHttpMessageHandler(() => {
                var handler = new HttpClientHandler();

                // handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

                return handler;
            });//.AddHttpMessageHandler<CustomAuthorizationHeaderHandler>();

            builder.Services.AddHttpClient<ShoppingListService>(client => {
                // code to configure headers etc..
            }).ConfigurePrimaryHttpMessageHandler(() => {
                var handler = new HttpClientHandler();

                //  handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

                return handler;
            });//.AddHttpMessageHandler<CustomAuthorizationHeaderHandler>(); 


            var address = new Uri( new ConfigMock().GetSection("")["ShoppingWebAPIBaseAddress"]);

            builder.Services.AddHttpClient("log", client => {
                // code to configure headers etc..
                client.BaseAddress = address;

            }).ConfigurePrimaryHttpMessageHandler(() => {
                var handler = new HttpClientHandler();

                // handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

                return handler;
            });//.AddHttpMessageHandler<CustomAuthorizationHeaderHandler>();


            builder.Services.AddScoped<BrowserService>();

            builder.Services.AddBlazoredModal();
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddBlazoredToast();




            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = address });

            await builder.Build().RunAsync();
        }
    }
}

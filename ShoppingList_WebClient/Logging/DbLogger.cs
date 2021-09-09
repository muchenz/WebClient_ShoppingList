using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using ShoppingList_WebClient.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShoppingList_WebClient.Logging
{
    internal class DbLogger : ILogger
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authenticationStateProvider;

        public DbLogger(HttpClient httpClient, AuthenticationStateProvider authenticationStateProvider)
        {
            _httpClient = httpClient;
            _authenticationStateProvider = authenticationStateProvider;
        }
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var log = new Log();
            var rootLog = log;
            //if (LogLevel.Information == logLevel) return;
            // var rootException = exception;
            // var tempException = exception;
            var isInnerException = false;

            do
            {
                log.LogLevel = logLevel.ToString();
                log.StackTrace = exception?.StackTrace;
                log.ExceptionMessage = exception?.Message;
                log.CreatedDate = DateTime.Now.ToString();
                log.Source = "Client";
                log.Message = state.ToString();


                isInnerException = exception?.InnerException != null;

                if (isInnerException)
                {
                    var tempLogEntity = new Log();
                    log.Inner = tempLogEntity;
                    log = tempLogEntity;
                    exception = exception.InnerException;
                }


            } while (isInnerException);


            var serLog = System.Text.Json.JsonSerializer.Serialize(rootLog);

            Task.Run(async () =>
            {

                var res = await _httpClient.PostAsync("logs/logger",
                    new StringContent(serLog, System.Text.Encoding.UTF8, "application/json"));
                System.Diagnostics.Trace.WriteLine(res.StatusCode);
                System.Diagnostics.Trace.WriteLine(await res.Content.ReadAsStringAsync());




            });
        }
    }
}
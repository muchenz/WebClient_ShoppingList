//using Microsoft.AspNetCore.SignalR.Client;
//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace ShoppingList_WebClient.Services
//{
//    public class StateInfoService
//    {
//        private string _token;

//        public event Action<string> NewTokenLoadedEvent;
//        public string Token
//        {
//            get => _token; set
//            {
//                if (_token != value)
//                {
//                    _token = value;
//                    NewTokenLoadedEvent?.Invoke(value);
//                }
//            }
//        }
//        public string ClientSignalRID { get; set; }

//        public HubState HubState { get; set; } = new HubState();
//    }

//    public class HubState
//    {

//        event Action<HubConnection> HuBReadyEvent;
//        event Func<HubConnection, Task> HuBReadyEventAsync;
//        public HubConnection Hub { get; private set; }
//        public void CallHuBReady(HubConnection _con)
//        {
//            Hub = _con;
//            HuBReadyEvent?.Invoke(_con);
//            HuBReadyEventAsync?.Invoke(_con);
//        }

//        public void JoinToHub(Action<HubConnection> func)
//        {

//            if (Hub == null)
//            {
//                HuBReadyEvent += func;
//            }
//            else
//                func(Hub);
//        }

//        public void JoinToHub(Func<HubConnection, Task> func)
//        {

//            if (Hub == null)
//            {
//                HuBReadyEventAsync += func;
//            }
//            else
//                func(Hub);
//        }

//    }
//}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingList_WebClient.Services
{
    public class ConfigMock : IConfiguration
    {
        IConfigurationSection _section; 
        public ConfigMock()
        {
            System.Console.WriteLine("!! ctor ConfigMock");
            _section = new SectionMock("https://webapi.mcfly.ml/api/", "https://signalr.mcfly.ml/chatHub"); 
        }
        public string this[string key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            throw new NotImplementedException();
        }

        public IChangeToken GetReloadToken()
        {
            throw new NotImplementedException();
        }

        public IConfigurationSection GetSection(string key)
        {
            System.Console.WriteLine("!!GetSection");

            return _section;
              //  return new SectionMock("https://localhost:5001/api/", "https://signalr.mcfly.ml/chatHub");


        }

        public class SectionMock : IConfigurationSection
        {
            private readonly string _conf;
            private readonly string _conf2;

            public SectionMock(string conf, string conf2)
            {
                System.Console.WriteLine("!! ctor SectionMock");

                _conf = conf;
                _conf2 = conf2;
            }


            public string this[string key] { get 
                    {
                    System.Console.WriteLine("!! this[string key]");
                    System.Console.WriteLine("key  "+key);

                    var res =  key switch
                    {
                        "ShoppingWebAPIBaseAddress" => _conf,
                        _ => _conf2
                    };

                    System.Console.WriteLine("res  " + res);

                    return res;

                }
                
                
                set => throw new NotImplementedException(); }

            public string Key => throw new NotImplementedException();

            public string Path => throw new NotImplementedException();

            public string Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public IEnumerable<IConfigurationSection> GetChildren()
            {
                throw new NotImplementedException();
            }

            public IChangeToken GetReloadToken()
            {
                throw new NotImplementedException();
            }

            public IConfigurationSection GetSection(string key)
            {
                throw new NotImplementedException();
            }
        }
    }



}

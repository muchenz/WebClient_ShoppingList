using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ShoppingList_WebClient.Data
{
    public static class NavigationManagerExtension
    {
        public static string GetParameter(this NavigationManager navManager, string param)
        {
            var uri = "";
            if (navManager.Uri.EndsWith("#_=_"))
                uri = navManager.Uri.Replace("#_=_", "");
            else
                uri = navManager.Uri;

            var index = uri.IndexOf("?");

            if (index == -1) return null;

            uri = uri.Substring(index + 1);

            var arrayOfParams = uri.Split("&");

            var dic = new Dictionary<string, string>();

            foreach (var item in arrayOfParams)
            {
                var p = item.Split("=");

                dic.Add(HttpUtility.UrlDecode(p[0]), HttpUtility.UrlDecode(p[1]));
            }

            if (!dic.ContainsKey(param))
                return null;

            return dic[param];
        }
    }
}

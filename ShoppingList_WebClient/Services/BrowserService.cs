﻿using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace ShoppingList_WebClient.Services
{
    public class BrowserService
    {
        private readonly IJSRuntime _js;

        public BrowserService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task<BrowserDimension> GetDimensions()
        {
            return await _js.InvokeAsync<BrowserDimension>("getDimensions");
        }


        public static event Func<int, Task> OnResize;

        [JSInvokable]
        public static async Task OnBrowserResize(int windowWidth)
        {
            await OnResize?.Invoke(windowWidth);
        }

    }

    public class BrowserDimension
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
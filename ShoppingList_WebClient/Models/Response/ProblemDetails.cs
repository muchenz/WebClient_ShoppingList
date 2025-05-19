using System.Collections.Generic;

namespace ShoppingList_WebClient.Models.Response;


public class ProblemDetails
    {
        public string? Type { get; set; } = "about:blank";
        public string? Title { get; set; }
        public int? Status { get; set; }
        public string? Detail { get; set; }
        public string? Instance { get; set; }

        public Dictionary<string, string[]>? Errors { get; set; }
    }

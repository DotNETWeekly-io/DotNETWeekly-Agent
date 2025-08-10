using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SemanticKernalPlayground;

public class Issue
{
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("number")]
    public required int Number { get; set; }

    [JsonPropertyName("body")]
    public required string Body { get; set; }
}

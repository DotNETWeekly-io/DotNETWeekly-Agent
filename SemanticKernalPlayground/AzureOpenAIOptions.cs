using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernalPlayground;

public class AzureOpenAIOptions
{
    public required string DeploymentName { get; set; }

    public required string Endpoint { get; set; }

    public required string ModelId { get; set; }

    public required string APIKey { get; set; }
}


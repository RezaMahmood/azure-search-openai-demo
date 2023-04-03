using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using static System.Environment;
using Azure.Identity;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Azure;
using Azure.Search.Documents;
using System.Text.Json;

namespace OpenAIDemoDotNet
{
    public static class ChatFunc
    {
        private static OpenAIClient openAIClient;
        private static SearchClient searchClient;

        [FunctionName("Chat")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] ChatRequest chatRequest,
            ExecutionContext context,
            ILogger logger)
        {
            string azure_openai_endpoint = GetEnvironmentVariable("OPENAI_API_BASE");
            string azure_openai_key = GetEnvironmentVariable("OPENAI_API_KEY");
            string azure_search_index = GetEnvironmentVariable("AZURE_SEARCH_INDEX");
            string azure_search_endpoint = GetEnvironmentVariable("AZURE_SEARCH_ENDPOINT");
            string azure_search_key = GetEnvironmentVariable("AZURE_SEARCH_KEY");
            string azure_openai_chatgpt_deployment = GetEnvironmentVariable("AZURE_OPENAI_CHATGPT_DEPLOYMENT");
            string azure_openai_gpt_deployment = GetEnvironmentVariable("AZURE_OPENAI_GPT_DEPLOYMENT");
            string kb_fields_sourcepage = GetEnvironmentVariable("KB_FIELDS_SOURCEPAGE");
            string kb_fields_category = GetEnvironmentVariable("KB_FIELDS_CATEGORY");
            string kb_fields_content = GetEnvironmentVariable("KB_FIELDS_CONTENT");


            IAzureClients AzureClients = new AzureClients();

            openAIClient = AzureClients.GetOpenAIClient(azure_openai_endpoint, azure_openai_key);
            searchClient = AzureClients.GetSearchClient(azure_search_endpoint, azure_search_index, azure_search_key);

            string currentDirectory = context.FunctionAppDirectory;

            var chatReadRetrieveReadApproach = new ChatReadRetrieveReadApproach(searchClient, openAIClient, logger, azure_openai_chatgpt_deployment, azure_openai_gpt_deployment, kb_fields_sourcepage, kb_fields_category, kb_fields_content, currentDirectory);

            var answer = chatReadRetrieveReadApproach.Run(chatRequest.history, chatRequest.overrides);

            return new OkObjectResult(answer);
        }
    }
}

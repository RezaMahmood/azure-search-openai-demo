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
    public static class AskFunc
    {
        private static OpenAIClient openAIClient;
        private static SearchClient searchClient;


        [FunctionName("Ask")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] AskRequest askModel,
            ExecutionContext context,
            ILogger log)
        {
            log.LogInformation("OpenAI 'Ask' HTTP trigger function processed a request.");

            string azure_openai_endpoint = GetEnvironmentVariable("OPENAI_API_BASE");
            string azure_openai_key = GetEnvironmentVariable("OPENAI_API_KEY");
            string azure_search_index = GetEnvironmentVariable("AZURE_SEARCH_INDEX");
            string azure_search_endpoint = GetEnvironmentVariable("AZURE_SEARCH_ENDPOINT");
            string azure_search_key = GetEnvironmentVariable("AZURE_SEARCH_KEY");

            IAzureClients AzureClients = new AzureClients();

            openAIClient = AzureClients.GetOpenAIClient(azure_openai_endpoint, azure_openai_key);
            searchClient = AzureClients.GetSearchClient(azure_search_endpoint, azure_search_index, azure_search_key);

            string currentDirectory = context.FunctionDirectory;

            IApproach approachModel = GetApproachType(askModel.approach, context.FunctionAppDirectory, log);

            var answer = approachModel.Run(askModel.question, askModel.overrides);

            return new OkObjectResult(answer);
        }

        private static IApproach GetApproachType(string approach, string currentDirectory, ILogger log)
        {
            IApproach approachModel = null;
            string azure_openai_gpt_deployment = GetEnvironmentVariable("AZURE_OPENAI_GPT_DEPLOYMENT");
            string kb_fields_content = GetEnvironmentVariable("KB_FIELDS_CONTENT");
            string kb_fields_sourcepage = GetEnvironmentVariable("KB_FIELDS_SOURCEPAGE");
            string kb_fields_category = GetEnvironmentVariable("KB_FIELDS_CATEGORY");


            switch (approach)
            {
                case "rtr":
                    approachModel = new RetrieveThenReadApproach(searchClient, openAIClient, log, azure_openai_gpt_deployment, kb_fields_sourcepage, kb_fields_content, currentDirectory);
                    break;
                case "rrr":
                    approachModel = new ReadRetrieveReadApproach(searchClient, openAIClient, log, azure_openai_gpt_deployment, kb_fields_sourcepage, kb_fields_content, currentDirectory);
                    break;
                case "rda":
                    approachModel = new ReadDecomposeAskApproach(searchClient, openAIClient, log, azure_openai_gpt_deployment, kb_fields_sourcepage, kb_fields_content, currentDirectory);
                    break;
                default:
                    throw new Exception("Invalid approach");
            }

            return approachModel;
        }
    }
}

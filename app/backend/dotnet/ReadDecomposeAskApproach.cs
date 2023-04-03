using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Microsoft.Extensions.Logging;

namespace OpenAIDemoDotNet
{
    internal class ReadDecomposeAskApproach : IApproach
    {
        private SearchClient searchClient;
        private OpenAIClient openAIClient;
        private ILogger log;
        private string azure_openai_chatgpt_deployment;
        private string kb_fields_sourcepage;
        private string kb_fields_content;
        private string currentDirectory;

        public ReadDecomposeAskApproach(SearchClient searchClient, OpenAIClient openAIClient, ILogger log, string azure_openai_chatgpt_deployment, string kb_fields_sourcepage, string kb_fields_content, string currentDirectory)
        {
            this.searchClient = searchClient;
            this.openAIClient = openAIClient;
            this.log = log;
            this.azure_openai_chatgpt_deployment = azure_openai_chatgpt_deployment;
            this.kb_fields_sourcepage = kb_fields_sourcepage;
            this.kb_fields_content = kb_fields_content;
            this.currentDirectory = currentDirectory;
        }

        public string Run(string question, Overrides overrides)
        {
            throw new System.NotImplementedException();
        }
    }
}
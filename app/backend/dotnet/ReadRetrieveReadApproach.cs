using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Azure.AI.OpenAI;
using System.Collections.Generic;
using System.IO;
using System;
using Microsoft.Extensions.Logging;

namespace OpenAIDemoDotNet
{
    public class ReadRetrieveReadApproach : IApproach
    {
        protected string template;
        protected string sourcepage_field;
        protected string content_field;
        protected SearchClient searchClient;
        protected OpenAIClient openAIClient;
        protected string gpt_deployment;
        protected ILogger logger;

        public ReadRetrieveReadApproach(SearchClient searchClient, OpenAIClient openAIClient, ILogger logger, string gpt_deployment, string sourcepage_field, string content_field, string currentDirectory)
        {
            var templatepath = System.IO.Path.Combine(currentDirectory, "Prompts", "readretrievereadapproach.txt");
            if (File.Exists(templatepath))
            {
                template = File.ReadAllText(templatepath);
            }
            this.sourcepage_field = sourcepage_field;
            this.content_field = content_field;
            this.searchClient = searchClient;
            this.openAIClient = openAIClient;
            this.gpt_deployment = gpt_deployment;
            this.logger = logger;
        }

        public string Run(string question, Overrides overrides)
        {
            throw new NotImplementedException("Not implemented yet");

        }
    }
}
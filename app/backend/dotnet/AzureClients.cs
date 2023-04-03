using System;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Identity;
using Azure;

namespace OpenAIDemoDotNet
{
    // create a base class that accepts a search client
    public class AzureClients : IAzureClients
    {
        protected SearchClient searchClient;
        protected OpenAIClient openAIClient;

        public AzureClients()
        {
        }


        public OpenAIClient GetOpenAIClient(string endpoint, string key = "")
        {
            if (string.IsNullOrEmpty(key))
            {
                openAIClient = new OpenAIClient(new System.Uri(endpoint), new DefaultAzureCredential());
            }
            else
            {
                openAIClient = new OpenAIClient(new System.Uri(endpoint), new AzureKeyCredential(key));
            }

            return openAIClient;
        }

        public SearchClient GetSearchClient(string endpoint, string index, string key = "")
        {
            if (string.IsNullOrEmpty(key))
            {
                searchClient = new SearchClient(new Uri(endpoint), index, new DefaultAzureCredential());
            }
            else
            {
                searchClient = new SearchClient(new Uri(endpoint), index, new AzureKeyCredential(key));
            }

            return searchClient;
        }

    }

    public interface IAzureClients
    {
        OpenAIClient GetOpenAIClient(string endpoint, string key = "");
        SearchClient GetSearchClient(string endpoint, string key, string index);
    }

}
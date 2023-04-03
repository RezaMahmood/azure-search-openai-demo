using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Azure.AI.OpenAI;
using System.Collections.Generic;
using System.IO;
using System;
using Microsoft.Extensions.Logging;


namespace OpenAIDemoDotNet
{
    public class RetrieveThenReadApproach : IApproach
    {
        protected string template;
        protected string sourcepage_field;
        protected string content_field;
        protected SearchClient searchClient;
        protected OpenAIClient openAIClient;
        protected string gpt_deployment;
        protected ILogger logger;
        public RetrieveThenReadApproach(SearchClient searchClient, OpenAIClient openAIClient, ILogger logger, string gpt_deployment, string sourcepage_field, string content_field, string currentDirectory)
        {
            var templatepath = System.IO.Path.Combine(currentDirectory, "Prompts", "retrievethenreadapproach.txt");
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
            bool use_semantic_captions = overrides.semantic_captions ?? false;
            bool use_semantic_ranker = overrides.semantic_ranker ?? false;
            int top = overrides.top ?? 3;
            string exclude_category = string.IsNullOrEmpty(overrides.exclude_category) ? null : overrides.exclude_category;
            string filter = string.IsNullOrEmpty(exclude_category) ? null : string.Format("category ne '{0}'", exclude_category.Replace("'", "''"));

            SearchOptions searchOptions = new SearchOptions();

            if (use_semantic_ranker)
            {
                searchOptions.Size = top;
                searchOptions.QueryType = SearchQueryType.Semantic;
                searchOptions.QueryLanguage = QueryLanguage.EnUs;
                searchOptions.QuerySpeller = QuerySpellerType.Lexicon;
                searchOptions.SemanticConfigurationName = "default";
                if (use_semantic_captions)
                {
                    searchOptions.QueryCaption = @"extractive|highlight-false";
                }
                searchOptions.Filter = filter;
            }
            else
            {
                searchOptions.Size = top;
                searchOptions.Filter = filter;
            }

            SearchResults<SearchDocument> searchResults = searchClient.Search<SearchDocument>(question, searchOptions);
            var listResult = new List<string>();


            if (use_semantic_captions)
            {
                foreach (SearchResult<SearchDocument> result in searchResults.GetResults())
                {
                    SearchDocument doc = result.Document;
                    var docsourcepage = (string)doc[sourcepage_field];
                    var captions = (dynamic)doc["@search.captions"];
                    var allCaptionsList = new List<string>();
                    foreach (dynamic caption in captions)
                    {
                        var cleanCaption = caption["text"].ToString().Replace("\n", "").Replace("\r", "");
                        allCaptionsList.Add(cleanCaption);
                    }
                    string allCaptions = string.Join(".", allCaptionsList);

                    listResult.Add(docsourcepage + ": " + allCaptions);
                }

            }
            else
            {
                foreach (SearchResult<SearchDocument> result in searchResults.GetResults())
                {
                    SearchDocument doc = result.Document;
                    var doccontent = (string)doc[content_field];
                    var docsourcepage = (string)doc[sourcepage_field];
                    listResult.Add(docsourcepage + ":" + doccontent.Replace("\n", "").Replace("\r", ""));
                }

            }

            var content = string.Join("\n", listResult);

            var prompt = string.IsNullOrEmpty(overrides.prompt_template) ? string.Format(template, question, content) : string.Format(overrides.prompt_template, question, content);

            var completionsOptions = new CompletionsOptions();

            completionsOptions.Prompts.Add(prompt);
            completionsOptions.MaxTokens = 1024;
            completionsOptions.Temperature = overrides.temperature ?? 0.3f;
            completionsOptions.NucleusSamplingFactor = 1;
            completionsOptions.StopSequences.Add("\n");

            try
            {
                var completion = openAIClient.GetCompletions(deploymentOrModelName: gpt_deployment, completionsOptions);
                var answer = completion.Value.Choices[0].Text;
                var responseformat = "{{\"data_points\":\"{0}\", \"answer\":\"{1}\", \"thoughts\":\"{2}\"}}";
                var thoughts = string.Format(@"Question:<br>{0}<br><br>Prompt:<br>{1}", question, prompt.Replace("\n", "<br>"));
                var response = string.Format(responseformat, content, answer, thoughts);

                //return string.Format(response, content, answer.Value.Choices[0].Text, string.Format(@"Question:<br>{0}<br><br>Prompt:<br>{1}", question, prompt.Replace("\n", "<br>")));
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Azure OpenAI Exception occured");
            }

            return string.Empty;

        }
    }
}
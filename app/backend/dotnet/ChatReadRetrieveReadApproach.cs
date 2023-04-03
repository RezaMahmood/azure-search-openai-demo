using System;
using System.Collections.Generic;
using Azure.AI.OpenAI;
using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace OpenAIDemoDotNet
{
    internal class ChatReadRetrieveReadApproach
    {
        private SearchClient searchClient;
        private OpenAIClient openAIClient;
        private ILogger logger;
        private string azure_openai_chatgpt_deployment;
        private string azure_openai_gpt_deployment;
        private string kb_fields_sourcepage;
        private string kb_fields_category;
        private string kb_fields_content;

        private string promptprefix;
        private string promptquery;
        private string promptcontent;

        public ChatReadRetrieveReadApproach(SearchClient searchClient, OpenAIClient openAIClient, ILogger logger, string azure_openai_chatgpt_deployment, string azure_openai_gpt_deployment, string kb_fields_sourcepage, string kb_fields_category, string kb_fields_content, string currentDirectory)
        {
            this.searchClient = searchClient;
            this.openAIClient = openAIClient;
            this.logger = logger;
            this.azure_openai_chatgpt_deployment = azure_openai_chatgpt_deployment;
            this.azure_openai_gpt_deployment = azure_openai_gpt_deployment;
            this.kb_fields_sourcepage = kb_fields_sourcepage;
            this.kb_fields_category = kb_fields_category;
            this.kb_fields_content = kb_fields_content;

            var promptprefixtemplate = System.IO.Path.Combine(currentDirectory, "Prompts", "chatreadretrievereadapproach_prefix.txt");
            var promptcontenttemplate = System.IO.Path.Combine(currentDirectory, "Prompts", "chatreadretrievereadapproach_content.txt");
            var promptquerytemplate = System.IO.Path.Combine(currentDirectory, "Prompts", "chatreadretrievereadapproach_query.txt");

            promptprefix = GetFileContents(promptprefixtemplate);
            promptcontent = GetFileContents(promptcontenttemplate);
            promptquery = GetFileContents(promptquerytemplate);
        }

        private string GetFileContents(string promptprefixtemplate)
        {
            if (File.Exists(promptprefixtemplate))
            {
                return File.ReadAllText(promptprefixtemplate);
            }
            return string.Empty;
        }

        public string Run(List<History> history, Overrides overrides)
        {
            bool use_semantic_captions = overrides.semantic_captions ?? false;
            bool use_semantic_ranker = overrides.semantic_ranker ?? false;
            int top = overrides.top ?? 3;
            string exclude_category = string.IsNullOrEmpty(overrides.exclude_category) ? null : overrides.exclude_category;
            string filter = string.IsNullOrEmpty(exclude_category) ? null : string.Format("category ne '{0}'", exclude_category.Replace("'", "''"));

            // Generate an optimised keyword search query based on the chat history and the last question
            var query = GenerateQuery(history, false);
            var question = history.Last().user;
            var prompt = string.Format(promptquery, query, question);

            logger.LogInformation("Completion(1) Prompt: " + prompt);

            var completionsOptions = new CompletionsOptions();
            completionsOptions.Prompts.Add(prompt);
            completionsOptions.Temperature = 0.0f;
            completionsOptions.MaxTokens = 32;
            completionsOptions.NucleusSamplingFactor = 1;
            completionsOptions.StopSequences.Add("\n");

            string completionResponse = string.Empty;

            try
            {
                var completion = openAIClient.GetCompletions(deploymentOrModelName: azure_openai_gpt_deployment, completionsOptions);
                completionResponse = completion.Value.Choices[0].Text;
                logger.LogInformation("Completion(1) Response: " + completionResponse);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ChatReadRetrieveReadApproach calling Azure OpenAI");
            }


            // Retrieve relevant documents from the search index with the GPT optimised query
            SearchOptions searchOptions = new SearchOptions();
            if (use_semantic_ranker)
            {
                searchOptions.Filter = filter;
                searchOptions.QueryType = SearchQueryType.Semantic;
                searchOptions.QueryLanguage = QueryLanguage.EnUs;
                searchOptions.QuerySpeller = QuerySpellerType.Lexicon;
                searchOptions.SemanticConfigurationName = "default";
                searchOptions.Size = top;

                if (use_semantic_captions)
                {
                    searchOptions.QueryCaption = @"extractive|highlight-false";
                }
            }
            else
            {
                searchOptions.Filter = filter;
                searchOptions.QueryType = SearchQueryType.Full;
                searchOptions.Size = top;
            }


            SearchResults<SearchDocument> searchResults;
            var listResult = new List<string>();

            try
            {
                searchResults = searchClient.Search<SearchDocument>(completionResponse, searchOptions);


                if (use_semantic_captions)
                {
                    foreach (SearchResult<SearchDocument> searchResult in searchResults.GetResults())
                    {
                        SearchDocument doc = searchResult.Document;
                        var captions = (dynamic)doc["@search.captions"];
                        var docsource = doc[kb_fields_sourcepage].ToString();
                        var allCaptionsList = new List<string>();
                        foreach (dynamic caption in captions)
                        {
                            var cleanCaption = caption["test"].ToString().Replace("\n", "").Replace("\r", "");
                            allCaptionsList.Add(cleanCaption);
                        }
                        var allCaptions = string.Join(".", allCaptionsList);
                        listResult.Add(docsource + ":" + allCaptions);
                    }
                }
                else
                {
                    foreach (SearchResult<SearchDocument> result in searchResults.GetResults())
                    {
                        SearchDocument doc = result.Document;
                        var doccontent = (string)doc[kb_fields_content];
                        var docsourcepage = (string)doc[kb_fields_sourcepage];
                        listResult.Add(docsourcepage + ":" + doccontent.Replace("\n", "").Replace("\r", ""));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ChatReadRetrieveReadApproach calling Azure Search");
            }

            //var content = string.Join("\n", listResult);
            var content = listResult.ToArray();
            logger.LogInformation("Search Result: " + content);

            string followUpQuestionsPrompt = (bool)overrides.suggest_followup_questions ? promptcontent : string.Empty;

            string followUpPrompt = string.Empty;

            if (string.IsNullOrEmpty(overrides.prompt_template))
            {
                followUpPrompt = string.Format(promptprefix, followUpQuestionsPrompt, string.Empty, string.Join("\n", content), GenerateQuery(history));
            }
            else
            {
                if (overrides.prompt_template.StartsWith(">>>"))
                {
                    followUpPrompt = string.Format(promptprefix, followUpQuestionsPrompt, overrides.prompt_template.Substring(2) + "\n", content, GenerateQuery(history));
                }
                else
                {
                    followUpPrompt = string.Format(promptprefix, followUpQuestionsPrompt, string.Empty, content, GenerateQuery(history));
                }
            }

            // Generate a contextual and content specific answer using the search results and chat history
            completionsOptions = new CompletionsOptions();
            completionsOptions.Prompts.Add(followUpPrompt);
            completionsOptions.Temperature = overrides.temperature ?? 0.7f;
            completionsOptions.MaxTokens = 1024;
            completionsOptions.NucleusSamplingFactor = 1;

            completionsOptions.StopSequences.Add("<|im_end|>");
            completionsOptions.StopSequences.Add("<|im_start|>");

            logger.LogInformation("Completion(2) Prompt: " + followUpPrompt);

            try
            {
                var completion = openAIClient.GetCompletions(deploymentOrModelName: azure_openai_chatgpt_deployment, completionsOptions);
                var answer = completion.Value.Choices[0].Text;
                //var responseformat = "{{\"data_points\":\"{0}\", \"answer\":\"{1}\", \"thoughts\":\"{2}\"}}";
                var responseformat = "{{\"data_points\":{0}, \"answer\":\"{1}\", \"thoughts\":\"{2}\"}}";
                var thoughts = string.Format(@"Searched for:<br>{0}<br><br>Prompt:<br>{1}", question, followUpPrompt.Replace("\n", "<br>"));
                var response = string.Format(responseformat, JsonConvert.SerializeObject(content), answer, thoughts);

                logger.LogInformation("Completion(2) Response: " + answer);

                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Azure OpenAI Exception occurred getting Follow Up answer");
            }

            return string.Empty;
        }

        private string GenerateQuery(List<History> history, bool include_last_turn = true, int approx_max_tokens = 1000)
        {
            var history_text = new StringBuilder();
            List<History> workingHistory = history.ToList();
            if (!include_last_turn && history.Count() > 1)
            {
                workingHistory.RemoveAt(workingHistory.Count - 1);
            }

            workingHistory.Reverse();

            foreach (History historyItem in workingHistory)
            {
                history_text.Append(@"<|im_start|>user").Append("\n").Append(historyItem.user).Append("\n").Append(@"<|im_end|>").Append("\n");
                history_text.Append(@"<|im_start|>assistant").Append("\n");
                if (!string.IsNullOrEmpty(historyItem.bot))
                {
                    history_text.Append(historyItem.bot).Append("\n");
                }

                history_text.Append(@"<|im_end|>").Append("\n");

                if (history_text.Length > approx_max_tokens * 4)
                {
                    break;
                }
            }

            return history_text.ToString();
        }
    }
}
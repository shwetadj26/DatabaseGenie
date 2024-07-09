using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace TeamSaga.Services
{
    public class QueryService
    {
        private readonly IConfiguration _configuration;
        private readonly OpenAIClient _openAiClient;
       

        public QueryService(IConfiguration configuration)
        {
            _configuration = configuration;
            _openAiClient = new OpenAIClient(new Uri(_configuration["AzureOpenAI:openAIEndpoint"]), new AzureKeyCredential(_configuration["AzureOpenAI:ApiKey"]));
        }

        public async Task<string> GetSqlQuery(string userQuery)
        {
            int maxRetries = 5;
            int delay = 1000; // initial delay in milliseconds

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var chatCompletionsOptions = new ChatCompletionsOptions
                    {
                        Messages = { new ChatRequestUserMessage(userQuery) },
                        AzureExtensionsOptions = new AzureChatExtensionsOptions
                        {
                            Extensions = { new AzureCognitiveSearchChatExtensionConfiguration
                    {
                        Authentication = new OnYourDataApiKeyAuthenticationOptions(_configuration["AzureCognitiveSearch:SearchKey"]),
                        SearchEndpoint = new Uri(_configuration["AzureCognitiveSearch:SearchEndpoint"]),
                        IndexName = _configuration["AzureCognitiveSearch:SearchIndex"],
                        QueryType = new AzureCognitiveSearchQueryType("semantic"),
                        SemanticConfiguration = _configuration["AzureCognitiveSearch:SemanticConfiguration"],
                        Strictness = int.Parse(_configuration["AzureCognitiveSearch:Strictness"]),
                        DocumentCount = int.Parse(_configuration["AzureCognitiveSearch:DocumentCount"])
                    }}
                        },
                        DeploymentName = _configuration["AzureOpenAI:DeploymentName"],
                        Temperature = 0.3f
                    };

                    ChatCompletions chatCompletionsResponse = await _openAiClient.GetChatCompletionsAsync(chatCompletionsOptions);
                    var responseContent = chatCompletionsResponse.Choices[0].Message.Content;

                    // Extract SQL query from the responseContent
                    //var sqlQuery = ExtractSqlQuery(responseContent);
                    return responseContent;
                }
                catch (HttpRequestException ex) when (ex.StatusCode == (HttpStatusCode)429)
                {
                    // Rate limit exceeded, wait and retry
                    if (attempt == maxRetries - 1)
                    {
                        throw; // Rethrow the exception if we've reached the max retries
                    }
                    await Task.Delay(delay);
                    delay *= 2; // exponential backoff
                }
                catch (Exception ex)
                {
                    throw; // Rethrow other exceptions immediately
                }
            }

            throw new Exception("Max retries exceeded.");
        }


        private string ExtractSqlQuery(string responseContent)
        {
            // Implement a method to extract SQL query from the response content
            // For simplicity, let's assume the query is in a code block
            var startIndex = responseContent.IndexOf("```sql") + 5;
            var endIndex = responseContent.IndexOf("```", startIndex);
            return responseContent.Substring(startIndex, endIndex - startIndex).Trim();
        }

        

    }
}

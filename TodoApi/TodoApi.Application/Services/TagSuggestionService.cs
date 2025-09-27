using System;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.TextAnalytics;
using TodoApi.Application.Interfaces;

namespace TodoApi.Application.Services
{
    public class TagSuggestionService : ITagSuggestionService
    {
        private readonly TextAnalyticsClient? _client;
        private readonly bool _useDummy;

        public TagSuggestionService(string? endpoint, string? apiKey, bool useDummy = false)
        {
            _useDummy = useDummy;
            if (!_useDummy && !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
            {
                _client = new TextAnalyticsClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            }
        }

        public async Task<string[]> SuggestTagsAsync(string text)
        {
            if (_useDummy || _client == null || string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<string>();
            }

            var response = await _client.ExtractKeyPhrasesAsync(text);
            return response.Value.ToArray();
        }
    }
}

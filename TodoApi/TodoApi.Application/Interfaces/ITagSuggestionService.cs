using System.Threading.Tasks;

namespace TodoApi.Application.Interfaces
{
    public interface ITagSuggestionService
    {
        Task<string[]> SuggestTagsAsync(string text);
    }
}

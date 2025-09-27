using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.Interfaces;

namespace TodoApi.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagSuggestionsController : ControllerBase
    {
        private readonly ITagSuggestionService _tagSuggestionService;

        public TagSuggestionsController(ITagSuggestionService tagSuggestionService)
        {
            _tagSuggestionService = tagSuggestionService;
        }

        [HttpPost]
        public async Task<IActionResult> SuggestTags([FromBody] string text)
        {
            var tags = await _tagSuggestionService.SuggestTagsAsync(text);
            return Ok(tags);
        }
    }
}

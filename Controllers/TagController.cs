using Microsoft.AspNetCore.Mvc;

namespace TodoApi.Controllers
{
    [ApiController]
    [Route("api/tags")]
    public class TagController : ControllerBase
    {
        private readonly TagService _tagService;

        public TagController(TagService tagService)
        {
            _tagService = tagService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTag(Tag tag)
        {
            var createdTag = await _tagService.CreateTagAsync(tag);
            return CreatedAtAction(nameof(GetTagById), new { id = createdTag.Id }, createdTag);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTagById(int id)
        {
            var tag = await _tagService.GetTagByIdAsync(id);
            if (tag == null) return NotFound();

            return Ok(tag);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTags()
        {
            var tags = await _tagService.GetAllTagsAsync();
            return Ok(tags);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTag(int id, Tag tag)
        {
            var updatedTag = await _tagService.UpdateTagAsync(id, tag);
            if (updatedTag == null) return NotFound();

            return Ok(updatedTag);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            var result = await _tagService.DeleteTagAsync(id);
            if (!result) return NotFound();

            return NoContent();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;
// Include other necessary namespaces

[Route("api/[controller]")]
[ApiController]
public class TagController : ControllerBase
{
    private readonly ITagService _tagService;

    public TagController(ITagService tagService)
    {
        _tagService = tagService;
    }

    // POST: api/Tag
    [HttpPost]
    public async Task<IActionResult> CreateTag([FromBody] TagDto tagDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var createdTag = await _tagService.CreateTagAsync(tagDto);
        return CreatedAtAction(nameof(GetTag), new { id = createdTag.Id }, createdTag);
    }

    // GET: api/Tag/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTag(int id)
    {
        var tag = await _tagService.GetTagByIdAsync(id);

        if (tag == null)
        {
            return NotFound();
        }

        return Ok(tag);
    }

    // GET: api/Tag
    [HttpGet]
    public async Task<IActionResult> GetAllTags()
    {
        var tags = await _tagService.GetAllTagsAsync();
        return Ok(tags);
    }

    // PUT: api/Tag/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTag(int id, [FromBody] TagDto tagDto)
    {
        if (id != tagDto.Id)
        {
            return BadRequest("ID mismatch");
        }

        await _tagService.UpdateTagAsync(id, tagDto);
        return NoContent();
    }

    // DELETE: api/Tag/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        await _tagService.DeleteTagAsync(id);
        return NoContent();
    }

    // Additional actions can be added here
}

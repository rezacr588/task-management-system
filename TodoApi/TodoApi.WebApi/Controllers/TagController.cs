using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
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
        try
        {
            var tag = await _tagService.GetTagByIdAsync(id);
            return Ok(tag);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
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

        try
        {
            await _tagService.UpdateTagAsync(id, tagDto);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // DELETE: api/Tag/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        try
        {
            await _tagService.DeleteTagAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // Additional actions can be added here
}

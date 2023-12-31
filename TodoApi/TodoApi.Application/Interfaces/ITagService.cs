using System.Collections.Generic;
using System.Threading.Tasks;
using TodoApi.Application.DTOs;

namespace TodoApi.Application.Interfaces
{
    public interface ITagService
    {
        Task<TagDto> CreateTagAsync(TagDto tagDto);
        Task<TagDto> GetTagByIdAsync(int id);
        Task<IEnumerable<TagDto>> GetAllTagsAsync();
        Task UpdateTagAsync(int id, TagDto tagDto);
        Task DeleteTagAsync(int id);
    }
}

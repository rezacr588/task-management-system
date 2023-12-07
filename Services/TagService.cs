using TodoApi; // Replace with the actual namespace of your models
using Microsoft.EntityFrameworkCore;

public class TagService
{
    private readonly ApplicationDbContext _context;

    public TagService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Tag> CreateTagAsync(Tag tag)
    {
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task<Tag> GetTagByIdAsync(int id)
    {
        return await _context.Tags.FindAsync(id);
    }

    public async Task<IEnumerable<Tag>> GetAllTagsAsync()
    {
        return await _context.Tags.ToListAsync();
    }

    public async Task<Tag> UpdateTagAsync(int id, Tag updatedTag)
    {
        // First, find the existing Tag in the database by its ID. The FindAsync method
        // is used to retrieve an entity by its primary key. If the tag is not found,
        // null is returned, indicating that no update can be performed.
        var tag = await _context.Tags.FindAsync(id);
        if (tag == null)
        {
            return null;
        }

        // If the Tag is found, its properties are updated with the values from the
        // updatedTag object. This is where you map the updated values to the existing
        // entity. It's important to ensure that you only update the properties that
        // need to be changed.

        // Example: Update the 'Name' property of the Tag.
        // You can add more properties to update as per your model's definition.
        tag.Name = updatedTag.Name;

        // Additional property updates can go here.
        // For instance, if your Tag model has other properties like 'Description', 
        // you would update them similarly:
        // tag.Description = updatedTag.Description;

        // After updating the properties, save the changes to the database.
        // The SaveChangesAsync method applies the changes made to the DbContext 
        // (which include the updated Tag properties) to the database.
        await _context.SaveChangesAsync();

        // Return the updated Tag entity. This Tag entity now contains the updated
        // values and represents the current state of the record in the database.
        return tag;
    }

    public async Task<bool> DeleteTagAsync(int id)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag == null) return false;

        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync();
        return true;
    }
}

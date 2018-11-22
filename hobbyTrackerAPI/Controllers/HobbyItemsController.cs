using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using hobbyTrackerAPI.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;

namespace hobbyTrackerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HobbyItemsController : ControllerBase
    {
        private readonly hobbyTrackerAPIContext _context;
		private IConfiguration _configuration;

		public HobbyItemsController(hobbyTrackerAPIContext context, IConfiguration configuration)
        {
            _context = context;
			_configuration = configuration;
		}

        // GET: api/HobbyItems
        [HttpGet]
        public IEnumerable<HobbyItem> GetHobbyItem()
        {
            return _context.HobbyItem;
        }

        // GET: api/HobbyItems/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetHobbyItem([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var hobbyItem = await _context.HobbyItem.FindAsync(id);

            if (hobbyItem == null)
            {
                return NotFound();
            }

            return Ok(hobbyItem);
        }

        // PUT: api/HobbyItems/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutHobbyItem([FromRoute] int id, [FromBody] HobbyItem hobbyItem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != hobbyItem.Id)
            {
                return BadRequest();
            }

            _context.Entry(hobbyItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HobbyItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/HobbyItems
        [HttpPost]
        public async Task<IActionResult> PostHobbyItem([FromBody] HobbyItem hobbyItem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.HobbyItem.Add(hobbyItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetHobbyItem", new { id = hobbyItem.Id }, hobbyItem);
        }

        // DELETE: api/HobbyItems/[id]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHobbyItem([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var hobbyItem = await _context.HobbyItem.FindAsync(id);
            if (hobbyItem == null)
            {
                return NotFound();
            }

            _context.HobbyItem.Remove(hobbyItem);
            await _context.SaveChangesAsync();

            return Ok(hobbyItem);
        }

        private bool HobbyItemExists(int id)
        {
            return _context.HobbyItem.Any(e => e.Id == id);
		}


		// GET: api/Meme/Tags
		[Route("tags")]
		[HttpGet]
		public async Task<List<string>> GetTags()
		{
			var memes = (from m in _context.HobbyItem
						 select m.Tags).Distinct();

			var returned = await memes.ToListAsync();

			return returned;
		}

		// GET: api/Hobby/Tags
		[Route("tag")]
		[HttpGet]
		public async Task<List<HobbyItem>> GetTagsItem([FromQuery] string tags)
		{
			var memes = from m in _context.HobbyItem
						select m; //get all the memes


			if (!String.IsNullOrEmpty(tags)) //make sure user gave a tag to search
			{
				memes = memes.Where(s => s.Tags.ToLower().Equals(tags.ToLower())); // find the entries with the search tag and reassign
			}

			var returned = await memes.ToListAsync(); //return the memes

			return returned;
		}

		[HttpPost, Route("upload")]
		public async Task<IActionResult> UploadFile([FromForm]HobbyImageItem meme)
		{
			if (!Helpers.MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
			{
				return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
			}
			try
			{
				using (var stream = meme.Image.OpenReadStream())
				{
					var cloudBlock = await UploadToBlob(meme.Image.FileName, null, stream);
					//// Retrieve the filename of the file you have uploaded
					//var filename = provider.FileData.FirstOrDefault()?.LocalFileName;
					if (string.IsNullOrEmpty(cloudBlock.StorageUri.ToString()))
					{
						return BadRequest("An error has occured while uploading your file. Please try again.");
					}

					HobbyItem hobbyItem = new HobbyItem();
					hobbyItem.Title = meme.Title;
					hobbyItem.Tags = meme.Tags;

					System.Drawing.Image image = System.Drawing.Image.FromStream(stream);
					hobbyItem.Height = image.Height.ToString();
					hobbyItem.Width = image.Width.ToString();
					hobbyItem.Url = cloudBlock.SnapshotQualifiedUri.AbsoluteUri;
					hobbyItem.Uploaded = DateTime.Now.ToString();

					_context.HobbyItem.Add(hobbyItem);
					await _context.SaveChangesAsync();

					return Ok($"File: {meme.Title} has successfully uploaded");
				}
			}
			catch (Exception ex)
			{
				return BadRequest($"An error has occured. Details: {ex.Message}");
			}


		}

		private async Task<CloudBlockBlob> UploadToBlob(string filename, byte[] imageBuffer = null, System.IO.Stream stream = null)
		{

			var accountName = _configuration["AzureBlob:name"];
			var accountKey = _configuration["AzureBlob:key"]; ;
			var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
			CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

			CloudBlobContainer imagesContainer = blobClient.GetContainerReference("images");

			string storageConnectionString = _configuration["AzureBlob:connectionString"];

			// Check whether the connection string can be parsed.
			if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
			{
				try
				{
					// Generate a new filename for every new blob
					var fileName = Guid.NewGuid().ToString();
					fileName += GetFileExtention(filename);

					// Get a reference to the blob address, then upload the file to the blob.
					CloudBlockBlob cloudBlockBlob = imagesContainer.GetBlockBlobReference(fileName);

					if (stream != null)
					{
						await cloudBlockBlob.UploadFromStreamAsync(stream);
					}
					else
					{
						return new CloudBlockBlob(new Uri(""));
					}

					return cloudBlockBlob;
				}
				catch (StorageException ex)
				{
					return new CloudBlockBlob(new Uri(""));
				}
			}
			else
			{
				return new CloudBlockBlob(new Uri(""));
			}

		}

		private string GetFileExtention(string fileName)
		{
			if (!fileName.Contains("."))
				return ""; //no extension
			else
			{
				var extentionList = fileName.Split('.');
				return "." + extentionList.Last(); //assumes last item is the extension 
			}
		}
	}
}
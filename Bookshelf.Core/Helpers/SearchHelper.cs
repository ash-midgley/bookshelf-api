using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Bookshelf.Core
{
    public class SearchHelper : ISearchHelper
    {
        private IConfiguration _config;
        private readonly HttpClient _client = new HttpClient();
        private readonly string _queryParams = "printType=books&orderBy=relevance&maxResults=1";
        
        public SearchHelper(IConfiguration config)
        {
            _config = config;
        }

         public async Task<Book> PullGoogleBooksData(NewBookDto book)
        {
            var result = new Book
            {
                Title = book.Title,
                Author = book.Author,
                UserId = book.UserId,
                CategoryId = book.CategoryId,
                RatingId = book.RatingId,
                FinishedOn = book.FinishedOn,
                ImageUrl = _config["DefaultCover"]
            };

            var search = await SearchGoogleBooks(book.Title.ToLower(), book.Author.ToLower());

            if(search.TotalItems > 0)
            {
                var volume = search.Items.First().VolumeInfo;
                result.PageCount = volume.PageCount;
                
                if(volume.ImageLinks.Small != null)
                {
                    result.ImageUrl = volume.ImageLinks.Small;
                }
                else if(volume.ImageLinks.Thumbnail != null)
                {
                    result.ImageUrl = volume.ImageLinks.Thumbnail;
                }

                result.Summary = volume.Description;
            }

            return result;
        }

        private async Task<GoogleBookSearch> SearchGoogleBooks(string title, string author)
        {
            var apiUrl = _config["GoogleBooks:Url"];
            var apiKey = _config["GoogleBooks:Key"];
            var url = $"{apiUrl}/volumes?q={title}+inauthor:{author}&{_queryParams}&key={apiKey}";
            var json = await _client.GetStringAsync(url);
            return JsonSerializer.Deserialize<GoogleBookSearch>(json);
        }
    }
}
using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using MovieCatalogExam.DataTransferObjects;


namespace MovieCatalogExam;

    [TestFixture]
public class Tests
{
    private RestClient _client;

    private const string BaseUrl = "http://144.91.123.158:5000/";
    private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJjNGVmZGVlMS1jNTljLTQ0OTYtOWFlMi0xMDhmZWJiYzBlYjciLCJpYXQiOiIwNC8xOC8yMDI2IDA3OjQ1OjI3IiwiVXNlcklkIjoiODc3MDc4OTUtNGVmMy00Mjg3LTYyOTItMDhkZTc2OTcxYWI5IiwiRW1haWwiOiJib3Jpc2xhdkBib3Jpc2xhdi5ib3Jpc2xhdiIsIlVzZXJOYW1lIjoiQm9yaXNsYXZMTSIsImV4cCI6MTc3NjUxOTkyNywiaXNzIjoiTW92aWVDYXRhbG9nX0FwcF9Tb2Z0VW5pIiwiYXVkIjoiTW92aWVDYXRhbG9nX1dlYkFQSV9Tb2Z0VW5pIn0.uLii42PtWR9RfsS8hFYwpCtXebfqOEYyWW_SwCAw77s";
    private const string LoginEmail = "borislav@borislav.borislav";
    private const string LoginPassword = "Borislav123";
    
    [OneTimeSetUp]
    
    public void Setup()
    {
        string jwtToken = !string.IsNullOrWhiteSpace(StaticToken) 
            ? StaticToken 
            : GetJwtToken(LoginEmail, LoginPassword);


        var options = new RestClientOptions(BaseUrl)
        {
            Authenticator = new JwtAuthenticator(jwtToken)
        };

        this._client = new RestClient(options);
    }

    private string GetJwtToken(string email, string password)
    {
        var tempClient = new RestClient(BaseUrl);
        var request = new RestRequest("/api/User/Authentication", Method.Post);
        request.AddJsonBody(new { email, password });

        var response = tempClient.Execute(request);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = JsonSerializer.Deserialize<JsonElement>(response.Content!);
            var token = content.GetProperty("token").GetString();

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Token not found in the response.");
            }
            return token;
        }
        else
        {
            throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
        }
    }

    private static string? _lastCreatedMovieId;

    [Test, Order(1)]
    public void Test_CreateMovie()
    {
        var newMovie = new MovieDto
        {
            Title = "Rambo",
            Description = "Rambo first blood"
        };

        var request = new RestRequest("/api/Movie/Create", Method.Post);
        request.AddJsonBody(newMovie);

        var response = this._client.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = JsonSerializer.Deserialize<ApiResponseDto>(response.Content!);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Movie, Is.Not.Null);
        Assert.That(result.Movie.Id, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Msg, Is.EqualTo("Movie created successfully!"));
        _lastCreatedMovieId = result.Movie.Id;
    }

    [Test, Order(2)]
    public void Test_EditMovie()
    {
        var updatedMovie = new MovieDto
        {
            Title = "Rambo updated",
            Description = "Rambo first blood updated description."
        };

        var request = new RestRequest("/api/Movie/Edit", Method.Put);
        request.AddQueryParameter("movieId", _lastCreatedMovieId);
        request.AddJsonBody(updatedMovie);

        var response = this._client.Execute(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = JsonSerializer.Deserialize<ApiResponseDto>(response.Content!);
        Assert.That(result!.Msg, Is.EqualTo("Movie edited successfully!"));
    }

    [Test, Order(3)]
    public void Test_GetAllMovies()
    {
        var request = new RestRequest("/api/Catalog/All");

        var response = this._client.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var movies = JsonSerializer.Deserialize<List<MovieDto>>(response.Content!);
        Assert.That(movies, Is.Not.Null);
        Assert.That(movies.Count, Is.GreaterThan(0));
    }

    [Test, Order(4)]
    public void Test_DeleteMovie()
    {
        var request = new RestRequest("/api/Movie/Delete", Method.Delete);
        request.AddQueryParameter("movieId", _lastCreatedMovieId);

        var response = this._client.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = JsonSerializer.Deserialize<ApiResponseDto>(response.Content!);
        Assert.That(result!.Msg, Is.EqualTo("Movie deleted successfully!"));
    }

    [Test, Order(5)]
    public void Test_CreateMovie_With_MissingFields()
    {
        var incompleteMovie = new { Title = "" }; 

        var request = new RestRequest("/api/Movie/Create", Method.Post);
        request.AddJsonBody(incompleteMovie);

        var response = this._client.Execute(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test, Order(6)]
    public void Test_EditMovie_With_NonExistingId()
    {
        var nonExistingId = "ImaginaryID";
        var updatedMovie = new MovieDto
        {
            Title = "Title",
            Description = "Description"
        };

        var request = new RestRequest("/api/Movie/Edit", Method.Put);
        request.AddQueryParameter("movieId", nonExistingId);
        request.AddJsonBody(updatedMovie);

        var response = this._client.Execute(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var result = JsonSerializer.Deserialize<ApiResponseDto>(response.Content!);
        Assert.That(result!.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
    }

    [Test, Order(7)]
    public void Test_DeleteMovie_With_NonExistingId()
    {
        var nonExistingId = "Imaginary";

        var request = new RestRequest("/api/Movie/Delete", Method.Delete);
        request.AddQueryParameter("movieId", nonExistingId);

        var response = this._client.Execute(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var result = JsonSerializer.Deserialize<ApiResponseDto>(response.Content!);
        Assert.That(result!.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
    }


    [OneTimeTearDown]
    public void TearDown()
    {
        this._client.Dispose();
    }
}
using System.Text.Json.Serialization;

namespace MovieCatalogExam.DataTransferObjects;

public class ApiResponseDto
{
    [JsonPropertyName("msg")] public string Msg { get; set; }

    [JsonPropertyName("movie")] public MovieDto Movie { get; set; } = new MovieDto();
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RekordRest
{
  public class S3Uploader
  {
    private static readonly HttpClient _httpClient = new HttpClient(new SocketsHttpHandler()
    {
      PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    });

    public async Task<bool> UploadToS3(string url, string contentType, byte[] fileBytes)
    {
      using var streamContent = new StreamContent(new MemoryStream(fileBytes));
      streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

      // Use the shared static client
      var uploadResponse = await _httpClient.PutAsync(url, streamContent);
      if (!uploadResponse.IsSuccessStatusCode)
      {
        Console.WriteLine(await uploadResponse.Content.ReadAsStringAsync());
        return false;
      }
      return true;
    }
  }
}

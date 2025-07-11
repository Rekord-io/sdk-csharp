using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Globalization;


namespace RekordRest
{
  public partial class RekordRestClient
  {
    /// <returns>Rekord successfully created</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    public async Task<Rekord> CreateFileRekordAsync(RekordRequest rekordRequest, string contentType, byte[] fileBytes)
    {
      var key = Guid.NewGuid().ToString();
      var res = await CreatePayloadURLAsync(new Body
      {
        Key = key,
        ContentType = contentType,
        Workspace = rekordRequest.Workspace.ToString()
      });

      var succeeded = await new S3Uploader().UploadToS3(res.Url, contentType, fileBytes);
      if (!succeeded)
      {
        throw new Exception("Uploading file to S3 bucket failed");
      }

      rekordRequest.File = res.Key;

      return await CreateRekordAsync(rekordRequest);
    }

    static partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings)
    {
      settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
      settings.Converters.Add(new IsoDateTimeConverter
      {
        DateTimeStyles = DateTimeStyles.AdjustToUniversal,
        // "yyyy-MM-ddTHH:mm:ss.fffK" for 3 decimal places and 'Z'
        // If the endpoint wants NO milliseconds when they are zero,
        // this might be too much, but for ".650z" it should be fine.
        DateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ",
        Culture = CultureInfo.InvariantCulture // Always use invariant culture
      });
    }
  }

}

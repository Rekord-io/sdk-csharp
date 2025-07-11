using RekordRest;
using System.Net.Http.Headers;
namespace Rekord.Api.Test
{
  [TestClass]
  public class RekordTestClient
  {
    private RekordRestClient rekordClient;

    public RekordTestClient()
    {
      var httpClient = new HttpClient();
      // token_id
      var token = "";

      httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
      rekordClient = new RekordRestClient("https://btdryd5731.execute-api.eu-west-1.amazonaws.com/rekord-web-application-sto-api-gw-stage", httpClient);
    }

    [TestMethod]
    public async Task TestGetAndCreateRecordJson()
    {
      var rekords = await rekordClient.ListRekordsAsync(null, null, null, null);

      var rekord = rekords.Items.FirstOrDefault();
      if (rekord != null)
      {
        await rekordClient.GetRekordAsync(rekord.Id);
        await rekordClient.GetRekordMetaAsync(rekord.Id);
      }
      else
      {
        Assert.Fail("No rekord found!");
      }

      var workspaces = await rekordClient.ListWorkspacesAsync(null, null);
      var workspace = workspaces.Items.FirstOrDefault();
      if (workspace != null)
      {
        await rekordClient.GetWorkspaceAsync(workspace.Id);
      }
      else
      {
        Assert.Fail("No workspace found!");
      }

      var payload = new
      {
        Test = Guid.NewGuid().ToString()
      };

      var newlyCreated = await rekordClient.CreateRekordAsync(new RekordRequest
      {
        Description = "Test",
        IssuedAt = DateTime.UtcNow,
        Group = "test",
        OriginalFileName = "test.txt",
        Workspace = new Guid(workspace.Id),
        Payload = payload,
        PayloadType = RekordRequestPayloadType.JSON,
      });
      Console.WriteLine(newlyCreated.Id);
      Assert.IsNotNull(newlyCreated);
    }

    [TestMethod]
    public async Task TestGetAndCreateRecordFile()
    {
      var workspaces = await rekordClient.ListWorkspacesAsync(null, null);
      var workspace = workspaces.Items.FirstOrDefault();
      if (workspace == null)
      {
        Assert.Fail();
      }
      var key = Guid.NewGuid().ToString();
      var contentType = "application/pdf";
      var res = await rekordClient.CreatePayloadURLAsync(new Body
      {
        Key = key,
        ContentType = contentType,
        Workspace = workspace.Id
      });

      var fileBytes = File.ReadAllBytes(@"C:\Transactions\dummy.pdf");
      var succeeded = await UploadToS3(res.Url, contentType, fileBytes);
      Assert.IsTrue(succeeded);

      var rekordRequest = new RekordRequest
      {
        Description = "Test",
        IssuedAt = DateTime.UtcNow,
        Group = "test",
        OriginalFileName = "dummy.pdf",
        Workspace = new Guid(workspace.Id),
        PayloadType = RekordRequestPayloadType.FILE,
        File = res.Key
      };

      var createdRecord = await rekordClient.CreateRekordAsync(rekordRequest);
      Assert.IsNotNull(createdRecord);

      Assert.AreEqual(rekordRequest.Description, createdRecord.Description);
      Assert.AreEqual(rekordRequest.File, createdRecord.File);
      Assert.AreEqual(rekordRequest.Workspace.ToString(), createdRecord.Workspace);

      await rekordClient.GetPayloadURLAsync(createdRecord.Id);
    }

    [TestMethod]
    public async Task TestCreateRecordFileInOneStep()
    {
      var workspaces = await rekordClient.ListWorkspacesAsync(null, null);
      var workspace = workspaces.Items.FirstOrDefault();
      if (workspace == null)
      {
        Assert.Fail();
      }
      var rekordRequest = new RekordRequest
      {
        Description = "Test",
        IssuedAt = DateTime.UtcNow,
        Group = "test",
        OriginalFileName = "dummy.pdf",
        Workspace = new Guid(workspace.Id),
        PayloadType = RekordRequestPayloadType.FILE
      };

      var contentType = "application/pdf";
      var fileBytes = File.ReadAllBytes(@"C:\Transactions\dummy.pdf");

      var rekordCreated = await rekordClient.CreateFileRekordAsync(rekordRequest, contentType, fileBytes);
      Assert.IsNotNull(rekordCreated);
      var test = await rekordClient.GetRekordAsync(rekordCreated.Id);
      Console.WriteLine($"Test {test.Id}");
    }

    [TestMethod]
    public async Task TestWorkspace()
    {
      var workspaceName = $"Test_{Guid.NewGuid()}";
      var workspaceResponse = await rekordClient.CreateWorkspaceAsync(new WorkspaceRequest
      {
        Blockchain = "bsv",
        Name = workspaceName
      });

      var updatedWorkspaceName = workspaceName + "_new";
      await rekordClient.UpdateWorkspaceAsync(workspaceResponse.Id, new WorkspaceRequest
      {
        Blockchain = "bsv",
        Name = updatedWorkspaceName
      });

      var workspace = await rekordClient.GetWorkspaceAsync(workspaceResponse.Id);
      Assert.AreEqual(workspace.Name, updatedWorkspaceName);
      await rekordClient.DeleteWorkspaceAsync(workspace.Id);
    }

    private async Task<bool> UploadToS3(string url, string contentType, byte[] fileBytes)
    {
      var client = new HttpClient();
      using var streamContent = new StreamContent(new MemoryStream(fileBytes));

      streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
      // Upload to bucket
      var uploadResponse = await client.PutAsync(url, streamContent);
      if (!uploadResponse.IsSuccessStatusCode)
      {
        Console.WriteLine(await uploadResponse.Content.ReadAsStringAsync());
        return false;
      }
      return true;
    }
  }
}

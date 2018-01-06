using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using File = Google.Apis.Drive.v3.Data.File;

namespace TGOZ_GoogleDrive
{
    public class Program
    {
        //https://developers.google.com/drive/v3/web/quickstart/dotnet

        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/drive-dotnet-quickstart.json
        static string[] Scopes = { DriveService.Scope.Drive, DriveService.Scope.DriveAppdata, DriveService.Scope.DriveMetadata };
        static string ApplicationName = "Drive API .NET Quickstart";

        static void Main(string[] args)
        {
            UserCredential credential;

            using (var stream =
                new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/drive-dotnet-quickstart.json");
                 
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            var stagingFolderSearch = service.Files.List();
            stagingFolderSearch.Q = "name='Staging' and mimeType='application/vnd.google-apps.folder'";
            stagingFolderSearch.PageSize = 1;


            //var testFolderSearch = service.Files.List();
            ////testFolderSearch.Q = //"name='Shared with me' and " +
            //                      //  "mimeType='application/vnd.google-apps.folder'";
            //testFolderSearch.PageSize = 1000;

            //IList<File> stagingFolders = testFolderSearch.Execute().Files;

            //foreach (var folder in stagingFolders.Where(x => x.Name.Contains("me")))
            //{
            //    Console.WriteLine(folder.Name);
            //}

            //Console.ReadLine();

            IList<File> stagingFolder = stagingFolderSearch.Execute().Files;
            var stagingFolderId = stagingFolder.FirstOrDefault()?.Id;

            // Define parameters of request.
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.Q = "sharedWithMe"; //"nd name='Test Sharing'";
            listRequest.PageSize = 100;
            listRequest.Fields = "nextPageToken, files(name, id, owners, permissions, *)";

            // List files.
            var files = listRequest.Execute().Files;
            Console.WriteLine("Files:");
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    var copyRequest = service.Files.Copy(new File {Parents = new List<string>{stagingFolderId}}, file.Id);
                    var newFile = copyRequest.Execute();

                    var updateRequest = service.Files.Update(new File {Name = newFile.Name.Replace("Copy of ", "")},newFile.Id);
                    newFile = updateRequest.Execute();

                    var ownerEmail = file.Owners.FirstOrDefault()?.EmailAddress;
                    
                    service.Permissions.Create(new Permission {EmailAddress = ownerEmail, Role = "writer", Type = "user"},
                        newFile.Id).Execute();

                    foreach (var permission in file.Permissions.Where(x =>
                        x.EmailAddress != ownerEmail && x.EmailAddress != "tgoz@tgoz.com.au"))
                    {
                        service.Permissions.Create(new Permission { EmailAddress = permission.EmailAddress, Role = permission.Role, Type = permission.Type },
                            newFile.Id).Execute();

                    }

                    var tgozSharedPermission = file.Permissions.Single(x => x.EmailAddress == "tgoz@tgoz.com.au");


                    //var getRequest = service.Files.Get(newFile.Id);
                    //var result = getRequest.Execute();


                    //var updateOriginal = service.Files.Update(new File{}, file.Id);
                    //updateOriginal.

                    //var deleteRequest = service.Files.Delete(file.Id).Execute();
                    service.Permissions.Delete(file.Id, tgozSharedPermission.Id).Execute();
                }
            }
            else
            {
                Console.WriteLine("No files found.");
            }
            Console.Read();

        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using Microsoft.Extensions.Configuration;
using Helpers;

namespace graphconsoleapp
{
    class Program
    {
        // The method retrieves the configuration details from the appsettings.json file
        private static IConfigurationRoot LoadAppSettings()
        {
            try
            {
                var config = new ConfigurationBuilder()
                                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json", false, true)
                                .Build();

                if (string.IsNullOrEmpty(config["applicationId"]) ||
                    string.IsNullOrEmpty(config["tenantId"]))
                {
                    return null;
                }

                return config;
            }
            catch (System.IO.FileNotFoundException)
            {
                return null;
            }
        }

        // This method will create an instance of the clients used to call Microsoft Graph.
        private static IAuthenticationProvider CreateAuthorizationProvider(IConfigurationRoot config, string userName, SecureString userPassword)
        {
            var clientId = config["applicationId"];
            var authority = $"https://login.microsoftonline.com/{config["tenantId"]}/v2.0";

            List<string> scopes = new List<string>();
            scopes.Add("User.Read");
            scopes.Add("Files.Read");
            scopes.Add("Files.ReadWrite");
            scopes.Add("Sites.Read.All");

            var cca = PublicClientApplicationBuilder.Create(clientId)
                                                    .WithAuthority(authority)
                                                    .Build();
            return MsalAuthenticationProvider.GetInstance(cca, scopes.ToArray(), userName, userPassword);
        }

        // This method creates an instance of the GraphServiceClient object.
        private static GraphServiceClient GetAuthenticatedGraphClient(IConfigurationRoot config, string userName, SecureString userPassword)
        {
            var authenticationProvider = CreateAuthorizationProvider(config, userName, userPassword);
            var graphClient = new GraphServiceClient(authenticationProvider);
            return graphClient;
        }

        // This method prompts the user for their password:
        private static SecureString ReadPassword()
        {
            Console.WriteLine("Enter your password");
            SecureString password = new SecureString();
            while (true)
            {
                ConsoleKeyInfo c = Console.ReadKey(true);
                if (c.Key == ConsoleKey.Enter)
                {
                    break;
                }
                password.AppendChar(c.KeyChar);
                Console.Write("*");
            }
            Console.WriteLine();
            return password;
        }

        // The method prompts the user for their username:
        private static string ReadUsername()
        {
            string username;
            Console.WriteLine("Enter your username");
            username = Console.ReadLine();
            return username;
        }

        // This method displays all the files along with the respective ids
        static void ViewFiles(GraphServiceClient client)
        {
            // request 1 - get user's files
            var request = client.Me.Drive.Root.Children.Request();

            var results = request.GetAsync().Result;
            foreach (var file in results)
            {
                Console.WriteLine(file.Id + ": " + file.Name);
            }
        }

        // download a file from the user's OneDrive
        static void DownloadFiles(GraphServiceClient client)
        {
            var fileId = "REPLACE_THIS";
            var request = client.Me.Drive.Items[fileId].Content.Request();

            var stream = request.GetAsync().Result;
            var driveItemPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "driveItem_" + fileId + ".file");
            var driveItemFile = System.IO.File.Create(driveItemPath);
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(driveItemFile);
            Console.WriteLine("Saved file to: " + driveItemPath);
        }

        // This method uploads Files < 4MB in size to OneDrive
        static void UploadFiles(GraphServiceClient client)
        {
            // get reference to stream of file in OneDrive
            var fileName = "smallfile.txt";
            var filePath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), fileName);
            Console.WriteLine("Uploading file: " + fileName);
            // get a stream of the local file
            FileStream fileStream = new FileStream(filePath, FileMode.Open);
            // upload the file to OneDrive
            var uploadedFile = client.Me.Drive.Root
                                        .ItemWithPath("smallfile.txt")
                                        .Content
                                        .Request()
                                        .PutAsync<DriveItem>(fileStream)
                                        .Result;
            Console.WriteLine("File uploaded to: " + uploadedFile.WebUrl);
        }

        // This method uploads Files > 4MB in size to OneDrive
        static void UploadLargeFiles(GraphServiceClient client)
        {
            var fileName = "largefile.zip";
            var filePath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), fileName);
            Console.WriteLine("Uploading file: " + fileName);

            // load resource as a stream
            using (Stream stream = new FileStream(filePath, FileMode.Open))
            {
                var uploadSession = client.Me.Drive.Root
                                                .ItemWithPath(fileName)
                                                .CreateUploadSession()
                                                .Request()
                                                .PostAsync()
                                                .Result;

                // create upload task
                var maxChunkSize = 320 * 1024;
                var largeUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, stream, maxChunkSize);

                // create progress implementation
                IProgress<long> uploadProgress = new Progress<long>(uploadBytes =>
                {
                    Console.WriteLine($"Uploaded {uploadBytes} bytes of {stream.Length} bytes");
                });

                // upload file
                UploadResult<DriveItem> uploadResult = largeUploadTask.UploadAsync(uploadProgress).Result;
                if (uploadResult.UploadSucceeded)
                {
                    Console.WriteLine("File uploaded to user's OneDrive root folder: " + uploadResult.Location.AbsolutePath);
                }
            }
        }

        // This method displays files the currently signed-in user has accessed or modified 
        static void FilesAccessedByUser(GraphServiceClient client)
        {
            var request = client.Me.Insights.Used.Request();

            var results = request.GetAsync().Result;
            foreach (var resource in results)
            {
                Console.WriteLine("(" + resource.ResourceVisualization.Type + ") - " + resource.ResourceVisualization.Title);
                Console.WriteLine("  Last Accessed: " + resource.LastUsed.LastAccessedDateTime.ToString());
                Console.WriteLine("  Last Modified: " + resource.LastUsed.LastModifiedDateTime.ToString());
                Console.WriteLine("  Id: " + resource.Id);
                Console.WriteLine("  ResourceId: " + resource.ResourceReference.Id);
            }
        }


        /** This method displays files the currently signed-in user has trending around him.
        Rich relationship exists connecting a user to documents that are trending around the user (are relevant to the user). 
        OneDrive files, and files stored on SharePoint team sites can trend around the user.*/
        static void FilesTrendingAroundUser(GraphServiceClient client)
        {
            var request = client.Me.Insights.Trending.Request();

            var results = request.GetAsync().Result;
            foreach (var resource in results)
            {
                Console.WriteLine("(" + resource.ResourceVisualization.Type + ") - " + resource.ResourceVisualization.Title);
                Console.WriteLine("  Weight: " + resource.Weight);
                Console.WriteLine("  Id: " + resource.Id);
                Console.WriteLine("  ResourceId: " + resource.ResourceReference.Id);
            }
        }

        static void Main(string[] args)
        {
            var config = LoadAppSettings();
            if (config == null)
            {
                Console.WriteLine("Invalid appsettings.json file.");
                return;
            }

            var userName = ReadUsername();
            var userPassword = ReadPassword();
            var client = GetAuthenticatedGraphClient(config, userName, userPassword);

            ViewFiles(client);
            // DownloadFiles(client);
            // UploadFiles(client);
            // UploadLargeFiles(client);
            // FilesAccessedByUser(client);
            // FilesTrendingAroundUser(client);
        }
    }
}

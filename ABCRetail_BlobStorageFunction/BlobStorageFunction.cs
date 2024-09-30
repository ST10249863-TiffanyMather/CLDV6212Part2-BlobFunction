using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;
using System;
using Azure.Storage.Blobs.Models;
using Azure;

namespace ABCRetail_BlobStorageFunction
{
    public class BlobStorageFunction
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobStorageFunction()
        {
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            //initialize BlobServiceClient using connection string
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        //****************
        //Code Attribution
        //The following coode was taken from StackOverflow:
        //Author: Claire Furney
        //Link: https://stackoverflow.com/questions/60899947/read-blob-storage-azure-function-httptrigger
        //****************

        //function that handles HTTP POST requests to upload data to Blob Storage
        [Function("UploadImageFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            //Logger instance to log information about function's execution
            var logger = executionContext.GetLogger("UploadToBlob");
            logger.LogInformation("Uploading to Blob Storage...");


            try
            {
                // Get a BlobContainerClient for "products"
                var containerClient = _blobServiceClient.GetBlobContainerClient("products");

                await containerClient.CreateIfNotExistsAsync();

                //get original filename from headers
                if (!req.Headers.TryGetValues("file-name", out var fileNameValues))
                {
                    throw new Exception("File name is missing in the request headers.");
                }

                string originalFileName = fileNameValues.FirstOrDefault();
                if (string.IsNullOrEmpty(originalFileName))
                {
                    throw new Exception("Invalid file name.");
                }

                var blobClient = containerClient.GetBlobClient(originalFileName);

                //upload HTTP request body stream to blob storage
                using (var stream = req.Body)
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                //success response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"Blob '{originalFileName}' uploaded successfully.");
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error uploading to Blob Storage: {ex.Message}");

                //error response
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Failed to upload blob.");
                return errorResponse;
            }
        }
    }
}
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using DotLiquid;
using System.Text;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Azure.Storage.Blobs;

namespace LiquidTransform.functionapp.v2
{
    public static class LiquidTransformer
    {
        /// <summary>
        /// Converts Json to XML using a Liquid mapping. The filename of the liquid map needs to be provided in the path. 
        /// The tranformation is executed with the HTTP request body as input.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="inputBlob"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("LiquidTransformer")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "liquidtransformer/{liquidtransformfilename}")] HttpRequest req,
            string liquidtransformfilename,
            [Blob("liquid-transforms/{liquidtransformfilename}", FileAccess.Read)] Stream inputBlob,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string storageOverride = req.Query["storage"];
            if(!String.IsNullOrEmpty(storageOverride))
            {
                try 
                {
                    BlobServiceClient blobServiceClient = new BlobServiceClient(storageOverride);
                    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("liquid-transforms");
                    BlobClient blobClient = containerClient.GetBlobClient(liquidtransformfilename);
                    MemoryStream blobStream = new MemoryStream();
                    blobClient.DownloadTo(blobStream);
                    inputBlob = blobStream;
                    inputBlob.Position = 0;
                }
                catch(Exception ex)
                {
                    log.LogError(ex.Message, ex);
                    return new BadRequestObjectResult("Error getting liquid transform: " + ex);
                }
            }

            if (inputBlob == null)
            {
                log.LogError("inputBlob null");
                return new NotFoundObjectResult("Liquid transform not found");
            }


            // This indicates the response content type. If set to application/json it will perform additional formatting
            // Otherwise the Liquid transform is returned unprocessed.
            string requestContentType = req.ContentType;
            string responseContentType = req.Headers["Accept"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Load the Liquid transform in a string
            var sr = new StreamReader(inputBlob);
            var liquidTransform = sr.ReadToEnd();

            var contentReader = ContentFactory.GetContentReader(requestContentType);
            var contentWriter = ContentFactory.GetContentWriter(responseContentType);

            Hash inputHash;

            try
            {
                inputHash = contentReader.ParseRequestAsync(requestBody);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);
                return new BadRequestObjectResult("Error parsing request body: " + ex);
            }

            // Register the Liquid custom filter extensions
            Template.RegisterFilter(typeof(CustomFilters));

            // Execute the Liquid transform
            Template template;

            try
            {
                template = Template.Parse(liquidTransform);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);
                return new BadRequestObjectResult("Error parsing Liquid template: " + ex);
            }

            string output = string.Empty;

            try
            {
                output = template.Render(inputHash);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);
                return new BadRequestObjectResult("Error rendering Liquid template: " + ex);
            }

            if (template.Errors != null && template.Errors.Count > 0)
            {
                if (template.Errors[0].InnerException != null)
                {
                    return new BadRequestObjectResult(String.Format("Error rendering Liquid template: {0}", template.Errors[0].InnerException));
                }
                else
                {
                    return new BadRequestObjectResult(String.Format("Error rendering Liquid template: {0}", template.Errors[0].Message));
                }
            }

            try
            {
                var content = contentWriter.CreateResponse(output);

                return new OkObjectResult(content);
            }
            catch (Exception ex)
            {
                // Just log the error, and return the Liquid output without parsing
                log.LogError(ex.Message, ex);

                return new OkObjectResult(output);
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TennisBookings.ResultsProcessing;
using TennisBookings.Web.BackgroundServices;

namespace TennisBookings.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ResultsController : Controller
    {
        private readonly IResultProcessor _resultProcessor;
        private readonly ILogger<ResultsController> _logger;
        private readonly FileProcessingChannel _fileProcessingChannel;

        public ResultsController(
            IResultProcessor resultProcessor,
            ILogger<ResultsController> logger,
            //This is where we inject the FileProcessingChannel
            FileProcessingChannel fileProcessingChannel)
        {
            _resultProcessor = resultProcessor;
            _logger = logger;
            _fileProcessingChannel = fileProcessingChannel;
        }

        [HttpGet]
        public IActionResult UploadResults()
        {
            return View();
        }

        [HttpGet("v2")]
        public IActionResult UploadResultsV2()
        {
            return View();
        }

        [HttpPost("FileUpload")]
        public async Task<IActionResult> FileUpload(IFormFile file)
        {
            var sw = Stopwatch.StartNew();

            if (file is object && file.Length > 0)
            {
                var fileName = Path.GetTempFileName(); // Upload to a temp file path

                await using var stream = new FileStream(fileName, FileMode.Create);

                await file.CopyToAsync(stream);

                stream.Position = 0;

                await _resultProcessor.ProcessAsync(stream);

                System.IO.File.Delete(fileName); // Delete the temp file
            }

            sw.Stop();

            _logger.LogInformation($"Time taken for result upload and processing " +
                $"was {sw.ElapsedMilliseconds}ms.");

            return RedirectToAction("UploadComplete");
        }

        [HttpPost("FileUploadV2")]
        public async Task<IActionResult> FileUploadV2(IFormFile file, CancellationToken cancellationToken)
        {
            //This times the response.
            var sw = Stopwatch.StartNew();

            //This block of code helps to confirm that the file contains data
            if (file is object && file.Length > 0)
            {
                //This creates a temporary file name for the file
                var fileName = Path.GetTempFileName();

                //This copies creates, writes and copies data from the temporary file.
                using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    await file.CopyToAsync(stream, cancellationToken);
                }

                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                //This ensures that if the program can't handle the request after 3 seconds it is terminated
                cts.CancelAfter(TimeSpan.FromSeconds(3)); // wait max 3 seconds

                try
                {
                    //This is where we try to write the file name into the FileProcessingChannel
                    var fileWritten = await _fileProcessingChannel.AddFileAsync(fileName, cts.Token);

                    if (fileWritten)
                    {
                        //after the file is written we stop the timer and we measure the time taken.
                        sw.Stop();

                        _logger.LogInformation($"Time for result upload was {sw.ElapsedMilliseconds}ms.");

                        return RedirectToAction("UploadComplete");
                    }
                }
                //If the file doesn't send, we delete it and classify it as an upload failure.
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    System.IO.File.Delete(fileName); // Delete the temp file to cleanup
                }
            }

            return RedirectToAction("UploadFailed");
        }

        [HttpGet("FileUploadComplete")]
        public IActionResult UploadComplete()
        {
            return View();
        }

        [HttpGet("FileUploadFailed")]
        public IActionResult UploadFailed()
        {
            return View();
        }
    }
}

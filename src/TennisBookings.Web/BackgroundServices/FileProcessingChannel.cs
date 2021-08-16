using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TennisBookings.Web.BackgroundServices
{
    public class FileProcessingChannel
    {
        //The line of code below specifies the maximum number of messages that can be transmitted through the channel at any point or the other.
        private const int MaxMessagesInChannel = 100;

        private readonly Channel<string> _channel;
        private readonly ILogger<FileProcessingChannel> _logger;

        public FileProcessingChannel(ILogger<FileProcessingChannel> logger)
        {
            //This is where we specifiy that we can have multiple people uploading files
            //The BoundedChannelOption is what ensures that users wait if there the maximum number of requests on the channel has been read.
            var options = new BoundedChannelOptions(MaxMessagesInChannel)
            {
                SingleWriter = false,
                SingleReader = true                
            };

            //This is where we acccept the temporary file name of the uploaded file (as a string)
            _channel = Channel.CreateBounded<string>(options);

            _logger = logger;
        }

        //This handles the process of adding a file, the filename is accepted as a parameter along with a cancellation token
        public async Task<bool> AddFileAsync(string fileName, CancellationToken ct = default)
        {
            //This code block ensures that the file will be written to the channel as long as the maximum limit hasn't been reached or it will wait for space to be made available
            while (await _channel.Writer.WaitToWriteAsync(ct) && !ct.IsCancellationRequested)
            {
                if (_channel.Writer.TryWrite(fileName))
                {
                    Log.ChannelMessageWritten(_logger, fileName);

                    return true;
                }
            }

            return false;
        }

        //This method below simply calls and returns the IAsyncEnumerable from the read or Async method that is exposed to the file processing channel reader.
        //Note that the file processing channel is registered as a singleton service within the dependency injection container for this web app.
        public IAsyncEnumerable<string> ReadAllAsync(CancellationToken ct = default) =>
            _channel.Reader.ReadAllAsync(ct);

        public bool TryCompleteWriter(Exception ex = null) => _channel.Writer.TryComplete(ex);

        internal static class EventIds
        {
            public static readonly EventId ChannelMessageWritten = new EventId(100, "ChannelMessageWritten");
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _channelMessageWritten = LoggerMessage.Define<string>(
                LogLevel.Information,
                EventIds.ChannelMessageWritten,
                "Filename {FileName} was written to the channel.");

            public static void ChannelMessageWritten(ILogger logger, string fileName)
            {
                _channelMessageWritten(logger, fileName, null);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly:InternalsVisibleTo("Selenium.Spotfire.Tests")]
namespace Selenium.Spotfire
{
    /// <summary>
    /// A simple HTTP server. We use this to serve up a page that uses the Spotfire JavaScript API - it isn't possible to interact with the API from a file based page
    /// Will serve up requests for any embedded resource from the assembly within the SpotfireWrapper folder.
    /// Ignores directory names etc.
    /// Only handles a single request at a time
    /// Runs automatically on test assembly startup
    /// </summary>
    static internal class SpotfireWrapperServer
    {
        public static string Port { get; private set; }

        private const string localListenerURL = @"http://+:";
        private static HttpListener Listener;
        private static readonly Dictionary<string, byte[]> Content = new Dictionary<string, byte[]>();

        // We keep count of how many times we're asked to start/stop so that we only stop when everyone is finished with us
        // Note there is no attempt to make this thread safe, just 
        private static long ReferenceCount;

        /// <summary>
        /// Starts the server and caches all the resources ready to send them
        /// </summary>
        /// <param name="testContext"></param>
        public static void StartupServer()
        {
            ReferenceCount++;

            if (ReferenceCount != 1)
            {
                return;
            }
            int portNumber = 8080;
            bool success = false;

            do
            {
                Port = portNumber.ToString();
                try
                {
                    Listener = new HttpListener();
                    Listener.Prefixes.Add(uriPrefix: $"{localListenerURL}{Port}/");
                    Listener.Start();
                    success = true;
                }
                catch
                {
                    portNumber++;
                    if (portNumber > 9000)
                    {
                        // something very odd going on, so just throw the error
                        throw;
                    }
                }
            } while (!success);


            // only load the content cache once
            if (Content.Count == 0)
            {
                foreach (string file in Assembly.GetExecutingAssembly().GetManifestResourceNames())
                {
                    if (file.Contains(".SpotfireWrapper."))
                    {
                        Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(file);
                        using (var reader = new StreamReader(stream))
                        {
                            string fileContent = reader.ReadToEnd();
                            string[] filenameParts = file.Split('.');

                            Content.Add((filenameParts[filenameParts.Length - 2] + "." + filenameParts[filenameParts.Length - 1]).ToLower(CultureInfo.InvariantCulture),
                                System.Text.Encoding.UTF8.GetBytes(fileContent));
                        }
                    }
                }
            }

            // Asynchronously handle requests
            Listener.BeginGetContext(HandleRequests, Listener);
        }

        /// <summary>
        /// Handle a request. Look for a matching resource file and send it
        /// </summary>
        private static void HandleRequests(IAsyncResult res)
        {
            HttpListener listener = (HttpListener)res.AsyncState;
            if (listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = listener.EndGetContext(res);
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    string requestFile = request.Url.AbsolutePath.ToLower(CultureInfo.InvariantCulture);
                    requestFile = requestFile.Substring(requestFile.LastIndexOf('/') + 1);
                    // Construct a response.
                    if (Content.ContainsKey(requestFile))
                    {
                        byte[] buffer = Content[requestFile];

                        // Get a response stream and write the response to it.
                        response.ContentLength64 = buffer.Length;
                        System.IO.Stream output = response.OutputStream;
                        output.Write(buffer, 0, buffer.Length);
                        // You must close the output stream.
                        output.Close();
                    }
                    else
                    {
                        response.StatusCode = 404;
                        response.OutputStream.Close();
                    }

                    listener.BeginGetContext(HandleRequests, listener);
                }
                catch (HttpListenerException)
                {
                    // Eat it - the listener has been stopped
                }
                catch (ObjectDisposedException)
                {
                    // Eat it - a different condition when the listener has stopped
                }
            }
        }

        /// <summary>
        /// Stop the server
        /// </summary>
        public static void StopServer()
        {
            ReferenceCount--;
            if (ReferenceCount <= 0)
            {
                Listener.Stop();
                Listener = null;
                ReferenceCount = 0;
            }
        }
    }
}

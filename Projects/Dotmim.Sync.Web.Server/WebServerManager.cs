﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dotmim.Sync.Web.Server
{
    /// <summary>
    /// Contains all providers registered on the server side
    /// </summary>
    public class WebServerManager : ICollection<WebServerOrchestrator>, IList<WebServerOrchestrator>
    {
        private List<WebServerOrchestrator> innerCollection = new List<WebServerOrchestrator>();
        public IMemoryCache Cache { get; }
        public IHostingEnvironment Environment { get; }

        public string Hint { get; set; }

        public WebServerManager(IMemoryCache cache, IHostingEnvironment env)
        {
            this.Cache = cache;
            this.Environment = env;
        }


        /// <summary>
        /// Habdle request
        /// </summary>
        public async Task HandleRequestAsync(HttpContext context, CancellationToken cancellationToken = default, IProgress<ProgressArgs> progress = null)
        {

            if (context.Request.Method.ToLowerInvariant() == "get")
            {
                await this.WriteHelloAsync(context, cancellationToken);
                return;
            }

            if (!WebServerOrchestrator.TryGetHeaderValue(context.Request.Headers, "dotmim-sync-scope-name", out var scopeName))
                throw new HttpHeaderMissingExceptiopn("dotmim-sync-scope-name");

            await this[scopeName].HandleRequestAsync(context, cancellationToken, progress).ConfigureAwait(false);
        }



        /// <summary>
        /// Add a new WebServerOrchestrator to the collection of WebServerOrchestrator
        /// </summary>
        public void Add(WebServerOrchestrator wsp)
        {
            if (innerCollection.Any(st => st.ScopeName == wsp.ScopeName))
                throw new Exception($"Scope {wsp.ScopeName} already exists in the collection");

            innerCollection.Add(wsp);
        }


        /// <summary>
        /// Get a WebServerOrchestrator by its scope name
        /// </summary>
        public WebServerOrchestrator this[string scopeName]
        {
            get
            {
                if (string.IsNullOrEmpty(scopeName))
                    throw new ArgumentNullException("scopeName");

                var wsp = innerCollection.FirstOrDefault(c => c.ScopeName.Equals(scopeName, SyncGlobalization.DataSourceStringComparison));

                if (wsp == null)
                    throw new ArgumentNullException($"Scope name {scopeName} does not exists");

                return wsp;
            }
        }

        /// <summary>
        /// Get a WebServerOrchestrator by its scope name
        /// </summary>
        public WebServerOrchestrator GetOrchestrator(string scopeName)
        {
            if (string.IsNullOrEmpty(scopeName))
                throw new ArgumentNullException("scopeName");

            var wsp = innerCollection.FirstOrDefault(c => c.ScopeName.Equals(scopeName, SyncGlobalization.DataSourceStringComparison));

            if (wsp == null)
                throw new ArgumentNullException($"Scope name {scopeName} does not exists");

            return wsp;
        }

        /// <summary>
        /// Get a WebServerOrchestrator with Scope name == SyncOptions.
        /// </summary>
        public WebServerOrchestrator GetOrchestrator(HttpContext context)
        {
            if (!WebServerOrchestrator.TryGetHeaderValue(context.Request.Headers, "dotmim-sync-scope-name", out var scopeName))
                throw new HttpHeaderMissingExceptiopn("dotmim-sync-scope-name");

            return GetOrchestrator(scopeName);

        }


        private async Task WriteHelloAsync(HttpContext context, CancellationToken cancellationToken)
        {
            var httpResponse = context.Response;
            var stringBuilder = new StringBuilder();

            if (this.Environment != null && this.Environment.IsDevelopment())
            {
                stringBuilder.AppendLine("<!doctype html>");
                stringBuilder.AppendLine("<html>");
                stringBuilder.AppendLine("<head>");
                stringBuilder.AppendLine("<meta charset='utf-8'>");
                stringBuilder.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1, shrink-to-fit=no'>");
                stringBuilder.AppendLine("<script src='https://cdn.jsdelivr.net/gh/google/code-prettify@master/loader/run_prettify.js'></script>");
                stringBuilder.AppendLine("<link rel='stylesheet' href='https://stackpath.bootstrapcdn.com/bootstrap/4.4.1/css/bootstrap.min.css' integrity='sha384-Vkoo8x4CGsO3+Hhxv8T/Q5PaXtkKtu6ug5TOeNV6gBiFeWPGFN9MuhOf23Q9Ifjh' crossorigin='anonymous'>");
                stringBuilder.AppendLine("</head>");
                stringBuilder.AppendLine("<title>Web Server properties</title>");
                stringBuilder.AppendLine("<body>");


                stringBuilder.AppendLine("<div class='container'>");
                stringBuilder.AppendLine("<h2>Web Server properties</h2>");

                foreach (var webOrchestrator in this)
                {

                    stringBuilder.AppendLine("<ul class='list-group mb-2'>");
                    //stringBuilder.AppendLine("<div class='row'><div class='col'>");
                    //stringBuilder.AppendLine($"<span class='d-block p-2 bg-primary text-white'>ScopeName: {webOrchestrator.ScopeName}</span>");
                    stringBuilder.AppendLine($"<li class='list-group-item active'>ScopeName: {webOrchestrator.ScopeName}</li>");
                    stringBuilder.AppendLine("</ul>");

                    var s = JsonConvert.SerializeObject(webOrchestrator.Setup, Formatting.Indented);
                    stringBuilder.AppendLine("<ul class='list-group mb-2'>");
                    stringBuilder.AppendLine($"<li class='list-group-item list-group-item-primary'>Setup</li>");
                    stringBuilder.AppendLine($"<li class='list-group-item list-group-item-light'>");
                    stringBuilder.AppendLine("<pre class='prettyprint' style='border:0px;font-size:75%'>");
                    stringBuilder.AppendLine(s);
                    stringBuilder.AppendLine("</pre>");
                    stringBuilder.AppendLine("</li>");
                    stringBuilder.AppendLine("</ul>");

                    s = JsonConvert.SerializeObject(webOrchestrator.Provider, Formatting.Indented);
                    stringBuilder.AppendLine("<ul class='list-group mb-2'>");
                    stringBuilder.AppendLine($"<li class='list-group-item list-group-item-primary'>Provider</li>");
                    stringBuilder.AppendLine($"<li class='list-group-item list-group-item-light'>");
                    stringBuilder.AppendLine("<pre class='prettyprint' style='border:0px;font-size:75%'>");
                    stringBuilder.AppendLine(s);
                    stringBuilder.AppendLine("</pre>");
                    stringBuilder.AppendLine("</li>");
                    stringBuilder.AppendLine("</ul>");

                    s = JsonConvert.SerializeObject(webOrchestrator.Options, Formatting.Indented);
                    stringBuilder.AppendLine("<ul class='list-group mb-2'>");
                    stringBuilder.AppendLine($"<li class='list-group-item list-group-item-primary'>Options</li>");
                    stringBuilder.AppendLine($"<li class='list-group-item list-group-item-light'>");
                    stringBuilder.AppendLine("<pre class='prettyprint' style='border:0px;font-size:75%'>");
                    stringBuilder.AppendLine(s);
                    stringBuilder.AppendLine("</pre>");
                    stringBuilder.AppendLine("</li>");
                    stringBuilder.AppendLine("</ul>");

                    s = JsonConvert.SerializeObject(webOrchestrator.WebServerOptions, Formatting.Indented);
                    stringBuilder.AppendLine("<ul class='list-group mb-2'>");
                    stringBuilder.AppendLine($"<li class='list-group-item list-group-item-primary'>Web Server Options</li>");
                    stringBuilder.AppendLine($"<li class='list-group-item list-group-item-light'>");
                    stringBuilder.AppendLine("<pre class='prettyprint' style='border:0px;font-size:75%'>");
                    stringBuilder.AppendLine(s);
                    stringBuilder.AppendLine("</pre>");
                    stringBuilder.AppendLine("</li>");
                    stringBuilder.AppendLine("</ul>");



                }
                stringBuilder.AppendLine("</div>");
                stringBuilder.AppendLine("</body>");
                stringBuilder.AppendLine("</html>");

            }
            else
            {
                stringBuilder.AppendLine("<div>Server is configured to Production mode. No options displayed.</div>");
            }

            await httpResponse.WriteAsync(stringBuilder.ToString(), cancellationToken);


        }


        public void Clear() => this.innerCollection.Clear();
        public WebServerOrchestrator this[int index] => innerCollection[index];
        public int Count => innerCollection.Count;
        public bool IsReadOnly => false;
        WebServerOrchestrator IList<WebServerOrchestrator>.this[int index] { get => this.innerCollection[index]; set => this.innerCollection[index] = value; }
        public bool Remove(WebServerOrchestrator item) => innerCollection.Remove(item);
        public bool Contains(WebServerOrchestrator item) => innerCollection.Any(st => st.ScopeName.Equals(item.ScopeName, SyncGlobalization.DataSourceStringComparison));
        public bool Contains(string scopeName) => innerCollection.Any(st => st.ScopeName.Equals(scopeName, SyncGlobalization.DataSourceStringComparison));
        public void CopyTo(WebServerOrchestrator[] array, int arrayIndex) => innerCollection.CopyTo(array, arrayIndex);
        public int IndexOf(WebServerOrchestrator item) => innerCollection.IndexOf(item);
        public void RemoveAt(int index) => innerCollection.RemoveAt(index);
        public override string ToString() => this.innerCollection.Count.ToString();
        public void Insert(int index, WebServerOrchestrator item) => this.innerCollection.Insert(index, item);
        public IEnumerator<WebServerOrchestrator> GetEnumerator() => innerCollection.GetEnumerator();
        IEnumerator<WebServerOrchestrator> IEnumerable<WebServerOrchestrator>.GetEnumerator() => this.innerCollection.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.innerCollection.GetEnumerator();

    }
}

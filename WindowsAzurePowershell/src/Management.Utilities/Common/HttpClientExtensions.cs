﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Management.Utilities.Common
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Xml.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class HttpClientExtensions
    {
        private static void AddUserAgent(HttpClient client)
        {
            if (!client.DefaultRequestHeaders.UserAgent.Contains(ApiConstants.UserAgentValue))
            {
                client.DefaultRequestHeaders.UserAgent.Add(ApiConstants.UserAgentValue);
            }
        }

        private static void LogResponse(
            string statusCode,
            HttpResponseHeaders headers,
            string content,
            Action<string> logger)
        {
            if (logger != null)
            {
                logger(General.GetHttpResponseLog(statusCode, headers, content));
            }
        }

        private static void LogRequest(
            string method,
            string requestUri,
            HttpRequestHeaders headers,
            string body,
            Action<string> logger)
        {
            if (logger != null)
            {
                logger(General.GetHttpRequestLog(method, requestUri, headers, body));
            }
        }

        private static T GetFormat<T>(
            HttpClient client,
            string requestUri,
            Action<string> logger,
            Func<string, string> formatter,
            Func<string, T> serializer)
            where T: class, new()
        {
            AddUserAgent(client);
            LogRequest(
                HttpMethod.Get.Method,
                client.BaseAddress + requestUri,
                client.DefaultRequestHeaders,
                string.Empty,
                logger);
            HttpResponseMessage response = client.GetAsync(requestUri).Result;
            string content = response.EnsureSuccessStatusCode().Content.ReadAsStringAsync().Result;
            LogResponse(response.StatusCode.ToString(), response.Headers, formatter(content), logger);

            try 
            {	        
                return serializer(content);
            }
            catch (Exception)
            {
                return new T();
            }
        }

        private static string GetRawBody(
            HttpClient client,
            string requestUri,
            Action<string> logger,
            Func<string, string> formatter)
        {
            AddUserAgent(client);
            LogRequest(
                HttpMethod.Get.Method,
                client.BaseAddress + requestUri,
                client.DefaultRequestHeaders,
                string.Empty,
                logger);
            HttpResponseMessage response = client.GetAsync(requestUri).Result;
            string content = response.EnsureSuccessStatusCode().Content.ReadAsStringAsync().Result;
            LogResponse(response.StatusCode.ToString(), response.Headers, formatter(content), logger);

            return content;
        }

        private static T CallRestApiWithJsonPayload<T>(
            this HttpClient client,
            string requestUri,
            T json,
            Action<string> logger,
            string method)
        {
            AddUserAgent(client);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            LogRequest(
                method,
                client.BaseAddress + requestUri,
                client.DefaultRequestHeaders,
                JsonConvert.SerializeObject(json, Formatting.Indented),
                logger);
            HttpResponseMessage response;

            switch (method.ToUpper())
            {
                case WebRequestMethods.Http.Post: response = client.PostAsJsonAsync(requestUri, json).Result; break;
                case WebRequestMethods.Http.Put: response = client.PutAsJsonAsync(requestUri, json).Result; break;
                default: throw new InvalidOperationException();
            }

            string content = response.EnsureSuccessStatusCode().Content.ReadAsStringAsync().Result;
            LogResponse(
                response.StatusCode.ToString(),
                response.Headers,
                General.TryFormatJson(content),
                logger);

            return JsonConvert.DeserializeObject<T>(content);
        }

        public static T GetJson<T>(this HttpClient client, string requestUri, Action<string> logger)
            where T : class, new()
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return GetFormat<T>(client, requestUri, logger, General.TryFormatJson, JsonConvert.DeserializeObject<T>);
        }

        public static string GetXml(this HttpClient client, string requestUri, Action<string> logger)
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            return GetRawBody(client, requestUri, logger, General.FormatXml);
        }

        public static T GetXml<T>(this HttpClient client, string requestUri, Action<string> logger)
            where T: class, new()
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            return GetFormat<T>(client, requestUri, logger, General.FormatXml, General.DeserializeXmlString<T>);
        }

        public static T PostJson<T>(
            this HttpClient client,
            string requestUri,
            T json,
            Action<string> logger)
        {
            return CallRestApiWithJsonPayload<T>(client, requestUri, json, logger, WebRequestMethods.Http.Post);
        }

        public static T PutJson<T>(
            this HttpClient client,
            string requestUri,
            T json,
            Action<string> logger)
        {
            return CallRestApiWithJsonPayload<T>(client, requestUri, json, logger, WebRequestMethods.Http.Put);
        }

        public static void Delete(this HttpClient client, string requestUri, Action<string> logger)
        {
            AddUserAgent(client);
            LogRequest(
                HttpMethod.Delete.Method,
                client.BaseAddress + requestUri,
                client.DefaultRequestHeaders,
                string.Empty,
                logger);
            HttpResponseMessage response = client.DeleteAsync(requestUri).Result;
            string content = response.EnsureSuccessStatusCode().Content.ReadAsStringAsync().Result;
            LogResponse(response.StatusCode.ToString(), response.Headers, content, logger);
        }
		
		public static Task<HttpResponseMessage> GetAsync(this HttpClient client, string requestUri, Action<string> Logger)
        {
            AddUserAgent(client);
			LogRequest(
				HttpMethod.Get.Method,
				client.BaseAddress + requestUri,
				client.DefaultRequestHeaders,
				string.Empty,
				Logger);
			return client.GetAsync(requestUri);
        }

        public static Task<HttpResponseMessage> PostAsJsonAsyncWithoutEnsureSuccessStatusCode(
            this HttpClient client,
            string requestUri,
            JObject json,
            Action<string> Logger)
        {
            AddUserAgent(client);

            LogRequest(
                HttpMethod.Post.Method,
                client.BaseAddress + requestUri,
                client.DefaultRequestHeaders,
                JsonConvert.SerializeObject(json, Formatting.Indented),
                Logger);
            return client.PostAsJsonAsync(requestUri, json);
        }
    }
}

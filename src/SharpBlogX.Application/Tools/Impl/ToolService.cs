﻿using IP2Region;
using SharpBlogX.Dto.Tools.Params;
using SharpBlogX.Extensions;
using SharpBlogX.Options;
using SharpBlogX.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TencentCloud.Cdn.V20180606;
using TencentCloud.Cdn.V20180606.Models;
using TencentCloud.Common;
using TencentCloud.Common.Profile;
using Microsoft.Extensions.Logging;

namespace SharpBlogX.Tools.Impl
{
    public class ToolService : ServiceBase, IToolService
    {
        private readonly IHttpClientFactory _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly TencentCloudOptions _tencentCloudOptions;
        private readonly NotificationOptions _notificationOptions;
        private readonly ILogger<ToolService> _logger;

        public ToolService(IHttpClientFactory httpClient,
                           IHttpContextAccessor httpContextAccessor,
                           IOptions<TencentCloudOptions> tencentCloudOptions,
                           IOptions<NotificationOptions> notificationOptions,
                           ILogger<ToolService> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _tencentCloudOptions = tencentCloudOptions.Value;
            _notificationOptions = notificationOptions.Value;
            _logger = logger;
        }

        /// <summary>
        /// Get bing background url.
        /// </summary>
        /// <returns></returns>
        [Route("api/tool/bing/url")]
        public async Task<BlogResponse<string>> GetBingBackgroundUrlAsync()
        {
            var response = new BlogResponse<string>();

            var api = "https://cn.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&pid=hp&FORM=BEHPTB";

            using var client = _httpClient.CreateClient();
            var json = await client.GetStringAsync(api);

            var obj = JObject.Parse(json);
            var url = $"https://cn.bing.com{obj["images"].First()["url"]}";

            response.Result = url;
            return response;
        }

        /// <summary>
        /// Get bing background image.
        /// </summary>
        /// <returns></returns>
        [Route("api/tool/bing/img")]
        public async Task<FileContentResult> GetBingBackgroundImgAsync()
        {
            var url = (await GetBingBackgroundUrlAsync()).Result;

            using var client = _httpClient.CreateClient();
            var bytes = await client.GetByteArrayAsync(url);

            return new FileContentResult(bytes, "image/jpeg");
        }

        /// <summary>
        /// Get the region by ip address.
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/tool/ip2region")]
        public async Task<BlogResponse<List<string>>> Ip2RegionAsync(string ip)
        {
            var response = new BlogResponse<List<string>>();

            if (ip.IsNullOrEmpty())
            {
                ip = _httpContextAccessor.HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault() ??
                     _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
                     _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            }
            else
            {
                if (!ip.IsIp())
                {
                    response.IsFailed("The ip address error.");
                    return response;
                }
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), "Resources/ip2region.db");

            using var _search = new DbSearcher(path);
            var block = await _search.BinarySearchAsync(ip);

            var region = block.Region.Split("|").Distinct().Where(x => x != "0").ToList();

            region.AddFirst(ip);

            response.Result = region;
            return response;
        }

        /// <summary>
        /// Send a message to weixin.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [Route("api/tool/send")]
        public async Task<BlogResponse> SendMessageAsync(SendMessageInput input)
        {
            var response = new BlogResponse();

            var content = new StringContent($"text={input.Text}&desp={input.Desc}");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            using var client = _httpClient.CreateClient();
            _logger.LogInformation($"FtqqUrl: {_notificationOptions.FtqqUrl}");
            await client.PostAsync(_notificationOptions.FtqqUrl, content);

            return response;
        }

        /// <summary>
        /// Get img by url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        [Route("api/tool/img")]
        public async Task<FileContentResult> GetImgAsync([Required] string url)
        {
            using var client = _httpClient.CreateClient();
            var bytes = await client.GetByteArrayAsync(url);

            return new FileContentResult(bytes, "image/jpeg");
        }

        /// <summary>
        /// Purge the cdn url cache.
        /// </summary>
        /// <param name="urls"></param>
        /// <returns></returns>
        [Route("api/tool/cdn/purge/url")]
        public async Task<BlogResponse<PurgeUrlsCacheResponse>> PurgeCdnUrlsAsync(List<string> urls)
        {
            var result = new BlogResponse<PurgeUrlsCacheResponse>();

            var parameters = new { Urls = urls }.SerializeToJson();
            DoCdnAction(out CdnClient client, out PurgeUrlsCacheRequest req, parameters);

            var resp = await client.PurgeUrlsCache(req);

            result.IsSuccess(resp);
            return result;
        }

        /// <summary>
        /// Purge the cdn path cache.
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        [Route("api/tool/cdn/purge/path")]
        public async Task<BlogResponse<PurgePathCacheResponse>> PurgeCdnPathsAsync(List<string> paths)
        {
            var result = new BlogResponse<PurgePathCacheResponse>();

            var parameters = new { Paths = paths, FlushType = "flush" }.SerializeToJson();
            DoCdnAction(out CdnClient client, out PurgePathCacheRequest req, parameters);

            var resp = await client.PurgePathCache(req);

            result.IsSuccess(resp);
            return result;
        }

        /// <summary>
        /// Push the cdn url cache.
        /// </summary>
        /// <param name="urls"></param>
        /// <returns></returns>
        [Route("api/tool/cdn/push/url")]
        public async Task<BlogResponse<PushUrlsCacheResponse>> PushCdnUrlsAsync(List<string> urls)
        {
            var result = new BlogResponse<PushUrlsCacheResponse>();

            var parameters = new { Urls = urls }.SerializeToJson();
            DoCdnAction(out CdnClient client, out PushUrlsCacheRequest req, parameters);

            var resp = await client.PushUrlsCache(req);

            result.IsSuccess(resp);
            return result;
        }

        private void DoCdnAction<T>(out CdnClient client, out T req, string json)
        {
            var cred = new Credential
            {
                SecretId = _tencentCloudOptions.SecretId,
                SecretKey = _tencentCloudOptions.SecretKey
            };
            var httpProfile = new HttpProfile { Endpoint = $"cdn.tencentcloudapi.com" };

            var clientProfile = new ClientProfile { HttpProfile = httpProfile };

            client = new CdnClient(cred, "", clientProfile);
            req = json.DeserializeToObject<T>();
        }
    }
}
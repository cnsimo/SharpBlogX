﻿using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace SharpBlogX.Extensions
{
    public static class Extensions
    {
        /// <summary>
        /// Convert object to json string
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string SerializeToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.None, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        /// <summary>
        /// Convert json string to <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T DeserializeToObject<T>(this string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// String to <see cref="ObjectId"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ObjectId ToObjectId(this string id)
        {
            return new ObjectId(id);
        }

        /// <summary>
        /// The string time format is converted to DateTime
        /// </summary>
        /// <param name="time"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this string time, DateTime defaultValue = default)
        {
            if (time.IsNullOrEmpty())
                return defaultValue;

            return DateTime.TryParse(time, out var dateTime) ? dateTime : defaultValue;
        }

        /// <summary>
        /// Generate post link
        /// </summary>
        /// <param name="url"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string GeneratePostUrl(this string url, DateTime time)
        {
            return $"{time:yyyy-MM-dd}-{url}";
        }

        /// <summary>
        /// Format time
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string FormatTime(this DateTime time)
        {
            return time.ToString("MMMM dd, yyyy HH:mm", new CultureInfo("en-us"));
        }

        /// <summary>
        /// Save the array type file to the specified path
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task DownloadAsync(this byte[] buffer, string path)
        {
            using var ms = new MemoryStream(buffer);
            using var stream = new FileStream(path, FileMode.Create);

            var bytes = new byte[1024];
            var size = await ms.ReadAsync(bytes.AsMemory(0, bytes.Length));
            while (size > 0)
            {
                await stream.WriteAsync(bytes.AsMemory(0, size));
                size = await ms.ReadAsync(bytes.AsMemory(0, bytes.Length));
            }
        }

        /// <summary>
        /// Get ip address
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string GetIpAddress(this HttpRequest request)
        {
            var ip = request.Headers["X-Real-IP"].FirstOrDefault() ??
                     request.Headers["X-Forwarded-For"].FirstOrDefault() ??
                     request.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            return ip;
        }

        /// <summary>
        /// Check the ip address
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static bool IsIp(this string ip)
        {
            var regex = new Regex(@"^(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])$");

            return regex.IsMatch(ip);
        }

        /// <summary>
        /// Convert <paramref name="dic"/> to query string
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public static string ToQueryString(this Dictionary<string, string> dic)
        {
            return dic.Select(x => $"{x.Key}={x.Value}").JoinAsString("&");
        }

        /// <summary>
        /// Convert <paramref name="dic"/> to query string with encode
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public static string ToQueryStringWithEncode(this Dictionary<string, string> dic)
        {
            return dic.Select(x => $"{HttpUtility.UrlEncode(x.Key, Encoding.UTF8)}={HttpUtility.UrlEncode(x.Value, Encoding.UTF8)}").JoinAsString("&");
        }

        /// <summary>
        /// Generate random code
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string GenerateRandomCode(this int length)
        {
            int rand;
            char code;
            var randomcode = string.Empty;
            var random = new Random();

            for (int i = 0; i < length; i++)
            {
                rand = random.Next();

                if (rand % 3 == 0)
                {
                    code = (char)('A' + (char)(rand % 26));
                }
                else
                {
                    code = (char)('0' + (char)(rand % 10));
                }

                randomcode += code.ToString();
            }
            return randomcode;
        }

        /// <summary>
        /// Convert <paramref name="timestamp"/> to <see cref="DateTime"/>
        /// </summary>
        /// <param name="timestamp">秒</param>
        /// <returns></returns>
        public static DateTime TimestampToDateTime(this string timestamp)
        {
            var date = DateTimeOffset.FromUnixTimeSeconds(long.Parse(timestamp));
            return date.DateTime.ToLocalTime();
        }

        /// <summary>
        /// Get data from json file
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static async Task<T> FromJsonFile<T>(this string filePath, string key = "") where T : new()
        {
            if (!File.Exists(filePath)) return new T();

            using StreamReader reader = new StreamReader(filePath);
            var json = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(key)) return JsonConvert.DeserializeObject<T>(json);

            return JsonConvert.DeserializeObject<object>(json) is not JObject obj ? new T() : JsonConvert.DeserializeObject<T>(obj[key].ToString());
        }

        /// <summary>
        /// Create self-signed certificate
        /// </summary>
        /// <param name="address"></param>
        /// <param name="DistinguishedName"></param>
        /// <returns></returns>
        public static X509Certificate2 CreateSelfSignedCertificate(this IPAddress address, string DistinguishedName = "")
        {
            if (DistinguishedName == "") { DistinguishedName = "CN=" + address; }
            using (RSA rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(new X500DistinguishedName(DistinguishedName), rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));
                request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
                SubjectAlternativeNameBuilder subjectAlternativeName = new SubjectAlternativeNameBuilder();
                subjectAlternativeName.AddIpAddress(address);

                request.CertificateExtensions.Add(subjectAlternativeName.Build());
                return request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));
            }
        }
    }
}
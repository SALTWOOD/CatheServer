using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatheServer.Modules.ApiRoutes
{
    public class IpRateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _memoryCache;

        public IpRateLimitingMiddleware(RequestDelegate next, IMemoryCache memoryCache)
        {
            _next = next;
            _memoryCache = memoryCache;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            if (ipAddress == null) return;
            var cacheEntry = _memoryCache.GetOrCreate(ipAddress, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return new IpRateLimitInfo { Count = 0, IsBanned = false };
            });

            if (cacheEntry.IsBanned)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("You are banned for 10 minutes due to exceeding the rate limit.");
                return;
            }

            cacheEntry.Count++;

            if (cacheEntry.Count > 10)
            {
                cacheEntry.IsBanned = true;
                _memoryCache.Set(ipAddress, cacheEntry, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("You are banned for 10 minutes due to exceeding the rate limit.");
                return;
            }

            _memoryCache.Set(ipAddress, cacheEntry);
            await _next(context);
        }
    }

    public struct IpRateLimitInfo
    {
        public int Count { get; set; }
        public bool IsBanned { get; set; }
    }
}

using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace CacheHeaderTestingApi.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class CacheHeaderTestController : ControllerBase
{
    private static readonly StatusCodeResult NotModifiedStatusCodeResult = new((int) HttpStatusCode.NotModified);
    private const int MaxDelayInMs = 1000;
    private const int MaxETagLength = 500;

    [HttpGet]
    public async Task<ActionResult<CacheHeaderTestResponse>> Get(ushort okResponseTimeMs = 200,
        ushort notModifiedResponseTimeMs = 50,
        ushort maxAge = 180,
        ushort sMaxAge = 120,
        ushort staleWhileRevalidate = 60,
        ushort staleWhileError = 300,
        ushort age = 5,
        bool mustRevalidate = false,
        bool proxyRevalidate = false,
        bool noCache = false,
        string eTag = "foo")
    {
        var stopWatch = Stopwatch.StartNew();
        if (eTag.Length > MaxETagLength)
            return BadRequest($"Max etag length is {MaxETagLength}");

        if (Request.Headers.IfNoneMatch == eTag)
        {
            if (notModifiedResponseTimeMs > MaxDelayInMs)
                return BadRequest($"Max delay in ms is {MaxDelayInMs}");
            long msToDelay304 = notModifiedResponseTimeMs - stopWatch.ElapsedMilliseconds;
            if (msToDelay304 > 0)
                await Task.Delay((int)msToDelay304).ConfigureAwait(false);
            Response.Headers[nameof(CacheHeaderTestResponse.ServerTimeTaken)] = stopWatch.Elapsed.ToString();
            return NotModifiedStatusCodeResult;
        }

        if (okResponseTimeMs > MaxDelayInMs)
            return BadRequest($"Max delay in ms is {MaxDelayInMs}");
        SetCacheHeaders(maxAge, sMaxAge, staleWhileRevalidate, staleWhileError, age, mustRevalidate, proxyRevalidate, noCache);

        Response.Headers.ETag = eTag;

        long msToDelay = okResponseTimeMs - stopWatch.ElapsedMilliseconds;
        if (msToDelay > 0)
            await Task.Delay((int)msToDelay).ConfigureAwait(false);

        return new CacheHeaderTestResponse
        {
            MaxAge = maxAge,
            SMaxAge = sMaxAge,
            StaleWhileError = staleWhileError,
            StaleWhileRevalidate = staleWhileRevalidate,
            ETag = eTag,
            ServerTimeTaken = stopWatch.Elapsed.ToString(),
        };
    }
    
    [HttpGet("LastModified")]
    public async Task<ActionResult<CacheHeaderTestResponse>> GetWithLastModified(ushort okResponseTimeMs = 200,
        ushort notModifiedResponseTimeMs = 50,
        ushort maxAge = 180,
        ushort sMaxAge = 120,
        ushort staleWhileRevalidate = 60,
        ushort staleWhileError = 300,
        ushort age = 5,
        bool mustRevalidate = false,
        bool proxyRevalidate = false,
        bool noCache = false,
        string lastModified = "Fri, 1 Apr 2022 16:52:15 GMT")
    {
        var stopWatch = Stopwatch.StartNew();
        if (lastModified.Length > MaxETagLength)
            return BadRequest($"Max lastModified length is {MaxETagLength}");

        if (Request.Headers.IfModifiedSince == lastModified)
        {
            if (notModifiedResponseTimeMs > MaxDelayInMs)
                return BadRequest($"Max delay in ms is {MaxDelayInMs}");
            long msToDelay304 = notModifiedResponseTimeMs - stopWatch.ElapsedMilliseconds;
            if (msToDelay304 > 0)
                await Task.Delay((int)msToDelay304).ConfigureAwait(false);
            Response.Headers[nameof(CacheHeaderTestResponse.ServerTimeTaken)] = stopWatch.Elapsed.ToString();
            return NotModifiedStatusCodeResult;
        }

        if (okResponseTimeMs > MaxDelayInMs)
            return BadRequest($"Max delay in ms is {MaxDelayInMs}");

        SetCacheHeaders(maxAge, sMaxAge, staleWhileRevalidate, staleWhileError, age, mustRevalidate, proxyRevalidate, noCache);

        Response.Headers.LastModified = lastModified;

        long msToDelay = okResponseTimeMs - stopWatch.ElapsedMilliseconds;
        if (msToDelay > 0)
            await Task.Delay((int)msToDelay).ConfigureAwait(false);

        return new CacheHeaderTestResponse
        {
            MaxAge = maxAge,
            SMaxAge = sMaxAge,
            StaleWhileError = staleWhileError,
            StaleWhileRevalidate = staleWhileRevalidate,
            LastModified = lastModified,
            ServerTimeTaken = stopWatch.Elapsed.ToString(),
        };
    }

    private void SetCacheHeaders(ushort maxAge, ushort sMaxAge, ushort staleWhileRevalidate, ushort staleWhileError,
        ushort age, bool mustRevalidate, bool proxyRevalidate, bool noCache)
    {
        string cacheControl = !noCache
            ? "public"
            : "no-cache"; // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Cache-Control#no-cache
        cacheControl += $", max-age={maxAge}, s-maxage={sMaxAge}";
        if (staleWhileRevalidate > 0)
            cacheControl += $", stale-while-revalidate={staleWhileRevalidate}";
        if (staleWhileError > 0)
            cacheControl += $", stale-while-error={staleWhileError}";
        if (mustRevalidate) // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Cache-Control#must-revalidate
            cacheControl += ", must-revalidate";
        else if (proxyRevalidate) // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Cache-Control#proxy-revalidate
            cacheControl += ", proxy-revalidate";
        Response.Headers.CacheControl = cacheControl;

        if (age != default)
            Response.Headers.Age = age.ToString();
    }
}
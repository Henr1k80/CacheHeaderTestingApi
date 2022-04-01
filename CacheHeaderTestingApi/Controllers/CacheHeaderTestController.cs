using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace CacheHeaderTestingApi.Controllers;

[ApiController]
[Route("[controller]")]
public class CacheHeaderTestController : ControllerBase
{
    private static readonly StatusCodeResult NotModifiedStatusCodeResult = new((int) HttpStatusCode.NotModified);
    private const int MaxDelayInMs = 1000;
    private const int MaxETagLength = 500;

    [HttpGet]
    public async Task<ActionResult<CacheHeaderTestResponse>> Get(ushort okResponseTimeMs = 150,
        ushort notModifiedResponseTimeMs = 50,
        ushort maxAge = 180,
        ushort sMaxAge = 120,
        ushort staleWhileRevalidate = 60,
        ushort staleWhileError = 300,
        string eTag = "foo")
    {
        var stopWatch = Stopwatch.StartNew();
        if (eTag.Length > MaxETagLength)
            return BadRequest($"Max etag length is {MaxETagLength}");

        if (Request.Headers.IfNoneMatch == eTag)
        {
            if (notModifiedResponseTimeMs > MaxDelayInMs)
                return BadRequest($"Max delay in ms is {MaxDelayInMs}");
            var timeToDelay304 = TimeSpan.FromMilliseconds(notModifiedResponseTimeMs) - stopWatch.Elapsed;
            await Task.Delay(timeToDelay304).ConfigureAwait(false);
            Response.Headers[nameof(CacheHeaderTestResponse.ServerTimeTaken)] = stopWatch.Elapsed.ToString();
            return NotModifiedStatusCodeResult;
        }

        if (okResponseTimeMs > MaxDelayInMs)
            return BadRequest($"Max delay in ms is {MaxDelayInMs}");
        Response.Headers.ETag = eTag;
        Response.Headers.CacheControl =
            $"public, max-age={maxAge}, s-maxage={sMaxAge}, stale-while-revalidate={staleWhileRevalidate}, stale-while-error={staleWhileError}";
        var timeToDelay = TimeSpan.FromMilliseconds(okResponseTimeMs) - stopWatch.Elapsed;
        await Task.Delay(timeToDelay).ConfigureAwait(false);
        return new CacheHeaderTestResponse
        {
            MaxAge = maxAge,
            SMaxAge = sMaxAge,
            StaleWhileError = staleWhileError,
            StaleWhileRevalidate = staleWhileRevalidate,
            ServerTimeTaken = stopWatch.Elapsed,
        };
    }
}
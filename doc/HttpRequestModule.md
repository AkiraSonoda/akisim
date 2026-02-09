# HttpRequestModule Technical Documentation

## Overview

The HttpRequestModule is a critical infrastructure component for OpenSimulator/Akisim that provides comprehensive HTTP client functionality for LSL scripts. This non-shared region module implements the backend for LSL HTTP functions like `llHTTPRequest()`, enabling scripts to communicate with external web services, APIs, and web servers. The module features advanced HTTP client capabilities including SSL/TLS support, request throttling, redirect handling, proxy support, and comprehensive security controls. It's essential for any scripting scenarios that require external web connectivity, making it a core component for modern virtual world applications.

## Architecture

The HttpRequestModule implements the following interfaces:
- `INonSharedRegionModule` - Per-region module lifecycle management
- `IHttpRequestModule` - HTTP request service interface contract

### Key Components

1. **HTTP Client Management**
   - **Dual Client Strategy**: Separate HTTP clients for certificate verification and non-verification scenarios
   - **Connection Pooling**: Configurable connection pooling for optimal performance
   - **SSL/TLS Configuration**: Modern SSL/TLS protocol support with certificate validation options
   - **Proxy Support**: Full proxy server support with exception handling

2. **Request Throttling System**
   - **Per-Object Throttling**: Individual throttling limits for each scripted object
   - **Per-Owner Throttling**: Aggregated throttling limits per object owner
   - **Burst Control**: Configurable burst limits with rate limiting
   - **Dynamic Adjustment**: Token bucket algorithm for smooth rate limiting

3. **Request Processing Engine**
   - **Asynchronous Processing**: Non-blocking HTTP request processing using JobEngine
   - **Redirect Handling**: Automatic HTTP redirect following with security validation
   - **Response Management**: Comprehensive response processing with size limits
   - **Error Handling**: Robust error handling with detailed status reporting

4. **Security and Filtering**
   - **URL Filtering**: Configurable outbound URL filtering for security
   - **Certificate Validation**: Optional SSL certificate validation
   - **Content Limits**: Configurable response body size limits
   - **Header Validation**: Proper HTTP header handling and validation

## Configuration

### Module Activation

Set in `[Modules]` section:
```ini
[Modules]
HttpRequestModule = true
```

### HTTP Client Configuration

Configure in `[ScriptsHttpRequestModule]` section:
```ini
[ScriptsHttpRequestModule]
MaxPoolThreads = 8                    ; Maximum worker threads for HTTP requests
PrimRequestsBurst = 3.0               ; Burst limit per object
PrimRequestsPerSec = 1.0              ; Requests per second per object
PrimOwnerRequestsBurst = 5.0          ; Burst limit per owner
PrimOwnerRequestsPerSec = 25.0        ; Requests per second per owner
RequestsTimeOut = 30000               ; Request timeout in milliseconds (200-60000)
```

### Network Configuration

Configure in `[Network]` section:
```ini
[Network]
HttpBodyMaxLenMAX = 16384             ; Maximum HTTP response body length
```

### Proxy Configuration

Configure in `[Startup]` section:
```ini
[Startup]
HttpProxy = http://proxy.example.com:8080
HttpProxyExceptions = localhost;127.0.0.1;*.local
```

### Default Behavior

- **Enabled by Default**: HttpRequestModule loads by default as it's essential for LSL HTTP functionality
- **Certificate Verification**: SSL certificates are verified by default (can be disabled per request)
- **Automatic Redirects**: HTTP redirects are followed automatically up to 10 times
- **Connection Reuse**: HTTP connections are pooled and reused for efficiency

## Features

### LSL HTTP Functions Implementation

#### llHTTPRequest() Backend

The module provides the complete backend implementation for LSL `llHTTPRequest()`:

```lsl
key llHTTPRequest(string url, list parameters, string body);
```

Supported parameters:
- `HTTP_METHOD` - GET, POST, PUT, DELETE, etc.
- `HTTP_MIMETYPE` - Content-Type header
- `HTTP_BODY_MAXLENGTH` - Maximum response body size
- `HTTP_VERIFY_CERT` - Enable/disable certificate verification
- `HTTP_VERBOSE_THROTTLE` - Throttling verbosity
- `HTTP_CUSTOM_HEADER` - Custom HTTP headers
- `HTTP_PRAGMA_NO_CACHE` - Add Pragma: no-cache header

#### HTTP Response Handling

```lsl
http_response(key request_id, integer status, list metadata, string body)
{
    // Handle HTTP response
    if (status == 200)
    {
        llOwnerSay("Success: " + body);
    }
    else
    {
        llOwnerSay("HTTP Error: " + (string)status);
    }
}
```

### Advanced HTTP Features

#### SSL/TLS Support

The module supports modern SSL/TLS protocols:
- TLS 1.0, 1.1, 1.2, and 1.3
- Certificate validation (optional)
- Custom certificate validation callbacks
- Certificate revocation checking disabled for performance

#### Request Methods

Supports all standard HTTP methods:
- GET, POST, PUT, DELETE
- HEAD, OPTIONS, PATCH
- Custom HTTP methods

#### Custom Headers

Full support for custom HTTP headers:
```lsl
list params = [
    HTTP_CUSTOM_HEADER, "Authorization", "Bearer token123",
    HTTP_CUSTOM_HEADER, "X-Custom-Header", "value"
];
key request = llHTTPRequest(url, params, body);
```

### Throttling and Rate Limiting

#### Per-Object Throttling

Each scripted object has independent throttling:
- Configurable requests per second limit
- Burst capacity for occasional spikes
- Token bucket algorithm implementation

#### Per-Owner Throttling

Aggregate limits for all objects owned by a user:
- Prevents abuse through multiple objects
- Higher limits for legitimate use cases
- Separate burst and sustained rate limits

### Security Features

#### URL Filtering

Configurable outbound URL filtering:
- Whitelist/blacklist support
- Protocol filtering (HTTP/HTTPS)
- Domain and IP address filtering
- Port number restrictions

#### Request Validation

Comprehensive request validation:
- URL format validation
- Header validation and sanitization
- Content-Type validation
- Request size limits

## Technical Implementation

### HTTP Client Architecture

#### Dual Client Strategy

```csharp
private static HttpClient VeriFyCertClient = null;      // Certificate verification enabled
private static HttpClient VeriFyNoCertClient = null;    // Certificate verification disabled

public HttpClient GetHttpClient(bool verify)
{
    return verify ? VeriFyCertClient : VeriFyNoCertClient;
}
```

#### Client Configuration

```csharp
// Certificate verification disabled client
SocketsHttpHandler shhnc = new()
{
    AllowAutoRedirect = false,
    AutomaticDecompression = DecompressionMethods.None,
    ConnectTimeout = TimeSpan.FromMilliseconds(httpTimeout),
    PreAuthenticate = false,
    UseCookies = false,
    MaxConnectionsPerServer = maxThreads < 10 ? maxThreads : 10,
    PooledConnectionLifetime = TimeSpan.FromMinutes(3)
};

shhnc.SslOptions.EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13;
shhnc.SslOptions.RemoteCertificateValidationCallback = (message, cert, chain, errors) =>
{
    errors &= ~(SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch);
    return errors == SslPolicyErrors.None;
};
```

### Throttling Implementation

#### Token Bucket Algorithm

```csharp
private struct ThrottleData
{
    public double lastTime;
    public float control;
}

public bool CheckThrottle(uint localID, UUID ownerID)
{
    double now = Util.GetTimeStamp();
    bool ret;

    if (m_RequestsThrottle.TryGetValue(localID, out ThrottleData th))
    {
        double delta = now - th.lastTime;
        th.lastTime = now;

        float add = (float)(m_primPerSec * delta);
        th.control += add;
        if (th.control > m_primBurst)
        {
            th.control = m_primBurst - 1;
            ret = true;
        }
        else
        {
            ret = th.control > 0;
            if (ret)
                th.control--;
        }
    }
    else
    {
        th = new ThrottleData()
        {
            lastTime = now,
            control = m_primBurst - 1,
        };
        ret = true;
    }
    m_RequestsThrottle[localID] = th;

    return ret;
}
```

### Request Processing Implementation

#### Asynchronous Request Execution

```csharp
public UUID StartHttpRequest(uint localID, UUID itemID, string url,
        List<string> parameters, Dictionary<string, string> headers, string body)
{
    UUID reqID = UUID.Random();
    HttpRequestClass htc = new();

    // Configure request based on parameters
    if (parameters is not null)
    {
        for (int i = 0; i < parameters.Count; i += 2)
        {
            switch (Int32.Parse(parameters[i]))
            {
                case (int)HttpRequestConstants.HTTP_METHOD:
                    htc.HttpMethod = parameters[i + 1];
                    break;
                case (int)HttpRequestConstants.HTTP_MIMETYPE:
                    htc.HttpMIMEType = parameters[i + 1];
                    break;
                case (int)HttpRequestConstants.HTTP_VERIFY_CERT:
                    htc.HttpVerifyCert = (int.Parse(parameters[i + 1]) != 0);
                    break;
                // ... other parameters
            }
        }
    }

    // Set request properties
    htc.RequestModule = this;
    htc.LocalID = localID;
    htc.ItemID = itemID;
    htc.Url = url;
    htc.ReqID = reqID;
    htc.OutboundBody = body;
    htc.Headers = headers;

    lock (m_mainLock)
        m_pendingRequests.Add(reqID, htc);

    htc.Process();
    return reqID;
}
```

#### HTTP Request Execution

```csharp
public void SendRequest()
{
    if (Removed)
         return;

    HttpResponseMessage responseMessage = null;
    HttpRequestMessage request = null;
    try
    {
        HttpClient client = RequestModule.GetHttpClient(HttpVerifyCert);
        request = new (new HttpMethod(HttpMethod), Url);

        // Set request body
        if (!string.IsNullOrEmpty(OutboundBody))
        {
            byte[] data = Util.UTF8.GetBytes(OutboundBody);
            request.Content = new ByteArrayContent(data);
        }

        // Add headers
        foreach (KeyValuePair<string, string> entry in Headers)
            AddHeader(entry.Key, entry.Value, request);

        if (HttpPragmaNoCache)
            request.Headers.TryAddWithoutValidation("Pragma", "no-cache");

        // Execute request
        responseMessage = client.Send(request, HttpCompletionOption.ResponseHeadersRead);
        Status = (int)responseMessage.StatusCode;

        // Read response body
        if (responseMessage.Content is not null)
        {
            Stream resStream = responseMessage.Content.ReadAsStream();
            if(resStream is not null)
            {
                int maxBytes = HttpBodyMaxLen;
                byte[] buf = new byte[maxBytes];
                int totalBodyBytes = 0;
                int count;
                do
                {
                    count = resStream.Read(buf, totalBodyBytes, maxBytes - totalBodyBytes);
                    totalBodyBytes += count;
                } while (count > 0 && totalBodyBytes < maxBytes);

                if (totalBodyBytes > 0)
                {
                    string tempString = Util.UTF8.GetString(buf, 0, totalBodyBytes);
                    ResponseBody = tempString.Replace("\r", "");
                }
            }
        }
    }
    catch (HttpRequestException e)
    {
        Status = e.StatusCode is null ? 499 : (int)e.StatusCode;
        ResponseBody = e.Message;
    }
    finally
    {
        // Handle redirects and completion
        if (!Removed)
        {
            if (Status == (int)HttpStatusCode.MovedPermanently ||
                Status == (int)HttpStatusCode.Found ||
                Status == (int)HttpStatusCode.SeeOther ||
                Status == (int)HttpStatusCode.TemporaryRedirect)
            {
                // Handle redirect logic
                HandleRedirect(responseMessage);
            }
            else
            {
                ResponseBody ??= string.Empty;
                RequestModule.GotCompletedRequest(this);
            }
        }
        responseMessage?.Dispose();
        request?.Dispose();
    }
}
```

### Redirect Handling Implementation

```csharp
private void HandleRedirect(HttpResponseMessage responseMessage)
{
    if (Redirects >= MaxRedirects)
    {
        Status = 499;
        ResponseBody = "Number of redirects exceeded max redirects";
        RequestModule.GotCompletedRequest(this);
        return;
    }

    if (responseMessage?.Headers?.Location == null)
    {
        Status = 499;
        ResponseBody = "HTTP redirect code but no location header";
        RequestModule.GotCompletedRequest(this);
        return;
    }

    Uri locationUri = responseMessage.Headers.Location;

    // Handle relative URLs
    if (!locationUri.IsAbsoluteUri)
    {
        Uri reqUri = responseMessage.RequestMessage.RequestUri;
        string newloc = reqUri.Scheme + "://" + reqUri.DnsSafeHost + ":" +
            reqUri.Port + "/" + locationUri.OriginalString;
        if (!Uri.TryCreate(newloc, UriKind.RelativeOrAbsolute, out locationUri))
        {
            Status = 499;
            ResponseBody = "HTTP redirect code but invalid location header";
            RequestModule.GotCompletedRequest(this);
            return;
        }
    }

    // Check URL filter
    if (!RequestModule.CheckAllowed(locationUri))
    {
        Status = 499;
        ResponseBody = "URL from HTTP redirect blocked: " + locationUri.AbsoluteUri;
        RequestModule.GotCompletedRequest(this);
        return;
    }

    // Follow redirect
    Status = 0;
    Url = locationUri.AbsoluteUri;
    Redirects++;
    ResponseBody = null;
    Process();
}
```

## Performance Characteristics

### Resource Usage

- **Memory Footprint**: Moderate memory usage - maintains request queues and HTTP clients
- **CPU Impact**: Low CPU overhead - efficient asynchronous processing
- **Network Usage**: Direct network usage for HTTP requests with connection pooling
- **Thread Usage**: Configurable thread pool for request processing

### Scalability Features

- **Connection Pooling**: HTTP connection reuse for improved performance
- **Request Queuing**: Asynchronous request processing with job queuing
- **Resource Limits**: Configurable limits prevent resource exhaustion
- **Throttling**: Built-in rate limiting prevents abuse

### Performance Optimization

- **Persistent Connections**: HTTP/1.1 connection reuse
- **Efficient Parsing**: Optimized HTTP response parsing
- **Memory Streaming**: Streaming response body reading
- **Concurrent Processing**: Multiple concurrent requests supported

## Usage Examples

### Basic HTTP GET Request

```lsl
// Simple HTTP GET request
default
{
    state_entry()
    {
        string url = "https://api.example.com/data";
        key request = llHTTPRequest(url, [], "");
        llOwnerSay("Sent GET request: " + (string)request);
    }

    http_response(key request_id, integer status, list metadata, string body)
    {
        if (status == 200)
        {
            llOwnerSay("Success: " + body);
        }
        else
        {
            llOwnerSay("HTTP Error: " + (string)status);
        }
    }
}
```

### POST Request with JSON Data

```lsl
// HTTP POST with JSON payload
default
{
    state_entry()
    {
        string url = "https://api.example.com/submit";
        string json_data = "{\"name\":\"test\",\"value\":123}";

        list params = [
            HTTP_METHOD, "POST",
            HTTP_MIMETYPE, "application/json"
        ];

        key request = llHTTPRequest(url, params, json_data);
        llOwnerSay("Sent POST request: " + (string)request);
    }

    http_response(key request_id, integer status, list metadata, string body)
    {
        llOwnerSay("Response: " + (string)status + " - " + body);
    }
}
```

### Request with Custom Headers

```lsl
// HTTP request with custom headers
default
{
    state_entry()
    {
        string url = "https://api.example.com/protected";

        list params = [
            HTTP_METHOD, "GET",
            HTTP_CUSTOM_HEADER, "Authorization", "Bearer your-token-here",
            HTTP_CUSTOM_HEADER, "X-API-Version", "1.0",
            HTTP_VERIFY_CERT, TRUE
        ];

        key request = llHTTPRequest(url, params, "");
    }

    http_response(key request_id, integer status, list metadata, string body)
    {
        if (status == 200)
        {
            llOwnerSay("Authenticated request successful");
            // Parse response body
        }
        else if (status == 401)
        {
            llOwnerSay("Authentication failed");
        }
        else
        {
            llOwnerSay("Request failed: " + (string)status);
        }
    }
}
```

### File Upload Simulation

```lsl
// Simulate file upload with PUT method
string base64_data;

default
{
    state_entry()
    {
        // Simulate binary data as base64
        base64_data = llStringToBase64("This is file content");
        upload_file();
    }

    upload_file()
    {
        string url = "https://storage.example.com/upload/file.txt";

        list params = [
            HTTP_METHOD, "PUT",
            HTTP_MIMETYPE, "text/plain",
            HTTP_CUSTOM_HEADER, "Content-Encoding", "base64",
            HTTP_BODY_MAXLENGTH, 4096
        ];

        key request = llHTTPRequest(url, params, base64_data);
        llOwnerSay("Uploading file...");
    }

    http_response(key request_id, integer status, list metadata, string body)
    {
        if (status == 200 || status == 201)
        {
            llOwnerSay("File uploaded successfully");
        }
        else
        {
            llOwnerSay("Upload failed: " + (string)status);
        }
    }
}
```

### API Polling with Error Handling

```lsl
// Poll API with error handling and retry logic
integer retry_count = 0;
integer max_retries = 3;
float retry_delay = 5.0;

default
{
    state_entry()
    {
        poll_api();
    }

    poll_api()
    {
        string url = "https://api.example.com/status";

        list params = [
            HTTP_METHOD, "GET",
            HTTP_MIMETYPE, "application/json",
            HTTP_VERIFY_CERT, TRUE,
            HTTP_PRAGMA_NO_CACHE, TRUE
        ];

        key request = llHTTPRequest(url, params, "");
        llOwnerSay("Polling API... (attempt " + (string)(retry_count + 1) + ")");
    }

    http_response(key request_id, integer status, list metadata, string body)
    {
        if (status == 200)
        {
            retry_count = 0;  // Reset on success
            llOwnerSay("API response: " + body);

            // Process the response
            process_api_response(body);

            // Schedule next poll
            llSetTimerEvent(30.0);  // Poll every 30 seconds
        }
        else if (status >= 500 && retry_count < max_retries)
        {
            // Server error - retry
            retry_count++;
            llOwnerSay("Server error " + (string)status + ", retrying in " + (string)retry_delay + " seconds");
            llSetTimerEvent(retry_delay);
        }
        else
        {
            retry_count = 0;
            llOwnerSay("Request failed: " + (string)status + " - " + body);
            llSetTimerEvent(60.0);  // Retry after longer delay
        }
    }

    timer()
    {
        llSetTimerEvent(0.0);
        poll_api();
    }

    process_api_response(string response)
    {
        // Parse JSON response and take action
        // This would typically involve JSON parsing
        llOwnerSay("Processing: " + response);
    }
}
```

### Webhook Integration

```lsl
// Send webhook notifications
list notification_queue;

default
{
    state_entry()
    {
        // Example: queue some notifications
        notification_queue = [
            "User logged in",
            "Object created",
            "Event completed"
        ];

        process_queue();
    }

    process_queue()
    {
        if (llGetListLength(notification_queue) > 0)
        {
            string message = llList2String(notification_queue, 0);
            notification_queue = llDeleteSubList(notification_queue, 0, 0);

            send_webhook(message);
        }
    }

    send_webhook(string message)
    {
        string webhook_url = "https://hooks.example.com/webhook";
        string payload = "{\"text\":\"" + message + "\",\"timestamp\":\"" +
                        (string)llGetUnixTime() + "\"}";

        list params = [
            HTTP_METHOD, "POST",
            HTTP_MIMETYPE, "application/json",
            HTTP_CUSTOM_HEADER, "X-Webhook-Source", "OpenSim",
            HTTP_BODY_MAXLENGTH, 1024
        ];

        key request = llHTTPRequest(webhook_url, params, payload);
        llOwnerSay("Sending webhook: " + message);
    }

    http_response(key request_id, integer status, list metadata, string body)
    {
        if (status >= 200 && status < 300)
        {
            llOwnerSay("Webhook delivered successfully");
        }
        else
        {
            llOwnerSay("Webhook failed: " + (string)status);
            // Could implement retry logic here
        }

        // Process next item in queue
        llSetTimerEvent(1.0);
    }

    timer()
    {
        llSetTimerEvent(0.0);
        process_queue();
    }
}
```

## Integration Points

### With Script Engines

- **Request Initiation**: Provides backend for `llHTTPRequest()` LSL function
- **Response Delivery**: Delivers HTTP responses via `http_response()` event
- **YEngine Integration**: Direct integration with YEngine for efficient processing
- **XEngine Compatibility**: Compatible with XEngine for legacy script support

### With Security Systems

- **URL Filtering**: Integrates with OutboundUrlFilter for security control
- **Certificate Validation**: Configurable SSL/TLS certificate validation
- **Rate Limiting**: Built-in throttling prevents abuse and DoS attacks
- **Content Filtering**: Response body size limits prevent memory exhaustion

### With Network Infrastructure

- **Proxy Support**: Full HTTP proxy support with exception handling
- **Connection Management**: Efficient HTTP connection pooling and reuse
- **Protocol Support**: Modern HTTP/1.1 and HTTP/2 support
- **SSL/TLS Integration**: Complete SSL/TLS protocol support

### With Event System

- **Response Events**: Triggers `http_response` events in scripts
- **Error Handling**: Comprehensive error reporting through event system
- **Status Reporting**: Detailed HTTP status code reporting
- **Metadata Delivery**: HTTP headers and metadata delivery to scripts

## Security Features

### Request Security

- **URL Validation**: Comprehensive URL format and security validation
- **Protocol Filtering**: Configurable protocol support (HTTP/HTTPS)
- **Header Sanitization**: HTTP header validation and sanitization
- **Content Limits**: Configurable request and response size limits

### SSL/TLS Security

- **Modern Protocols**: Support for TLS 1.0 through 1.3
- **Certificate Validation**: Optional certificate chain validation
- **Custom Validation**: Configurable certificate validation callbacks
- **Cipher Suites**: Modern cipher suite support

### Access Control

- **Outbound Filtering**: Configurable outbound URL filtering
- **Domain Restrictions**: Domain and IP address access control
- **Port Filtering**: Configurable port access restrictions
- **Script Isolation**: Request isolation per script instance

### Rate Limiting Security

- **DoS Prevention**: Built-in rate limiting prevents denial of service
- **Resource Protection**: Throttling protects server resources
- **Fair Usage**: Per-owner limits ensure fair resource usage
- **Abuse Prevention**: Prevents HTTP request abuse through scripts

## Debugging and Troubleshooting

### Common Issues

1. **Requests Timeout**: Check timeout configuration and network connectivity
2. **Certificate Errors**: Verify SSL certificate validation settings
3. **Throttling Errors**: Check rate limiting configuration and usage
4. **Blocked URLs**: Verify outbound URL filter configuration

### Diagnostic Procedures

1. **Module Loading**: Check logs for HttpRequestModule loading messages
2. **Request Tracking**: Monitor request queues and completion rates
3. **Error Analysis**: Review HTTP status codes and error messages
4. **Performance Monitoring**: Track request timing and resource usage

### Debug Configuration

Enable detailed logging for troubleshooting:

```ini
[Logging]
LogLevel = DEBUG

[Modules]
HttpRequestModule = true

[ScriptsHttpRequestModule]
MaxPoolThreads = 8
PrimRequestsBurst = 3.0
PrimRequestsPerSec = 1.0
PrimOwnerRequestsBurst = 5.0
PrimOwnerRequestsPerSec = 25.0
RequestsTimeOut = 30000

[Network]
HttpBodyMaxLenMAX = 16384
```

### Debug Methods

```csharp
// Monitor request statistics
public int PendingRequestCount
{
    get
    {
        lock (m_mainLock)
            return m_pendingRequests.Count;
    }
}

// Check throttling status
public bool IsThrottled(uint localID, UUID ownerID)
{
    return !CheckThrottle(localID, ownerID);
}
```

## Use Cases

### Web Service Integration

- **API Consumption**: Consume REST APIs and web services from scripts
- **Data Synchronization**: Synchronize virtual world data with external systems
- **Authentication**: Implement OAuth and other authentication flows
- **Real-time Updates**: Receive real-time updates from external services

### External Communication

- **Webhooks**: Send webhook notifications to external systems
- **IoT Integration**: Communicate with Internet of Things devices
- **Database Access**: Access external databases through web APIs
- **File Operations**: Upload and download files from web storage

### Automation and Monitoring

- **System Integration**: Integrate with external monitoring and automation systems
- **Alerting**: Send alerts and notifications to external services
- **Logging**: Forward logs and events to external logging services
- **Metrics**: Send metrics and analytics to external systems

### Content Delivery

- **Dynamic Content**: Fetch dynamic content from web servers
- **Media Streaming**: Integrate with media streaming services
- **Content Management**: Manage content through external CMS systems
- **Asset Delivery**: Deliver assets and resources from web servers

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the CoreModuleFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via configuration
- Logging provides visibility into module loading decisions

### Configuration Migration

When upgrading from previous versions:

- Verify `[Modules]` configuration section includes `HttpRequestModule = true`
- Test HTTP request functionality after deployment
- Update throttling and timeout configurations as needed
- Validate SSL/TLS and proxy configurations

### Deployment Considerations

- **Network Security**: Configure outbound URL filtering for security
- **Performance Tuning**: Adjust throttling limits based on expected usage
- **SSL/TLS Configuration**: Configure certificate validation policies
- **Monitoring**: Monitor HTTP request patterns and performance

## Configuration Examples

### Basic Configuration

```ini
[Modules]
HttpRequestModule = true
```

### Performance Optimized Configuration

```ini
[Modules]
HttpRequestModule = true

[ScriptsHttpRequestModule]
MaxPoolThreads = 16               ; Higher thread count for busy sims
PrimRequestsBurst = 5.0          ; Higher burst for legitimate usage
PrimRequestsPerSec = 2.0         ; Higher sustained rate
PrimOwnerRequestsBurst = 10.0    ; Higher owner burst
PrimOwnerRequestsPerSec = 50.0   ; Higher owner rate
RequestsTimeOut = 45000          ; Longer timeout for slow APIs

[Network]
HttpBodyMaxLenMAX = 32768        ; Larger response bodies
```

### Security Focused Configuration

```ini
[Modules]
HttpRequestModule = true

[ScriptsHttpRequestModule]
MaxPoolThreads = 4               ; Conservative thread count
PrimRequestsBurst = 2.0          ; Lower burst limits
PrimRequestsPerSec = 0.5         ; Conservative rate limiting
PrimOwnerRequestsBurst = 3.0     ; Lower owner burst
PrimOwnerRequestsPerSec = 10.0   ; Conservative owner rate
RequestsTimeOut = 15000          ; Shorter timeout

[Network]
HttpBodyMaxLenMAX = 8192         ; Smaller response limit

# Configure URL filtering in separate config file
```

### Development Configuration

```ini
[Modules]
HttpRequestModule = true

[ScriptsHttpRequestModule]
MaxPoolThreads = 8
PrimRequestsBurst = 10.0         ; High limits for testing
PrimRequestsPerSec = 5.0
PrimOwnerRequestsBurst = 20.0
PrimOwnerRequestsPerSec = 100.0
RequestsTimeOut = 60000          ; Long timeout for debugging

[Network]
HttpBodyMaxLenMAX = 65536        ; Large responses for testing

[Logging]
LogLevel = DEBUG
```

## Best Practices

### Script Development

1. **Error Handling**: Always implement proper error handling in `http_response()`
2. **Rate Limiting**: Be aware of throttling limits and implement appropriate delays
3. **Response Limits**: Consider response body size limits when designing requests
4. **Security**: Validate and sanitize data received from external sources

### Performance Guidelines

1. **Connection Reuse**: Use persistent connections where possible
2. **Request Batching**: Batch multiple operations into single requests
3. **Caching**: Implement caching for frequently requested data
4. **Async Patterns**: Use non-blocking patterns in script design

### Security Practices

1. **URL Validation**: Implement URL filtering for production deployments
2. **Authentication**: Use proper authentication mechanisms for API access
3. **Data Validation**: Validate all data received from external sources
4. **Monitoring**: Monitor HTTP request patterns for unusual activity

## Future Enhancements

### Potential Improvements

1. **HTTP/2 Support**: Enhanced HTTP/2 protocol support
2. **WebSocket Support**: WebSocket connectivity for real-time communication
3. **Enhanced Caching**: Built-in response caching mechanisms
4. **Metrics Collection**: Enhanced metrics and monitoring capabilities

### Compatibility Considerations

1. **Protocol Evolution**: Support for emerging HTTP protocols
2. **Security Standards**: Implementation of evolving security standards
3. **Performance Optimization**: Continued performance improvements
4. **API Evolution**: Support for evolving web API standards

### Integration Opportunities

1. **Service Mesh**: Integration with service mesh technologies
2. **API Gateway**: Enhanced API gateway integration
3. **Monitoring Tools**: Better integration with monitoring and observability tools
4. **Cloud Services**: Enhanced integration with cloud service providers
# UrlModule Technical Documentation

## Overview

The **UrlModule** is a shared region module that provides external HTTP URL endpoints for LSL (Linden Scripting Language) scripts in OpenSimulator. It implements the HTTP-In functionality, allowing scripts to request external URLs and receive HTTP requests from external sources through functions like `llRequestURL()` and `llRequestSecureURL()`, enabling powerful web service integration and external communication capabilities.

## Purpose

The UrlModule serves as a critical scripting infrastructure component that:

- **External URL Provisioning**: Creates unique HTTP/HTTPS endpoints for scripts via `llRequestURL()` and `llRequestSecureURL()`
- **HTTP Request Processing**: Handles incoming HTTP requests to script-requested URLs
- **Script-Web Integration**: Bridges LSL scripts with external web services and applications
- **Request Management**: Manages HTTP request lifecycle, timeouts, and response handling
- **Resource Control**: Enforces URL limits and prevents resource exhaustion
- **Security Implementation**: Provides secure HTTPS endpoints and request validation

## Architecture

### Core Components

```
┌─────────────────────────────────────┐
│           UrlModule                 │
├─────────────────────────────────────┤
│      ISharedRegionModule            │
│    - Cross-region URL management    │
│    - Shared HTTP server access      │
├─────────────────────────────────────┤
│         IUrlModule                  │
│    - Public API for scripts        │
│    - URL lifecycle management      │
├─────────────────────────────────────┤
│      URL Data Management            │
│    - UrlData: URL metadata         │
│    - RequestData: Request state     │
│    - Request/URL mapping            │
├─────────────────────────────────────┤
│     HTTP Server Integration         │
│    - MainServer HTTP/HTTPS access   │
│    - PollService event handling     │
│    - Custom URI path management     │
├─────────────────────────────────────┤
│     Request Processing Pipeline     │
│    - HttpRequestHandler entry      │
│    - Header/query processing       │
│    - Script event generation       │
├─────────────────────────────────────┤
│    Resource Management System       │
│    - URL count limits (TotalUrls)  │
│    - Per-object URL tracking       │
│    - Timeout handling              │
└─────────────────────────────────────┘
```

### Data Flow Architecture

```
Script calls llRequestURL()
     ↓
UrlModule.RequestURL()
     ↓
Generate unique URL code
     ↓
Create HTTP endpoint (/lslhttp/{urlcode})
     ↓
Register PollService handler
     ↓
Return URL to script via http_request event
     ↓
External client makes HTTP request
     ↓
HttpRequestHandler processes request
     ↓
Extract headers, body, query parameters
     ↓
Fire http_request event to script
     ↓
Script processes request
     ↓
Script calls llHttpResponse()
     ↓
UrlModule.HttpResponse()
     ↓
Return response to external client
```

### Data Structures

#### UrlData Class
```csharp
public class UrlData
{
    public UUID hostID;           // SceneObjectPart UUID
    public UUID groupID;          // SceneObjectGroup UUID
    public UUID itemID;           // Script item UUID
    public IScriptModule engine;  // Script engine reference
    public string url;            // Full external URL
    public UUID urlcode;          // Unique URL identifier
    public Dictionary<UUID, RequestData> requests; // Active requests
    public bool isSsl;           // HTTPS flag
    public Scene scene;          // Region scene reference
    public bool allowXss;        // Cross-origin support
}
```

#### RequestData Class
```csharp
public class RequestData
{
    public UUID requestID;                        // Unique request identifier
    public Dictionary<string, string> headers;    // HTTP headers
    public string body;                          // Request body
    public int responseCode;                     // HTTP response code
    public string responseBody;                  // Response content
    public string responseType = "text/plain";   // Response MIME type
    public bool requestDone;                     // Processing complete flag
    public int startTime;                        // Request timestamp
    public bool responseSent;                    // Response sent flag
    public string uri;                           // Original request URI
    public UUID hostID;                          // Host object UUID
    public Scene scene;                          // Region scene reference
}
```

## Interface Implementation

The module implements:
- **ISharedRegionModule**: Cross-region module functionality
- **IUrlModule**: Public API for script engine integration

### IUrlModule Interface

```csharp
public interface IUrlModule
{
    string ExternalHostNameForLSL { get; }
    UUID RequestURL(IScriptModule engine, SceneObjectPart host, UUID itemID, Hashtable options);
    UUID RequestSecureURL(IScriptModule engine, SceneObjectPart host, UUID itemID, Hashtable options);
    void ReleaseURL(string url);
    void HttpResponse(UUID request, int status, string body);
    void HttpContentType(UUID request, string type);
    string GetHttpHeader(UUID request, string header);
    int GetFreeUrls();
    void ScriptRemoved(UUID itemID);
    void ObjectRemoved(UUID objectID);
    int GetUrlCount(UUID groupID);
}
```

## Configuration

### Network Configuration

Configure in OpenSim.ini [Network] section:

```ini
[Network]
ExternalHostNameForLSL = "your.domain.com"
http_listener_port = 9000
https_listener = true
https_port = 9001
shard = "OpenSim"
user_agent = "OpenSim LSL (Mozilla Compatible)"
```

### LSL Functions Configuration

Configure URL limits in OpenSim.ini [LL-Functions] section:

```ini
[LL-Functions]
max_external_urls_per_simulator = 15000
```

### Module Loading Configuration

Configure in OpenSim.ini [Modules] section:

```ini
[Modules]
UrlModule = true  ; Enable UrlModule (default: true)
```

### Configuration Implementation

```csharp
public void Initialise(IConfigSource config)
{
    IConfig networkConfig = config.Configs["Network"];
    m_enabled = false;

    if (networkConfig != null)
    {
        m_lsl_shard = networkConfig.GetString("shard", m_lsl_shard);
        m_lsl_user_agent = networkConfig.GetString("user_agent", m_lsl_user_agent);
        ExternalHostNameForLSL = config.Configs["Network"].GetString("ExternalHostNameForLSL", null);
        bool ssl_enabled = config.Configs["Network"].GetBoolean("https_listener", false);
        m_HttpPort = (uint)config.Configs["Network"].GetInt("http_listener_port", 9000);

        if (ssl_enabled)
            m_HttpsPort = (uint)config.Configs["Network"].GetInt("https_port", (int)m_HttpsPort);
    }

    IConfig llFunctionsConfig = config.Configs["LL-Functions"];
    if (llFunctionsConfig != null)
        TotalUrls = llFunctionsConfig.GetInt("max_external_urls_per_simulator", DefaultTotalUrls);
    else
        TotalUrls = DefaultTotalUrls;
}
```

## Core Functionality

### URL Request Management

#### RequestURL Method (HTTP)

```csharp
public UUID RequestURL(IScriptModule engine, SceneObjectPart host, UUID itemID, Hashtable options)
{
    UUID urlcode = UUID.Random();

    if (!m_enabled)
    {
        engine.PostScriptEvent(itemID, "http_request", new Object[] {
            urlcode.ToString(), "URL_REQUEST_DENIED", m_ErrorStr });
        return urlcode;
    }

    lock (m_UrlMap)
    {
        if (m_UrlMap.Count >= TotalUrls)
        {
            engine.PostScriptEvent(itemID, "http_request", new Object[] {
                urlcode.ToString(), "URL_REQUEST_DENIED", "Too many URLs already open" });
            return urlcode;
        }

        string url = "http://" + ExternalHostNameForLSL + ":" + m_HttpServer.Port.ToString() +
                     "/lslhttp/" + urlcode.ToString();

        UrlData urlData = new()
        {
            hostID = host.UUID,
            groupID = host.ParentGroup.UUID,
            itemID = itemID,
            engine = engine,
            url = url,
            urlcode = urlcode,
            isSsl = false,
            requests = new Dictionary<UUID, RequestData>(),
            scene = host.ParentGroup.Scene
        };

        // Configure CORS if requested
        if (options != null && options["allowXss"] != null)
            urlData.allowXss = true;

        m_UrlMap[url] = urlData;

        // Track URLs per object
        if (m_countsPerSOG.TryGetValue(groupID, out int urlcount))
            m_countsPerSOG[groupID] = ++urlcount;
        else
            m_countsPerSOG[groupID] = 1;

        // Register HTTP handler
        string uri = "/lslhttp/" + urlcode.ToString();
        PollServiceEventArgs args = new(HttpRequestHandler, uri, HasEvents, GetEvents, NoEvents, Drop, urlcode, 25000);
        m_HttpServer.AddPollServiceHTTPHandlerVarPath(args);

        engine.PostScriptEvent(itemID, "http_request", new Object[] {
            urlcode.ToString(), "URL_REQUEST_GRANTED", url + "/" });
    }

    return urlcode;
}
```

#### RequestSecureURL Method (HTTPS)

```csharp
public UUID RequestSecureURL(IScriptModule engine, SceneObjectPart host, UUID itemID, Hashtable options)
{
    UUID urlcode = UUID.Random();

    if (!m_enabled)
    {
        engine.PostScriptEvent(itemID, "http_request", new Object[] {
            urlcode.ToString(), "URL_REQUEST_DENIED", m_ErrorStr });
        return urlcode;
    }

    if (m_HttpsServer == null)
    {
        engine.PostScriptEvent(itemID, "http_request", new Object[] {
            urlcode.ToString(), "URL_REQUEST_DENIED", "" });
        return urlcode;
    }

    // Similar implementation but with HTTPS server and /lslhttps/ path
    string url = "https://" + ExternalHostNameForLSL + ":" + m_HttpsServer.Port.ToString() +
                 "/lslhttps/" + urlcode.ToString();

    // Register with HTTPS server
    string uri = "/lslhttps/" + urlcode.ToString();
    PollServiceEventArgs args = new(HttpRequestHandler, uri, HasEvents, GetEvents, NoEvents, Drop, urlcode, 25000);
    m_HttpsServer.AddPollServiceHTTPHandlerVarPath(args);
}
```

### HTTP Request Processing

#### HttpRequestHandler Method

```csharp
public OSHttpResponse HttpRequestHandler(UUID requestID, OSHttpRequest request)
{
    lock (request)
    {
        string uri = request.RawUrl;

        // Parse URI to extract URL code and path info
        string uri_tmp;
        string pathInfo;
        int pos = uri.IndexOf('/', 45); // /lslhttp/uuid/ <-
        if (pos >= 45)
        {
            uri_tmp = uri[..pos];
            pathInfo = uri[pos..];
        }
        else
        {
            uri_tmp = uri;
            pathInfo = string.Empty;
        }

        // Construct full URL key for lookup
        string urlkey;
        if (uri.Contains("lslhttps"))
            urlkey = "https://" + ExternalHostNameForLSL + ":" + m_HttpsServer.Port.ToString() + uri_tmp;
        else
            urlkey = "http://" + ExternalHostNameForLSL + ":" + m_HttpServer.Port.ToString() + uri_tmp;

        if (!m_UrlMap.TryGetValue(urlkey, out UrlData url))
        {
            request.InputStream.Dispose();
            return errorResponse(request, (int)HttpStatusCode.NotFound);
        }

        // Create request data
        RequestData requestData = new()
        {
            requestID = requestID,
            requestDone = false,
            startTime = System.Environment.TickCount,
            uri = uri,
            hostID = url.hostID,
            scene = url.scene,
            headers = new Dictionary<string, string>()
        };

        // Process HTTP headers
        NameValueCollection headers = request.Headers;
        if (headers.Count > 0)
        {
            for (int i = 0; i < headers.Count; ++i)
            {
                string name = headers.GetKey(i);
                if (!string.IsNullOrEmpty(name))
                    requestData.headers[name] = headers[i];
            }
        }

        // Process query string
        NameValueCollection query = request.QueryString;
        if (query.Count > 0)
        {
            StringBuilder sb = new();
            for (int i = 0; i < query.Count; ++i)
            {
                string key = query.GetKey(i);
                if (string.IsNullOrEmpty(key))
                    sb.AppendFormat("{0}&", query[i]);
                else
                    sb.AppendFormat("{0}={1}&", key, query[i]);
            }
            if (sb.Length > 1)
                sb.Remove(sb.Length - 1, 1);
            requestData.headers["x-query-string"] = sb.ToString();
        }

        // Add custom headers
        requestData.headers["x-remote-ip"] = request.RemoteIPEndPoint.Address.ToString();
        requestData.headers["x-path-info"] = pathInfo;
        requestData.headers["x-script-url"] = url.url;

        // Store request
        lock (url.requests)
        {
            url.requests.Add(requestID, requestData);
        }
        m_RequestMap.Add(requestID, url);

        // Read request body
        string requestBody;
        if (request.InputStream.Length > 0)
        {
            using (StreamReader reader = new(request.InputStream, Encoding.UTF8))
                requestBody = reader.ReadToEnd();
        }
        else
            requestBody = string.Empty;

        request.InputStream.Dispose();

        // Fire script event
        url.engine.PostScriptEvent(url.itemID, "http_request",
            new Object[] { requestID.ToString(), request.HttpMethod, requestBody });

        return null; // Async response via PollService
    }
}
```

### Response Management

#### HttpResponse Method

```csharp
public void HttpResponse(UUID request, int status, string body)
{
    if (m_RequestMap.TryGetValue(request, out UrlData urlData) && urlData != null)
    {
        lock (urlData.requests)
        {
            if (urlData.requests.TryGetValue(request, out RequestData rd) && rd != null)
            {
                if (!rd.responseSent)
                {
                    string responseBody = body;

                    // Handle IE compatibility for text/plain
                    if (rd.responseType.Equals("text/plain"))
                    {
                        if (rd.headers.TryGetValue("user-agent", out string value))
                        {
                            if (value != null && value.Contains("MSIE", StringComparison.InvariantCultureIgnoreCase))
                            {
                                // Wrap HTML escaped response for IE
                                responseBody = "<html>" + System.Web.HttpUtility.HtmlEncode(body) + "</html>";
                            }
                        }
                    }

                    rd.responseCode = status;
                    rd.responseBody = responseBody;
                    rd.requestDone = true;
                    rd.responseSent = true;
                }
            }
        }
    }
}
```

#### HttpContentType Method

```csharp
public void HttpContentType(UUID request, string type)
{
    if (m_RequestMap.TryGetValue(request, out UrlData urlData) && urlData != null)
    {
        urlData.requests[request].responseType = type;
    }
}
```

### PollService Event Handling

#### GetEvents Method

```csharp
protected Hashtable GetEvents(UUID requestID, UUID sessionID)
{
    if (!m_RequestMap.TryGetValue(requestID, out UrlData url))
        return new Hashtable();

    RequestData requestData = null;
    bool timeout = false;

    lock (url.requests)
    {
        requestData = url.requests[requestID];
        if (requestData == null)
            return new Hashtable();

        timeout = System.Environment.TickCount - requestData.startTime > 25000;
        if (!requestData.requestDone && !timeout)
            return new Hashtable();

        url.requests.Remove(requestID);
        m_RequestMap.Remove(requestID);
    }

    if (timeout)
    {
        return new Hashtable()
        {
            ["int_response_code"] = 500,
            ["str_response_string"] = "Script timeout",
            ["content_type"] = "text/plain",
            ["keepalive"] = false
        };
    }

    // Build response headers with Second Life information
    Hashtable headers = new();
    if (url.scene is not null)
    {
        SceneObjectPart sop = url.scene.GetSceneObjectPart(url.hostID);
        if (sop != null)
        {
            RegionInfo ri = url.scene.RegionInfo;
            Vector3 position = sop.AbsolutePosition;
            Vector3 velocity = sop.Velocity;
            Quaternion rotation = sop.GetWorldRotation();

            headers["X-SecondLife-Object-Name"] = sop.Name;
            headers["X-SecondLife-Object-Key"] = sop.UUID.ToString();
            headers["X-SecondLife-Region"] = string.Format("{0} ({1}, {2})", ri.RegionName, ri.WorldLocX, ri.WorldLocY);
            headers["X-SecondLife-Local-Position"] = string.Format("({0:0.000000}, {1:0.000000}, {2:0.000000})", position.X, position.Y, position.Z);
            headers["X-SecondLife-Local-Velocity"] = string.Format("({0:0.000000}, {1:0.000000}, {2:0.000000})", velocity.X, velocity.Y, velocity.Z);
            headers["X-SecondLife-Local-Rotation"] = string.Format("({0:0.000000}, {1:0.000000}, {2:0.000000}, {3:0.000000})", rotation.X, rotation.Y, rotation.Z, rotation.W);
            headers["X-SecondLife-Owner-Key"] = sop.OwnerID.ToString();
        }
    }

    if (!string.IsNullOrWhiteSpace(m_lsl_shard))
        headers["X-SecondLife-Shard"] = m_lsl_shard;
    if (!string.IsNullOrWhiteSpace(m_lsl_user_agent))
        headers["User-Agent"] = m_lsl_user_agent;
    if (url.isSsl)
        headers.Add("Accept-CH", "UA");

    Hashtable response = new()
    {
        ["int_response_code"] = requestData.responseCode,
        ["str_response_string"] = requestData.responseBody,
        ["content_type"] = requestData.responseType,
        ["headers"] = headers,
        ["keepalive"] = false
    };

    if (url.allowXss)
        response["access_control_allow_origin"] = "*";

    return response;
}
```

## Resource Management

### URL Limits and Tracking

#### Per-Simulator Limits

```csharp
public const int DefaultTotalUrls = 15000;
public int TotalUrls { get; set; }

// Check in RequestURL/RequestSecureURL
if (m_UrlMap.Count >= TotalUrls)
{
    engine.PostScriptEvent(itemID, "http_request", new Object[] {
        urlcode.ToString(), "URL_REQUEST_DENIED", "Too many URLs already open" });
    return urlcode;
}
```

#### Per-Object URL Tracking

```csharp
protected readonly RwLockedDictionary<UUID, int> m_countsPerSOG = new RwLockedDictionary<UUID, int>();

// Track URL count per SceneObjectGroup
if (m_countsPerSOG.TryGetValue(groupID, out int urlcount))
    m_countsPerSOG[groupID] = ++urlcount;
else
    m_countsPerSOG[groupID] = 1;

// Cleanup on URL removal
if (m_countsPerSOG.TryGetValue(data.groupID, out int count))
{
    --count;
    if (count <= 0)
        m_countsPerSOG.Remove(data.groupID);
    else
        m_countsPerSOG[data.groupID] = count;
}
```

### Timeout Management

```csharp
// 25-second timeout for HTTP requests
protected Hashtable NoEvents(UUID requestID, UUID sessionID)
{
    if (!m_RequestMap.TryGetValue(requestID, out UrlData url))
        return new Hashtable();

    int startTime = url.requests[requestID].startTime;

    if (System.Environment.TickCount - startTime < 25000)
        return new Hashtable();

    // Remove timed-out request
    lock (url.requests)
    {
        url.requests.Remove(requestID);
    }
    m_RequestMap.Remove(requestID);

    return new Hashtable()
    {
        ["int_response_code"] = 500,
        ["str_response_string"] = "Script timeout",
        ["content_type"] = "text/plain",
        ["keepalive"] = false
    };
}
```

### Cleanup Management

#### Script Removal Cleanup

```csharp
public void ScriptRemoved(UUID itemID)
{
    List<string> removeURLs = new();

    foreach (KeyValuePair<string, UrlData> url in m_UrlMap)
    {
        if (url.Value.itemID == itemID)
        {
            RemoveUrl(url.Value);
            removeURLs.Add(url.Key);

            foreach (UUID req in url.Value.requests.Keys)
                m_RequestMap.Remove(req);
        }
    }

    foreach (string urlname in removeURLs)
        m_UrlMap.Remove(urlname);
}
```

#### Object Removal Cleanup

```csharp
public void ObjectRemoved(UUID objectID)
{
    List<string> removeURLs = new();

    foreach (KeyValuePair<string, UrlData> url in m_UrlMap)
    {
        if (url.Value.hostID == objectID)
        {
            RemoveUrl(url.Value);
            removeURLs.Add(url.Key);

            foreach (UUID req in url.Value.requests.Keys)
                m_RequestMap.Remove(req);
        }
    }

    foreach (string urlname in removeURLs)
        m_UrlMap.Remove(urlname);
}
```

## LSL Integration

### llRequestURL() Function

```lsl
// Request an HTTP URL
default
{
    state_entry()
    {
        llRequestURL();
    }

    http_request(key id, string method, string body)
    {
        if (method == URL_REQUEST_GRANTED)
        {
            llOwnerSay("URL: " + body);
            // URL is now available for external requests
        }
        else if (method == URL_REQUEST_DENIED)
        {
            llOwnerSay("URL request denied: " + body);
        }
        else
        {
            // Handle incoming HTTP request
            llOwnerSay("Received " + method + " request");
            llOwnerSay("Body: " + body);

            // Send response
            llHTTPResponse(id, 200, "Hello from OpenSim!");
        }
    }
}
```

### llRequestSecureURL() Function

```lsl
// Request an HTTPS URL
default
{
    state_entry()
    {
        llRequestSecureURL();
    }

    http_request(key id, string method, string body)
    {
        if (method == URL_REQUEST_GRANTED)
        {
            llOwnerSay("Secure URL: " + body);
            // HTTPS URL is now available
        }
        else
        {
            // Process HTTPS request
            llHTTPResponse(id, 200, "Secure response");
        }
    }
}
```

### Advanced Request Handling

```lsl
// Advanced HTTP request processing
default
{
    state_entry()
    {
        llRequestURL();
    }

    http_request(key id, string method, string body)
    {
        if (method == URL_REQUEST_GRANTED)
        {
            llOwnerSay("URL granted: " + body);
        }
        else
        {
            // Get request headers
            string userAgent = llGetHTTPHeader(id, "user-agent");
            string queryString = llGetHTTPHeader(id, "x-query-string");
            string pathInfo = llGetHTTPHeader(id, "x-path-info");
            string remoteIP = llGetHTTPHeader(id, "x-remote-ip");

            // Process different HTTP methods
            if (method == "GET")
            {
                string response = "Query: " + queryString + "\nPath: " + pathInfo;
                llHTTPResponse(id, 200, response);
            }
            else if (method == "POST")
            {
                // Set custom content type
                llSetContentType(id, "application/json");
                string jsonResponse = "{\"received\":\"" + body + "\"}";
                llHTTPResponse(id, 200, jsonResponse);
            }
            else
            {
                llHTTPResponse(id, 405, "Method not allowed");
            }
        }
    }
}
```

## Advanced Features

### Cross-Origin Resource Sharing (CORS)

```csharp
// Enable CORS in RequestURL options
Hashtable options = new Hashtable();
options["allowXss"] = true;

// Results in Access-Control-Allow-Origin: * header
if (url.allowXss)
    response["access_control_allow_origin"] = "*";
```

### Custom Response Headers

The UrlModule automatically adds Second Life-compatible headers:

- **X-SecondLife-Object-Name**: Name of the object hosting the script
- **X-SecondLife-Object-Key**: UUID of the object
- **X-SecondLife-Region**: Region name and coordinates
- **X-SecondLife-Local-Position**: Object position in region
- **X-SecondLife-Local-Velocity**: Object velocity vector
- **X-SecondLife-Local-Rotation**: Object rotation quaternion
- **X-SecondLife-Owner-Key**: Owner UUID
- **X-SecondLife-Shard**: Grid identifier
- **User-Agent**: Custom user agent string

### Internet Explorer Compatibility

```csharp
// Automatic HTML wrapping for IE text/plain responses
if (rd.responseType.Equals("text/plain"))
{
    if (value != null && value.Contains("MSIE", StringComparison.InvariantCultureIgnoreCase))
    {
        responseBody = "<html>" + System.Web.HttpUtility.HtmlEncode(body) + "</html>";
    }
}
```

## Performance Characteristics

### Thread-Safe Collections

```csharp
// High-performance thread-safe dictionaries
protected readonly RwLockedDictionary<UUID, UrlData> m_RequestMap = new RwLockedDictionary<UUID, UrlData>();
protected readonly RwLockedDictionary<string, UrlData> m_UrlMap = new RwLockedDictionary<string, UrlData>();
protected readonly RwLockedDictionary<UUID, int> m_countsPerSOG = new RwLockedDictionary<UUID, int>();
```

### Memory Management

- **Efficient Request Tracking**: Automatic cleanup of completed and timed-out requests
- **Resource Limits**: Configurable URL limits prevent memory exhaustion
- **Reference Management**: Proper disposal of HTTP streams and cleanup of object references
- **Scene Isolation**: Per-scene URL management with cleanup on scene removal

### HTTP Processing Efficiency

- **PollService Architecture**: Non-blocking HTTP request handling using OpenSim's PollService
- **Asynchronous Processing**: HTTP requests processed asynchronously with script events
- **Connection Pooling**: Leverages MainServer's HTTP connection pooling
- **Path Optimization**: Efficient URI parsing and lookup using string operations

## Security Considerations

### Input Validation

- **URI Length Validation**: Minimum URI length checks prevent malformed requests
- **Path Sanitization**: Proper parsing of request paths and query parameters
- **Header Validation**: Safe processing of HTTP headers with null checks
- **Body Size Limits**: Controlled reading of request bodies using StreamReader

### Access Control

- **URL Uniqueness**: Cryptographically secure UUID-based URL generation
- **Scene Isolation**: URLs tied to specific regions and objects
- **Script Association**: Strict mapping between URLs and script items
- **Resource Ownership**: URLs automatically removed when objects/scripts are deleted

### Network Security

- **HTTPS Support**: Optional secure endpoints via configured HTTPS server
- **Remote IP Tracking**: Client IP addresses logged in x-remote-ip header
- **Timeout Protection**: 25-second request timeout prevents resource locks
- **Error Handling**: Graceful handling of malformed requests and exceptions

## Troubleshooting

### Common Issues

#### Module Not Loading
```
Symptom: UrlModule not appearing in logs
Solution: Set UrlModule = true in [Modules] section
Check: Verify module is in CreateSharedModules output
```

#### URLs Not Working
```
Symptom: llRequestURL() returns URL_REQUEST_DENIED
Causes:
- ExternalHostNameForLSL not configured
- Network configuration missing
- Too many URLs already open
- HTTP server not available

Solutions:
- Configure ExternalHostNameForLSL in [Network] section
- Verify http_listener_port and https_port settings
- Check TotalUrls limit in [LL-Functions] section
- Ensure MainServer HTTP services are running
```

#### External Requests Failing
```
Symptom: External HTTP requests to script URLs fail
Causes:
- DNS resolution issues
- Firewall blocking external access
- Port forwarding misconfigured
- SSL certificate problems (HTTPS)

Solutions:
- Verify ExternalHostNameForLSL resolves to server
- Check firewall rules for HTTP/HTTPS ports
- Configure port forwarding for http_listener_port
- Install valid SSL certificate for HTTPS
```

#### Script Timeouts
```
Symptom: HTTP requests timing out after 25 seconds
Causes:
- Script not calling llHTTPResponse()
- Script error preventing response
- Heavy script processing delaying response

Solutions:
- Ensure scripts always call llHTTPResponse()
- Add error handling in script http_request event
- Optimize script processing for faster responses
- Consider async processing patterns
```

### Debug Information

Enable detailed logging for troubleshooting:

```csharp
private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

// Debug statements provide detailed request tracking:
if (m_log.IsDebugEnabled) m_log.DebugFormat(
    "Set up incoming request url {0} for {1} in {2} {3}",
    uri, itemID, host.Name, host.LocalId);

if (m_log.IsDebugEnabled) m_log.DebugFormat(
    "Releasing url {0} for {1} in {2}",
    url, data.itemID, data.hostID);
```

### Testing Procedures

1. **Configure Network**: Set ExternalHostNameForLSL and ports
2. **Test Basic URL**: Create script with llRequestURL()
3. **Verify External Access**: Make HTTP request from external client
4. **Test HTTPS**: Configure SSL and test llRequestSecureURL()
5. **Load Testing**: Test with multiple concurrent URLs
6. **Cleanup Testing**: Verify URL cleanup on script/object removal

## Migration Notes

### From Mono.Addins to Factory

The module has been migrated from Mono.Addins to factory-based loading:

- **Removed Dependencies**: No longer requires Mono.Addins references
- **Configuration Control**: Loading controlled by [Modules] UrlModule setting
- **Enhanced Logging**: Improved operational visibility and debugging
- **Backward Compatibility**: Maintains full API and functionality compatibility

### Configuration Changes

The module now supports configuration-based loading:

```ini
# Old behavior: Loaded automatically via Mono.Addins
# New behavior: Configurable enablement
[Modules]
UrlModule = true  # Default: true (essential for LSL HTTP-In)
```

### Upgrade Considerations

- Update configuration files to include [Modules] section if needed
- Verify network configuration remains correct
- Test URL functionality after upgrade
- Check script compatibility with new module loading
- Monitor logging for new message formats

## Related Components

### Dependencies
- **ISharedRegionModule**: Module interface contract
- **IUrlModule**: Public API interface
- **MainServer**: HTTP/HTTPS server access
- **PollService**: Asynchronous request handling
- **IScriptModule**: Script engine integration

### Integration Points
- **LSL Functions**: llRequestURL, llRequestSecureURL, llHTTPResponse, llSetContentType, llGetHTTPHeader
- **Script Engines**: YEngine, XEngine integration via IScriptModule interface
- **HTTP Server**: MainServer HTTP listener and HTTPS support
- **Scene Management**: Object and script lifecycle event handling
- **Security System**: Scene permissions and object ownership validation

## Future Enhancements

### Potential Improvements

- **HTTP/2 Support**: Upgrade to HTTP/2 for improved performance
- **WebSocket Integration**: Support for WebSocket connections from scripts
- **Request Queuing**: Advanced request queuing and priority handling
- **Rate Limiting**: Per-script and per-object request rate limits
- **Content Caching**: HTTP response caching for frequently requested content

### LSL Function Extensions

- **llRequestWebSocket()**: WebSocket endpoint creation
- **llHTTPResponseChunked()**: Chunked transfer encoding support
- **llHTTPHeader()**: Custom response header setting
- **llHTTPCookie()**: HTTP cookie management
- **llHTTPRedirect()**: HTTP redirect responses

### Monitoring and Analytics

- **Request Metrics**: Detailed request/response statistics
- **Performance Monitoring**: Request processing time tracking
- **Error Analytics**: Failed request categorization and reporting
- **Usage Statistics**: Per-script and per-object usage reporting
- **Bandwidth Monitoring**: Network traffic analysis and reporting

---

*This documentation covers UrlModule as integrated with the factory-based loading system, removing dependency on Mono.Addins while maintaining full LSL HTTP-In functionality, external URL management, and script-web service integration capabilities.*
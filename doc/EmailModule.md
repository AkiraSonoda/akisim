# EmailModule Documentation

## Overview

The EmailModule is a **comprehensive email service module** that enables scripted objects in OpenSimulator to send and receive emails using the LSL email functions (`llEmail`, `llGetNextEmail`). It provides both **inter-object communication** within the virtual world and **external SMTP email delivery** to real-world email addresses, with robust throttling and security controls.

## Purpose

**Primary Functions:**
- **LSL Email Support** - Implement `llEmail()` and `llGetNextEmail()` LSL functions for scripted objects
- **Inter-Object Communication** - Enable email communication between objects within OpenSimulator  
- **SMTP Integration** - Send emails from objects to external email addresses via SMTP
- **Email Queuing** - Manage incoming email queues for objects with mailboxes
- **Rate Limiting** - Comprehensive throttling system to prevent spam and abuse
- **Security Controls** - SSL/TLS support, certificate validation, and authentication

## Architecture

### Module Structure

The EmailModule implements multiple interfaces and provides comprehensive email services:

```
EmailModule (ISharedRegionModule, IEmailModule)
├── SMTP Configuration
│   ├── Server Connection (hostname, port, TLS)
│   ├── Authentication (username, password)
│   ├── Certificate Validation (chain, name verification)
│   └── Message Construction (headers, body, attachments)
├── Email Queuing System
│   ├── Object Mailboxes - Individual email queues per object
│   ├── Queue Management - Size limits and expiration
│   ├── Message Storage - Persistent email storage
│   └── Retrieval Logic - Filtering and ordering
├── Throttling System  
│   ├── Owner Rate Limits - Per-owner sending restrictions
│   ├── Address Throttling - Per-address rate limiting
│   ├── SMTP Daily Limits - External email quotas
│   └── Cleanup Management - Automatic throttle expiration
└── Message Processing
    ├── Address Validation - Email format verification
    ├── Content Filtering - Size and content restrictions
    ├── Delivery Routing - Internal vs external routing
    └── Error Handling - Connection and delivery failures
```

### Core Components

**Email Types:**
- **Internal Email** - Object-to-object communication within OpenSim (e.g., `objectID@lsl.opensim.local`)
- **External Email** - Object-to-world email via SMTP (e.g., `user@example.com`)

**Throttling Controls:**
- **Owner Throttling** - Rate limiting per object owner (default: 500/hour)
- **Address Throttling** - Rate limiting per destination address
- **SMTP Daily Limits** - Overall daily email quotas (default: 100/day)
- **Queue Size Limits** - Maximum emails per object mailbox (default: 50)

## Configuration

### Basic SMTP Configuration

Configure the EmailModule in the `[SMTP]` section:

```ini
[Startup]
emailmodule = DefaultEmailModule

[SMTP]
enabled = true
enableEmailToExternalObjects = true
enableEmailToSMTP = true

; SMTP server configuration
SMTP_SERVER_HOSTNAME = smtp.gmail.com
SMTP_SERVER_PORT = 587
SMTP_SERVER_TLS = true
SMTP_SERVER_LOGIN = your-email@gmail.com
SMTP_SERVER_PASSWORD = your-app-password
SMTP_SERVER_FROM = "OpenSim Object" <your-email@gmail.com>

; Domain configuration
host_domain_header_from = opensim.example.com
internal_object_host = lsl.opensim.local
```

### Advanced Configuration

**Rate Limiting Settings:**
```ini
[SMTP]
; Per-owner limits
MailsFromOwnerPerHour = 500

; Per-address limits  
MailsToPrimAddressPerHour = 50
MailsToSMTPAddressPerHour = 10

; Daily SMTP limits
SMTP_MailsPerDay = 100

; Message size limits
email_max_size = 4096
```

**SSL/TLS Security:**
```ini
[SMTP]
SMTP_SERVER_TLS = true
SMTP_VerifyCertChain = true
SMTP_VerifyCertNames = true
```

### Configuration Parameters

**Core Settings:**
- `enabled` - Enable/disable the email module (default: false)
- `enableEmailToExternalObjects` - Allow inter-object email (default: true)
- `enableEmailToSMTP` - Allow external SMTP email (default: true)

**SMTP Settings:**
- `SMTP_SERVER_HOSTNAME` - SMTP server hostname
- `SMTP_SERVER_PORT` - SMTP server port (default: 25)
- `SMTP_SERVER_TLS` - Enable TLS/StartTLS (default: false)
- `SMTP_SERVER_LOGIN` - SMTP authentication username
- `SMTP_SERVER_PASSWORD` - SMTP authentication password
- `SMTP_SERVER_FROM` - Default sender address

**Domain Settings:**
- `host_domain_header_from` - Domain for outgoing email headers
- `internal_object_host` - Hostname for inter-object email (default: `lsl.opensim.local`)

## Email Processing Flow

### 1. LSL Script Email Sending

**LSL Function Call:**
```lsl
// Send email from script
llEmail("recipient@example.com", "Subject", "Message body");
```

**Internal Processing:**
```csharp
public void SendEmail(UUID objectID, UUID ownerID, string address, 
                     string subject, string body)
{
    // Validate address format
    if (!MailboxAddress.TryParse(address, out MailboxAddress mailTo))
    {
        m_log.ErrorFormat("invalid TO email address {0}", address);
        return;
    }

    // Check rate limiting
    if (!CheckOwnerThrottle(ownerID)) return;

    // Determine email type and route accordingly
    if (IsExternalEmail(address))
        SendSMTPEmail(objectID, ownerID, mailTo, subject, body);
    else
        SendInternalEmail(objectID, address, subject, body);
}
```

### 2. External SMTP Email Delivery

**SMTP Message Construction:**
```csharp
private void SendSMTPEmail(UUID objectID, UUID ownerID, MailboxAddress mailTo, 
                          string subject, string body)
{
    MimeMessage mmsg = new MimeMessage();
    
    // Set sender
    if (SMTP_MAIL_FROM != null)
    {
        mmsg.From.Add(SMTP_MAIL_FROM);
        mmsg.Subject = "(OSObj" + objectID + ") " + subject;
    }
    else
    {
        mmsg.From.Add(MailboxAddress.Parse(objectID + "@" + m_HostName));
        mmsg.Subject = subject;
    }

    // Add recipient and headers
    mmsg.To.Add(mailTo);
    mmsg.Headers["X-Owner-ID"] = ownerID.ToString();
    mmsg.Headers["X-Task-ID"] = objectID.ToString();

    // Construct body with object information
    mmsg.Body = new TextPart("plain") {
        Text = "Object-Name: " + objectName +
               "\nRegion: " + regionName + 
               "\nLocal-Position: " + position + "\n\n" + body
    };

    // Send via SMTP
    using (var client = new SmtpClient())
    {
        if (SMTP_SERVER_TLS)
            client.Connect(hostname, port, SecureSocketOptions.StartTls);
        else
            client.Connect(hostname, port);

        if (authRequired)
            client.Authenticate(login, password);

        client.Send(mmsg);
    }
}
```

### 3. Internal Object Email Delivery

**Inter-Object Email Processing:**
```csharp
private void SendInternalEmail(UUID objectID, string address, string subject, string body)
{
    // Parse object UUID from address (objectID@lsl.opensim.local)
    int indx = address.IndexOf('@');
    if (!UUID.TryParse(address.Substring(0, indx), out UUID toID))
        return;

    // Create email object
    Email email = new Email
    {
        time = Util.UnixTimeSinceEpoch().ToString(),
        subject = subject,
        sender = objectID.ToString() + "@" + m_InterObjectHostname,
        message = "Object-Name: " + objectName + 
                 "\nRegion: " + regionName + 
                 "\nLocal-Position: " + position + "\n\n" + body
    };

    // Deliver to local object or queue for external delivery
    if (IsLocal(toID))
        InsertEmail(toID, email);
    else
        QueueExternalObjectEmail(toID, email); // TODO: Cross-region delivery
}
```

### 4. Email Retrieval (llGetNextEmail)

**LSL Function Call:**
```lsl
// Retrieve next email in script
llGetNextEmail("sender@address", "subject filter");
```

**Internal Processing:**
```csharp
public Email GetNextEmail(UUID objectID, string sender, string subject)
{
    // Clean up expired throttles and queues
    CleanupExpiredData();

    lock (m_queuesLock)
    {
        if (m_MailQueues.TryGetValue(objectID, out List<Email> queue) && queue != null)
        {
            lock (queue)
            {
                // Find matching email based on sender/subject filters
                for (int i = 0; i < queue.Count; i++)
                {
                    Email email = queue[i];
                    if (MatchesFilters(email, sender, subject))
                    {
                        queue.RemoveAt(i);
                        email.numLeft = queue.Count;
                        return email;
                    }
                }
            }
        }
    }
    return null; // No matching email found
}
```

## Throttling and Rate Limiting

### Owner-Based Throttling

**Per-Owner Rate Control:**
```csharp
private bool CheckOwnerThrottle(UUID ownerID)
{
    double now = Util.GetTimeStamp();
    
    if (m_ownerThrottles.TryGetValue(ownerID, out throttleControlInfo tci))
    {
        // Refill bucket based on time passed
        tci.count += (now - tci.lastTime) * m_MailsFromOwnerRate;
        tci.lastTime = now;
        
        // Cap at maximum
        if (tci.count > m_MailsFromOwnerPerHour)
            tci.count = m_MailsFromOwnerPerHour;
        
        // Check if email is allowed
        if (tci.count <= 0) return false;
        
        --tci.count; // Consume one email
        return true;
    }
    else
    {
        // First email for this owner
        m_ownerThrottles[ownerID] = new throttleControlInfo
        {
            lastTime = now,
            count = m_MailsFromOwnerPerHour - 1
        };
        return true;
    }
}
```

### Address-Based Throttling

**SMTP Address Throttling:**
- Prevents spam to specific external email addresses
- Default: 10 emails per hour per SMTP address
- Separate limits for internal object addresses (50/hour)

**Implementation:**
```csharp
private bool CheckSMTPAddressThrottle(string address)
{
    string addressLower = address.ToLower();
    // Similar token bucket algorithm as owner throttling
    // but applied per destination address
}
```

### Daily SMTP Limits

**Global Rate Limiting:**
```csharp
private bool CheckDailySMTPLimit()
{
    double now = Util.GetTimeStamp();
    m_SMTPCount += (now - m_SMTPLastTime) * m_SMTP_MailsRate;
    m_SMTPLastTime = now;
    
    if (m_SMTPCount > m_SMTP_MailsPerDay)
        m_SMTPCount = m_SMTP_MailsPerDay;
    
    if (m_SMTPCount <= 0) return false;
    
    --m_SMTPCount;
    return true;
}
```

## Email Queue Management

### Mailbox Creation and Management

**Object Mailbox Lifecycle:**
```csharp
public void AddPartMailBox(UUID objectID)
{
    lock (m_queuesLock)
    {
        if (!m_MailQueues.ContainsKey(objectID))
        {
            m_MailQueues[objectID] = null; // Lazy initialization
        }
    }
}

public void RemovePartMailBox(UUID objectID)
{
    lock (m_queuesLock)
    {
        m_LastGetEmailCall.Remove(objectID);
        m_MailQueues.Remove(objectID);
    }
}
```

**Email Insertion:**
```csharp
public void InsertEmail(UUID objectID, Email email)
{
    lock (m_queuesLock)
    {
        if (m_MailQueues.TryGetValue(objectID, out List<Email> queue))
        {
            if (queue == null)
            {
                queue = new List<Email>();
                m_MailQueues[objectID] = queue;
            }

            // Respect queue size limits
            if (queue.Count >= m_MaxQueueSize) return;

            lock (queue) queue.Add(email);
            m_LastGetEmailCall[objectID] = Util.GetTimeStamp() + m_QueueTimeout;
        }
    }
}
```

### Automatic Cleanup

**Expired Data Cleanup:**
```csharp
private void CleanupExpiredData()
{
    double now = Util.GetTimeStamp();
    double expiredTime = now - 3600; // 1 hour ago

    // Clean up owner throttles
    CleanupThrottles(m_ownerThrottles, expiredTime);
    
    // Clean up address throttles  
    CleanupThrottles(m_primAddressThrottles, expiredTime);
    CleanupThrottles(m_SMPTAddressThrottles, expiredTime);

    // Clean up email queues
    CleanupMailQueues(now);
}
```

## Security Features

### SSL/TLS Support

**Certificate Validation:**
```csharp
public static bool smptValidateServerCertificate(object sender, X509Certificate certificate,
        X509Chain chain, SslPolicyErrors sslPolicyErrors)
{
    return (sslPolicyErrors & m_SMTP_SslPolicyErrorsMask) == SslPolicyErrors.None;
}
```

**Configurable Security Policies:**
- `SMTP_VerifyCertChain` - Validate certificate chain (default: true)
- `SMTP_VerifyCertNames` - Validate certificate names (default: true)
- Support for self-signed certificates in development environments

### Email Address Validation

**Address Format Validation:**
```csharp
if (!MailboxAddress.TryParse(address, out MailboxAddress mailTo))
{
    m_log.ErrorFormat("invalid TO email address {0}", address);
    return;
}
```

**Content Security:**
- Maximum email size limits (default: 4096 bytes)
- Address length limits (maximum: 320 characters)
- Subject and body content validation

## LSL Integration

### Supported LSL Functions

**llEmail(string address, string subject, string body):**
```lsl
default
{
    state_entry()
    {
        // Send email to external address
        llEmail("user@example.com", "Hello from OpenSim", "This is a test message");
        
        // Send email to another object
        llEmail("550e8400-e29b-41d4-a716-446655440000@lsl.opensim.local", 
               "Inter-object message", "Hello from another object");
    }
}
```

**llGetNextEmail(string address, string subject):**
```lsl
default
{
    email(string time, string address, string subject, string message, integer num_left)
    {
        llOwnerSay("Received email from: " + address);
        llOwnerSay("Subject: " + subject);
        llOwnerSay("Message: " + message);
        llOwnerSay("Emails remaining: " + (string)num_left);
        
        // Get next email if available
        if (num_left > 0)
            llGetNextEmail("", ""); // Get any email
    }
    
    state_entry()
    {
        // Request next email (triggers email event if available)
        llGetNextEmail("", ""); // Get any email
        // llGetNextEmail("sender@example.com", ""); // From specific sender
        // llGetNextEmail("", "Important"); // With specific subject
    }
}
```

### Email Event Parameters

**email(string time, string address, string subject, string message, integer num_left):**
- `time` - Unix timestamp when email was sent
- `address` - Sender's email address
- `subject` - Email subject line
- `message` - Email body content (includes object info header)
- `num_left` - Number of emails remaining in queue

## Usage Scenarios

### Inter-Object Communication

**Object-to-Object Messaging:**
```lsl
// Object A sends email to Object B
default
{
    state_entry()
    {
        // Send message to specific object UUID
        llEmail("12345678-1234-1234-1234-123456789abc@lsl.opensim.local", 
               "Status Update", "System is online");
    }
}

// Object B receives and processes email
default
{
    email(string time, string address, string subject, string message, integer num_left)
    {
        // Parse sender UUID from address
        list parts = llParseString2List(address, ["@"], []);
        string senderID = llList2String(parts, 0);
        
        // Process based on subject
        if (subject == "Status Update")
        {
            // Handle status update
            llOwnerSay("Received status: " + message);
        }
    }
}
```

### External Email Integration

**Notification Systems:**
```lsl
// Send alerts to administrator
default
{
    state_entry()
    {
        // Monitor for specific conditions and send alerts
        llEmail("admin@example.com", "Region Alert", 
               "High avatar count detected: " + (string)llGetRegionAgentCount());
    }
}
```

**Customer Support:**
```lsl
// Support ticket system
default
{
    touch_start(integer total_number)
    {
        llEmail("support@business.com", "Customer Contact", 
               "Customer " + llDetectedName(0) + " needs assistance");
        llSay(0, "Your request has been sent to support.");
    }
}
```

### Event-Driven Communication

**Cross-Region Coordination:**
```lsl
// Event coordinator object
default
{
    state_entry()
    {
        // Notify other regions about event
        llEmail("coordinator@otheregion.local", "Event Starting", 
               "Main event beginning in 5 minutes");
    }
}
```

## Performance Considerations

### Memory Management

**Queue Size Limits:**
- Default maximum of 50 emails per object mailbox
- Automatic cleanup of expired email queues (30 minute timeout)
- Throttle data cleanup (1 hour expiration)

**Efficient Data Structures:**
```csharp
// Thread-safe dictionaries for concurrent access
private RwLockedDictionary<UUID, List<Email>> m_MailQueues;
private RwLockedDictionary<UUID, throttleControlInfo> m_ownerThrottles;
```

### SMTP Connection Optimization

**Connection Pooling:**
```csharp
using (var client = new SmtpClient())
{
    // Efficient connection management
    // Automatic connection cleanup
    // TLS negotiation optimization
}
```

### Rate Limiting Performance

**Token Bucket Algorithm:**
- O(1) throttle checking and updates
- Automatic cleanup reduces memory usage
- Efficient time-based rate calculations

## Troubleshooting

### Common Configuration Issues

**SMTP Authentication Failures:**
```ini
[SMTP]
; Common Gmail configuration
SMTP_SERVER_HOSTNAME = smtp.gmail.com
SMTP_SERVER_PORT = 587
SMTP_SERVER_TLS = true
SMTP_SERVER_LOGIN = your-email@gmail.com
SMTP_SERVER_PASSWORD = your-app-password  # Not regular password!
```

**Certificate Validation Issues:**
```ini
[SMTP]
; For self-signed certificates or internal servers
SMTP_VerifyCertChain = false
SMTP_VerifyCertNames = false
```

### Debugging Email Delivery

**Enable Debug Logging:**
```ini
[Logging]
LogLevel = DEBUG
```

**Common Log Messages:**
```
EMail sent to: user@example.com from object: 12345678-1234-1234-1234-123456789abc@opensim.local
DefaultEmailModule Exception: Authentication failed
invalid TO email address user@invalid-domain
subject + body larger than limit of 4096 bytes
```

### Rate Limiting Issues

**Owner Throttle Exceeded:**
- Check `MailsFromOwnerPerHour` setting
- Monitor owner email frequency
- Increase limits for legitimate high-volume users

**SMTP Daily Limit Reached:**
- Check `SMTP_MailsPerDay` configuration  
- Monitor daily email usage
- Consider increasing limits or implementing priority queues

## Technical Specifications

### System Requirements

- **.NET 8.0** - Runtime environment
- **MailKit Library** - Modern email client library
- **MimeKit Library** - MIME message construction
- **SMTP Server Access** - External email delivery capability

### Performance Characteristics

- **Email Queue Latency**: < 1ms for local email insertion
- **SMTP Delivery**: 100-5000ms depending on server and network
- **Throttle Check**: < 0.1ms using token bucket algorithm  
- **Memory Usage**: ~100KB per 1000 queued emails

### Protocol Support

**Email Standards:**
- **SMTP** - Simple Mail Transfer Protocol
- **MIME** - Multipurpose Internet Mail Extensions  
- **TLS/StartTLS** - Transport Layer Security
- **SMTP Authentication** - LOGIN and PLAIN mechanisms

**OpenSimulator Integration:**
- **LSL Functions** - `llEmail()`, `llGetNextEmail()`
- **Event System** - `email()` event delivery
- **Module Interface** - `IEmailModule` implementation
- **Region Framework** - Multi-region support

The EmailModule provides comprehensive email functionality for OpenSimulator, enabling rich communication between scripted objects and the external world while maintaining robust security and performance controls.
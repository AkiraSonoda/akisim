# SampleMoneyModule Technical Documentation

## Overview

The **SampleMoneyModule** is an optional OpenSimulator module that provides basic economy and currency functionality for virtual worlds. It implements a simple money system with support for user balances, transactions, and XMLRPC-based external integrations. The module serves as both a functional economy implementation and a reference for custom money module development.

## Architecture and Interfaces

### Core Interfaces
- **ISharedRegionModule**: Shared across regions module lifecycle
- **IMoneyModule**: Economy-specific functionality interface for external access

### Key Components
- **Balance Management**: User currency balance tracking and updates
- **Transaction Processing**: Secure money transfers between users
- **XMLRPC Integration**: External web service integration for payments
- **Client Protocol**: Viewer currency display and transaction support
- **Event System**: Money-related event notifications for other modules

## Economy System

### Currency Management
The module implements a basic virtual currency system:
- **User Balances**: Per-user currency amount tracking
- **Transaction Logging**: Optional transaction history and auditing
- **Balance Persistence**: Currency data stored in user profiles
- **Starting Balance**: Configurable initial currency for new users

### Transaction Types
- **Avatar-to-Avatar**: Direct transfers between users
- **Object Purchases**: In-world marketplace transactions
- **Land Purchases**: Parcel buying and selling
- **Upload Fees**: Texture and asset upload costs
- **Group Transactions**: Currency operations involving groups

## Configuration System

### Module Enablement
```ini
[Modules]
; Enable SampleMoneyModule for basic economy functionality
EconomyModule = SampleMoneyModule
```

### Economy Configuration
```ini
[Economy]
; Starting balance for new users (default: 1000)
StartingBalance = 1000

; Enable transaction logging (default: false)
LogTransactions = false

; Currency symbol displayed to users (default: OS$)
CurrencySymbol = OS$

; Enable XMLRPC money handlers (default: true)
EnableXMLRPCHandlers = true

; XMLRPC server port for external integration (default: none)
XmlRpcPort = 8080
```

### Configuration Options
- **StartingBalance**: Initial currency amount for new users
- **LogTransactions**: Enable detailed transaction logging
- **CurrencySymbol**: Display symbol for currency in viewer
- **EnableXMLRPCHandlers**: Enable external web service integration
- **XmlRpcPort**: Port for XMLRPC money service (optional)

## Client Integration

### Balance Display
The module handles client requests for currency information:

```csharp
public void OnMoneyBalanceRequest(IClientAPI client, UUID agentID, UUID SessionID, UUID TransactionID)
{
    int balance = GetBalance(agentID);
    client.SendMoneyBalance(TransactionID, true, new byte[0], balance, 0, UUID.Zero, false, UUID.Zero, false, 0, String.Empty);
}
```

### Transaction Processing
Client-initiated money transfers:

```csharp
public void OnMoneyTransferRequest(IClientAPI client, UUID agentID, UUID SessionID,
                                 UUID sourceID, UUID destID, int amount, int transactiontype, string description)
{
    bool success = TransferMoney(sourceID, destID, amount, transactiontype, description);
    // Send transaction result to client
}
```

### Event Handling
- **Money Balance Requests**: Client requests for current balance display
- **Transfer Requests**: Avatar-initiated money transfers
- **Transaction Confirmations**: Success/failure notifications
- **Balance Updates**: Real-time balance change notifications

## XMLRPC Integration

### External Payment System
The module provides XMLRPC handlers for external payment integration:

```csharp
public XmlRpcResponse BalanceRequest(XmlRpcRequest request, IPEndPoint remoteClient)
public XmlRpcResponse UserPaymentMethod(XmlRpcRequest request, IPEndPoint remoteClient)
public XmlRpcResponse TransactionRequest(XmlRpcRequest request, IPEndPoint remoteClient)
```

### Supported XMLRPC Methods
- **balance_request**: Query user balance from external systems
- **user_payment**: Process external payments into virtual world
- **transaction_request**: Handle transaction requests from web interfaces
- **currency_quote**: Get current exchange rates or currency information

### Integration Examples
External web applications can integrate with the money system:
```php
// PHP example for balance query
$client = new xmlrpc_client("/", "your-opensim-server.com", 8080);
$msg = new xmlrpcmsg("balance_request", array(
    new xmlrpcval($userUUID, "string")
));
$response = $client->send($msg);
```

## Transaction Processing

### Money Transfer Flow
1. **Validation**: Verify source has sufficient funds
2. **Authorization**: Check transfer permissions and limits
3. **Execution**: Update source and destination balances
4. **Notification**: Send updates to affected clients
5. **Logging**: Record transaction details (if enabled)

### Transfer Validation
```csharp
private bool ValidateTransfer(UUID sourceID, UUID destID, int amount)
{
    // Check source has sufficient balance
    if (GetBalance(sourceID) < amount) return false;

    // Validate positive amount
    if (amount <= 0) return false;

    // Check destination exists
    if (!UserExists(destID)) return false;

    return true;
}
```

### Balance Management
```csharp
private bool TransferMoney(UUID sourceID, UUID destID, int amount, int type, string description)
{
    if (!ValidateTransfer(sourceID, destID, amount)) return false;

    // Update balances atomically
    DeductBalance(sourceID, amount);
    AddBalance(destID, amount);

    // Log transaction if enabled
    if (LogTransactions)
        LogTransaction(sourceID, destID, amount, type, description);

    return true;
}
```

## Event System Integration

### Money Events
The module fires events for money-related activities:

```csharp
public event MoneyTransferEvent OnMoneyTransfer;
public event MoneyBalanceUpdateEvent OnBalanceUpdate;
```

### Event Notifications
- **Transaction Events**: Notify other modules of money transfers
- **Balance Changes**: Update displays and dependent systems
- **Purchase Events**: Integration with commerce modules
- **Error Events**: Transaction failure notifications

### Module Integration
Other modules can subscribe to money events:
```csharp
// Example: Commerce module integration
IMoneyModule moneyModule = scene.RequestModuleInterface<IMoneyModule>();
if (moneyModule != null)
{
    moneyModule.OnMoneyTransfer += HandleMoneyTransfer;
}
```

## Security Considerations

### Transaction Security
- **Balance Validation**: Prevent negative balances and overdrafts
- **User Authorization**: Verify transaction permissions
- **Input Sanitization**: Validate all transaction parameters
- **Rate Limiting**: Prevent rapid-fire transaction abuse

### XMLRPC Security
- **Request Validation**: Verify external request authenticity
- **Parameter Checking**: Sanitize all XMLRPC inputs
- **Access Control**: Limit XMLRPC access to authorized systems
- **SSL/TLS Support**: Secure communication for payment data

### Data Integrity
- **Atomic Transactions**: Ensure balance consistency
- **Error Handling**: Graceful failure and rollback
- **Audit Trail**: Optional transaction logging for compliance
- **Balance Verification**: Periodic balance integrity checks

## API Interface

### IMoneyModule Methods

#### GetBalance(UUID userID)
```csharp
public int GetBalance(UUID userID)
```
- **Purpose**: Retrieve current balance for specified user
- **Parameters**: `userID` - UUID of the user
- **Returns**: Integer balance amount (0 if user not found)
- **Usage**: Check user's current currency amount

#### TransferMoney(UUID from, UUID to, int amount)
```csharp
public bool TransferMoney(UUID from, UUID to, int amount, int type, string description)
```
- **Purpose**: Transfer money between users
- **Parameters**: Source user, destination user, amount, transaction type, description
- **Returns**: Boolean success status
- **Usage**: Process money transfers between avatars

#### AddBalance(UUID userID, int amount)
```csharp
public bool AddBalance(UUID userID, int amount)
```
- **Purpose**: Add currency to user's balance (administrative function)
- **Parameters**: `userID` - User UUID, `amount` - Amount to add
- **Returns**: Boolean success status
- **Usage**: Admin commands, rewards, external payments

## Performance Considerations

### Efficient Operations
- **In-Memory Caching**: Cache frequently accessed balances
- **Atomic Updates**: Minimal database operations per transaction
- **Event Optimization**: Efficient event notification system
- **XMLRPC Pooling**: Connection pooling for external services

### Scalability
- **Database Backend**: Configurable persistence layer
- **Load Distribution**: Shared module for multi-region efficiency
- **Transaction Queuing**: Handle high transaction volumes
- **Memory Management**: Efficient balance cache management

### Optimization Strategies
- **Batch Processing**: Group multiple transactions when possible
- **Cache Management**: Smart cache invalidation and updates
- **Connection Pooling**: Efficient database and XMLRPC connections
- **Event Batching**: Reduce event notification overhead

## Module Lifecycle

### Initialization
```csharp
public void Initialise(IConfigSource source)
```
- **Configuration Loading**: Read [Economy] section settings
- **XMLRPC Setup**: Initialize external integration handlers
- **Balance Cache**: Set up in-memory balance caching

### Region Integration
```csharp
public void AddRegion(Scene scene)
public void RegionLoaded(Scene scene)
```
- **Interface Registration**: Register IMoneyModule with scene
- **Event Subscription**: Subscribe to client and scene events
- **Service Availability**: Make money services available to other modules

### Cleanup
```csharp
public void RemoveRegion(Scene scene)
public void Close()
```
- **Event Unsubscription**: Clean up client event subscriptions
- **Cache Cleanup**: Clear balance cache and temporary data
- **XMLRPC Shutdown**: Stop external integration services

## Error Handling and Validation

### Transaction Validation
```csharp
if (amount <= 0)
{
    client.SendAlertMessage("Invalid transaction amount");
    return false;
}

if (GetBalance(sourceID) < amount)
{
    client.SendAlertMessage("Insufficient funds");
    return false;
}
```

### Common Error Messages
- **"Insufficient funds"**: User lacks required balance
- **"Invalid transaction amount"**: Negative or zero amount specified
- **"User not found"**: Destination user doesn't exist
- **"Transaction failed"**: General transaction processing error
- **"Service unavailable"**: Money module not loaded or configured

### Error Recovery
- **Graceful Degradation**: System functions without money module
- **Transaction Rollback**: Reverse failed transactions
- **Client Notification**: Clear error feedback to users
- **Logging**: Comprehensive error logging for debugging

## Administrative Features

### Console Commands
The module may provide console commands for administration:
- **money show <userID>**: Display user's current balance
- **money add <userID> <amount>**: Add money to user's account
- **money transfer <from> <to> <amount>**: Admin money transfer
- **money stats**: Show economy statistics and totals

### Monitoring and Statistics
- **Total Currency**: Track total money in circulation
- **Transaction Volume**: Monitor transaction rates and amounts
- **User Balances**: Summary of user balance distribution
- **Error Rates**: Track transaction failure rates

### Debugging Support
- **Verbose Logging**: Detailed transaction and balance logging
- **Balance Verification**: Commands to check balance integrity
- **Cache Statistics**: Monitor cache hit rates and performance
- **XMLRPC Diagnostics**: External integration debugging

## Integration Examples

### Basic Economy Setup
```ini
[Modules]
EconomyModule = SampleMoneyModule

[Economy]
StartingBalance = 1000
CurrencySymbol = OS$
LogTransactions = true
```

### External Integration
```csharp
// Web service integration example
IMoneyModule money = scene.RequestModuleInterface<IMoneyModule>();
if (money != null)
{
    // Process external payment
    bool success = money.AddBalance(userUUID, paymentAmount);

    // Check user balance
    int balance = money.GetBalance(userUUID);
}
```

### Commerce Module Integration
```csharp
// Example: Marketplace module using money system
public bool ProcessPurchase(UUID buyerID, UUID sellerID, int price)
{
    IMoneyModule money = scene.RequestModuleInterface<IMoneyModule>();
    if (money == null) return false;

    return money.TransferMoney(buyerID, sellerID, price,
                              (int)TransactionType.Purchase, "Marketplace purchase");
}
```

## Migration Notes

### Factory Integration
- **Mono.Addins Removal**: Module doesn't use Mono.Addins (already factory-compatible)
- **Configuration-based Loading**: Controlled via EconomyModule setting in [Modules]
- **Default Behavior**: Disabled by default, requires explicit configuration
- **Logging Integration**: Comprehensive debug and info logging for operations

### Configuration Migration
```ini
# Enable the economy module
[Modules]
EconomyModule = SampleMoneyModule

# Configure economy settings
[Economy]
StartingBalance = 1000
EnableXMLRPCHandlers = true
```

### Dependencies
- **Scene Management**: Integration with scene and region lifecycle
- **Client API**: Client event handling for money transactions
- **User Services**: Access to user account information
- **External Services**: Optional XMLRPC integration with web services

## Troubleshooting

### Common Issues

#### Module Not Loading
- **Check Configuration**: Ensure EconomyModule = "SampleMoneyModule" in [Modules]
- **Module Name**: Configuration values are case-sensitive
- **Log Messages**: Check for loading debug messages
- **Dependencies**: Verify all required assemblies are available

#### Balance Not Updating
- **Client Refresh**: Money balance may require client logout/login
- **Cache Issues**: Check balance cache consistency
- **Database Connection**: Verify persistent storage connectivity
- **Event Delivery**: Ensure client event notifications are working

#### XMLRPC Integration Issues
- **Port Configuration**: Check XmlRpcPort setting and firewall rules
- **Handler Registration**: Verify XMLRPC handlers are properly registered
- **Request Format**: Ensure external requests match expected format
- **Authentication**: Check external system authentication requirements

#### Transaction Failures
- **Insufficient Funds**: Most common cause of transaction failure
- **User Validation**: Ensure both source and destination users exist
- **Amount Validation**: Check for negative or zero amounts
- **Module State**: Verify money module is properly initialized

## Usage Examples

### Basic Configuration
```ini
[Modules]
EconomyModule = SampleMoneyModule

[Economy]
StartingBalance = 1000
CurrencySymbol = L$
LogTransactions = false
```

### Enhanced Configuration
```ini
[Modules]
EconomyModule = SampleMoneyModule

[Economy]
StartingBalance = 5000
CurrencySymbol = OS$
LogTransactions = true
EnableXMLRPCHandlers = true
XmlRpcPort = 8080
```

### Programmatic Usage
```csharp
// Get money module interface
IMoneyModule moneyModule = scene.RequestModuleInterface<IMoneyModule>();

if (moneyModule != null)
{
    // Check user balance
    int balance = moneyModule.GetBalance(userUUID);

    // Transfer money between users
    bool success = moneyModule.TransferMoney(
        sourceUUID, destUUID, amount,
        (int)TransactionType.Gift, "Birthday gift");

    // Add money (administrative)
    moneyModule.AddBalance(userUUID, bonusAmount);
}
```

This documentation reflects the SampleMoneyModule implementation in `src/OpenSim.Region.OptionalModules/World/MoneyModule/SampleMoneyModule.cs` and its integration with the factory-based module loading system.
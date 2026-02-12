# Azure Function - IoT Hub Trigger

This Azure Function is triggered by messages from an IoT Hub and logs them.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- An Azure IoT Hub
- Azure Function App (stayproai-fa)

## Configuration

This function uses **Managed Identity** for secure authentication to IoT Hub.

### Get IoT Hub Details

1. Go to Azure Portal
2. Navigate to your IoT Hub
3. Go to **Built-in endpoints** under **Hub settings**
4. Note the **Event Hub-compatible name**
5. Note your IoT Hub name (e.g., "myiothub")

### Application Settings Required

Set these in the Function App (stayproai-fa) Configuration:

- `IoTHubConnectionString__fullyQualifiedNamespace`: `<your-iot-hub-name>.servicebus.windows.net`
- `IoTHubConnectionString__credential`: `managedidentity`
- `IoTHubConnectionString__clientId`: Your user-assigned identity's Client ID
- `IoTHubEventHubName`: The Event Hub-compatible name from your IoT Hub

### Configure User-Assigned Managed Identity

1. **Get your existing User-Assigned Managed Identity details**:
   - Go to Azure Portal → Search for your User-Assigned Managed Identity
   - Note the **Client ID** (you'll need this)
   - Note the **Principal ID** (Object ID)

2. **Assign the identity to stayproai-fa**:
   - Go to Function App → Identity
   - Click **User assigned** tab
   - Click **+ Add**
   - Select your user-assigned managed identity
   - Click **Add**

3. **Grant permissions to IoT Hub**:
   - Go to your IoT Hub
   - Select **Access control (IAM)**
   - Click **Add role assignment**
   - Select **Azure Event Hubs Data Receiver** role
   - Assign access to: Managed Identity
   - Select **User-assigned managed identity**
   - Select your managed identity
   - Review + assign

## Local Development

**Note**: Managed identity works in Azure but for local development you'll need to:
- Use Azure CLI: `az login` to authenticate
- Or use a connection string temporarily in `local.settings.json`

1. Update `local.settings.json` with your IoT Hub details:
   - Set `IoTHubConnectionString__fullyQualifiedNamespace` to `<your-iot-hub-name>.servicebus.windows.net`
   - Set `IoTHubEventHubName` to your Event Hub-compatible name

2. Restore packages:
   ```bash
   dotnet restore
   ```

3. Build the project:
   ```bash
   dotnet build
   ```

4. Run locally:
   ```bash
   func start
   ```

## Deploy to Azure

### Option 1: Using Azure Functions Core Tools

```bash
func azure functionapp publish stayproai-fa
```

### Option 2: Using VS Code Azure Functions Extension

1. Install the Azure Functions extension
2. Sign in to Azure
3. Right-click on the Function App and select "Deploy to Function App"
4. Select stayproai-fa

### Option 3: Using Azure CLI

```bash
# Build and publish
dotnet publish -c Release -o ./publish

# Create a zip
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy
az functionapp deployment source config-zip -g <resource-group> -n stayproai-fa --src deploy.zip
```

## After Deployment

1. **Configure application settings**:
   - Go to Azure Portal → Function App (stayproai-fa)
   - Go to **Configuration** under **Settings**
   - Add/update these application settings:
     - `IoTHubConnectionString__fullyQualifiedNamespace`: `<your-iot-hub-name>.servicebus.windows.net`
     - `IoTHubConnectionString__credential`: `managedidentity`
     - `IoTHubConnectionString__clientId`: `<your-user-assigned-identity-client-id>`
     - `IoTHubEventHubName`: Your Event Hub-compatible name
   - Save the configuration

2. **Verify managed identity assignment and permissions**:
   - Ensure the user-assigned identity is attached to stayproai-fa (Identity → User assigned)
   - Ensure it has **Azure Event Hubs Data Receiver** role on the IoT Hub
   - Check in IoT Hub → Access control (IAM) → Role assignments

## Testing

Send messages to your IoT Hub from a device, and check the Function App logs to see them being received and logged.

You can view logs in:
- Azure Portal > Function App > Log stream
- Application Insights (if configured)

## Function Details

- **Trigger**: IoT Hub (Event Hub trigger)
- **Runtime**: .NET 8.0 Isolated
- **Function**: Logs incoming messages and device information

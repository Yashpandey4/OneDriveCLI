# CLI to interact with OneDrive Files

This .NET Core Console app will allow you to change and interact with files in the signed-in user's OneDrive account.

- Upload Files to OneDrive
- Download Files from OneDrive
- View Files in OneDrive, including
  - All files relevant to (trending around) the signed in User
  -

## Prerequisites

- Office 365 Tenancy
- [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio Code](https://code.visualstudio.com/)

## Pre-Run Steps

- Create an Azure AD application by following the instructions below and collect these data elements:
  - tenantId
  - applicationId
- Rename the file **appsettings.json.example** to **appsettings.json**
- Update the properties in the **appsettings.json** with the values from your account settings in [Azure Active Directory](https://aad.portal.azure.com/).

### AAD configuration

- Go to account settings in [Azure Active Directory](https://aad.portal.azure.com/).
  - Select Manage > App registrations in the left-hand navigation.
  - On the App registrations page, select New registration.
  - On the Register an application page, set the values as follows:
    - Name: Graph Console App
    - Supported account types: Accounts in this organizational directory only
  - On the Graph Console App page, copy the value of the Application (client) ID and Directory (tenant) ID.
  - Select Manage > Authentication.
  - In the Platform configurations section, select the Add a platform button. Then in the Configure platforms panel, select the Mobile and desktop applications button.
  - In the Redirect URIs section of the Configure Desktop + devices panel, select the entry that ends with nativeclient, and then select the Configure button.
  - In the Authentication panel, scroll down to the Default client type section and set the toggle to Yes.
- Grant Azure AD application permissions to Microsoft Graph
  - On the App registrations page, select the Graph Console App.
  - Select API Permissions in the left-hand navigation panel.
  - Select the Add a permission button.
  - In the Request API permissions panel that appears, select Microsoft Graph from the Microsoft APIs tab.
  - Add the following permissions:
    - Files.Read
    - Files.ReadWrite
    - Sites.Read.All

## Running the Application

- Clone the repo: `git clone https://github.com/Yashpandey4/OneDriveCLI`
- While in the project root, add the required dependencies:

```
dotnet add package Microsoft.Identity.Client
dotnet add package Microsoft.Graph
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.FileExtensions
dotnet add package Microsoft.Extensions.Configuration.Json
```

- Rename the file **appsettings.json.example** to **appsettings.json** and replace the values from your account settings in [Azure Active Directory](https://aad.portal.azure.com/) described in the steps above.
- Run the Project:

```
dotnet build
dotnet run
```

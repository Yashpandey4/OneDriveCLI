# Table of Contents

1. [High level stepwise overview](#overview)
2. [Tackling Individual Tasks](#tasks)
  - [Uploading files to OneDrive](#upload)
    - [Uploading new small files up to 4 MB](#less4)
    - [To update or replace an existing file up to 4 MB](#lesser4)
    - [Uploading large files greater than 4 MB](#more4)
    - [Cancelling an upload session](#cancel)
    - [Resuming an upload session](#resume)
    - [Files trending around a user](#trend)
    - [Listing files the user has accessed or modified](#access)
  - [Create a sharing link for a DriveItem](#share)
    - [Creating Global Share Links](#global)
    - [Creating company sharable links](#internal)
    - [Creating embedded links](#embed)
    - [Send a sharing invitation](#invite)
    - [List people with permissions](#shareList)
    - [Update sharing permission](#updateShare)
    - [Accessing shared DriveItems](#access)
    - [Get sharing permission for a file or folder](#fileShare)
  - [Creating expirable links for Document Sharing](#expire)
  - [User Authentication](#auth)
    - [code flow](#code_flow)
    - [token flow](#token_flow)
    - [Sign the user out](#sign_out)
  - [Deleting a file](#delete)
3. [salesforce integration](#salesforce_integration)
4. [Remarks](#remarks)
5. [Tools and Useful Links](#tools)

# High level stepwise overview <a name="overview"></a>
(Note: SalesForce Support is yet to be added)
1. The User logs in the Graph Node Endpoint with the account in which the file he wants to share is located via browser
2. Alternatively, a codeflow or tokenflow based approach automates the authenticatGet sharing permission for a file or folderion process
3. The File to be shared is created/fetched and transferred to a folder 'SalesForce' in the users OneDrive
4. Based on the size (4 MB) the file is uploaded to user's OneDrive.
5. Share Links for the file is programmatically generated using a user defined access level (user can be asked if he would like to share the file globally or within the org, and for which access level: View/Comment/Edit)
6. We re-upload the file back to source (SalesForce in this case) and share the URL with relevant people
7. User is signed out and all auth keys/tokens are deleted

# Tackling Individual Tasks <a name="tasks"></a>

## Uploading files to OneDrive <a name="upload"></a>

### Uploading new small files up to 4 MB <a name="less4"></a>

```
HTTP PUT https://graph.microsoft.com/v1.0/me/drive/root:/myNewSmallFile.txt:/content
Content-Type: text/plain

This is a new small file
```

### To update or replace an existing file up to 4 MB <a name="lesser4"></a>

```
HTTP PUT https://graph.microsoft.com/v1.0/me/drive/items/{item-id}/content
Content-Type: text/plain

A new small file
```

### Uploading large files greater than 4 MB <a name="more4"></a>

#### 1. start by creating a new upload session:

```
HTTP POST https://graph.microsoft.com/v1.0/me/drive/root/createUploadSession
Content-Type: application/json

{
"item": { "name": "largefile.zip"}
}
```

#### 2. This will return the upload session that contains the URL to upload the file and when the temporary upload location will expire:

```
HTTP/1.1 200 OK
Content-Type: application/json

{
"uploadUrl": "https://sn3302.up.1drv.com/up/example-hash",
"expirationDateTime": "Example-date-timestamp"
}
```

#### 3. Once you've created the upload session, you can then upload bytes to the temporary file in OneDrive.

```
// In this example, you're uploading the first 25 bytes for a 128-byte file
HTTP PUT https://sn3302.up.1drv.com/up/example-hash
Content-Length: 26
Content-Range: bytes 0-25/128

// <bytes 0-25 of the file>
```

#### Response:

```
HTTP/1.1 202 Accepted
Content-Type: application/json
{ "expirationDateTime": "2019-12-29T09:21:55.523Z", "nextExpectedRanges": ["26-"] }
```

### Cancelling an upload session <a name="cancel"></a>

`HTTP DELETE https://sn3302.up.1drv.com/up/example-hash`

### Resuming an upload session <a name="resume"></a>

`HTTP GET https://sn3302.up.1drv.com/up/example-hash`

### Files trending around a user <a name="trend"></a>

`HTTP GET https://graph.microsoft.com/v1.0/me/insights/trending`

This request will return a collection of documents, each with an assigned weight that can be used to rank relevance:

```
{
  "value": [
  {
    "id": "id-value",
    "weight": "weight-value",
    "resourceVisualization": {
      "title": "title-value",
      "type": "type-value",
      "mediaType": "mediaType-value",
      "previewImageUrl": "previewImageUrl-value",
      "previewText": "previewText-value",
      "containerWebUrl": "containerWebUrl-value",
      "containerDisplayName": "containerDisplayName-value",
      "containerType": "containerType-value"
    },
    "resourceReference": {
        "webUrl": "webUrl-value",
        "id": "id-value",
        "type": "type-value"
      }
    }
  ]
}
```

### Listing files the user has accessed or modified <a name="access"></a>

`HTTP GET https://graph.microsoft/com/v1.0/me/insights/used`

This request will return a similar response to the trending endpoint except each item returned doesn't include a weight and in the collection will include a lastused object with two properties:

- lastAccessedDateTime: The timestamp when the file was last accessed by the user.
- lastModifiedDateTime: The timestamp when the file was last modified by the user.

## Create a sharing link for a DriveItem <a name="share"></a>

### HTTP request

```
POST /drives/{driveId}/items/{itemId}/createLink
POST /groups/{groupId}/drive/items/{itemId}/createLink
POST /me/drive/items/{itemId}/createLink
POST /sites/{siteId}/drive/items/{itemId}/createLink
POST /users/{userId}/drive/items/{itemId}/createLink
```

### Response

The response will be 201 Created if a new sharing link is created for the item or 200 OK if an existing link is returned.

#### Creating Global Share Links: <a name="global"></a>

##### Request:

```
POST /me/drive/items/{item-id}/createLink
Content-type: application/json

{
  "type": "view",
  "scope": "anonymous"
}
```

##### Response:

```
HTTP/1.1 201 Created
Content-Type: application/json

{
  "id": "123ABC",
  "roles": ["write"],
  "link": {
    "type": "view",
    "scope": "anonymous",
    "webUrl": "https://1drv.ms/hash-code",
    "application": {
      "id": "1234",
      "displayName": "Sample Application"
    },
  }
}
```

#### Creating company sharable links: <a name="internal"></a>

##### Request:

```
POST /me/drive/items/{item-id}/createLink
Content-Type: application/json

{
  "type": "edit",
  "scope": "organization"
}
```

##### Response:

```
HTTP/1.1 201 Created
Content-Type: application/json

{
  "id": "123ABC",
  "roles": ["write"],
  "link": {
    "type": "edit",
    "scope": "organization",
    "webUrl": "https://contoso-my.sharepoint.com/personal/url",
    "application": {
      "id": "1234",
      "displayName": "Sample Application"
    },
  }
}
```

#### Creating embedded links: <a name="embed"></a>

Note: Embed links are only supported for OneDrive personal.

##### Request:

```
POST /me/drive/items/{item-id}/createLink
Content-Type: application/json

{
  "type": "embed"
}
```

##### Response:

```
HTTP/1.1 201 Created
Content-Type: application/json

{
  "id": "123ABC",
  "roles": ["read"],
  "link": {
    "type": "embed",
    "webHtml": "<IFRAME src=\"https://onedrive.live.com/...\"></IFRAME>",
    "webUrl": "https://onedive.live.com/...",
    "application": {
      "id": "1234",
      "displayName": "Sample Application"
    },
  }
}
```

#### Send a sharing invitation: <a name="share"></a>

Sends a sharing invitation for a DriveItem. A sharing invitation provides permissions to the recipients and optionally sends them an email with a sharing link.

##### Request
```
POST /drives/{drive-id}/items/{item-id}/invite
POST /groups/{group-id}/drive/items/{item-id}/invite
POST /me/drive/items/{item-id}/invite
POST /sites/{siteId}/drive/items/{itemId}/invite
POST /users/{userId}/drive/items/{itemId}/invite
```

##### Request Body
```
{
  "requireSignIn": false,
  "sendInvitation": false,
  "roles": [ "read | write"],
  "recipients": [
    { "@odata.type": "microsoft.graph.driveRecipient" },
    { "@odata.type": "microsoft.graph.driveRecipient" }
  ],
  "message": "string"
}
```

- recipients:	Collection(DriveRecipient) -	A collection of recipients who will receive access and the sharing invitation.
- message:	String -	A plain text formatted message that is included in the sharing invitation. Maximum length 2000 characters.
- requireSignIn:	Boolean -	Specifies whether the recipient of the invitation is required to sign-in to view the shared item.
- sendInvitation:	Boolean	- If true, a sharing link is sent to the recipient. Otherwise, a permission is granted directly without sending a notification.
- roles:	Collection(String) -	Specify the roles that are to be granted to the recipients of the sharing invitation.

**Example**
```
POST /me/drive/items/{item-id}/invite
Content-type: application/json

{
  "recipients": [
    {
      "email": "pratyushpandey4@geminid.com"
    }
  ],
  "message": "Here's the file that we're collaborating on.",
  "requireSignIn": true,
  "sendInvitation": true,
  "roles": [ "write" ]
}
```

**Response**
```
HTTP/1.1 200 OK
Content-type: application/json

{
  "value": [
    {
      "grantedTo": {
        "user": {
          "displayName": "Kartik Soneji",
          "id": "42F177F1-22C0-4BE3-900D-4507125C5C20"
        }
      },
      "id": "CCFC7CA3-7A19-4D57-8CEF-149DB9DDFA62",
      "invitation": {
        "email": "ryan@contoso.com",
        "signInRequired": true
      },
      "roles": [ "write" ]
    }
  ]
}
```

#### List sharing permissions on a DriveItem <a name="shareList"></a>
The permissions collection includes potentially sensitive information and may not be available for every caller.
- For the owner of the item, all sharing permissions will be returned. This includes co-owners.
- For a non-owner caller, only the sharing permissions that apply to the caller are returned.
- Sharing permission properties that contain secrets (e.g. shareId and webUrl) are only returned for callers that are able to create the sharing permission.

##### Request
```
GET /drives/{drive-id}/items/{item-id}/permissions
GET /groups/{group-id}/drive/items/{item-id}/permissions
GET /me/drive/items/{item-id}/permissions
GET /me/drive/root:/{path}:/permissions
GET /sites/{siteId}/drive/items/{itemId}/permissions
GET /users/{userId}/drive/items/{itemId}/permissions
```

Effective sharing permissions of a DriveItem can come from two sources:

- Sharing permissions applied directly on the DriveItem itself
- Sharing permissions inherited from the DriveItem's ancestors
- Callers can differentiate if the permission is inherited or not by checking the inheritedFrom property. This property is an itemReference resource referencing the ancestor that the permission is inherited from.

##### Example
`GET /me/drive/items/{item-id}/permissions`

**Response**
```
HTTP/1.1 200 OK
Content-Type: application/json

{
  "value": [
    {
      "id": "1",
      "roles": ["write"],
      "link": {
        "webUrl": "https://onedrive.live.com/redir?resid=5D33DD65C6932946!70859&authkey=!AL7N1QAfSWcjNU8&ithint=folder%2cgif",
        "type": "edit"
      }
    },
    {
      "id": "2",
      "roles": ["write"],
      "grantedTo": {
        "user": {
          "id": "5D33DD65C6932946",
          "displayName": "John Doe"
        }
      },
      "inheritedFrom": {
        "driveId": "1234567890ABD",
        "id": "1234567890ABC!123",
        "path": "/drive/root:/Documents" }
    },
    {
      "id": "3",
      "roles": ["write"],
      "link": {
        "webUrl": "https://onedrive.live.com/redir?resid=5D33DD65C6932946!70859&authkey=!AL7N1QAfSWcjNU8&ithint=folder%2cgif",
        "type": "edit",
        "application": {
          "id": "12345",
          "displayName": "Contoso Time Manager"
        }
      }
    }
  ]
}
```

#### Update sharing permission <a name="updateShare">

##### Request

```
PATCH /drives/{drive-id}/items/{item-id}/permissions/{perm-id}
PATCH /groups/{group-id}/drive/items/{item-id}/permissions/{perm-id}
PATCH /me/drive/items/{item-id}/permissions/{perm-id}
PATCH /sites/{site-id}/drive/items/{item-id}/permissions/{perm-id}
PATCH /users/{user-id}/drive/items/{item-id}/permissions/{perm-id}
```

**Example**
```
PATCH /me/drive/items/{item-id}/permissions/{perm-id}
Content-type: application/json

{
  "roles": [ "read" ]
}
```

**Response**
```
HTTP/1.1 200 OK
Content-type: application/json

{
  "grantedTo": {
    "user": {
      "displayName": "Kartik Soneji",
      "id": "efee1b77-fb3b-4f65-99d6-274c11914d12"
    }
  },
  "id": "1",
  "roles": [ "read" ]
}
```

#### Accessing shared DriveItems  <a name="access"></a>

- Access a shared DriveItem or a collection of shared items by using a shareId or sharing URL.
- To use a sharing URL with this API, your app needs to transform the URL into a sharing token.

Request: `GET /shares/{shareIdOrEncodedSharingUrl}`

**Encoding sharing URLs**
To encode a sharing URL, use the following logic:
- First, use base64 encode the URL.
- Convert the base64 encoded result to unpadded base64url format by removing = characters from the end of the value, replacing / with _ and + with -.)
- Append u! to be beginning of the string.

Here is encoding logic in C#
```
string sharingUrl = "https://onedrive.live.com/redir?resid=1231244193912!12&authKey=1201919!12921!1";
string base64Value = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sharingUrl));
string encodedUrl = "u!" + base64Value.TrimEnd('=').Replace('/','_').Replace('+','-');
```

**Response**

```
HTTP/1.1 200 OK
Content-type: application/json

{
  "id": "B64397C8-07AE-43E4-920E-32BFB4331A5B",
  "name": "contoso project.docx",
  "owner": {
    "user": {
      "id": "98E88F1C-F8DC-47CC-A406-C090248B30E5",
      "displayName": "Kartik Soneji"
    }
  }
}
```

##### Access the shared item directly

While the SharedDriveItem contains some useful information, most apps will want to directly access the shared DriveItem. The SharedDriveItem resource includes a root and items relationships which can access content within the scope of the shared item.

**Request**
`GET /shares/{shareIdOrUrl}/driveItem`

**Response**
```
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": "9FFFDB3C-5B87-4062-9606-1B008CA88E44",
  "name": "contoso project.docx",
  "eTag": "2246BD2D-7811-4660-BD0F-1CF36133677B,1",
  "file": {},
  "size": 109112
}
```

##### Example (shared folder)

**Request**
`GET /shares/{shareIdOrUrl}/driveItem?$expand=children`

**Response**
```
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": "9FFFDB3C-5B87-4062-9606-1B008CA88E44",
  "name": "Contoso Project",
  "eTag": "2246BD2D-7811-4660-BD0F-1CF36133677B,1",
  "folder": {},
  "size": 10911212,
  "children": [
    {
      "id": "AFBBDD79-868E-452D-AD4D-24697D4A4044",
      "name": "Propsoal.docx",
      "file": {},
      "size": 19001
    },
    {
      "id": "A91FE90A-2F2C-4EE6-B412-C4FFBA3F71A6",
      "name": "Update to Proposal.docx",
      "file": {},
      "size": 91001
    }
  ]
}
```

#### Get sharing permission for a file or folder  <a name="fileShare"></a>

##### Request

```
GET /drives/{drive-id}/items/{item-id}/permissions/{perm-id}
GET /groups/{group-id}/drive/items/{item-id}/permissions/{perm-id}
GET /me/drive/items/{item-id}/permissions/{perm-id}
GET /sites/{site-id}/drive/items/{item-id}/permissions/{perm-id}
GET /users/{user-id}/drive/items/{item-id}/permissions/{perm-id}
```

##### Response
```
HTTP/1.1 200 OK
Content-type: application/json

{
  "grantedTo": {
    "user": {
      "displayName": "Kartik Soneji",
      "id": "efee1b77-fb3b-4f65-99d6-274c11914d12"
    }
  },
  "id": "1",
  "roles": [ "write" ]
}
```

- The Permission resource uses facets to provide information about the kind of permission represented by the resource.
- Permissions with a link facet represent sharing links created on the item. Sharing links contain a unique token that provides access to the item for anyone with the link.
- Permissions with a invitation facet represent permissions added by inviting specific users or groups to have access to the file.

#### Creating expirable links for Document Sharing <a name="expire"></a>

We can configure this option in Office 365 admin center like below: (No 'obvious' way to do it programatically)
![alt text](https://github.com/Yashpandey4/OneDriveCLI/blob/master/Helpers/expire.png "Setting Expiration Date for Shared Docs")

Note: this only applies to external (anonymous) links. Internal (organisational) links dont expire.

##### Deleting permissions using REST API calls
**Request**
```
DELETE /drives/{drive-id}/items/{item-id}/permissions/{perm-id}
DELETE /groups/{group-id}/drive/items/{item-id}/permissions/{perm-id}
DELETE /me/drive/items/{item-id}/permissions/{perm-id}
DELETE /sites/{site-id}/drive/items/{item-id}/permissions/{perm-id}
DELETE /users/{user-id}/drive/items/{item-id}/permissions/{perm-id}
```

**Response**
`HTTP/1.1 204 No Content`

**Optional Headers**
- if-match: string - If this request header is included and the eTag (or cTag) provided does not match the current tag on the item, a 412 Precondition Failed response is returned and the item will not be deleted.

**Note** 
- Drives with a driveType of personal (OneDrive Personal) cannot create or modify permissions on the root DriveItem.
- This is the same as revoking file permission from the UI: The web-url associated with the perm-id deleted ceases to work.

**Extra: Manual ways to expire a link** 
- Please see [this](https://support.microsoft.com/en-us/office/stop-sharing-onedrive-or-sharepoint-files-or-folders-or-change-permissions-0a36470f-d7fe-40a0-bd74-0ac6c1e13323) link.

## Authentication <a name="auth"></a>

Other than registering the app in AAD described in [this README](https://github.com/Yashpandey4/OneDriveCLI/blob/master/README.md), the other methods are described below

### Token Flow <a name="token_flow"></a>

This flow is useful for quickly obtaining an access token to use the OneDrive API in an interactive fashion. This flow does not provide a refresh token, and therefore is not a good fit for longterm access to resources.

1. To start the sign-in process with the token flow, use a web browser or web-browser control to load a URL request.

```
GET https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={client_id}
&scope={scope}
&response_type=token
&redirect_uri={redirect_uri}
```

Where:

- client_id The client ID value created for your application.
- redirect_uri: The redirect URL that the browser is sent to when authentication is complete.
- response_type: The type of response expected from the authorization flow. For this flow, the value must be token.
- scope: A space-separated list of scopes your application requires.

2. Upon successful authentication and authorization of your application, the web browser is redirected to the redirect URL provided with additional parameters added to the URL.

```
https://myapp.com/auth-redirect#access_token=EwC...EB
&authentication_token=eyJ...3EM
&token_type=bearer
&expires_in=3600
&scope=onedrive.readwrite
&user_id=3626...1d
```

### Code Flow <a name="code_flow"></a>

The code flow for authentication is a three-step process with separate calls to authenticate and authorize the application and to generate an access token to use the OneDrive API. This also allows your application to receive a refresh token that will enable long-term use of the API in some scenarios, to allow access when the user isn't actively using your application.

1. To start the sign-in process with the code flow, use a web browser or web-browser control to load this URL request.

```
GET https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={client_id}
&scope={scope}
&response_type=code
&redirect_uri={redirect_uri}
```

Response: Upon successful authentication and authorization of your application, the web browser will be redirected to your redirect URL with additional parameters added to the URL. `https://myapp.com/auth-redirect?code=df6aa589-1080-b241-b410-c4dff65dbf7c`

2. Redeem the code for access tokens

```
POST https://login.microsoftonline.com/common/oauth2/v2.0/token
Content-Type: application/x-www-form-urlencoded

client_id={client_id}
&redirect_uri={redirect_uri}
&client_secret={client_secret}
&code={code}
&grant_type=authorization_code
```

Response:

```
{
  "token_type":"bearer",
  "expires_in": 3600,
  "scope":"wl.basic onedrive.readwrite",
  "access_token":"EwCo...AA==",
  "refresh_token":"eyJh...9323"
}
```

3. Get a new access token or refresh token

```
POST https://login.microsoftonline.com/common/oauth2/v2.0/token
Content-Type: application/x-www-form-urlencoded

client_id={client_id}
&redirect_uri={redirect_uri}
&client_secret={client_secret}
&refresh_token={refresh_token}
&grant_type=refresh_token
```

Response:

```
{
  "token_type":"bearer",
  "expires_in": 3600,
  "scope": "wl.basic onedrive.readwrite wl.offline_access",
  "access_token":"EwCo...AA==",
  "refresh_token":"eyJh...9323"
}
```

refresh_token: if you requested the offline_access scope

You can now store and use the access_token to make authenticated requests to Microsoft Graph.

### Sign the user out <a name="sign_out"></a>

1. Delete any cached access_token or refresh_token values you've previously received from the OAuth flow.
2. Perform any sign out actions in your application (for example, cleaning up local state, removing any cached items, etc.).
3. Make a call to the authorization web service using this URL: `GET https://login.microsoftonline.com/common/oauth2/v2.0/logout?post_logout_redirect_uri={redirect-uri}`

## Deleting a file <a name="delete"></a>

### HTTP request

```
DELETE /drives/{drive-id}/items/{item-id}
DELETE /groups/{group-id}/drive/items/{item-id}
DELETE /me/drive/items/{item-id}
DELETE /sites/{siteId}/drive/items/{itemId}
DELETE /users/{userId}/drive/items/{itemId}
```

Optional Header:

- if-match: String - If this request header is included and the eTag (or cTag) provided does not match the current tag on the item, a 412 Precondition Failed response is returned and the item will not be deleted.

### Response

If successful, this call returns a 204 No Content response to indicate that resource was deleted and there was nothing to return.  
`HTTP/1.1 204 No Content`

## SalesForce Integration <a name="salesforce_integration"></a>

TO - DO

# Remarks: <a name="remarks"></a>

- Links created using this action do not expire unless a default expiration policy is enforced for the organization.
- Links are visible in the sharing permissions for the item and can be removed by an owner of the item.
- Links always point to the current version of a item unless the item is checked out (SharePoint only).
- We can have multiple shared links with different priviliges for one document.

# Tools and Useful Links: <a name="tools"></a>

- [Graph Explorer](https://developer.microsoft.com/en-us/graph/graph-explorer)
- [OneDrive Rest API](https://docs.microsoft.com/en-us/onedrive/developer/rest-api/?view=odsp-graph-online)
- [OneDrive Integration Concepts](https://docs.microsoft.com/en-us/onedrive/developer/rest-api/concepts/?view=odsp-graph-online): To understand the difference between different endpoints documented in this markdown guide.

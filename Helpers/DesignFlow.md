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
2. Alternatively, a codeflow or tokenflow based approach automates the authentication process
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

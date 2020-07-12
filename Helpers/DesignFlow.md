# Tackling Individual Tasks
## Create a sharing link for a DriveItem
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

#### Creating Global Share Links:
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

#### Creating company sharable links:
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

#### Creating embedded links:
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

#### Remarks:
- Links created using this action do not expire unless a default expiration policy is enforced for the organization.
- Links are visible in the sharing permissions for the item and can be removed by an owner of the item.
- Links always point to the current version of a item unless the item is checked out (SharePoint only).
- We cannot have multiple shared links with different priviliges for one document (needs to be verified)

#### Tools and Useful Links:
- [Graph Explorer](https://developer.microsoft.com/en-us/graph/graph-explorer)
- [OneDrive Rest API](https://docs.microsoft.com/en-us/onedrive/developer/rest-api/?view=odsp-graph-online)

#### Creating expirable links for Document Sharing
We can configure this option in Office 365 admin center like below: (No 'obvious' way to do it programatically)
![alt text](https://github.com/Yashpandey4/OneDriveCLI/blob/master/Helpers/expire.png "Setting Expiration Date for Shared Docs")

Note: this only applies to external (anonymous) links. Internal (organisational) links dont expire.

## Authentication
Other than registering the app in AAD described in the root markdown README, the other methods are described below 

### Token Flow
This flow is useful for quickly obtaining an access token to use the OneDrive API in an interactive fashion. This flow does not provide a refresh token, and therefore is not a good fit for longterm access to resources.
1. To start the sign-in process with the token flow, use a web browser or web-browser control to load a URL request.  
```
GET https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={client_id}&scope={scope}
    &response_type=token&redirect_uri={redirect_uri}
```
Where:
  - client_id	The client ID value created for your application.
  - redirect_uri:	The redirect URL that the browser is sent to when authentication is complete.
  - response_type:	The type of response expected from the authorization flow. For this flow, the value must be token.
  - scope:	A space-separated list of scopes your application requires.
2. Upon successful authentication and authorization of your application, the web browser is redirected to the redirect URL provided with additional parameters added to the URL.
```
https://myapp.com/auth-redirect#access_token=EwC...EB
  &authentication_token=eyJ...3EM&token_type=bearer&expires_in=3600
  &scope=onedrive.readwrite&user_id=3626...1d
```

### Code Flow
The code flow for authentication is a three-step process with separate calls to authenticate and authorize the application and to generate an access token to use the OneDrive API. This also allows your application to receive a refresh token that will enable long-term use of the API in some scenarios, to allow access when the user isn't actively using your application.

1. To start the sign-in process with the code flow, use a web browser or web-browser control to load this URL request.
```
GET https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={client_id}&scope={scope}
  &response_type=code&redirect_uri={redirect_uri}
```
Response: Upon successful authentication and authorization of your application, the web browser will be redirected to your redirect URL with additional parameters added to the URL. `https://myapp.com/auth-redirect?code=df6aa589-1080-b241-b410-c4dff65dbf7c`

2. Redeem the code for access tokens
```
POST https://login.microsoftonline.com/common/oauth2/v2.0/token
Content-Type: application/x-www-form-urlencoded

client_id={client_id}&redirect_uri={redirect_uri}&client_secret={client_secret}
&code={code}&grant_type=authorization_code
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

client_id={client_id}&redirect_uri={redirect_uri}&client_secret={client_secret}
&refresh_token={refresh_token}&grant_type=refresh_token
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

### Sign the user out
1. Delete any cached access_token or refresh_token values you've previously received from the OAuth flow.
2. Perform any sign out actions in your application (for example, cleaning up local state, removing any cached items, etc.).
3. Make a call to the authorization web service using this URL: `GET https://login.microsoftonline.com/common/oauth2/v2.0/logout?post_logout_redirect_uri={redirect-uri}`

# High level stepwise overview:
## Creating and Sharing Files
1. The User logs in the Graph Node Endpoint with the account in which the file he wants to share is located via browser
2. Alternatively, a codeflow or tokenflow based approach automates the authentication process
3. The File to be shared is created/fetched and transferred to a folder 'SalesForce' in the users OneDrive
4. Based on the size (4 MB) the file is uploaded to user's OneDrive.
5. Share Links for the file is programmatically generated using a user defined access level (user can be asked if he would like to share the file globally or within the org, and for which access level: View/Comment/Edit)
6. We re-upload the file back to source (SalesForce in this case) and share the URL with relevant people
7. User is signed out and all auth keys/tokens are deleted

## SalesForce Integration
TO - DO
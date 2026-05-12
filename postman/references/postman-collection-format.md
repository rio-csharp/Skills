# Postman Collection JSON Format

A Postman collection is a JSON file that groups API requests with shared configuration.

## Top-Level Structure

```json
{
  "info": { ... },
  "item": [ ... ],
  "variable": [ ... ],
  "auth": { ... }
}
```

## info

Metadata about the collection.

```json
"info": {
  "name": "My API Collection",
  "description": "API tests for the checkout service",
  "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
}
```

## item

An array of items. Each item is either a request or a folder (containing more items).

```json
"item": [
  {
    "name": "Get User",
    "request": { ... }
  },
  {
    "name": "User Folder",
    "item": [
      { "name": "Create User", "request": { ... } }
    ]
  }
]
```

## request

```json
"request": {
  "method": "GET",
  "header": [
    { "key": "Content-Type", "value": "application/json" }
  ],
  "url": {
    "raw": "https://api.example.com/users/1",
    "protocol": "https",
    "host": ["api", "example", "com"],
    "path": ["users", "1"]
  },
  "body": {
    "mode": "raw",
    "raw": "{\"name\": \"John\"}"
  },
  "description": "Get a user by ID"
}
```

## test

Postman scripts run after the request completes.

```json
"item": [
  {
    "name": "Get User",
    "event": [
      {
        "listen": "test",
        "script": {
          "type": "text/javascript",
          "exec": [
            "pm.test('Status is 200', function() {",
            "  pm.response.to.have.status(200);",
            "});",
            "pm.test('Has name', function() {",
            "  var json = pm.response.json();",
            "  pm.expect(json.name).to.not.be.empty;",
            "});"
          ]
        }
      }
    ],
    "request": { ... }
  }
]
```

## Variables

Collection-level variables:

```json
"variable": [
  { "key": "baseUrl", "value": "https://api.example.com" },
  { "key": "apiKey", "value": "" }
]
```

Use in requests: `{{baseUrl}}/users`

## Auth

Collection-level authentication:

```json
"auth": {
  "type": "bearer",
  "bearer": [
    { "key": "token", "value": "{{token}}", "type": "string" }
  ]
}
```

## Key Fields Reference

| Field | Description |
|-------|-------------|
| `info.name` | Collection name |
| `info.description` | Collection description |
| `item[].name` | Request or folder name |
| `item[].request.method` | HTTP method (GET, POST, PUT, DELETE, etc.) |
| `item[].request.url.raw` | Full URL string |
| `item[].event[].script.exec` | Array of test script lines |
| `variable[].key` | Variable name |
| `variable[].value` | Default variable value |

## Reading a Collection

```bash
cat collection.json | jq .
cat collection.json | jq '.info.name'
cat collection.json | jq '.item[].name'
```
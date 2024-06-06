# dotpaste

Another terminal-friendly pastebin.

## Description

All data will be volatile. No persistence.

Operations are made at filesystem-level, instead of a database-level.

## Environment

You only need to set an upload's directory. Default value will create an `uploads` directory in the executable's path.

You could set it from the environment variable: `DOTPASTE_UPLOADS_PATH`

Or, pass it as an argument when running the server:

`dotnet dotpaste.dll --uploads-path=/my/custom/path`

`dotnet dotpaste.dll --uploads-path /my/custom/path`

`dotnet dotpaste.dll -u /my/custom/path`

**NOTE**: Arguments will take precedence over env-vars.

## Usage

Upload file's content from ___STDIN___:

`cat my_file | curl -sF 'content=<-' http://mydomain[:$PORT]`

`cat my_file | curl -H 'Content-Type: text/plain' --data-binary @- http://mydomain[:$PORT]`

Upload a file directly:

`curl -H 'Content-Type: text/plain' --data-binary @my_file http://mydomain[:$PORT]`

The responses will be an URL to the file's content:

`http://mydomain[:$PORT]/content/Aai4q`

## Responses

By default, all responses are plain text.

For html responses, you could apply syntax higlighting by adding the query param `lang`.

`http://mydomain[:$PORT]/content/Aai4q?lang=csharp`

For a list of supported languages see:

https://prismjs.com/#supported-languages

## TODO

- Limit content's length.
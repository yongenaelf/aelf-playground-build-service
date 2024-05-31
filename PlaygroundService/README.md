# Playground Service

## Backend Service Details
This is a C# backend service to execute dotnet build on the project uploaded by the user
Service is hosted as a REST API which connects to a Orleans cluster to execute the build

## API Endpoints

### POST /api/build
 sample API: http://localhost:7020/playground/build
 input: fileData which is a zip file of the project to be built
 
#### Usage

```sh
response=$(curl -s -w "%{http_code}" -X POST "http://localhost:7020/playground/build" \
-H "accept: application/json" \
-H "Content-Type: multipart/form-data" \
-F "contractFiles=@/path/to/your/project.zip" \
-o output.dll)

http_code=${response: -3} # get the last 3 characters, which is the status code
if [ "$http_code" != "200" ]; then
    cat output.dll | jq '.Message' # print the error message
fi
```

```C#
var client = new HttpClient();
var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:7020/playground/build");
var content = new MultipartFormDataContent();
content.Add(new StreamContent(File.OpenRead("/Users/kanth/Documents/misc/aelf/playground/MyProject.zip")), "contractFiles", "/Users/xyz/Documents/aelf/playground/MyProject.zip");
request.Content = content;
var response = await client.SendAsync(request);
response.EnsureSuccessStatusCode();
Console.WriteLine(await response.Content.ReadAsStringAsync());
if (response.IsSuccessStatusCode)
{
    using (var stream = await response.Content.ReadAsStreamAsync())
    {
        using (var fileStream = File.Create("output.dll"))
        {
            stream.CopyTo(fileStream);
        }
    }
}
```

```js
const formdata = new FormData();
formdata.append("contractFiles", fileInput.files[0], "MyProject.zip");

const requestOptions = {
 method: "POST",
 body: formdata,
 redirect: "follow"
};

fetch("http://localhost:7020/playground/build", requestOptions)
        .then((response) => {
         if (!response.ok) {
          throw new Error("Network response was not ok");
         }
         return response.blob();
        })
        .then((blob) => {
         // Create a new object URL for the blob
         const url = URL.createObjectURL(blob);

         // Create a link and programmatically click it to download the file
         const link = document.createElement('a');
         link.href = url;
         link.setAttribute('download', 'output.dll');
         document.body.appendChild(link);
         link.click();

         // Remove the link from the body
         document.body.removeChild(link);
        })
        .catch((error) => console.error(error));
```

#### Postman Collection

[Playground Service Postman Collection](./Aelf Playground.postman_collection.json)
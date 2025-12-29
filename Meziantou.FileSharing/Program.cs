using Meziantou.FileSharing;

var builder = WebApplication.CreateBuilder();
builder.Services.AddSingleton<FileService>();

var app = builder.Build();
app.MapGet("/", (HttpContext context) =>
{
    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    return TypedResults.Content($$"""
   <!DOCTYPE html>
<html>

<head>
  <meta charset="utf-8">
  <title>File Sharing</title>
  <link rel="icon" href="/favicon.svg">
  <style>
    body {
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
      max-width: 900px;
      margin: 40px auto;
      padding: 0 20px;
      line-height: 1.6;
      color: #333;
    }

    h1 {
      color: #2c3e50;
    }

    h2 {
      color: #34495e;
      margin-top: 30px;
      border-bottom: 2px solid #3498db;
      padding-bottom: 5px;
    }

    h3 {
      color: #555;
      margin-top: 20px;
    }

    code {
      background: #f4f4f4;
      padding: 2px 6px;
      border-radius: 3px;
      font-family: 'Consolas', 'Monaco', monospace;
      font-size: 0.9em;
    }

    pre {
      background: #2d2d2d;
      color: #f8f8f2;
      padding: 15px;
      border-radius: 5px;
      overflow-x: auto;
      margin: 10px 0;
    }

    pre code {
      background: none;
      padding: 0;
      color: #f8f8f2;
    }

    .form-section {
      background: #f9f9f9;
      padding: 20px;
      border-radius: 8px;
      margin: 20px 0;
      border: 1px solid #ddd;
    }

    button {
      background: #3498db;
      color: white;
      border: none;
      padding: 10px 20px;
      border-radius: 5px;
      cursor: pointer;
      font-size: 1em;
    }

    button:hover {
      background: #2980b9;
    }

    input[type="file"] {
      margin: 10px 0;
    }

    .quick-links {
      background: #e8f4f8;
      padding: 15px;
      border-radius: 5px;
      margin: 20px 0;
    }

    .quick-links ul {
      margin: 10px 0;
    }
  </style>
</head>

<body>
  <h1>📁 File Sharing Service</h1>

  <div class="quick-links">
    <strong>Quick Links:</strong>
    <ul>
      <li><a href="/files">📋 List all files</a></li>
    </ul>
  </div>

  <div class="form-section">
    <h2>Upload Files (Web Form)</h2>
    <form method="post" enctype="multipart/form-data">
      <input type="file" name="files" multiple />
      <button type="submit">Upload</button>
    </form>
  </div>

  <h2>📤 Upload Files via Command Line</h2>

  <h3>PowerShell</h3>
  <pre><code># Upload a single file
Invoke-WebRequest -Uri "{{baseUrl}}/" -Method Post -InFile "file.txt" -ContentType "multipart/form-data"

# Upload multiple files
$files = @("file1.txt", "file2.pdf", "image.jpg")
foreach ($file in $files) {
  Invoke-WebRequest -Uri "{{baseUrl}}/" -Method Post -InFile $file -ContentType "multipart/form-data"
}</code></pre>

  <h3>curl (Windows/Linux/macOS)</h3>
  <pre><code># Upload a single file
curl -F "files=@file.txt" {{baseUrl}}/

# Upload multiple files
curl -F "files=@file1.txt" -F "files=@file2.pdf" -F "files=@image.jpg" {{baseUrl}}/</code></pre>

  <h2>📥 Download Files via Command Line</h2>

  <h3>PowerShell</h3>
  <pre><code># Download a file
Invoke-WebRequest -Uri "{{baseUrl}}/files/file.txt" -OutFile "file.txt"

# Download with original filename preserved
$filename = "file.txt"
Invoke-WebRequest -Uri "{{baseUrl}}/files/$filename" -OutFile $filename

# Download all files (requires listing first)
$files = (Invoke-WebRequest -Uri "{{baseUrl}}/files").Content | ConvertFrom-Json
foreach ($file in $files) {
  Invoke-WebRequest -Uri "{{baseUrl}}/files/$file" -OutFile $file
}</code></pre>

  <h3>curl (Windows/Linux/macOS)</h3>
  <pre><code># Download a file
curl -o file.txt {{baseUrl}}/files/file.txt

# Download with original filename preserved
curl -O -J {{baseUrl}}/files/file.txt

# Download multiple files
curl -O -J {{baseUrl}}/files/file1.txt -O -J {{baseUrl}}/files/file2.pdf</code></pre>

  <h3>wget (Linux/macOS)</h3>
  <pre><code># Download a file
wget {{baseUrl}}/files/file.txt

# Download with custom output name
wget -O myfile.txt {{baseUrl}}/files/file.txt

# Download multiple files
wget {{baseUrl}}/files/file1.txt {{baseUrl}}/files/file2.pdf {{baseUrl}}/files/image.jpg</code></pre>

  <h2>🔍 List Files</h2>

  <h3>PowerShell</h3>
  <pre><code># Get list of files as JSON
$files = Invoke-RestMethod -Uri "{{baseUrl}}/files"
$files

# Pretty print
$files | ForEach-Object { Write-Host $_ }</code></pre>

  <h3>curl</h3>
  <pre><code># Get list of files
curl {{baseUrl}}/files

# Pretty print with jq (if installed)
curl {{baseUrl}}/files | jq</code></pre>

  <h3>wget</h3>
  <pre><code># Get list of files
wget -qO- {{baseUrl}}/files</code></pre>
</body>

</html>
""", "text/html");
});

app.MapPost("/", async (FileService service, IFormFileCollection files) =>
{
    foreach (var file in files)
    {
        await using var stream = file.OpenReadStream();
        await service.AddFileAsync(file.FileName, stream);
    }
    return TypedResults.Redirect("/");
}).DisableAntiforgery();

app.MapGet("/files", (FileService service) =>
{
    return TypedResults.Ok(service.GetFiles());
});

app.MapGet("/files/{name}", (FileService service, string name) =>
{
    var stream = service.GetByName(name);
    return TypedResults.Stream(stream, "application/octet-stream", name);
});

app.Run();

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

var app = WebApplication.Create();

app.MapGet("/", () =>
{
    return Results.Content(@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>Image Uploader</title>
                        <style>
                            body {
                                font-family: Arial, sans-serif;
                            }
        
                            .form-container {
                                max-width: 400px;
                                margin: 0 auto;
                                padding: 20px;
                                background-color: #f2f2f2;
                                border-radius: 5px;
                                box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
                            }
        
                            .form-container h1 {
                                text-align: center;
                                margin-bottom: 20px;
                            }
        
                            .form-container label {
                                display: block;
                                margin-bottom: 10px;
                            }
        
                            .form-container input[type='text'],
                            .form-container input[type='file'] {
                                width: 100%;
                                padding: 10px;
                                border: 1px solid #ccc;
                                border-radius: 5px;
                            }
        
                            .form-container input[type='submit'] {
                                width: 100%;
                                padding: 10px;
                                background-color: #4caf50;
                                color: #fff;
                                border: none;
                                border-radius: 5px;
                                cursor: pointer;
                            }
        
                            .form-container input[type='submit']:hover {
                                background-color: #45a049;
                            }
                        </style>
                    </head>
                    <body>
                        <div class='form-container'>
                            <h1>Upload Image</h1>
                            <form action='/image' method='post' enctype='multipart/form-data'>
                                <label for='imageTitle'>Title of image:</label>
                                <input type='text' id='imageTitle' name='imageTitle' required>
            
                                <label for='imageFile'>Image file (JPEG, PNG, GIF):</label>
                                <input type='file' id='imageFile' name='imageFile' accept='.jpeg, .png, .gif' required>
            
                                <input type='submit' value='Upload'>
                            </form>
                        </div>
                    </body>
                    </html>
            ", "text/html");
});

app.MapPost("/image", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var title = form["imageTitle"];
    var file = form.Files.GetFile("imageFile");
    var fileExtension = Path.GetExtension(file.FileName).ToLower();

    if (string.IsNullOrEmpty(title) || file == null || file.Length == 0)
    {
        return Results.BadRequest("Invalid request. Title and image file are required.");
    }

    if (fileExtension != ".jpeg" && fileExtension != ".gif" && fileExtension != ".png")
    {
        return Results.BadRequest("Invalid file format. Only .jpeg, .png, and .gif are supported.");
    }

    var imageId = Guid.NewGuid().ToString();
    var imagePath = Path.Combine("pictures", $"{imageId}{Path.GetExtension(file.FileName)}");
    using (var fileStream = new FileStream(imagePath, FileMode.Create))
    {
        await file.CopyToAsync(fileStream);
    }

    var imageDetails = new
    {
        Id = imageId,
        Title = title.ToString(),
        FileName = file.FileName,
        FileExtension = fileExtension
    };

    var jsonData = JsonSerializer.Serialize(imageDetails);
    var jsonPath = "pictures/data.json";
    if (File.Exists(jsonPath))
    {
        File.Delete(jsonPath);
    }
   await File.WriteAllTextAsync(jsonPath, $"{jsonData}{Environment.NewLine}");


    var redirectUrl = $@"/pictures/{imageId}";
    return Results.Redirect(redirectUrl);
});


app.MapGet("/pictures/{id}", (string id) =>
{
    var jsonData = File.ReadAllText("pictures/data.json");
    var root = getRoot(jsonData);
    string imageId = root.GetProperty("Id").GetString();
    string title = root.GetProperty("Title").GetString();

    if (imageId != id)
    {
        return Results.NotFound("Image not found");
    }

    var htmlContent = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <title>{title}</title>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    background-color: #f8f8f8;
                    margin: 0;
                    padding: 20px;
                }}

                h1 {{
                    text-align: center;
                    color: #333;
                }}

                .image-container {{
                    text-align: center;
                    margin-top: 20px;
                }}

                .image-container img {{
                    max-width: 100%;
                    height: auto;
                }}

                .button-container {{
                    text-align: center;
                    margin-top: 20px;
                }}

                .button-container button {{
                    padding: 10px 20px;
                    font-size: 16px;
                    background-color: #333;
                    color: #fff;
                    border: none;
                    cursor: pointer;
                }}
            </style>
            </head>
            <body>
                <h1>{title}</h1>
                <div class='image-container'>
                    <img src='/store-image' alt='{title}' />
                </div>
                <div class='button-container'>
                    <button onclick='redirectBack()'>Upload Again!</button>
                </div>
                <script>
                    function redirectBack() {{
                        window.location.href = '/';
                    }}
                </script>
            </body>
            </html>
";
    return Results.Content(htmlContent, "text/html");
});

app.MapGet("/store-image", () =>
{
    var jsonData = File.ReadAllText("pictures/data.json");
    var root = getRoot(jsonData.ToString());
    string imageId = root.GetProperty("Id").GetString();
    string fileExtension = root.GetProperty("FileExtension").GetString();
    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "pictures", $"{imageId}{fileExtension}");

    return Results.File(imagePath, "image/jpeg");
});

JsonElement getRoot(string jsonData)
{
    JsonDocument document = JsonDocument.Parse(jsonData);
    JsonElement root = document.RootElement;
    return root;
}

app.Run();

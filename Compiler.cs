using System;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using System.Diagnostics;
class Generator
{


    static async Task Main(string[] args)
    {

        Int64 place_id = 0;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Loxer's Game Quick Acess");
        Console.WriteLine("Input game id:");
        Console.ForegroundColor = ConsoleColor.Blue;
        while (place_id==0)
        {
            Console.Write(">");
            try
            {
                place_id = Convert.ToInt64(Console.ReadLine());
            }
            catch{}
    
        }

        string linkcode = "";
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Input pscode link code, type no for no ps code. (its the link code, not share code):");
        Console.ForegroundColor = ConsoleColor.Blue;
        while (linkcode=="")
        {
            Console.Write(">");
            try
            {
                linkcode = Console.ReadLine();
                if (linkcode.ToLower().Equals("no")){
                    linkcode = "";
                    break;
                }
            }
            catch{}

        }
        string launcher_name = "";
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Input launcher name:");
        Console.ForegroundColor = ConsoleColor.Blue;
        while (launcher_name == "")
        {
            Console.Write(">");
            try
            {
                launcher_name = Console.ReadLine();
            }
            catch{}
    
        }
        
        Console.ForegroundColor = ConsoleColor.Gray;
        await build_files(place_id,linkcode,launcher_name);
        
    }
    
    static async Task build_files(Int64 place_id, string linkcode, string name)
    {   
        Console.WriteLine("Creating launcher for game id: "+place_id);
        using HttpClient client = new HttpClient();
        string projectDir = Path.Combine(Directory.GetCurrentDirectory(), "GenLauncher");
        if(Path.Exists(projectDir))
            System.IO.Directory.Delete(projectDir,true);
        Directory.CreateDirectory(projectDir);
        string thumbnail = "https://thumbnails.roblox.com/v1/places/gameicons?placeIds="+place_id+"&size=512x512&format=Png&isCircular=false";
        
        string response = await client.GetStringAsync(thumbnail);
        string url = response[response.IndexOf("https://")..(response.IndexOf(""","version""")-1)];
        Console.WriteLine("Got thumbnail");
        byte[] image_data = await client.GetByteArrayAsync(url);

        //resize and reformat png
        using var image_stream = new MemoryStream(image_data);
        using var image = Image.Load(image_stream);
        using var image_output = new MemoryStream();
        //image.Mutate(x=>x.Resize(256,256));
        image.Save(image_output, new PngEncoder());

        //load data n write headers for ico
        byte[] res_image_data = image_output.ToArray();
        using(var fs = new FileStream(Path.Combine(projectDir,"logo.ico"), FileMode.Create))
        using(var writer = new BinaryWriter(fs))
        {
            //ico headers : reversed, type, numb images
            writer.Write((short)0);
            writer.Write((short)1);
            writer.Write((short)1);
            //icon directory| width, height, color, revered, color planes, bits per pixel, size of image ata, offset of image data
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((short)1);
            writer.Write((short)32);
            writer.Write(res_image_data.Length);
            writer.Write(22);
            
            // save data
            writer.Write(res_image_data);
        }
        

        //await File.WriteAllBytesAsync(Path.Combine(projectDir, "logo.png"), data);
        //ConvertPngToIco(Path.Combine(projectDir, "logo.png"), Path.Combine(projectDir, "logo.ico"));
        var sb = new StringBuilder();

        sb.AppendLine("using System.Diagnostics;");
        sb.AppendLine("using Microsoft.Win32;");
        sb.AppendLine("namespace Launcher");
        sb.AppendLine("{");
        sb.AppendLine("    public class MainProgram");
        sb.AppendLine("    {");
        sb.AppendLine("        static void Main(string[] args)");
        sb.AppendLine("        {");
        sb.AppendLine("            string Roblox_EXE = Roblox_Version();");
        sb.AppendLine("            Process.Start(Roblox_EXE, \"roblox://placeId="+place_id+"&linkCode="+linkcode+"\");");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        static string Roblox_Version()");
        sb.AppendLine("        {");
        sb.AppendLine("            try{");
        sb.AppendLine("                string keyPath = @\"SOFTWARE\\Classes\\roblox-player\\DefaultIcon\";");
        sb.AppendLine("                var key = Registry.CurrentUser.OpenSubKey(keyPath);");
        sb.AppendLine("                if (key!= null){");
        sb.AppendLine("                    return Convert.ToString(key.GetValue(null));");
        sb.AppendLine("                } else");
        sb.AppendLine("                {");
        sb.AppendLine("                    Console.WriteLine(\"Key is null\");");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("            catch");
        sb.AppendLine("            {");
        sb.AppendLine("                Console.WriteLine(\"Error occured when pulling version\");");
        sb.AppendLine("            }");
        sb.AppendLine("            return \"null\";");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");


        File.WriteAllText(Path.Combine(projectDir, name+".cs"),sb.ToString());


                // Write .csproj
        File.WriteAllText(Path.Combine(projectDir, name+".csproj"),
        @"
        <Project Sdk=""Microsoft.NET.Sdk"">

        <PropertyGroup>
            <OutputType>WinExe</OutputType>
            <TargetFramework>net10.0</TargetFramework>

            <ApplicationIcon>logo.ico</ApplicationIcon>

            <PublishSingleFile>true</PublishSingleFile>
            <SelfContained>true</SelfContained>
            <RuntimeIdentifier>win-x64</RuntimeIdentifier>
            <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>

            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
        </PropertyGroup>
        </Project>

        ");
        
        Console.WriteLine("Creating Binary");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "publish -c Release",
            WorkingDirectory = projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var proc = Process.Start(psi);
        proc.WaitForExit();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Created Launch Binary!");
        //\bin\Release\net10.0\win-x64\publish
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("Removing temp files and putting launcher to desktop");    
        string userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string launcher_dir = Path.Combine(projectDir,@"bin","Release","net10.0","win-x64","publish",name+".exe");
        Console.Write("Created launcher at "+Path.Combine(userDir,"Desktop",name+".exe"));
        File.Move(launcher_dir,Path.Combine(userDir,"Desktop",name+".exe"),true);
        System.IO.Directory.Delete(projectDir,true);
        Console.ReadKey();
    }
}

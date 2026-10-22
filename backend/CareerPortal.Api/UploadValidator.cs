using System.IO.Compression;
using System.Text;
public static class UploadValidator
{
    public const int MaxBytes=10*1024*1024;
    public static async Task<(byte[]? Data,string Name,string Mime,string? Error)> Read(HttpRequest request)
    {
        var name=request.Query["fileName"].ToString().Normalize();
        if(string.IsNullOrWhiteSpace(name)||name.Length>180||name!=Path.GetFileName(name)||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0||name.Any(char.IsControl)||name.EndsWith('.')||name.EndsWith(' '))return(null,name,"","Tên file không hợp lệ.");
        var ext=Path.GetExtension(name).ToLowerInvariant();
        if(ext is not(".pdf" or ".docx"))return(null,name,"","Chỉ nhận PDF hoặc DOCX.");
        if(request.ContentLength>MaxBytes)return(null,name,"","File vượt quá 10 MB.");
        using var output=new MemoryStream();var buffer=new byte[65536];int n;
        while((n=await request.Body.ReadAsync(buffer,request.HttpContext.RequestAborted))>0){if(output.Length+n>MaxBytes)return(null,name,"","File vượt quá 10 MB.");await output.WriteAsync(buffer.AsMemory(0,n));}
        var data=output.ToArray();if(data.Length==0)return(null,name,"","File rỗng.");
        if(ext==".pdf"){
            if(data.Length<10||!data.AsSpan(0,5).SequenceEqual("%PDF-"u8)||!Encoding.ASCII.GetString(data.AsSpan(Math.Max(0,data.Length-2048))).Contains("%%EOF"))return(null,name,"","Nội dung không phải PDF hợp lệ.");
        }else{
            try{using var zip=new ZipArchive(new MemoryStream(data));
                if(zip.GetEntry("[Content_Types].xml")==null||zip.GetEntry("word/document.xml")==null||zip.Entries.Count>2000||zip.Entries.Sum(x=>x.Length)>50L*1024*1024||zip.Entries.Any(x=>x.FullName.Contains("..")||x.FullName.EndsWith("vbaProject.bin",StringComparison.OrdinalIgnoreCase)))return(null,name,"","Cấu trúc DOCX không hợp lệ.");
            }catch(InvalidDataException){return(null,name,"","Nội dung không phải DOCX hợp lệ.");}
        }
        return(data,name,ext==".pdf"?"application/pdf":"application/vnd.openxmlformats-officedocument.wordprocessingml.document",null);
    }
}

// Solution: SentinelCore
// Project:   SentinelCoreAdmin.Core
// File:         FileService.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using Newtonsoft.Json;

using SentinelCoreAdmin.Core.Contracts.Services;




namespace SentinelCoreAdmin.Core.Services;





public class FileService : IFileService
{

    public void Delete(string folderPath, string fileName)
    {
        if (fileName != null && File.Exists(Path.Combine(folderPath, fileName)))
        {
            File.Delete(Path.Combine(folderPath, fileName));
        }
    }








    public T Read<T>(string folderPath, string fileName)
    {
        string path = Path.Combine(folderPath, fileName);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json);
        }

        return default;
    }








    public void Save<T>(string folderPath, string fileName, T content)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileContent = JsonConvert.SerializeObject(content);
        File.WriteAllText(Path.Combine(folderPath, fileName), fileContent, System.Text.Encoding.UTF8);
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CheckConsecutiveStudioRuns
{
    public static class Utils
    {
        private const int NumberOfSecondsToCheckLastRun = 3;

        public static string GetStringSha256Hash(string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                return String.Empty;
            }

            using (var sha = new SHA256Managed())
            {
                byte[] textData = Encoding.UTF8.GetBytes(text);
                byte[] hash = sha.ComputeHash(textData);

                return BitConverter.ToString(hash).Replace("-", String.Empty);
            }
        }
        public static bool HasProperty(object objectToCheck, string methodName)
        {
            PropertyInfo propertyInfo= objectToCheck.GetType().GetProperty(methodName);
            if (propertyInfo!=null)
            {
                return true;
            }
            return false;
        }
        public static string GetPathToPersistanceFile(string projectName, string pathToProject)
        {
            var hash = GetStringSha256Hash(pathToProject);
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            return Path.Combine(appDataPath, "UiPathProjects", $"{projectName} - {hash}.txt");
        }

        private static void CreatePersistanceFileIfNotExists(string filePath)
        {
            if (File.Exists(filePath))
            {
                return;
            }

            var uiPathProjectsFolder = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(uiPathProjectsFolder))
            {
                var directory = Directory.CreateDirectory(uiPathProjectsFolder);
            }

            if (!File.Exists(filePath))
            {
                var file = File.Create(filePath);
                file.Close();
            }
        }

        public static void WritePersistanceFile(string persistanceFilePath, int count, string hash, DateTime lastUpdateTime)
        {
            CreatePersistanceFileIfNotExists(persistanceFilePath);

            var persistanceInfo = new PersistanceInfoModel()
            {
                Count = count,
                Hash = hash,
                LastUpdateTime = lastUpdateTime
            };

            File.WriteAllText(persistanceFilePath, JsonSerializer.Serialize(persistanceInfo));
        }
        public static void WritePersistanceFile(string persistanceFilePath,  PersistanceInfoModel[] persistanceInfo)
        {
            CreatePersistanceFileIfNotExists(persistanceFilePath);
            
            File.WriteAllText(persistanceFilePath, JsonSerializer.Serialize(persistanceInfo));
        }

        public static PersistanceInfoModel[] ReadPersistanceFileArray(string persistanceFilePath)
        {
            if (!File.Exists(persistanceFilePath))
            {
                return null;
            }

            var fileJson = File.ReadAllText(persistanceFilePath);
            PersistanceInfoModel[] information;
            try
            {
                information = JsonSerializer.Deserialize<PersistanceInfoModel[]>(fileJson);
            }
            catch
            {
                return null;
            }

            return information;
        }
        public static PersistanceInfoModel ReadPersistanceFile(string persistanceFilePath)
        {
            if (!File.Exists(persistanceFilePath))
            {
                return null;
            }

            var fileJson = File.ReadAllText(persistanceFilePath);
            PersistanceInfoModel information;
            try
            {
                information = JsonSerializer.Deserialize<PersistanceInfoModel>(fileJson);
            }
            catch
            {
                return null;
            }

            return information;
        }

        public static bool WasLastRunTooRecent(string persistanceFilePath)
        {
            var persistanceInfo = ReadPersistanceFile(persistanceFilePath);

            if (persistanceInfo == null)
            {
                return false;
            }

            return (DateTime.Now - persistanceInfo.LastUpdateTime).TotalSeconds < NumberOfSecondsToCheckLastRun;
        }

        public static string ComputeHashFromMainXaml(string projectFilePath)
        {
            if (!File.Exists(projectFilePath))
            {
                return string.Empty;
            }

            var mainXamlPath = Path.Combine(Path.GetDirectoryName(projectFilePath), "Main.xaml");

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(mainXamlPath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        public static string ComputeHashFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return string.Empty;
            }

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static PersistanceInfoModel[] GenerateHashFile(string projectFilePath)
        {
            string projectDirectory = Path.GetDirectoryName(projectFilePath);
                       
            string[] xamlFiles = Directory.GetFiles(projectDirectory, "*.xaml", SearchOption.AllDirectories);
            
            PersistanceInfoModel[] persistanceInfoModels = new PersistanceInfoModel[xamlFiles.Length+1];
            #region Special Handling for Project.Json file
            var hashFilePath = Path.Combine(Path.GetDirectoryName(projectFilePath), "project.json");
            persistanceInfoModels[0] = new PersistanceInfoModel
            {
                Count = 1,
                FileName = hashFilePath,
                Hash = ComputeHashFromFile(hashFilePath),
                LastUpdateTime = DateTime.Now
            };
            #endregion

            int counter = 1;
            foreach (var item in xamlFiles)
            {
                hashFilePath = item;
                persistanceInfoModels[counter] = new PersistanceInfoModel
                {
                    Count = 1,
                    FileName = hashFilePath,
                    Hash = ComputeHashFromFile(hashFilePath),
                    LastUpdateTime = DateTime.Now
                };
                counter++;
            }

            return persistanceInfoModels;
        }
        public static PersistanceInfoModel GetFileDetails(string fileName, PersistanceInfoModel[] fileData)
        {
            IEnumerable<PersistanceInfoModel> result = from info in fileData
                                                       where info.FileName == fileName
                                                       select info;
            PersistanceInfoModel data=null;
            if (result!=null)
            {
                PersistanceInfoModel[] resultArr = result.ToArray();
                if (resultArr!=null && resultArr.Length>0)
                {
                    data = resultArr[0];
                }                 
            }
            return data;
        }
    }
}

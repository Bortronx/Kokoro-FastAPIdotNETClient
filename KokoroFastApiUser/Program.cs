using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

/*
 * This is a C# console application that converts text files to audio using a local FastAPI TTS service.
 * It reads .txt files from the current directory, splits the text into chunks, and sends them to the TTS API.
 * The generated audio files are saved in an output directory, and all audio chunks are merged into a single file.
 *
 * The application supports various parameters such as chunk size, voice model, speed, and output format.
 *
 * Many options are hardcoded like API URL, Output Folder, etc. but can be changed in the code.
 */
class Program
{
    private static int NextChunkIndex;
    private static string OutputFolderName = "Output";
    private static int ExceptionCount = 0;
    private static string DockerTTSContainerID = "900d87d42630e13c9335230aa60464a43656c8d29bb7d5a88158575f639ccb0f";
    private static string LocalAPIURL = "http://localhost:8880/v1/audio/speech";

    private static DirectoryInfo CurrentOutputDirectory;

    static async Task Main(string[] args)
    {
       

        bool IsEndOfFile = false;
        string LastAudioFileName = "output";
        string FileFormat = "mp3";
        string NextTextToConvert = "";
        string FileNameWithExtension = "";
        string FileName = "";
        int MaxBytes = 500;
        string Model = "kokoro";
        string Voice = "af_bella";
        float Speed = 1.0f;
        int StartLineIndex = 0;
        int StartWordIndex = 0;
        int LastLineIndex = 0;
        int LastWordIndex = 0;
        bool IsManualParameterSelectionEnabled = true;
        int StartFromChunk= 0;
        int NumFailedSplits = 0;

        void SetParameters()
        {
            // if arguments are given set NextChunkIndex, OutputFolderName and LastLineIndex
            foreach (var arg in args)
            {
                var splitArg = arg.Split('=');
                var key = splitArg[0];
                var value = splitArg[1];

                switch (key)
                {
                    case "NextChunkIndex":
                        NextChunkIndex = int.Parse(value);
                        break;
                    case "OutputFolderName":
                        OutputFolderName = value;
                        break;
                    case "MaxCharacters":
                        MaxBytes = int.Parse(value);
                        break;
                    case "Model":
                        Model = value;
                        break;
                    case "Voice":
                        Voice = value;
                        break;
                    case "Speed":
                        Speed = float.Parse(value);
                        break;
                    case "Continue":
                        var indices = value.Split(',');
                        StartLineIndex = int.Parse(indices[0]);
                        StartWordIndex = int.Parse(indices[1]);
                        break;
                    case "IsManual":
                        IsManualParameterSelectionEnabled = bool.Parse(value);
                        break;
                    case "StartFromChunk":
                        StartFromChunk = int.Parse(value);
                        break;
                    case "FileFormat":
                        FileFormat = value;
                        break;
                    case "DockerTTSContainerID":
                        DockerTTSContainerID = value;
                        break;
                    case "LocalAPIURL":
                        LocalAPIURL = value;
                        break;
                    case "RestartAPI":
                        if (bool.Parse(value))
                        {
                            RestartAPI();
                        }
                        break;
                }
            }
        }

        SetParameters();

        if (IsManualParameterSelectionEnabled)
        {

            // Message Showing  all the argument options
            Console.WriteLine("|---------------------------------------------|");
            Console.WriteLine("Separate Arguments with spaces");
            Console.WriteLine("Arguments:");
            Console.WriteLine("NextChunkIndex=<int> - Set the next chunk index.");
            Console.WriteLine("OutputFolderName=<string> - Set the output folder name.");
            Console.WriteLine("LastLineIndex=<int> - Set the last line index.");
            Console.WriteLine("LastWordIndex=<int> - Set the last word index.");
            Console.WriteLine("MaxCharacters=<int> - Set the maximum number of characters per chunk.");
            Console.WriteLine("Model=<string> - Set the model to use.");
            Console.WriteLine("Voice=<string> - Set the voice to use.");
            Console.WriteLine("Speed=<float> - Set the speed of the voice.");
            Console.WriteLine("StartFromChunk=<int> - Chunk to start from");
            Console.WriteLine("Continue=<int,int> - Continue from a specific line and word index.");
            Console.WriteLine("FileFormat=<string> - Ex: mp3");
            Console.WriteLine("DockerTTSContainerID=<string> - Set the Docker TTS Container ID. This is required for restarting the API.");
            Console.WriteLine("LocalAPIURL=<string> - KokoroTTS FastAPI local host url");
            Console.WriteLine("RestartAPI=<bool> - Restarts API. True only option");
            Console.WriteLine("|---------------------------------------------|");

            var readLine = Console.ReadLine();
           if (readLine != null) args = readLine.Split(' ');


           SetParameters();
        }
        

        // Start Timer
        var watch = System.Diagnostics.Stopwatch.StartNew();

        //  Get current directory and find the first file with .txt extension
        string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.txt");

        if (files.Length <= 0)
        {
            Console.WriteLine("No .txt file found in the current directory.");
            Console.ReadLine();
            return;
        }

        for (int j = 0; j < files.Length; j++)
        {
            NextChunkIndex = 0;

            // Start Timer
            var fileTimeWatch = System.Diagnostics.Stopwatch.StartNew();

            // Get Only the file name with extension
            FileNameWithExtension = Path.GetFileName(files[j]);
            FileName = Path.GetFileNameWithoutExtension(files[j]);

            Console.WriteLine($"File Used: {FileNameWithExtension}");

          
   


            // Import text from file and split it into 500kb chunks containing full words
            string[] lines = File.ReadAllLines(FileNameWithExtension);

            // Calculate the number of ConvertedTextsToAudio based on the MaxBytes words and lines
            FileInfo fileInfo = new FileInfo(FileNameWithExtension);

            // Devide totalBytes by MaxBytes to get the number of convertedTextsToAudio
            int NumConvertedTextsToAudio = (int)Math.Ceiling((double)fileInfo.Length / MaxBytes);

            Console.WriteLine($"Book Size: {fileInfo.Length}");
            Console.WriteLine($"Total Lines: {lines.Length}");
            Console.WriteLine($"Total Expected Converted Texts to Audio: {NumConvertedTextsToAudio}");

            // for each line split into words
            for (var index = StartLineIndex; index < lines.Length; index++)
            {
                var line = lines[index];
                string[] words = line.Split(' ');

                // Add new line character before the first word
                NextTextToConvert += Environment.NewLine;

                // for each word in the line
                for (var i = StartWordIndex; i < words.Length; i++)
                {
                    var word = words[i];

                    // Check that NextTextToConvert is not bigger than MaxBytes
                    if (Encoding.UTF8.GetByteCount(NextTextToConvert + word + " ") < MaxBytes)
                    {
                        NextTextToConvert += word + " ";
                    }
                    else
                    {
                        if (StartFromChunk == 0 || NextChunkIndex == StartFromChunk)
                        {
                            StartFromChunk = 0;

                            bool IsSucessfull = await ConvertTextToAudio(NextTextToConvert, FileName, FileFormat, Model, Voice, Speed, NumConvertedTextsToAudio);

                            if (!IsSucessfull)
                            {
                                // Set the NextWordToConvert to half the words from NextWordToConvert
                                var halfWords = NextTextToConvert.Split(' ').Length / 2;
                                var HalfWordsToConvert = string.Join(" ", NextTextToConvert.Split(' ').Take(halfWords)) + " ";

                                // Convert the first half of the words

                                IsSucessfull = await ConvertTextToAudio(HalfWordsToConvert, FileName, FileFormat, Model, Voice, Speed, NumConvertedTextsToAudio);

                                NextChunkIndex++;

                                // Convert the second half of the words
                                 
                                var SecondHalfWordsToConvert = string.Join(" ", NextTextToConvert.Split(' ').Skip(halfWords)) + " ";

                                IsSucessfull = await ConvertTextToAudio(SecondHalfWordsToConvert, FileName, FileFormat, Model, Voice, Speed, NumConvertedTextsToAudio);

                                if (!IsSucessfull)
                                {
                                    NumFailedSplits++;
                                    Console.Write($"Number of Failed Splits: {NumFailedSplits}");

                                }

                            }

                        }
                        else if (StartFromChunk > 0)
                        {
                            NextChunkIndex++;
                        }

                        NextTextToConvert = word + " ";

                        LastWordIndex = i;

                        // Print Last Line and Word
                        Console.WriteLine($"Line: {LastLineIndex} WordIndex: {LastWordIndex}");
                    }


                }

                LastLineIndex = index;

            }

            await ConvertTextToAudio(NextTextToConvert, FileName, FileFormat, Model, Voice, Speed, NumConvertedTextsToAudio);

            // Print Last Line and Word
            Console.WriteLine($"Last Line: {LastLineIndex} Last WordIndex: {LastWordIndex}");


            // Merge all .mp3 audios in OutputDirectory
            MergeChunksAtOutput(CurrentOutputDirectory);

            fileTimeWatch.Stop();
            Console.WriteLine($"File Execution Time: {fileTimeWatch.ElapsedMilliseconds / 1000} seconds");
        }

        // Stop Timer and show total time
        watch.Stop();
        Console.WriteLine($"Total Execution Time: {watch.ElapsedMilliseconds / 1000} seconds");

        Console.ReadLine();
    }

    public static async Task<bool> ConvertTextToAudio(string NextTextToConvert, string LastAudioFileName, string FileFormat, string Model, string Voice, float Speed, int NumExpectedConvertedTextsToAudio = 100)
    {

        string json = "";

        try
        {

       
            //Start Timer
            var watch = System.Diagnostics.Stopwatch.StartNew();

            using (var client =
                   new HttpClient
                   {
                       Timeout = TimeSpan.FromMinutes(10) // Set a higher timeout
                   }
                  )
            {
                var requestBody = new
                {
                    model = Model, // Not used but required for compatibility
                    input = NextTextToConvert,
                    voice = Voice,
                    response_format = FileFormat, // Supported: mp3, wav, opus, flac
                    speed = Speed
                };


                json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");


                HttpResponseMessage response = await client.PostAsync(LocalAPIURL, content);

                if (response.IsSuccessStatusCode)
                {
                    byte[] audioData = await response.Content.ReadAsByteArrayAsync();

                    // Set Current Directory


                    if (!Directory.Exists($"{OutputFolderName}\\{LastAudioFileName}"))
                    {
                        // Remove All Special Characters from the FileName
                        // Limit to 50 characters
                        LastAudioFileName = new string(LastAudioFileName.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());

                        if (LastAudioFileName.Length > 50)
                        {
                            LastAudioFileName = LastAudioFileName.Substring(0, 50);
                        }

                        CurrentOutputDirectory = Directory.CreateDirectory($"{OutputFolderName}\\{LastAudioFileName}");
                    }
                    else
                    {
                        CurrentOutputDirectory = new DirectoryInfo($"{OutputFolderName}\\{LastAudioFileName}");
                    }

                    int digits = NumExpectedConvertedTextsToAudio == 0 ? 1 : (int)Math.Floor(Math.Log10(Math.Abs(NumExpectedConvertedTextsToAudio)) + 1);

                    var NumWithPadding = NextChunkIndex.ToString().PadLeft(digits, '0');

                    var newFilePath = Path.Combine(CurrentOutputDirectory.FullName,
                        $"{LastAudioFileName}{NumWithPadding}.{FileFormat}");

                    await File.WriteAllBytesAsync(newFilePath, audioData);

                    // End Timer
                    watch.Stop();

                    Console.WriteLine($"Audio saved as {newFilePath}");
                    Console.WriteLine($"Creation Time: {watch.ElapsedMilliseconds / 1000} seconds");

                    NextChunkIndex++;

                    return true;
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");

                    return false;
                }
            }

        }
        catch (Exception e)
        {
            Console.WriteLine($"Catched Exception: \n {e}");
            Console.WriteLine($"Request body: \n {json}");

            if (ExceptionCount > 1)
            {
                Console.WriteLine($"Reached Multiple Exceptions. Restarting Dockered API");

                RestartAPI();
            }

            ExceptionCount++;

            return false;
        }
    }

    private static void RestartAPI(int TimeToWaitAfterLaunchingDocker = 10, int TimeToWaitAfterWSLShutdown = 5, int TimeToWaitBeforeWSLShutdown = 5, int TimeToWaitAfterRestart = 15000)
    {
        //Run the following .bat as CMD Commands:
        /*
            @ECHO OFF
            tskill / v "Docker Desktop"
            taskkill / F / IM com.docker.backend.exe
            timeout 5
            wsl--shutdown
            cmd.exe / c start cmd.exe / c wsl - d docker - desktop--command exit
            timeout 5
            wsl - l - v
            "C:\Program Files\Docker\Docker\Docker Desktop.exe"
            timeout 5
            docker start [DockerTTSContainerID]
            PAUSE
        */

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/C tskill /v \"Docker Desktop\" && taskkill /F /IM com.docker.backend.exe && timeout {TimeToWaitBeforeWSLShutdown} && wsl --shutdown && cmd.exe /c start cmd.exe /c wsl -d docker-desktop --command exit && timeout {TimeToWaitAfterWSLShutdown} && wsl -l -v && \"C:\\Program Files\\Docker\\Docker\\Docker Desktop.exe\" && timeout {TimeToWaitAfterLaunchingDocker} && docker start {DockerTTSContainerID} ",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,

            //Verb = "runas" // This line makes the process run as admin
        };

        using (var process = Process.Start(processStartInfo))
        {

            process.WaitForExit();
            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            Console.WriteLine(output);

            // Wait some time
            System.Threading.Thread.Sleep(TimeToWaitAfterRestart);

        }
    }

    // Merge all .mp3 audios in OutputDirectory
    public static void MergeChunksAtOutput(DirectoryInfo OutputDirectory)
    {
        var files = OutputDirectory.GetFiles("*.mp3").ToList();

        if (files.Count > 1)
        {
            var outputFilePath = Path.Combine(OutputDirectory.FullName, $"{OutputDirectory.Name}.mp3");

            // Delete the file if it already exists
            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);

                // Remove file from the files
                files.RemoveAll(x => x.Name == $"{OutputDirectory.Name}.mp3");

            }


            using (var fs = new FileStream(outputFilePath, FileMode.Create))
            {
                try
                {

                    files = files.OrderBy(x => x.Name).ToList();


                    foreach (var file in files)
                    {
                        // Print Files in Order
                        Console.WriteLine(file.Name);

                        using (var fileStream = file.OpenRead())
                        {
                            fileStream.CopyTo(fs);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            Console.WriteLine($"Merged all .mp3 files in {OutputDirectory.Name}.mp3");
        }
    }
}

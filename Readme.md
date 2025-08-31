# Kokoro-FastAPI .NET Client  

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg?logo=dotnet)](https://dotnet.microsoft.com/)  
[![Docker](https://img.shields.io/badge/Docker-Enabled-blue?logo=docker)](https://www.docker.com/)  

A **.NET Client** for [Kokoro-FastAPI](https://github.com/remsky/Kokoro-FastAPI), built to handle **long-file text-to-speech (TTS)** efficiently.  
It improves performance and provides greater control over converting long text files into speech.

---

## 🚀 Features
- Convert **long `.txt` files** into speech using Kokoro-FastAPI.  
- Fine-grained control over chunk size, starting points, and continuation.  
- Automatic **Docker container restart** handling.  
- Configurable **voice, model, and speed**.  

---

## 📦 Build

### ✅ Prerequisites
- [.NET 8.0](https://dotnet.microsoft.com/)  
- [Docker](https://www.docker.com/)  
  - [KokoroTTS Docker](https://github.com/remsky/Kokoro-FastAPI/tree/release)  

---

### ▶️ Launch Server on Docker
1. Place your books in **`.txt` format** next to the executable.  
   - Use [Calibre Converter](https://calibre-ebook.com/) to convert `.pdf`/`.epub` → `.txt`.  
2. Double-click the executable to start the app.  
   - Watch the terminal window for logs and status updates.

---

## ⚙️ Command-Line Arguments

| Argument                        | Type     | Description |
|---------------------------------|----------|-------------|
| `NextChunkIndex=<int>`          | Integer  | Set the next chunk index. |
| `OutputFolderName=<string>`     | String   | Set the output folder name. |
| `LastLineIndex=<int>`           | Integer  | Set the last line index. |
| `LastWordIndex=<int>`           | Integer  | Set the last word index. |
| `MaxCharacters=<int>`           | Integer  | Maximum characters per chunk. |
| `Model=<string>`                | String   | TTS model to use. |
| `Voice=<string>`                | String   | Voice to use. |
| `Speed=<float>`                 | Float    | Voice playback speed. |
| `Continue=<int,int>`            | Tuple    | Continue from line + word index. |
| `IsManual=<bool>`               | Boolean  | Enable/disable manual parameter selection. |
| `StartFromChunk=<int>`          | Integer  | Resume from a specific chunk. |
| `DockerTTSContainerID=<string>`| String   | Docker TTS Container ID *(required for restart)*. |

---

### 🔧 Quick Run
Skip manual inputs and run directly from CMD:

```bash
KokoroFastApiUser.exe IsManual=false
```

---

### 💡 Example
```bash
KokoroFastApiUser.exe Voice=af_sky StartFromChunk=4895
```

---

## ⚠️ Troubleshooting

Sometimes the model running inside the container may **hang indefinitely**.  

- The client will attempt to automatically restart the Docker container.  
- If restart fails:  
  1. Copy the chunk number where processing stopped.  
  2. Manually restart the Docker container.  
  3. Resume with the following command:  

     ```bash
     KokoroFastApiUser.exe StartFromChunk=<chunkNumber>
     ```

---

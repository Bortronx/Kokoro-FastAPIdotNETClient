# Kokoro-FastAPI .NET Client

TTS Kokoro-FastAPI .NET Client for long files.  This speeds up and give a lot of control over the conversion of long text files to speech using .NET



## Build
### PreRequisites
- .NET
- Docker
  - KokoroTTS Docker
    - See Quick Start (docker compose) https://github.com/remsky/Kokoro-FastAPI/tree/release 


Launch Server On Docker
1 - Place books in .txt format next to the executable. 
    You can use Calibre Converter to convert book from pdf/epub format to .txt (https://calibre-ebook.com/).
2 - Double click the executable Run the application. See the messages on the window for more info

Arguments:
    NextChunkIndex=<int> - Set the next chunk index.
    OutputFolderName=<string> - Set the output folder name.
    LastLineIndex=<int> - Set the last line index.
    LastWordIndex=<int> - Set the last word index.
    MaxCharacters=<int> - Set the maximum number of characters per chunk.
    Model=<string> - Set the model to use.
    Voice=<string> - Set the voice to use.
    Speed=<float> - Set the speed of the voice.
    Continue=<int,int> - Continue from a specific line and word index.
    IsManual=<bool> - Enable manual parameter selection.
    StartFromChunk=<int>
    DockerTTSContainerID=<string> - Set the Docker TTS Container ID.This is required for restarting the API.

To quickly run ignoring all input rung from cmd using the following instead of step 2:
KokoroFastApiUser.exe IsManual=false;

Example:
KokoroFastApiUser.exe Voice=af_sky StartFromChunk=4895

Sometimes the model running on the container loads to infinity. When this happens the software tries to restart the docker container. If that fails copy the chunk number where you left off and manually restart the docker container and use the chunk number as the StartFromChunk parameter to continue the process.


## Development

### Requirements
- Net 8.0 Framework
- Kokoro-FastAPI
    - See Quick Start (docker compose) https://github.com/remsky/Kokoro-FastAPI/tree/release 
- Docker

### TODO
- [ ] Find a better way to manage infinite loading on the container. 
- [ ] Launch Dockered conatiner automatically if already installed
- [ ] Fix Docker Restarting issues
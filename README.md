# SLU Transcribe

Webbsida för diktering: spela in tal i webbläsaren, servern skickar ljudet till
[Berget AI](https://berget.ai) (modell `KBLab/kb-whisper-large`) och texten visas för användaren.

Stack: ASP.NET Core (.NET 10, Razor Pages) + htmx. Ljudet sparas inte på servern.

## Kom igång

```powershell
cd src/SluTranscribe.Web
dotnet user-secrets set Berget:ApiKey <din-berget-api-nyckel>
dotnet run --launch-profile https
```

Öppna https://localhost:7243, tillåt mikrofonen och klicka **Starta inspelning**.
Mikrofonåtkomst kräver HTTPS (eller `localhost`).

## API-nyckel för Berget AI

Appen behöver en API-nyckel från Berget AI och startar inte utan den.
Lägg aldrig nyckeln i `appsettings.json` eller i git.

**Lokalt (utveckling)** – använd user-secrets, som sparas i din användarprofil utanför repot:

```powershell
cd src/SluTranscribe.Web
dotnet user-secrets set Berget:ApiKey <din-berget-api-nyckel>
dotnet user-secrets list          # kontrollera att den är satt
```

**I drift (server, container, CI)** – sätt miljövariabeln `Berget__ApiKey`
(dubbelt understreck motsvarar `:`):

```powershell
$env:Berget__ApiKey = "<din-berget-api-nyckel>"   # PowerShell
```

```bash
export Berget__ApiKey="<din-berget-api-nyckel>"   # bash
```

Starta om appen efter att nyckeln har ändrats. Är nyckeln fel visar sidan
"Servern kunde inte autentisera mot Berget AI".

## Konfiguration

| Nyckel             | Standard                     | Beskrivning                          |
|--------------------|------------------------------|--------------------------------------|
| `Berget:ApiKey`    | –                            | Krävs. User-secrets / `Berget__ApiKey` |
| `Berget:BaseUrl`   | `https://api.berget.ai/v1/`  | OpenAI-kompatibel bas-URL            |
| `Berget:Model`     | `KBLab/kb-whisper-large`     | Transkriberingsmodell                |
| `Berget:Language`  | `sv`                         | Språkhint, tomt = autodetektering    |

Max uppladdningsstorlek är 25 MB.

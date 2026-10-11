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

## Docker / produktion (Linux)

Imagen är en Linux-image (`mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled`): minimal,
utan shell, och kör som icke-root-användare. Appen lyssnar på HTTP port **8080**. HTTPS
sköts av en reverse proxy framför containern (krävs, eftersom webbläsare bara ger
mikrofonåtkomst över HTTPS).

### Bygga

```bash
docker build -t slu-transcribe .
```

Bygget fungerar på Linux, macOS och Windows (Docker Desktop). Imagen får samma
CPU-arkitektur som datorn den byggs på. Är servern ARM (arm64) men byggdatorn x64, eller
tvärtom, ange plattform:

```bash
docker buildx build --platform linux/arm64 -t slu-transcribe .          # bara arm64
docker buildx build --platform linux/amd64,linux/arm64 \
  -t <registry>/slu-transcribe:1.0 --push .                             # båda, till registry
```

Kontrollera serverns arkitektur med `uname -m` (`x86_64` = amd64, `aarch64` = arm64).

### Köra med Docker Compose

```bash
cp .env.example .env        # fyll i Berget__ApiKey
docker compose up -d --build
docker compose logs -f
```

[compose.yaml](compose.yaml) publicerar porten bara på `127.0.0.1:8080`, så att endast
proxyn på samma server når appen. Antiforgery-nycklar sparas i volymen `dp-keys`
och överlever omstarter och uppdateringar.

### Köra med docker run

```bash
docker run -d --name slu-transcribe --restart unless-stopped \
  -p 127.0.0.1:8080:8080 \
  -e Berget__ApiKey=<din-berget-api-nyckel> \
  -v slu-dp-keys:/keys \
  slu-transcribe
```

Saknas `Berget__ApiKey` avslutas containern direkt med ett felmeddelande i loggen.

### Reverse proxy

Proxyn måste:

- terminera HTTPS och skicka `X-Forwarded-Proto` och `X-Forwarded-For`
- tillåta request bodies upp till **25 MB** (nginx har 1 MB som standard)
- ha timeout på minst 2 minuter (transkribering av långa inspelningar)

Exempel för nginx:

```nginx
server {
    listen 443 ssl;
    server_name diktering.example.se;
    # ssl_certificate / ssl_certificate_key ...

    client_max_body_size 25m;

    location / {
        proxy_pass         http://127.0.0.1:8080;
        proxy_set_header   Host $host;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_read_timeout 180s;
    }
}
```

Appen litar på `X-Forwarded-*` från alla avsändare (`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`),
så containerns port får inte vara nåbar direkt utifrån.

### Hälsokontroll

`GET /healthz` returnerar `200 Healthy`. Imagen saknar curl, så kontrollen görs från
proxyn eller övervakningen, inte med `HEALTHCHECK` i Dockerfile.

## Konfiguration

| Nyckel             | Standard                     | Beskrivning                          |
|--------------------|------------------------------|--------------------------------------|
| `Berget:ApiKey`    | –                            | Krävs. User-secrets / `Berget__ApiKey` |
| `Berget:BaseUrl`   | `https://api.berget.ai/v1/`  | OpenAI-kompatibel bas-URL            |
| `Berget:Model`     | `KBLab/kb-whisper-large`     | Transkriberingsmodell                |
| `Berget:Language`  | `sv`                         | Språkhint, tomt = autodetektering    |
| `Recording:MaxMinutes` | `20`                     | Maxlängd per inspelning i minuter    |
| `DataProtection:KeysPath` | – (i Docker: `/keys`) | Katalog för antiforgery-nycklar      |

Max uppladdningsstorlek är 25 MB.

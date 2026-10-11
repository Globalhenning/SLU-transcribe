(() => {
    const button = document.getElementById("record-btn");
    if (!button) return;

    const label = button.querySelector(".label");
    const timerEl = document.getElementById("timer");
    const statusEl = document.getElementById("status");
    const form = document.getElementById("upload-form");
    const input = document.getElementById("audio-input");

    const mimeType = ["audio/webm;codecs=opus", "audio/webm", "audio/mp4", "audio/ogg;codecs=opus"]
        .find(t => window.MediaRecorder && MediaRecorder.isTypeSupported(t)) ?? "";

    // Gränser från servern. 64 kbit/s räcker gott för tal och ger ~0,5 MB per minut.
    const maxSeconds = Number(button.dataset.maxSeconds) || 1200;
    const maxBytes = Number(button.dataset.maxBytes) || 25 * 1024 * 1024;
    const audioBitsPerSecond = 64000;
    const warnSeconds = 60;

    let recorder = null;
    let chunks = [];
    let startedAt = 0;
    let tick = null;
    let warned = false;

    const setStatus = text => { statusEl.textContent = text; };

    const fmt = s => `${String(Math.floor(s / 60)).padStart(2, "0")}:${String(s % 60).padStart(2, "0")}`;

    const updateTimer = () => {
        const s = Math.min(Math.floor((Date.now() - startedAt) / 1000), maxSeconds);
        timerEl.textContent = `${fmt(s)} / ${fmt(maxSeconds)}`;

        if (s >= maxSeconds) {
            stop("Maxlängden är nådd – inspelningen skickas för transkribering.");
        } else if (!warned && maxSeconds - s <= warnSeconds) {
            warned = true;
            timerEl.classList.add("warning");
            setStatus("Mindre än 1 minut kvar – inspelningen stoppas automatiskt.");
        }
    };

    async function start() {
        if (!navigator.mediaDevices?.getUserMedia || !window.MediaRecorder) {
            setStatus("Din webbläsare stöder inte ljudinspelning.");
            return;
        }

        let stream;
        try {
            stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        } catch (err) {
            setStatus(err.name === "NotAllowedError"
                ? "Åtkomst till mikrofonen nekades. Tillåt mikrofonen i webbläsaren och försök igen."
                : "Kunde inte starta mikrofonen: " + err.message);
            return;
        }

        chunks = [];
        recorder = new MediaRecorder(stream, mimeType ? { mimeType, audioBitsPerSecond } : { audioBitsPerSecond });
        recorder.ondataavailable = e => { if (e.data.size > 0) chunks.push(e.data); };
        recorder.onstop = () => {
            stream.getTracks().forEach(t => t.stop());
            upload();
        };
        recorder.start();

        startedAt = Date.now();
        warned = false;
        timerEl.classList.remove("warning");
        button.classList.add("recording");
        label.textContent = "Stoppa inspelning";
        setStatus("Spelar in…");
        updateTimer();
        tick = setInterval(updateTimer, 500);
    }

    function stop(message = "") {
        if (!recorder) return;
        clearInterval(tick);
        button.classList.remove("recording");
        label.textContent = "Starta inspelning";
        setStatus(message);
        recorder.stop();
        recorder = null;
    }

    function upload() {
        const type = chunks[0]?.type || mimeType || "audio/webm";
        const blob = new Blob(chunks, { type });
        if (blob.size === 0) {
            setStatus("Inspelningen var tom.");
            return;
        }
        if (blob.size > maxBytes) {
            setStatus(`Inspelningen är för stor (${(blob.size / 1048576).toFixed(1)} MB, max ${Math.round(maxBytes / 1048576)} MB). Gör en kortare inspelning.`);
            return;
        }

        const ext = type.includes("mp4") ? "m4a" : type.includes("ogg") ? "ogg" : "webm";
        const dt = new DataTransfer();
        dt.items.add(new File([blob], `diktering.${ext}`, { type: type.split(";")[0] }));
        input.files = dt.files;
        htmx.trigger(form, "submit");
    }

    button.addEventListener("click", () => (recorder ? stop() : start()));
    timerEl.textContent = `${fmt(0)} / ${fmt(maxSeconds)}`;
})();

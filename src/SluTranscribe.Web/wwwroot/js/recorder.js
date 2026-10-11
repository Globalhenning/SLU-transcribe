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

    let recorder = null;
    let chunks = [];
    let startedAt = 0;
    let tick = null;

    const setStatus = text => { statusEl.textContent = text; };

    const updateTimer = () => {
        const s = Math.floor((Date.now() - startedAt) / 1000);
        timerEl.textContent = `${String(Math.floor(s / 60)).padStart(2, "0")}:${String(s % 60).padStart(2, "0")}`;
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
        recorder = new MediaRecorder(stream, mimeType ? { mimeType } : undefined);
        recorder.ondataavailable = e => { if (e.data.size > 0) chunks.push(e.data); };
        recorder.onstop = () => {
            stream.getTracks().forEach(t => t.stop());
            upload();
        };
        recorder.start();

        startedAt = Date.now();
        updateTimer();
        tick = setInterval(updateTimer, 500);
        button.classList.add("recording");
        label.textContent = "Stoppa inspelning";
        setStatus("Spelar in…");
    }

    function stop() {
        clearInterval(tick);
        button.classList.remove("recording");
        label.textContent = "Starta inspelning";
        setStatus("");
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

        const ext = type.includes("mp4") ? "m4a" : type.includes("ogg") ? "ogg" : "webm";
        const dt = new DataTransfer();
        dt.items.add(new File([blob], `diktering.${ext}`, { type: type.split(";")[0] }));
        input.files = dt.files;
        htmx.trigger(form, "submit");
    }

    button.addEventListener("click", () => (recorder ? stop() : start()));
})();

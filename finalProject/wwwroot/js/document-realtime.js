(function () {
    const realtimeRoot = document.querySelector("[data-document-realtime], [data-document-detail-id]");
    if (!realtimeRoot) {
        return;
    }

    const recordSeparator = String.fromCharCode(30);
    const statusBadge = document.getElementById("documentRealtimeStatus");
    const notificationsBody = document.getElementById("documentRealtimeNotifications");
    const mode = realtimeRoot.getAttribute("data-document-realtime");
    const detailDocumentId = Number(realtimeRoot.getAttribute("data-document-detail-id") || 0);
    const detailAlert = document.querySelector("[data-document-detail-alert]");
    const eventNames = ["DocumentCreated", "DocumentUpdated", "DocumentArchived", "DocumentReindexed"];

    function setStatus(text, cssClass) {
        if (!statusBadge) {
            return;
        }

        statusBadge.textContent = text;
        statusBadge.className = "badge " + cssClass;
    }

    function escapeHtml(value) {
        return String(value || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    function formatTime(value) {
        const date = value ? new Date(value) : new Date();
        return Number.isNaN(date.getTime()) ? new Date().toLocaleString() : date.toLocaleString();
    }

    function prependNotification(payload) {
        if (!notificationsBody) {
            return;
        }

        const emptyRow = notificationsBody.querySelector("[data-empty-realtime-row]");
        if (emptyRow) {
            emptyRow.remove();
        }

        const row = document.createElement("tr");
        if (mode === "admin") {
            row.innerHTML = `
                <td>${escapeHtml(payload.teacherUploader)}</td>
                <td>${escapeHtml(payload.document)}</td>
                <td>${escapeHtml(payload.subject)}</td>
                <td>${escapeHtml(payload.action)}</td>
                <td>${escapeHtml(formatTime(payload.occurredAtUtc))}</td>`;
        } else {
            row.innerHTML = `
                <td>${escapeHtml(payload.document)}</td>
                <td>${escapeHtml(payload.subject)}</td>
                <td>${escapeHtml(payload.action)}</td>
                <td>${escapeHtml(payload.status)}</td>
                <td>${escapeHtml(formatTime(payload.occurredAtUtc))}</td>`;
        }

        notificationsBody.prepend(row);
    }

    function updateDocumentRow(payload) {
        const existingRow = document.querySelector(`tr[data-document-id="${payload.documentId}"]`);
        if (existingRow) {
            const statusCell = existingRow.querySelector("[data-document-status]");
            if (statusCell) {
                statusCell.textContent = payload.status || statusCell.textContent;
            }

            existingRow.classList.add("table-info");
            window.setTimeout(function () {
                existingRow.classList.remove("table-info");
            }, 2400);
            return;
        }

        const tableBody = document.querySelector("[data-documents-table-body]");
        if (!tableBody || !payload.documentId) {
            return;
        }

        const emptyRow = tableBody.querySelector("[data-empty-documents-row]");
        if (emptyRow) {
            emptyRow.remove();
        }

        const row = document.createElement("tr");
        row.setAttribute("data-document-id", payload.documentId);
        if (mode === "admin") {
            row.innerHTML = `
                <td>${escapeHtml(payload.document)}</td>
                <td>${escapeHtml(payload.subject)}</td>
                <td></td>
                <td>${escapeHtml(payload.teacherUploader)}</td>
                <td>${escapeHtml(formatTime(payload.occurredAtUtc))}</td>
                <td data-document-status>${escapeHtml(payload.status)}</td>
                <td></td>
                <td>
                    <div class="btn-group btn-group-sm" role="group">
                        <a class="btn btn-outline-primary" href="/Documents/Details/${payload.documentId}">Xem</a>
                        <a class="btn btn-outline-primary" href="/Documents/Download/${payload.documentId}">Tai xuong</a>
                    </div>
                </td>`;
        } else {
            row.innerHTML = `
                <td>${escapeHtml(payload.document)}</td>
                <td>${escapeHtml(payload.subject)}</td>
                <td></td>
                <td></td>
                <td></td>
                <td data-document-status>${escapeHtml(payload.status)}</td>
                <td></td>
                <td>${escapeHtml(formatTime(payload.occurredAtUtc))}</td>
                <td>
                    <div class="d-flex flex-wrap gap-1">
                        <a class="btn btn-sm btn-outline-primary" href="/TeacherDocuments/Details/${payload.documentId}">Xem</a>
                    </div>
                </td>`;
        }

        row.classList.add("table-info");
        tableBody.prepend(row);
        window.setTimeout(function () {
            row.classList.remove("table-info");
        }, 2400);
    }

    function updateDetailPage(eventName, payload) {
        if (!detailDocumentId || Number(payload.documentId) !== detailDocumentId) {
            return;
        }

        if (detailAlert) {
            detailAlert.textContent = eventName === "DocumentReindexed"
                ? "Tai lieu vua duoc re-index. Trang se tai lai de cap nhat noi dung chunk."
                : `Tai lieu vua duoc cap nhat: ${payload.action || eventName}.`;
            detailAlert.classList.remove("d-none");
        }

        if (eventName === "DocumentReindexed") {
            window.setTimeout(function () {
                window.location.reload();
            }, 1200);
        }
    }

    function handleInvocation(message) {
        if (message.type !== 1 || !eventNames.includes(message.target)) {
            return;
        }

        const payload = message.arguments && message.arguments[0];
        if (!payload) {
            return;
        }

        prependNotification(payload);
        updateDocumentRow(payload);
        updateDetailPage(message.target, payload);
    }

    function connectWebSocket(connectionToken) {
        const scheme = window.location.protocol === "https:" ? "wss" : "ws";
        const url = `${scheme}://${window.location.host}/hubs/document-processing?id=${encodeURIComponent(connectionToken)}`;
        const socket = new WebSocket(url);

        socket.addEventListener("open", function () {
            socket.send(JSON.stringify({ protocol: "json", version: 1 }) + recordSeparator);
            setStatus("Connected", "text-bg-success");
        });

        socket.addEventListener("message", function (event) {
            const records = String(event.data).split(recordSeparator).filter(Boolean);
            for (const record of records) {
                handleInvocation(JSON.parse(record));
            }
        });

        socket.addEventListener("close", function () {
            setStatus("Disconnected", "text-bg-secondary");
            window.setTimeout(start, 3000);
        });

        socket.addEventListener("error", function () {
            setStatus("Error", "text-bg-danger");
        });
    }

    async function start() {
        try {
            setStatus("Connecting", "text-bg-secondary");
            const response = await fetch("/hubs/document-processing/negotiate?negotiateVersion=1", {
                method: "POST",
                credentials: "same-origin"
            });
            if (!response.ok) {
                throw new Error("SignalR negotiate failed.");
            }

            const negotiate = await response.json();
            connectWebSocket(negotiate.connectionToken || negotiate.connectionId);
        } catch {
            setStatus("Disconnected", "text-bg-secondary");
            window.setTimeout(start, 3000);
        }
    }

    start();
})();

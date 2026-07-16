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
    const detailSnapshotUrl = realtimeRoot.getAttribute("data-document-snapshot-url");
    const detailAlert = document.querySelector("[data-document-detail-alert]");
    const eventNames = ["DocumentCreated", "DocumentUpdated", "DocumentArchived", "DocumentReindexed"];

    let isRefreshingDetail = false;
    let shouldRefreshAgain = false;

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

    function setText(selector, value) {
        const element = document.querySelector(selector);
        if (element) {
            element.textContent = value || "";
        }
    }

    function showDetailMessage(message, cssClass) {
        if (!detailAlert) {
            return;
        }

        detailAlert.textContent = message;
        detailAlert.className = `alert ${cssClass || "alert-info"}`;
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
        const title = payload.title || payload.document;
        const subject = payload.subjectName || payload.subject;
        const teacherName = payload.updatedByTeacherName || payload.teacherUploader;
        const occurredAt = payload.updatedAtUtc || payload.occurredAtUtc;
        if (mode === "admin") {
            row.innerHTML = `
                <td>${escapeHtml(teacherName)}</td>
                <td>${escapeHtml(title)}</td>
                <td>${escapeHtml(subject)}</td>
                <td>${escapeHtml(payload.action)}</td>
                <td>${escapeHtml(formatTime(occurredAt))}</td>`;
        } else {
            row.innerHTML = `
                <td>${escapeHtml(title)}</td>
                <td>${escapeHtml(subject)}</td>
                <td>${escapeHtml(payload.action)}</td>
                <td>${escapeHtml(payload.status)}</td>
                <td>${escapeHtml(formatTime(occurredAt))}</td>`;
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
        const title = payload.title || payload.document;
        const subject = payload.subjectName || payload.subject;
        const teacherName = payload.updatedByTeacherName || payload.teacherUploader;
        const occurredAt = payload.updatedAtUtc || payload.occurredAtUtc;
        if (mode === "admin") {
            row.innerHTML = `
                <td>${escapeHtml(title)}</td>
                <td>${escapeHtml(subject)}</td>
                <td></td>
                <td>${escapeHtml(teacherName)}</td>
                <td>${escapeHtml(formatTime(occurredAt))}</td>
                <td data-document-status>${escapeHtml(payload.status)}</td>
                <td></td>
                <td>
                    <div class="btn-group btn-group-sm" role="group">
                        <a class="btn btn-outline-primary" href="/Documents/Details/${payload.documentId}">Xem</a>
                        <a class="btn btn-outline-primary" href="/Documents/Download/${payload.documentId}">Tải xuống</a>
                    </div>
                </td>`;
        } else {
            row.innerHTML = `
                <td>${escapeHtml(title)}</td>
                <td>${escapeHtml(subject)}</td>
                <td></td>
                <td></td>
                <td></td>
                <td data-document-status>${escapeHtml(payload.status)}</td>
                <td></td>
                <td>${escapeHtml(formatTime(occurredAt))}</td>
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

    function renderCurrentContent(content) {
        const container = document.querySelector("[data-document-current-content]");
        if (!container) {
            return;
        }

        container.innerHTML = "";
        if (!content || !content.trim()) {
            const empty = document.createElement("p");
            empty.className = "text-muted mb-0";
            empty.textContent = "Chưa có nội dung được trích xuất.";
            container.appendChild(empty);
            return;
        }

        const pre = document.createElement("pre");
        pre.className = "mb-0 p-3 bg-light border rounded text-wrap";
        pre.style.whiteSpace = "pre-wrap";
        pre.textContent = content;
        container.appendChild(pre);
    }

    function renderChunks(chunks) {
        const container = document.querySelector("[data-document-chunks]");
        const summary = document.querySelector("[data-document-chunk-summary]");
        const safeChunks = Array.isArray(chunks) ? chunks : [];

        if (summary) {
            summary.textContent = `Đang hiển thị ${safeChunks.length} chunk theo thứ tự index.`;
        }

        if (!container) {
            return;
        }

        container.innerHTML = "";
        if (safeChunks.length === 0) {
            const empty = document.createElement("p");
            empty.className = "text-muted mb-0";
            empty.textContent = "Tài liệu này chưa có chunk để hiển thị.";
            container.appendChild(empty);
            return;
        }

        const list = document.createElement("div");
        list.className = "chunk-list";

        for (const chunk of safeChunks) {
            const item = document.createElement("article");
            item.className = "chunk-item";

            const meta = document.createElement("div");
            meta.className = "chunk-meta";
            meta.innerHTML = `
                <strong>Chunk #${escapeHtml(chunk.chunkIndex)}</strong>
                <span>${escapeHtml(chunk.wordCount)} word</span>
                <span>${escapeHtml(chunk.tokenCount)} token</span>
                <span>Vị trí ${escapeHtml(chunk.startPosition)}-${escapeHtml(chunk.endPosition)}</span>`;

            const content = document.createElement("pre");
            content.className = "chunk-content";
            content.textContent = chunk.content || "";

            item.appendChild(meta);
            item.appendChild(content);
            list.appendChild(item);
        }

        container.appendChild(list);
    }

    function updateDetailFromSnapshot(snapshot) {
        setText("[data-document-title]", snapshot.title);
        setText("[data-document-subject]", snapshot.subjectName);
        setText("[data-document-teacher]", snapshot.uploadedByTeacherName);
        setText("[data-document-status-detail]", snapshot.status);
        setText("[data-document-chunk-count]", String(snapshot.chunkCount ?? 0));
        setText("[data-document-content-version]", String(snapshot.contentVersion ?? ""));
        setText("[data-document-description]", snapshot.description && snapshot.description.trim() ? snapshot.description : "Chưa có mô tả.");

        const updatedWrapper = document.querySelector("[data-document-content-updated-wrapper]");
        const updatedText = document.querySelector("[data-document-content-updated]");
        if (updatedWrapper && updatedText) {
            if (snapshot.contentUpdatedAtText) {
                updatedText.textContent = snapshot.contentUpdatedAtText;
                updatedWrapper.classList.remove("d-none");
            } else {
                updatedText.textContent = "";
                updatedWrapper.classList.add("d-none");
            }
        }

        renderCurrentContent(snapshot.currentContent || "");
        renderChunks(snapshot.chunks);
    }

    async function refreshDetailFromServer() {
        if (!detailDocumentId || !detailSnapshotUrl) {
            return false;
        }

        if (isRefreshingDetail) {
            shouldRefreshAgain = true;
            return true;
        }

        isRefreshingDetail = true;
        try {
            const response = await fetch(detailSnapshotUrl, {
                method: "GET",
                credentials: "same-origin",
                headers: {
                    "Accept": "application/json"
                }
            });
            if (!response.ok) {
                throw new Error("Không thể lấy dữ liệu tài liệu mới nhất.");
            }

            const snapshot = await response.json();
            updateDetailFromSnapshot(snapshot);
            return true;
        } finally {
            isRefreshingDetail = false;
            if (shouldRefreshAgain) {
                shouldRefreshAgain = false;
                refreshDetailFromServer();
            }
        }
    }

    function updateDetailPage(eventName, payload) {
        if (!detailDocumentId || Number(payload.documentId) !== detailDocumentId) {
            return;
        }

        showDetailMessage("Đã nhận cập nhật realtime. Đang đồng bộ nội dung mới...", "alert-info");

        refreshDetailFromServer()
            .then(function (updated) {
                if (updated) {
                    const message = eventName === "DocumentReindexed"
                        ? "Tài liệu vừa được tạo lại chỉ mục. Nội dung và chunk đã được đồng bộ tự động."
                        : "Tài liệu vừa được cập nhật. Nội dung mới đã hiển thị tự động.";
                    showDetailMessage(message, "alert-success");
                    return;
                }

                showDetailMessage("Tài liệu vừa được cập nhật. Vui lòng tải lại trang để xem nội dung mới nhất.", "alert-warning");
            })
            .catch(function () {
                showDetailMessage("Không thể tự đồng bộ nội dung mới. Vui lòng tải lại trang.", "alert-warning");
            });
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
            setStatus("Đã kết nối", "text-bg-success");
        });

        socket.addEventListener("message", function (event) {
            const records = String(event.data).split(recordSeparator).filter(Boolean);
            for (const record of records) {
                handleInvocation(JSON.parse(record));
            }
        });

        socket.addEventListener("close", function () {
            setStatus("Mất kết nối", "text-bg-secondary");
            window.setTimeout(start, 3000);
        });

        socket.addEventListener("error", function () {
            setStatus("Lỗi", "text-bg-danger");
        });
    }

    async function start() {
        try {
            setStatus("Đang kết nối", "text-bg-secondary");
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
            setStatus("Mất kết nối", "text-bg-secondary");
            window.setTimeout(start, 3000);
        }
    }

    start();
})();

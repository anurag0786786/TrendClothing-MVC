var dataTable;
$(document).ready(function () { loadDataTable(); });

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { "url": "/Admin/User/GetAll" },
        "columns": [
            { "data": "name", "render": data => `<span style="font-weight:600;">${data}</span>` },
            { "data": "email" },
            {
                "data": "roles",
                "render": data => `<span style="display:inline-block;padding:3px 10px;background:var(--tc-surface-alt);border:1px solid var(--tc-border);border-radius:20px;font-size:11px;font-weight:700;">${data}</span>`
            },
            {
                "data": { id: "id", lockoutEnd: "lockoutEnd" },
                "render": function (data) {
                    var today = new Date().getTime();
                    var lockout = new Date(data.lockoutEnd).getTime();
                    if (lockout > today) {
                        return `<button class="tc-admin-btn-edit" style="background:#e0f2e9;color:var(--tc-success);border:1px solid #b7dfc9;" onclick="LockUnlock('${data.id}')">
                            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0110 0v4"/></svg>
                            Unlock
                        </button>`;
                    } else {
                        return `<button class="tc-admin-btn-delete" onclick="LockUnlock('${data.id}')">
                            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0110 0"/></svg>
                            Lock
                        </button>`;
                    }
                }
            }
        ]
    });
}

function LockUnlock(id) {
    $.ajax({
        url: "/Admin/User/LockUnlock",
        type: "POST",
        data: JSON.stringify(id),
        contentType: "application/json",
        success: function (data) {
            data.success ? toastr.success(data.message) : toastr.error(data.message);
            dataTable.ajax.reload();
        }
    });
}